// ShareX - A program that allows you to take screenshots and share any file type
// Copyright (c) 2007-2026 ShareX Team
// Licensed under the GNU General Public License v3. See LICENSE.txt in the repository root.

// Window enumeration via the Quartz window list. Window titles are only reported once the
// app has Screen Recording permission; owner names and bounds are always available.

import AppKit
import CoreGraphics

private func windowDictionaries(onScreenOnly: Bool) -> [[String: Any]] {
    var options: CGWindowListOption = [.excludeDesktopElements]
    if onScreenOnly {
        options.insert(.optionOnScreenOnly)
    }
    return (CGWindowListCopyWindowInfo(options, kCGNullWindowID) as? [[String: Any]]) ?? []
}

private func describe(_ info: [String: Any]) -> [String: Any]? {
    guard let number = info[kCGWindowNumber as String] as? NSNumber,
          let boundsDictionary = info[kCGWindowBounds as String] as? NSDictionary,
          let bounds = CGRect(dictionaryRepresentation: boundsDictionary as CFDictionary) else {
        return nil
    }
    return [
        "id": number.uint32Value,
        "pid": (info[kCGWindowOwnerPID as String] as? NSNumber)?.int32Value ?? 0,
        "layer": (info[kCGWindowLayer as String] as? NSNumber)?.int32Value ?? 0,
        "alpha": (info[kCGWindowAlpha as String] as? NSNumber)?.doubleValue ?? 1,
        "x": bounds.origin.x,
        "y": bounds.origin.y,
        "width": bounds.width,
        "height": bounds.height,
        "title": info[kCGWindowName as String] as? String ?? "",
        "owner": info[kCGWindowOwnerName as String] as? String ?? ""
    ]
}

/// Returns a JSON array of windows in front-to-back order:
/// [{ "id", "pid", "layer", "alpha", "x", "y", "width", "height", "title", "owner" }] with bounds in top-left global points.
@_cdecl("sharex_get_windows_json")
public func sharex_get_windows_json(_ onScreenOnly: Int32) -> UnsafeMutablePointer<CChar>? {
    return makeJSON(windowDictionaries(onScreenOnly: onScreenOnly != 0).compactMap(describe))
}

/// Returns the frontmost normal-layer window of the frontmost application, or 0.
@_cdecl("sharex_get_frontmost_window")
public func sharex_get_frontmost_window() -> UInt32 {
    guard let pid = NSWorkspace.shared.frontmostApplication?.processIdentifier else {
        return 0
    }
    for info in windowDictionaries(onScreenOnly: true) {
        let owner = (info[kCGWindowOwnerPID as String] as? NSNumber)?.int32Value
        let layer = (info[kCGWindowLayer as String] as? NSNumber)?.intValue
        if owner == pid && layer == 0, let number = info[kCGWindowNumber as String] as? NSNumber {
            return number.uint32Value
        }
    }
    return 0
}

/// Writes a window's bounds in top-left global points. Returns 0 on success.
@_cdecl("sharex_get_window_bounds")
public func sharex_get_window_bounds(_ windowID: UInt32,
                                     _ x: UnsafeMutablePointer<Double>, _ y: UnsafeMutablePointer<Double>,
                                     _ width: UnsafeMutablePointer<Double>, _ height: UnsafeMutablePointer<Double>) -> Int32 {
    guard let list = CGWindowListCopyWindowInfo([.optionIncludingWindow], windowID) as? [[String: Any]],
          let info = list.first,
          let boundsDictionary = info[kCGWindowBounds as String] as? NSDictionary,
          let bounds = CGRect(dictionaryRepresentation: boundsDictionary as CFDictionary) else {
        return ShareXStatus.notFound.rawValue
    }
    x.pointee = bounds.origin.x
    y.pointee = bounds.origin.y
    width.pointee = bounds.width
    height.pointee = bounds.height
    return ShareXStatus.ok.rawValue
}
