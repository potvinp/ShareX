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

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.Versioning;

namespace ShareX.HelpersLib
{
    /// <summary>
    /// Quartz window list access. A window "handle" on macOS is its CGWindowID stored in an IntPtr.
    /// </summary>
    [SupportedOSPlatform("macos")]
    public static class MacWindows
    {
        public sealed class Window
        {
            public IntPtr Handle { get; }
            public Rectangle Rectangle { get; }
            public string Title { get; }
            public string ProcessName { get; }
            public int ProcessId { get; }

            internal Window(MacWindowData data, MacDisplays.Display[] displays)
            {
                Handle = new IntPtr(data.Id);
                Rectangle = ToPixels(data.X, data.Y, data.Width, data.Height, displays);
                Title = data.Title ?? "";
                ProcessName = data.Owner ?? "";
                ProcessId = data.ProcessId;
            }
        }

        public static IntPtr GetForegroundWindow() => new IntPtr(MacNativeMethods.GetFrontmostWindow());

        /// <summary>Visible normal-level windows in front-to-back order, excluding ShareX's own windows.</summary>
        public static List<Window> GetWindows()
        {
            MacDisplays.Display[] displays = MacDisplays.GetDisplays();
            int currentProcessId = Environment.ProcessId;
            return MacNativeMethods.GetWindows(onScreenOnly: true)
                .Where(x => x.Layer == 0 && x.Alpha > 0 && x.Width > 0 && x.Height > 0 && x.ProcessId != currentProcessId)
                .Select(x => new Window(x, displays))
                .ToList();
        }

        public static Window GetWindow(IntPtr handle)
        {
            uint id = (uint)handle.ToInt64();
            if (id == 0)
            {
                return null;
            }

            MacWindowData data = MacNativeMethods.GetWindows(onScreenOnly: false).FirstOrDefault(x => x.Id == id);
            return data == null ? null : new Window(data, MacDisplays.GetDisplays());
        }

        public static Rectangle GetWindowRectangle(IntPtr handle)
        {
            if (MacNativeMethods.GetWindowBounds((uint)handle.ToInt64(), out double x, out double y, out double width, out double height) != MacNativeMethods.StatusOk)
            {
                return Rectangle.Empty;
            }
            return ToPixels(x, y, width, height, MacDisplays.GetDisplays());
        }

        private static Rectangle ToPixels(double x, double y, double width, double height, MacDisplays.Display[] displays)
        {
            RectangleF points = new RectangleF((float)x, (float)y, (float)width, (float)height);
            PointF center = new PointF(points.X + points.Width / 2, points.Y + points.Height / 2);
            return MacDisplays.FromPoint(displays, center).ToPixels(points);
        }
    }
}
