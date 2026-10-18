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
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.Versioning;
using System.Threading.Tasks;

namespace ShareX.HelpersLib
{
    /// <summary>ScreenCaptureKit screenshots in ShareX pixel coordinates (see <see cref="MacDisplays"/>).</summary>
    [SupportedOSPlatform("macos")]
    public static class MacScreenCapture
    {
        /// <summary>Captures a pixel rectangle, composing the parts that fall on each display.</summary>
        public static SKBitmap CaptureRectangle(Rectangle rect, bool captureCursor)
        {
            if (rect.Width <= 0 || rect.Height <= 0)
            {
                return null;
            }

            List<(MacDisplays.Display Display, Rectangle Part)> parts = new();
            foreach (MacDisplays.Display display in MacDisplays.GetDisplays())
            {
                Rectangle part = Rectangle.Intersect(display.PixelBounds, rect);
                if (part.Width > 0 && part.Height > 0)
                {
                    parts.Add((display, part));
                }
            }

            if (parts.Count == 0)
            {
                return null;
            }

            if (parts.Count == 1 && parts[0].Part == rect)
            {
                return CaptureDisplayPart(parts[0].Display, rect, captureCursor);
            }

            SKBitmap result = new SKBitmap(new SKImageInfo(rect.Width, rect.Height, SKColorType.Bgra8888, SKAlphaType.Premul));
            try
            {
                using SKCanvas canvas = new SKCanvas(result);
                canvas.Clear(SKColors.Transparent);
                foreach ((MacDisplays.Display display, Rectangle part) in parts)
                {
                    using SKBitmap bitmap = CaptureDisplayPart(display, part, captureCursor);
                    canvas.DrawBitmap(bitmap, SKRect.Create(part.X - rect.X, part.Y - rect.Y, part.Width, part.Height));
                }
                return result;
            }
            catch
            {
                result.Dispose();
                throw;
            }
        }

        /// <summary>Captures one window's own content (CGWindowID in <paramref name="handle"/>).</summary>
        public static SKBitmap CaptureWindow(IntPtr handle, bool includeShadow, bool captureCursor)
        {
            return RunNative(() =>
            {
                int status = MacNativeMethods.CaptureWindow((uint)handle.ToInt64(), includeShadow ? 1 : 0, captureCursor ? 1 : 0,
                    out IntPtr pixels, out int width, out int height, out int stride);
                ThrowIfFailed(status);
                return ToBitmap(pixels, width, height, stride);
            });
        }

        private static SKBitmap CaptureDisplayPart(MacDisplays.Display display, Rectangle part, bool captureCursor)
        {
            RectangleF points = display.ToPoints(part);
            SKBitmap bitmap = RunNative(() =>
            {
                int status = MacNativeMethods.CaptureDisplayRect(display.Id, points.X, points.Y, points.Width, points.Height, captureCursor ? 1 : 0,
                    out IntPtr pixels, out int width, out int height, out int stride);
                ThrowIfFailed(status);
                return ToBitmap(pixels, width, height, stride);
            });

            if (bitmap.Width == part.Width && bitmap.Height == part.Height)
            {
                return bitmap;
            }

            // Rounding of fractional points can change the size by a pixel; keep callers' geometry exact.
            using (bitmap)
            {
                return bitmap.Resize(new SKImageInfo(part.Width, part.Height, SKColorType.Bgra8888, SKAlphaType.Premul),
                    new SKSamplingOptions(SKFilterMode.Linear));
            }
        }

        /// <summary>
        /// Runs a blocking ScreenCaptureKit call on a worker thread. If called on the UI (main) thread, the wait pumps
        /// a nested dispatcher frame so the main run loop keeps turning while ScreenCaptureKit completes.
        /// </summary>
        private static SKBitmap RunNative(Func<SKBitmap> capture) => DesktopServices.Run(() => Task.Run(capture));

        private static unsafe SKBitmap ToBitmap(IntPtr pixels, int width, int height, int stride)
        {
            try
            {
                SKBitmap bitmap = new SKBitmap(new SKImageInfo(width, height, SKColorType.Bgra8888, SKAlphaType.Premul));
                byte* destination = (byte*)bitmap.GetPixels();
                byte* source = (byte*)pixels;
                int rowBytes = Math.Min(stride, bitmap.RowBytes);
                for (int y = 0; y < height; y++)
                {
                    Buffer.MemoryCopy(source + (long)y * stride, destination + (long)y * bitmap.RowBytes, bitmap.RowBytes, rowBytes);
                }
                bitmap.NotifyPixelsChanged();
                return bitmap;
            }
            finally
            {
                MacNativeMethods.Free(pixels);
            }
        }

        private static void ThrowIfFailed(int status)
        {
            switch (status)
            {
                case MacNativeMethods.StatusOk:
                    return;
                case MacNativeMethods.StatusPermissionDenied:
                    throw new UnauthorizedAccessException("Screen Recording permission is required to capture the screen.");
                case MacNativeMethods.StatusTimeout:
                    throw new TimeoutException("Screen capture timed out.");
                default:
                    throw new InvalidOperationException($"Screen capture failed (status {status}).");
            }
        }
    }
}
