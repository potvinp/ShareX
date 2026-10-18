// ShareX - A program that allows you to take screenshots and share any file type
// Copyright (c) 2007-2026 ShareX Team
// Licensed under the GNU General Public License v3. See LICENSE.txt in the repository root.

// C ABI helpers shared by every export. All exports use plain C types so ShareX.HelpersLib can
// P/Invoke them without struct layout agreements; structured data is returned as UTF-8 JSON.

import AppKit
import Foundation

/// Status codes returned by exports. Negative values are ShareX errors, positive values are OSStatus.
enum ShareXStatus: Int32 {
    case ok = 0
    case invalidArgument = -1
    case notFound = -2
    case timeout = -3
    case permissionDenied = -4
    case captureFailed = -5
}

/// Runs `body` on the main thread. AppKit objects (NSScreen, NSWindow) must only be touched there.
func onMain<T>(_ body: () -> T) -> T {
    if Thread.isMainThread {
        return body()
    }
    return DispatchQueue.main.sync(execute: body)
}

/// Height of the primary display in points, used to flip Cocoa's bottom-left origin to the
/// top-left global coordinate space used by Quartz and by ShareX.
func primaryScreenHeight() -> CGFloat {
    return CGDisplayBounds(CGMainDisplayID()).height
}

/// Converts a rectangle in top-left global points to Cocoa's bottom-left screen coordinates.
func cocoaRect(fromTopLeft rect: CGRect) -> NSRect {
    return NSRect(x: rect.origin.x, y: primaryScreenHeight() - rect.origin.y - rect.height, width: rect.width, height: rect.height)
}

/// Converts a Cocoa bottom-left rectangle to top-left global points.
func topLeftRect(fromCocoa rect: NSRect) -> CGRect {
    return CGRect(x: rect.origin.x, y: primaryScreenHeight() - rect.origin.y - rect.height, width: rect.width, height: rect.height)
}

/// Returns a malloc'd, NUL-terminated UTF-8 copy of the JSON encoding of `value`. Free with sharex_free.
func makeJSON(_ value: Any) -> UnsafeMutablePointer<CChar>? {
    guard JSONSerialization.isValidJSONObject(value),
          let data = try? JSONSerialization.data(withJSONObject: value),
          let string = String(data: data, encoding: .utf8) else {
        return strdup("[]")
    }
    return strdup(string)
}

@_cdecl("sharex_free")
public func sharex_free(_ pointer: UnsafeMutableRawPointer?) {
    free(pointer)
}
