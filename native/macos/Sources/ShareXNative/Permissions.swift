// ShareX - A program that allows you to take screenshots and share any file type
// Copyright (c) 2007-2026 ShareX Team
// Licensed under the GNU General Public License v3. See LICENSE.txt in the repository root.

import AppKit
import CoreGraphics

/// Returns 1 if the app may capture the screen, without prompting.
@_cdecl("sharex_screen_capture_preflight")
public func sharex_screen_capture_preflight() -> Int32 {
    return CGPreflightScreenCaptureAccess() ? 1 : 0
}

/// Asks the system to show its Screen Recording prompt (only shown once per app). Returns 1 if granted.
/// A grant normally takes effect only after the app restarts.
@_cdecl("sharex_screen_capture_request")
public func sharex_screen_capture_request() -> Int32 {
    return CGRequestScreenCaptureAccess() ? 1 : 0
}

/// Opens System Settings at a Privacy & Security pane. 0 = Screen Recording, 1 = Accessibility.
@_cdecl("sharex_open_privacy_settings")
public func sharex_open_privacy_settings(_ pane: Int32) {
    let anchor = pane == 1 ? "Privacy_Accessibility" : "Privacy_ScreenCapture"
    if let url = URL(string: "x-apple.systempreferences:com.apple.preference.security?\(anchor)") {
        onMain {
            _ = NSWorkspace.shared.open(url)
        }
    }
}
