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

namespace ShareX.HelpersLib
{
    /// <summary>Launch at login through SMAppService (System Settings > General > Login Items).</summary>
    [SupportedOSPlatform("macos")]
    public static class MacLoginItem
    {
        public static bool IsEnabled => MacNativeMethods.GetLoginItemStatus() == MacNativeMethods.LoginItemEnabled;

        public static void SetEnabled(bool enabled)
        {
            int status = MacNativeMethods.SetLoginItem(enabled ? 1 : 0);
            if (status != MacNativeMethods.StatusOk)
            {
                DebugHelper.WriteLine($"Unable to {(enabled ? "register" : "unregister")} the login item. Error: {status}");
            }

            // A login item the user switched off in System Settings can only be switched back on there.
            if (enabled && MacNativeMethods.GetLoginItemStatus() == MacNativeMethods.LoginItemRequiresApproval)
            {
                MacNativeMethods.OpenLoginItemsSettings();
            }
        }
    }
}
