// swift-tools-version:5.9

import PackageDescription

let package = Package(
    name: "ShareXNative",
    platforms: [
        .macOS(.v14)
    ],
    products: [
        .library(name: "sharex_macos", type: .dynamic, targets: ["ShareXNative"])
    ],
    targets: [
        .target(
            name: "ShareXNative",
            path: "Sources/ShareXNative",
            linkerSettings: [
                .linkedFramework("AppKit"),
                .linkedFramework("Carbon"),
                .linkedFramework("CoreGraphics"),
                .linkedFramework("ScreenCaptureKit"),
                .linkedFramework("Security"),
                .linkedFramework("ServiceManagement"),
                .unsafeFlags(["-Xlinker", "-install_name", "-Xlinker", "@rpath/libsharex_macos.dylib"])
            ]
        )
    ]
)
