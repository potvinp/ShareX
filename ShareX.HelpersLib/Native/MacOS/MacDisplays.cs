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
using System.Runtime.Versioning;

namespace ShareX.HelpersLib
{
    /// <summary>
    /// Maps between macOS global points and the pixel coordinates ShareX uses for screens, windows and captures.
    /// </summary>
    /// <remarks>
    /// Each display's pixel bounds are its frame in points multiplied by that display's backing scale, so a
    /// capture of a display is exactly its pixel bounds in size. With mixed scales (a Retina display next to
    /// a 1x display) pixel bounds of different displays can overlap; conversions resolve to the display that
    /// contains the point, so operations confined to one display stay exact.
    /// </remarks>
    [SupportedOSPlatform("macos")]
    public static class MacDisplays
    {
        public sealed class Display
        {
            public uint Id { get; }
            public RectangleF PointBounds { get; }
            public double Scale { get; }
            public bool Primary { get; }
            public Rectangle PixelBounds { get; }
            public Rectangle PixelWorkingArea { get; }

            internal Display(MacScreenData data)
            {
                Id = data.Id;
                Scale = data.Scale > 0 ? data.Scale : 1;
                Primary = data.Primary;
                PointBounds = new RectangleF((float)data.X, (float)data.Y, (float)data.Width, (float)data.Height);
                PixelBounds = Scaled(data.X, data.Y, data.Width, data.Height, Scale);
                PixelWorkingArea = Scaled(data.VisibleX, data.VisibleY, data.VisibleWidth, data.VisibleHeight, Scale);
            }

            public Point ToPixels(PointF point) =>
                new Point((int)Math.Round(point.X * Scale), (int)Math.Round(point.Y * Scale));

            public PointF ToPoints(Point pixel) =>
                new PointF((float)(pixel.X / Scale), (float)(pixel.Y / Scale));

            public Rectangle ToPixels(RectangleF rect) =>
                Scaled(rect.X, rect.Y, rect.Width, rect.Height, Scale);

            public RectangleF ToPoints(Rectangle rect) =>
                new RectangleF((float)(rect.X / Scale), (float)(rect.Y / Scale), (float)(rect.Width / Scale), (float)(rect.Height / Scale));

            private static Rectangle Scaled(double x, double y, double width, double height, double scale)
            {
                int left = (int)Math.Round(x * scale);
                int top = (int)Math.Round(y * scale);
                int right = (int)Math.Round((x + width) * scale);
                int bottom = (int)Math.Round((y + height) * scale);
                return Rectangle.FromLTRB(left, top, right, bottom);
            }
        }

        public static Display[] GetDisplays()
        {
            // NSScreen must be read on the main thread, which is Avalonia's UI thread on macOS.
            Display[] displays = DesktopServices.Run(() => MacNativeMethods.GetScreens().Select(x => new Display(x)).ToArray());
            if (displays.Length == 0)
            {
                throw new InvalidOperationException("No displays are available.");
            }
            return displays;
        }

        public static Display Primary => GetPrimary(GetDisplays());

        public static Display FromPoint(PointF point) => FromPoint(GetDisplays(), point);

        public static Display FromPixel(Point pixel) => FromPixel(GetDisplays(), pixel);

        public static Display FromPixelRectangle(Rectangle rect) => FromPixelRectangle(GetDisplays(), rect);

        internal static Display FromPoint(Display[] displays, PointF point) =>
            displays.FirstOrDefault(x => x.PointBounds.Contains(point)) ?? Nearest(displays, point, x => x.PointBounds);

        internal static Display FromPixel(Display[] displays, Point pixel) =>
            displays.FirstOrDefault(x => x.PixelBounds.Contains(pixel)) ?? Nearest(displays, pixel, x => x.PixelBounds);

        internal static Display FromPixelRectangle(Display[] displays, Rectangle rect)
        {
            // Prefer the display holding most of the rectangle, as Windows does for window placement.
            Display best = displays
                .Select(x => (Display: x, Area: Area(Rectangle.Intersect(x.PixelBounds, rect))))
                .OrderByDescending(x => x.Area)
                .First().Display;
            return Area(Rectangle.Intersect(best.PixelBounds, rect)) > 0 ? best : FromPixel(displays, rect.Center());
        }

        public static Point PointsToPixels(PointF point) => FromPoint(point).ToPixels(point);

        public static PointF PixelsToPoints(Point pixel) => FromPixel(pixel).ToPoints(pixel);

        public static Rectangle PointsToPixels(RectangleF rect)
        {
            Display display = FromPoint(new PointF(rect.X + rect.Width / 2, rect.Y + rect.Height / 2));
            return display.ToPixels(rect);
        }

        private static Display GetPrimary(Display[] displays) => displays.FirstOrDefault(x => x.Primary) ?? displays[0];

        private static Display Nearest(Display[] displays, PointF point, Func<Display, RectangleF> bounds) =>
            displays.OrderBy(x => DistanceSquared(bounds(x), point)).First();

        private static double DistanceSquared(RectangleF rect, PointF point)
        {
            double dx = Math.Max(0, Math.Max(rect.Left - point.X, point.X - rect.Right));
            double dy = Math.Max(0, Math.Max(rect.Top - point.Y, point.Y - rect.Bottom));
            return dx * dx + dy * dy;
        }

        private static long Area(Rectangle rect) => rect.IsEmpty ? 0 : (long)rect.Width * rect.Height;
    }
}
