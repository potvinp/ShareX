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

using System.Runtime.Versioning;
using System.Threading;

namespace ShareX.HelpersLib
{
    [SupportedOSPlatform("macos")]
    public static class MacPermissions
    {
        private static int screenCaptureRequested;

        public static bool HasScreenCaptureAccess => MacNativeMethods.ScreenCapturePreflight() == 1;

        /// <summary>
        /// Returns true if ShareX may capture the screen. Otherwise asks macOS for access, which registers ShareX in
        /// Privacy &amp; Security and may show the system's (localized) prompt, then opens the Screen Recording pane.
        /// </summary>
        public static bool EnsureScreenCaptureAccess()
        {
            if (HasScreenCaptureAccess)
            {
                return true;
            }

            DebugHelper.WriteLine("Screen Recording permission is not granted.");

            // macOS shows its prompt at most once per app and not at all on some versions, so the request alone is not
            // enough; open the pane as well so the user can always switch ShareX on. A grant applies after restart.
            if (Interlocked.Exchange(ref screenCaptureRequested, 1) == 0 && MacNativeMethods.ScreenCaptureRequest() == 1)
            {
                return true;
            }

            OpenScreenCaptureSettings();
            return false;
        }

        public static void OpenScreenCaptureSettings() => MacNativeMethods.OpenPrivacySettings(0);
    }
}
