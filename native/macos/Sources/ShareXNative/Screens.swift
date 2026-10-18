// ShareX - A program that allows you to take screenshots and share any file type
// Copyright (c) 2007-2026 ShareX Team
// Licensed under the GNU General Public License v3. See LICENSE.txt in the repository root.

import AppKit
import CoreGraphics

/// Returns a JSON array describing every active display:
/// [{ "id", "x", "y", "width", "height", "visibleX", "visibleY", "visibleWidth", "visibleHeight", "scale", "primary" }]
/// Frames are top-left global points; "scale" is the backing scale factor (pixels per point).
@_cdecl("sharex_get_screens_json")
public func sharex_get_screens_json() -> UnsafeMutablePointer<CChar>? {
    let screens: [[String: Any]] = onMain {
        NSScreen.screens.compactMap { (screen: NSScreen) -> [String: Any]? in
            guard let number = screen.deviceDescription[NSDeviceDescriptionKey("NSScreenNumber")] as? NSNumber else {
                return nil
            }
            let displayID = CGDirectDisplayID(number.uint32Value)
            let frame = CGDisplayBounds(displayID)
            let visible = topLeftRect(fromCocoa: screen.visibleFrame)
            return [
                "id": displayID,
                "x": frame.origin.x,
                "y": frame.origin.y,
                "width": frame.width,
                "height": frame.height,
                "visibleX": visible.origin.x,
                "visibleY": visible.origin.y,
                "visibleWidth": visible.width,
                "visibleHeight": visible.height,
                "scale": screen.backingScaleFactor,
                "primary": CGDisplayIsMain(displayID) != 0
            ]
        }
    }
    return makeJSON(screens)
}

/// Writes the mouse cursor location in top-left global points.
@_cdecl("sharex_get_cursor_position")
public func sharex_get_cursor_position(_ x: UnsafeMutablePointer<Double>, _ y: UnsafeMutablePointer<Double>) {
    let location = CGEvent(source: nil)?.location ?? .zero
    x.pointee = location.x
    y.pointee = location.y
}

/// Moves the mouse cursor to a location in top-left global points.
@_cdecl("sharex_set_cursor_position")
public func sharex_set_cursor_position(_ x: Double, _ y: Double) {
    CGWarpMouseCursorPosition(CGPoint(x: x, y: y))
    // Warping suppresses mouse events for a short interval unless the cursor is re-associated.
    CGAssociateMouseAndMouseCursorPosition(1)
}
