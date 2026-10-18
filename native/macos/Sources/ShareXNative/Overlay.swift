// ShareX - A program that allows you to take screenshots and share any file type
// Copyright (c) 2007-2026 ShareX Team
// Licensed under the GNU General Public License v3. See LICENSE.txt in the repository root.

// Raises Avalonia windows above the menu bar and Dock so capture overlays cover a whole display,
// including over full-screen apps and on every Space.

import AppKit

/// Avalonia's macOS platform handle is an NSView (or NSWindow); resolve either to its window.
private func resolveWindow(_ handle: UnsafeMutableRawPointer) -> NSWindow? {
    let object = Unmanaged<AnyObject>.fromOpaque(handle).takeUnretainedValue()
    if let window = object as? NSWindow {
        return window
    }
    return (object as? NSView)?.window
}

/// Moves the window to cover `x, y, width, height` (top-left global points) at overlay level and
/// brings it to the front with keyboard focus. Returns 0 on success.
@_cdecl("sharex_window_configure_overlay")
public func sharex_window_configure_overlay(_ handle: UnsafeMutableRawPointer?,
                                            _ x: Double, _ y: Double, _ width: Double, _ height: Double) -> Int32 {
    guard let handle else {
        return ShareXStatus.invalidArgument.rawValue
    }
    return onMain {
        guard let window = resolveWindow(handle) else {
            return ShareXStatus.notFound.rawValue
        }
        window.level = .screenSaver
        window.collectionBehavior = [.canJoinAllSpaces, .fullScreenAuxiliary, .stationary, .ignoresCycle]
        window.hasShadow = false
        window.animationBehavior = .none
        window.setFrame(cocoaRect(fromTopLeft: CGRect(x: x, y: y, width: width, height: height)), display: true)
        NSApp.activate()
        window.makeKeyAndOrderFront(nil)
        return ShareXStatus.ok.rawValue
    }
}

/// Brings ShareX to the foreground.
@_cdecl("sharex_activate_app")
public func sharex_activate_app() {
    onMain {
        NSApp.activate()
    }
}
