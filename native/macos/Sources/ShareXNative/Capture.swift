// ShareX - A program that allows you to take screenshots and share any file type
// Copyright (c) 2007-2026 ShareX Team
// Licensed under the GNU General Public License v3. See LICENSE.txt in the repository root.

// Screenshots via ScreenCaptureKit (macOS 14+). ScreenCaptureKit is asynchronous; these exports block
// the calling thread until the capture completes or times out. Its completion handlers run on
// ScreenCaptureKit's own queues, so blocking the main thread here does not deadlock.

import CoreGraphics
import Foundation
import ScreenCaptureKit

private let captureTimeout: DispatchTimeInterval = .seconds(10)

private func status(for error: Error?) -> Int32 {
    guard let error = error as NSError? else {
        return ShareXStatus.captureFailed.rawValue
    }
    if error.domain == SCStreamErrorDomain && error.code == SCStreamError.Code.userDeclined.rawValue {
        return ShareXStatus.permissionDenied.rawValue
    }
    return ShareXStatus.captureFailed.rawValue
}

/// Carries a completion handler's result back to the waiting thread. A class, so @Sendable handlers can
/// write to it; the semaphore orders the write before the read.
private final class CompletionResult<Value>: @unchecked Sendable {
    var value: Value?
    var status = ShareXStatus.ok.rawValue
}

private func currentShareableContent() -> (SCShareableContent?, Int32) {
    let semaphore = DispatchSemaphore(value: 0)
    let result = CompletionResult<SCShareableContent>()

    SCShareableContent.getExcludingDesktopWindows(false, onScreenWindowsOnly: true) { shareable, error in
        result.value = shareable
        if shareable == nil {
            result.status = status(for: error)
        }
        semaphore.signal()
    }

    if semaphore.wait(timeout: .now() + captureTimeout) == .timedOut {
        return (nil, ShareXStatus.timeout.rawValue)
    }
    return (result.value, result.status)
}

private func captureImage(_ filter: SCContentFilter, _ configuration: SCStreamConfiguration) -> (CGImage?, Int32) {
    let semaphore = DispatchSemaphore(value: 0)
    let result = CompletionResult<CGImage>()

    SCScreenshotManager.captureImage(contentFilter: filter, configuration: configuration) { captured, error in
        result.value = captured
        if captured == nil {
            result.status = status(for: error)
        }
        semaphore.signal()
    }

    if semaphore.wait(timeout: .now() + captureTimeout) == .timedOut {
        return (nil, ShareXStatus.timeout.rawValue)
    }
    return (result.value, result.status)
}

private func makeConfiguration(width: CGFloat, height: CGFloat, showCursor: Bool) -> SCStreamConfiguration {
    let configuration = SCStreamConfiguration()
    configuration.width = max(1, Int(width.rounded()))
    configuration.height = max(1, Int(height.rounded()))
    configuration.showsCursor = showCursor
    configuration.scalesToFit = false
    configuration.captureResolution = .best
    configuration.pixelFormat = kCVPixelFormatType_32BGRA
    configuration.colorSpaceName = CGColorSpace.sRGB
    return configuration
}

/// Renders `image` into a malloc'd BGRA premultiplied sRGB buffer (SkiaSharp Bgra8888/Premul).
private func copyPixels(_ image: CGImage,
                        _ pixels: UnsafeMutablePointer<UnsafeMutableRawPointer?>,
                        _ width: UnsafeMutablePointer<Int32>,
                        _ height: UnsafeMutablePointer<Int32>,
                        _ stride: UnsafeMutablePointer<Int32>) -> Int32 {
    let imageWidth = image.width
    let imageHeight = image.height
    let bytesPerRow = imageWidth * 4

    guard let buffer = malloc(bytesPerRow * imageHeight),
          let colorSpace = CGColorSpace(name: CGColorSpace.sRGB),
          let context = CGContext(data: buffer, width: imageWidth, height: imageHeight, bitsPerComponent: 8,
                                  bytesPerRow: bytesPerRow, space: colorSpace,
                                  bitmapInfo: CGImageAlphaInfo.premultipliedFirst.rawValue | CGBitmapInfo.byteOrder32Little.rawValue) else {
        return ShareXStatus.captureFailed.rawValue
    }

    context.draw(image, in: CGRect(x: 0, y: 0, width: imageWidth, height: imageHeight))

    pixels.pointee = buffer
    width.pointee = Int32(imageWidth)
    height.pointee = Int32(imageHeight)
    stride.pointee = Int32(bytesPerRow)
    return ShareXStatus.ok.rawValue
}

