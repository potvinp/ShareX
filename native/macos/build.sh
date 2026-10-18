#!/bin/sh
# Builds libsharex_macos.dylib as a universal (arm64 + x86_64) binary into native/macos/bin.
# Requires Xcode 15+ command line tools on macOS. ShareX.HelpersLib copies the result into the
# app output when building with -p:SharexTargetOS=macos.
set -eu

cd "$(dirname "$0")"

swift build -c release --arch arm64 --arch x86_64

mkdir -p bin
cp .build/apple/Products/Release/libsharex_macos.dylib bin/libsharex_macos.dylib

lipo -info bin/libsharex_macos.dylib
