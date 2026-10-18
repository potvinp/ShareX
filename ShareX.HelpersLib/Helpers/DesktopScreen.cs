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

using ShareX.AvaloniaUI.Integration;
using System;
using System.Drawing;
using System.Linq;

namespace ShareX.HelpersLib;

/// <summary>A display snapshot in physical pixels, matching screenshot coordinates.</summary>
public sealed class DesktopScreen
{
    public Rectangle Bounds { get; }
    public Rectangle WorkingArea { get; }
    public bool Primary { get; }

    private DesktopScreen(Avalonia.Platform.Screen screen)
    {
        Bounds = new Rectangle(screen.Bounds.X, screen.Bounds.Y, screen.Bounds.Width, screen.Bounds.Height);
        WorkingArea = new Rectangle(screen.WorkingArea.X, screen.WorkingArea.Y, screen.WorkingArea.Width, screen.WorkingArea.Height);
        Primary = screen.IsPrimary;
    }

    // macOS screens come from the native helper so they use the same pixel space as MacScreenCapture.
    [System.Runtime.Versioning.SupportedOSPlatform("macos")]
    private DesktopScreen(MacDisplays.Display display)
    {
        Bounds = display.PixelBounds;
        WorkingArea = display.PixelWorkingArea;
        Primary = display.Primary;
    }

    public static DesktopScreen[] AllScreens => OperatingSystem.IsMacOS()
        ? GetMacScreens()
        : DesktopServices.Run(() => DesktopServices.GetWindow().Screens.All.Select(x => new DesktopScreen(x)).ToArray());

    [System.Runtime.Versioning.SupportedOSPlatform("macos")]
    private static DesktopScreen[] GetMacScreens() => MacDisplays.GetDisplays().Select(x => new DesktopScreen(x)).ToArray();

    public static DesktopScreen PrimaryScreen => AllScreens.FirstOrDefault(x => x.Primary) ?? AllScreens.FirstOrDefault();

    public static DesktopScreen FromPoint(Point point)
    {
        if (OperatingSystem.IsMacOS()) return new DesktopScreen(MacDisplays.FromPixel(point));

        return DesktopServices.Run(() =>
        {
            var screens = DesktopServices.GetWindow().Screens;
            var screen = screens.ScreenFromPoint(new Avalonia.PixelPoint(point.X, point.Y)) ?? screens.Primary;
            return screen == null ? null : new DesktopScreen(screen);
        });
    }

    public static DesktopScreen FromRectangle(Rectangle bounds)
    {
        if (OperatingSystem.IsMacOS()) return new DesktopScreen(MacDisplays.FromPixelRectangle(bounds));

        return DesktopServices.Run(() =>
        {
            var screens = DesktopServices.GetWindow().Screens;
            var screen = screens.ScreenFromBounds(new Avalonia.PixelRect(bounds.X, bounds.Y, bounds.Width, bounds.Height)) ?? screens.Primary;
            return screen == null ? null : new DesktopScreen(screen);
        });
    }

    public static DesktopScreen FromHandle(IntPtr handle)
    {
        if (OperatingSystem.IsWindows()) return FromRectangle(NativeMethods.GetWindowRect(handle));
        if (OperatingSystem.IsMacOS()) return FromRectangle(MacWindows.GetWindowRectangle(handle));
        return PrimaryScreen;
    }
}