/// Captures a rectangle of one display at the display's native resolution.
/// The rectangle is in top-left global points and is clipped to the display.
@_cdecl("sharex_capture_display_rect")
public func sharex_capture_display_rect(_ displayID: UInt32,
                                        _ x: Double, _ y: Double, _ w: Double, _ h: Double,
                                        _ showCursor: Int32,
                                        _ pixels: UnsafeMutablePointer<UnsafeMutableRawPointer?>,
                                        _ width: UnsafeMutablePointer<Int32>,
                                        _ height: UnsafeMutablePointer<Int32>,
                                        _ stride: UnsafeMutablePointer<Int32>) -> Int32 {
    pixels.pointee = nil

    let (shareable, contentStatus) = currentShareableContent()
    guard let content = shareable else {
        return contentStatus
    }
    guard let display = content.displays.first(where: { $0.displayID == displayID }) else {
        return ShareXStatus.notFound.rawValue
    }

    let displayFrame = CGDisplayBounds(displayID)
    let requested = CGRect(x: x, y: y, width: w, height: h).intersection(displayFrame)
    if requested.isNull || requested.width < 1 || requested.height < 1 {
        return ShareXStatus.invalidArgument.rawValue
    }

    let filter = SCContentFilter(display: display, excludingWindows: [])
    let scale = CGFloat(filter.pointPixelScale)
    let configuration = makeConfiguration(width: requested.width * scale, height: requested.height * scale,
                                          showCursor: showCursor != 0)
    // sourceRect is relative to the display's top-left corner, in points.
    configuration.sourceRect = requested.offsetBy(dx: -displayFrame.origin.x, dy: -displayFrame.origin.y)

    let (captured, captureStatus) = captureImage(filter, configuration)
    guard let image = captured else {
        return captureStatus
    }
    return copyPixels(image, pixels, width, height, stride)
}

/// Captures a single window's own content, independent of what overlaps it.
@_cdecl("sharex_capture_window")
public func sharex_capture_window(_ windowID: UInt32,
                                  _ includeShadow: Int32,
                                  _ showCursor: Int32,
                                  _ pixels: UnsafeMutablePointer<UnsafeMutableRawPointer?>,
                                  _ width: UnsafeMutablePointer<Int32>,
                                  _ height: UnsafeMutablePointer<Int32>,
                                  _ stride: UnsafeMutablePointer<Int32>) -> Int32 {
    pixels.pointee = nil

    let (shareable, contentStatus) = currentShareableContent()
    guard let content = shareable else {
        return contentStatus
    }
    guard let window = content.windows.first(where: { $0.windowID == windowID }) else {
        return ShareXStatus.notFound.rawValue
    }

    let filter = SCContentFilter(desktopIndependentWindow: window)
    let scale = CGFloat(filter.pointPixelScale)
    let configuration = makeConfiguration(width: filter.contentRect.width * scale, height: filter.contentRect.height * scale,
                                          showCursor: showCursor != 0)
    configuration.ignoreShadowsSingleWindow = includeShadow == 0
    configuration.shouldBeOpaque = includeShadow == 0

    let (captured, captureStatus) = captureImage(filter, configuration)
    guard let image = captured else {
        return captureStatus
    }
    return copyPixels(image, pixels, width, height, stride)
}
