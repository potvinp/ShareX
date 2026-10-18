#region License Information (GPL v3)

/*
    ShareX - A program that allows you to take screenshots and share any file type
    Copyright (c) 2007-2026 ShareX Team

    This program is free software; you can redistribute it and/or
    modify it under the terms of the GNU General Public License
    as published by the Free Software Foundation; either version 2
    of the License, or (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program; if not, write to the Free Software
    Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.

    Optionally you can also view the license at <http://www.gnu.org/licenses/>.
*/

#endregion License Information (GPL v3)

using Avalonia.Threading;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace ShareX.HelpersLib;

/// <summary>macOS global hotkeys (Carbon RegisterEventHotKey) delivered on Avalonia's UI thread.</summary>
[SupportedOSPlatform("macos")]
public sealed class MacHotkeyHost : IHotkeyHost
{
    private static MacHotkeyHost current;

    private readonly int threadId = Environment.CurrentManagedThreadId;
    private readonly Stopwatch repeatLimitTimer = Stopwatch.StartNew();
    private readonly Dictionary<ushort, HotkeyInfo> registeredHotkeys = new();
    private ushort lastID;
    private bool initialized;
    private bool closing;

    public event HotkeyEventHandler HotkeyPress;
    public event EventHandler Closed;

    // Windows session messages have no macOS equivalent; the event is never raised.
    public event EventHandler<NativeWindowMessageEventArgs> NativeMessageReceived { add { } remove { } }

    public IntPtr Handle => IntPtr.Zero;
    public bool IsDisposed { get; private set; }
    public int HotkeyRepeatLimit { get; set; } = 1000;

    public unsafe void Initialize()
    {
        VerifyAccess();
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        if (initialized) return;
        if (current != null && !current.IsDisposed)
            throw new InvalidOperationException("Only one macOS hotkey host can be active.");

        int status = MacNativeMethods.HotkeysInitialize(&OnNativeHotkey);
        if (status != 0)
        {
            DebugHelper.WriteLine("Unable to install the hotkey handler. OSStatus: " + status);
            return;
        }
        current = this;
        initialized = true;
    }

    public void RegisterHotkey(HotkeyInfo hotkeyInfo)
    {
        VerifyAccess();
        ObjectDisposedException.ThrowIf(IsDisposed || closing, this);
        if (hotkeyInfo == null || hotkeyInfo.Status == HotkeyStatus.Registered) return;
        if (!hotkeyInfo.IsValidHotkey)
        {
            hotkeyInfo.Status = HotkeyStatus.NotConfigured;
            return;
        }
        Initialize();
        if (!initialized || !MacKeyCodes.TryGetKeyCode(hotkeyInfo.KeyCode, out uint keyCode))
        {
            DebugHelper.WriteLine("Unable to register hotkey: " + hotkeyInfo);
            hotkeyInfo.Status = HotkeyStatus.Failed;
            return;
        }
        if (hotkeyInfo.ID == 0)
        {
            hotkeyInfo.ID = NextID();
        }
        int status = MacNativeMethods.HotkeyRegister(hotkeyInfo.ID, keyCode, MacKeyCodes.GetModifiers(hotkeyInfo.ModifiersEnum));
        if (status != 0)
        {
            // eventHotKeyExistsErr (-9878) means another app or ShareX hotkey already owns the combination.
            DebugHelper.WriteLine($"Unable to register hotkey: {hotkeyInfo} (OSStatus: {status})");
            hotkeyInfo.ID = 0;
            hotkeyInfo.Status = HotkeyStatus.Failed;
            return;
        }
        registeredHotkeys[hotkeyInfo.ID] = hotkeyInfo;
        hotkeyInfo.Status = HotkeyStatus.Registered;
    }

    public bool UnregisterHotkey(HotkeyInfo hotkeyInfo)
    {
        VerifyAccess();
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        if (hotkeyInfo == null) return false;
        if (hotkeyInfo.ID > 0 && MacNativeMethods.HotkeyUnregister(hotkeyInfo.ID) == 0)
        {
            registeredHotkeys.Remove(hotkeyInfo.ID);
            hotkeyInfo.ID = 0;
            hotkeyInfo.Status = HotkeyStatus.NotConfigured;
            return true;
        }
        hotkeyInfo.Status = HotkeyStatus.Failed;
        return false;
    }

    private ushort NextID()
    {
        do
        {
            lastID = (ushort)(lastID == ushort.MaxValue ? 1 : lastID + 1);
        }
        while (registeredHotkeys.ContainsKey(lastID));
        return lastID;
    }

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static void OnNativeHotkey(uint id)
    {
        try
        {
            current?.OnHotkey((ushort)id);
        }
        catch (Exception exception)
        {
            // Never let a managed exception unwind into the Carbon event loop.
            Dispatcher.UIThread.Post(ExceptionDispatchInfo.Capture(exception).Throw);
        }
    }

    private void OnHotkey(ushort id)
    {
        if (IsDisposed || closing || !registeredHotkeys.TryGetValue(id, out HotkeyInfo hotkeyInfo)) return;
        if (CheckRepeatLimitTime())
        {
            HotkeyPress?.Invoke(id, hotkeyInfo.KeyCode, hotkeyInfo.ModifiersEnum);
        }
    }

    private bool CheckRepeatLimitTime()
    {
        if (HotkeyRepeatLimit > 0)
        {
            if (repeatLimitTimer.ElapsedMilliseconds < HotkeyRepeatLimit) return false;
            repeatLimitTimer.Restart();
        }
        return true;
    }

    public void Close()
    {
        VerifyAccess();
        if (IsDisposed || closing) return;
        closing = true;
        try
        {
            Closed?.Invoke(this, EventArgs.Empty);
        }
        finally
        {
            foreach (HotkeyInfo hotkey in registeredHotkeys.Values.ToArray())
            {
                MacNativeMethods.HotkeyUnregister(hotkey.ID);
                hotkey.ID = 0;
                hotkey.Status = HotkeyStatus.NotConfigured;
            }
            registeredHotkeys.Clear();
            if (current == this) current = null;
            IsDisposed = true;
            closing = false;
        }
    }

    public void Dispose() => Close();

    private void VerifyAccess()
    {
        if (Environment.CurrentManagedThreadId != threadId)
            throw new InvalidOperationException("The hotkey host must be accessed on the thread that created it.");
    }
}
