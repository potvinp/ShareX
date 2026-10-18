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

using Newtonsoft.Json;
using System;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace ShareX.HelpersLib
{
    /// <summary>
    /// Exports of libsharex_macos.dylib (native/macos). Coordinates are top-left global points
    /// unless noted; see <see cref="MacDisplays"/> for conversion to ShareX screen pixels.
    /// </summary>
    [SupportedOSPlatform("macos")]
    internal static class MacNativeMethods
    {
        private const string Library = "libsharex_macos";

        public const int StatusOk = 0;
        public const int StatusNotFound = -2;
        public const int StatusTimeout = -3;
        public const int StatusPermissionDenied = -4;

        [DllImport(Library, EntryPoint = "sharex_free")]
        public static extern void Free(IntPtr pointer);

        [DllImport(Library, EntryPoint = "sharex_get_screens_json")]
        private static extern IntPtr GetScreensJsonNative();

        [DllImport(Library, EntryPoint = "sharex_get_cursor_position")]
        public static extern void GetCursorPosition(out double x, out double y);

        [DllImport(Library, EntryPoint = "sharex_set_cursor_position")]
        public static extern void SetCursorPosition(double x, double y);

        [DllImport(Library, EntryPoint = "sharex_capture_display_rect")]
        public static extern int CaptureDisplayRect(uint displayId, double x, double y, double width, double height, int showCursor,
            out IntPtr pixels, out int pixelWidth, out int pixelHeight, out int stride);

        [DllImport(Library, EntryPoint = "sharex_capture_window")]
        public static extern int CaptureWindow(uint windowId, int includeShadow, int showCursor,
            out IntPtr pixels, out int pixelWidth, out int pixelHeight, out int stride);

        [DllImport(Library, EntryPoint = "sharex_get_windows_json")]
        private static extern IntPtr GetWindowsJsonNative(int onScreenOnly);

        [DllImport(Library, EntryPoint = "sharex_get_frontmost_window")]
        public static extern uint GetFrontmostWindow();

        [DllImport(Library, EntryPoint = "sharex_get_window_bounds")]
        public static extern int GetWindowBounds(uint windowId, out double x, out double y, out double width, out double height);

        [DllImport(Library, EntryPoint = "sharex_screen_capture_preflight")]
        public static extern int ScreenCapturePreflight();

        [DllImport(Library, EntryPoint = "sharex_screen_capture_request")]
        public static extern int ScreenCaptureRequest();

        [DllImport(Library, EntryPoint = "sharex_open_privacy_settings")]
        public static extern void OpenPrivacySettings(int pane);

        [DllImport(Library, EntryPoint = "sharex_hotkeys_initialize")]
        public static extern unsafe int HotkeysInitialize(delegate* unmanaged[Cdecl]<uint, void> callback);

        [DllImport(Library, EntryPoint = "sharex_hotkey_register")]
        public static extern int HotkeyRegister(uint id, uint keyCode, uint modifiers);

        [DllImport(Library, EntryPoint = "sharex_hotkey_unregister")]
        public static extern int HotkeyUnregister(uint id);

        [DllImport(Library, EntryPoint = "sharex_keychain_get_or_create_key")]
        public static extern int KeychainGetOrCreateKey([MarshalAs(UnmanagedType.LPUTF8Str)] string service,
            [MarshalAs(UnmanagedType.LPUTF8Str)] string account, byte[] output, int length);

        [DllImport(Library, EntryPoint = "sharex_window_configure_overlay")]
        public static extern int ConfigureOverlayWindow(IntPtr handle, double x, double y, double width, double height);

        [DllImport(Library, EntryPoint = "sharex_activate_app")]
        public static extern void ActivateApp();

        public static MacScreenData[] GetScreens() => ReadJson<MacScreenData[]>(GetScreensJsonNative()) ?? Array.Empty<MacScreenData>();

        public static MacWindowData[] GetWindows(bool onScreenOnly) =>
            ReadJson<MacWindowData[]>(GetWindowsJsonNative(onScreenOnly ? 1 : 0)) ?? Array.Empty<MacWindowData>();

        private static T ReadJson<T>(IntPtr pointer)
        {
            if (pointer == IntPtr.Zero)
            {
                return default;
            }

            try
            {
                return JsonConvert.DeserializeObject<T>(Marshal.PtrToStringUTF8(pointer));
            }
            finally
            {
                Free(pointer);
            }
        }
    }

    internal sealed class MacScreenData
    {
        [JsonProperty("id")] public uint Id { get; set; }
        [JsonProperty("x")] public double X { get; set; }
        [JsonProperty("y")] public double Y { get; set; }
        [JsonProperty("width")] public double Width { get; set; }
        [JsonProperty("height")] public double Height { get; set; }
        [JsonProperty("visibleX")] public double VisibleX { get; set; }
        [JsonProperty("visibleY")] public double VisibleY { get; set; }
        [JsonProperty("visibleWidth")] public double VisibleWidth { get; set; }
        [JsonProperty("visibleHeight")] public double VisibleHeight { get; set; }
        [JsonProperty("scale")] public double Scale { get; set; } = 1;
        [JsonProperty("primary")] public bool Primary { get; set; }
    }

    internal sealed class MacWindowData
    {
        [JsonProperty("id")] public uint Id { get; set; }
        [JsonProperty("pid")] public int ProcessId { get; set; }
        [JsonProperty("layer")] public int Layer { get; set; }
        [JsonProperty("alpha")] public double Alpha { get; set; } = 1;
        [JsonProperty("x")] public double X { get; set; }
        [JsonProperty("y")] public double Y { get; set; }
        [JsonProperty("width")] public double Width { get; set; }
        [JsonProperty("height")] public double Height { get; set; }
        [JsonProperty("title")] public string Title { get; set; } = "";
        [JsonProperty("owner")] public string Owner { get; set; } = "";
    }
}
