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

using System.Collections.Generic;

namespace ShareX.HelpersLib
{
    /// <summary>Maps saved Windows virtual-key hotkeys to macOS (Carbon) virtual key codes and modifiers.</summary>
    public static class MacKeyCodes
    {
        private const uint CmdKey = 0x0100;
        private const uint ShiftKey = 0x0200;
        private const uint OptionKey = 0x0800;
        private const uint ControlKey = 0x1000;

        // kVK_* values from Carbon's HIToolbox/Events.h. Print Screen, Scroll Lock and Pause map to
        // F13-F15, which is what PC keyboards send to a Mac for those keys.
        private static readonly Dictionary<InputKey, uint> keys = new()
        {
            [InputKey.A] = 0x00, [InputKey.S] = 0x01, [InputKey.D] = 0x02, [InputKey.F] = 0x03,
            [InputKey.H] = 0x04, [InputKey.G] = 0x05, [InputKey.Z] = 0x06, [InputKey.X] = 0x07,
            [InputKey.C] = 0x08, [InputKey.V] = 0x09, [InputKey.B] = 0x0B, [InputKey.Q] = 0x0C,
            [InputKey.W] = 0x0D, [InputKey.E] = 0x0E, [InputKey.R] = 0x0F, [InputKey.Y] = 0x10,
            [InputKey.T] = 0x11, [InputKey.O] = 0x1F, [InputKey.U] = 0x20, [InputKey.I] = 0x22,
            [InputKey.P] = 0x23, [InputKey.L] = 0x25, [InputKey.J] = 0x26, [InputKey.K] = 0x28,
            [InputKey.N] = 0x2D, [InputKey.M] = 0x2E,

            [InputKey.D1] = 0x12, [InputKey.D2] = 0x13, [InputKey.D3] = 0x14, [InputKey.D4] = 0x15,
            [InputKey.D6] = 0x16, [InputKey.D5] = 0x17, [InputKey.D9] = 0x19, [InputKey.D7] = 0x1A,
            [InputKey.D8] = 0x1C, [InputKey.D0] = 0x1D,

            [InputKey.Oemplus] = 0x18, [InputKey.OemMinus] = 0x1B, [InputKey.OemCloseBrackets] = 0x1E,
            [InputKey.OemOpenBrackets] = 0x21, [InputKey.OemQuotes] = 0x27, [InputKey.OemSemicolon] = 0x29,
            [InputKey.OemPipe] = 0x2A, [InputKey.Oemcomma] = 0x2B, [InputKey.OemQuestion] = 0x2C,
            [InputKey.OemPeriod] = 0x2F, [InputKey.Oemtilde] = 0x32,

            [InputKey.Return] = 0x24, [InputKey.Tab] = 0x30, [InputKey.Space] = 0x31, [InputKey.Back] = 0x33,
            [InputKey.Escape] = 0x35, [InputKey.Insert] = 0x72, [InputKey.Home] = 0x73, [InputKey.PageUp] = 0x74,
            [InputKey.Delete] = 0x75, [InputKey.End] = 0x77, [InputKey.PageDown] = 0x79,
            [InputKey.Left] = 0x7B, [InputKey.Right] = 0x7C, [InputKey.Down] = 0x7D, [InputKey.Up] = 0x7E,

            [InputKey.NumPad0] = 0x52, [InputKey.NumPad1] = 0x53, [InputKey.NumPad2] = 0x54, [InputKey.NumPad3] = 0x55,
            [InputKey.NumPad4] = 0x56, [InputKey.NumPad5] = 0x57, [InputKey.NumPad6] = 0x58, [InputKey.NumPad7] = 0x59,
            [InputKey.NumPad8] = 0x5B, [InputKey.NumPad9] = 0x5C, [InputKey.Decimal] = 0x41, [InputKey.Multiply] = 0x43,
            [InputKey.Add] = 0x45, [InputKey.Clear] = 0x47, [InputKey.Divide] = 0x4B, [InputKey.Subtract] = 0x4E,

            [InputKey.F1] = 0x7A, [InputKey.F2] = 0x78, [InputKey.F3] = 0x63, [InputKey.F4] = 0x76,
            [InputKey.F5] = 0x60, [InputKey.F6] = 0x61, [InputKey.F7] = 0x62, [InputKey.F8] = 0x64,
            [InputKey.F9] = 0x65, [InputKey.F10] = 0x6D, [InputKey.F11] = 0x67, [InputKey.F12] = 0x6F,
            [InputKey.F13] = 0x69, [InputKey.F14] = 0x6B, [InputKey.F15] = 0x71, [InputKey.F16] = 0x6A,
            [InputKey.F17] = 0x40, [InputKey.F18] = 0x4F, [InputKey.F19] = 0x50, [InputKey.F20] = 0x5A,

            [InputKey.PrintScreen] = 0x69, [InputKey.Scroll] = 0x6B, [InputKey.Pause] = 0x71
        };

        public static bool TryGetKeyCode(InputKey key, out uint keyCode) => keys.TryGetValue(key, out keyCode);

        /// <summary>Ctrl, Alt and Win map to the keys in the same physical position: Control, Option and Command.</summary>
        public static uint GetModifiers(Modifiers modifiers)
        {
            uint result = 0;
            if (modifiers.HasFlag(Modifiers.Control)) result |= ControlKey;
            if (modifiers.HasFlag(Modifiers.Alt)) result |= OptionKey;
            if (modifiers.HasFlag(Modifiers.Shift)) result |= ShiftKey;
            if (modifiers.HasFlag(Modifiers.Win)) result |= CmdKey;
            return result;
        }
    }
}
