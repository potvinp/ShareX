#!/bin/bash
# Builds Output/ShareX-<version>-macos-<arch>.dmg containing a signed ShareX.app.
#
# Usage: ShareX.Setup/macOS/package.sh arm64|x64 [Configuration]
#
# Signing (optional; ad-hoc signing is used otherwise, which runs locally but not on other Macs):
#   MACOS_SIGN_IDENTITY   "Developer ID Application: Name (TEAMID)" from the login keychain
# Notarization (optional; requires MACOS_SIGN_IDENTITY):
#   MACOS_NOTARY_KEY_PATH, MACOS_NOTARY_KEY_ID, MACOS_NOTARY_ISSUER   App Store Connect API key
#
# Each architecture gets its own bundle: the self-contained .NET runtime's framework assemblies are
# compiled per architecture, so they cannot be merged into one universal bundle with lipo.
set -euo pipefail

ARCH=${1:?usage: package.sh arm64|x64 [Configuration]}
CONFIGURATION=${2:-Release}

case "$ARCH" in
    arm64) PLATFORM=ARM64 ;;
    x64) PLATFORM=x64 ;;
    *) echo "Unknown architecture: $ARCH" >&2; exit 1 ;;
esac

ROOT=$(cd "$(dirname "$0")/../.." && pwd)
SETUP="$ROOT/ShareX.Setup/macOS"
VERSION=$(sed -n 's:.*<Version>\(.*\)</Version>.*:\1:p' "$ROOT/Directory.Build.props" | head -n 1)
WORK="$ROOT/Output/macos-$ARCH"
APP="$WORK/ShareX.app"
DMG="$ROOT/Output/ShareX-$VERSION-macos-$ARCH.dmg"
IDENTITY=${MACOS_SIGN_IDENTITY:--}

rm -rf "$WORK"
mkdir -p "$WORK"

if [ ! -f "$ROOT/native/macos/bin/libsharex_macos.dylib" ]; then
    "$ROOT/native/macos/build.sh"
fi

echo "Publishing ShareX $VERSION for osx-$ARCH ($CONFIGURATION)"
dotnet publish "$ROOT/ShareX/ShareX.csproj" -c "$CONFIGURATION" -p:SharexTargetOS=macos -p:Platform="$PLATFORM" \
    --self-contained true -o "$WORK/publish"

if [ ! -f "$WORK/publish/libsharex_macos.dylib" ]; then
    echo "libsharex_macos.dylib is missing from the publish output." >&2
    exit 1
fi

echo "Creating app bundle"
mkdir -p "$APP/Contents/MacOS" "$APP/Contents/Resources"
cp -R "$WORK/publish/." "$APP/Contents/MacOS/"
sed "s/@VERSION@/$VERSION/g" "$SETUP/Info.plist" > "$APP/Contents/Info.plist"

ICONSET="$WORK/ShareX.iconset"
LOGO="$ROOT/ShareX.HelpersLib/Resources/ShareX_Logo.png"
mkdir -p "$ICONSET"
for size in 16 32 128 256; do
    sips -z "$size" "$size" "$LOGO" --out "$ICONSET/icon_${size}x${size}.png" > /dev/null
    double=$((size * 2))
    if [ "$double" -le 256 ]; then
        sips -z "$double" "$double" "$LOGO" --out "$ICONSET/icon_${size}x${size}@2x.png" > /dev/null
    fi
done
iconutil -c icns "$ICONSET" -o "$APP/Contents/Resources/ShareX.icns"

echo "Signing with identity: $IDENTITY"
SIGN_OPTIONS=(--force --sign "$IDENTITY")
if [ "$IDENTITY" != "-" ]; then
    SIGN_OPTIONS+=(--timestamp --options runtime)
fi
# Sign every file before the bundle, as Avalonia's macOS deployment guide recommends for .NET apps.
# The main executable is skipped here: signing it validates the bundle's other files, so the bundle
# signature below covers it once everything else is signed.
find "$APP/Contents/MacOS" -type f ! -path "$APP/Contents/MacOS/ShareX" -print0 | while IFS= read -r -d '' file; do
    codesign "${SIGN_OPTIONS[@]}" --entitlements "$SETUP/ShareX.entitlements" "$file"
done
codesign "${SIGN_OPTIONS[@]}" --entitlements "$SETUP/ShareX.entitlements" "$APP"
codesign --verify --deep --strict --verbose=2 "$APP"

echo "Creating $DMG"
STAGING="$WORK/dmg"
mkdir -p "$STAGING"
cp -R "$APP" "$STAGING/"
ln -s /Applications "$STAGING/Applications"
rm -f "$DMG"
hdiutil create -volname "ShareX" -srcfolder "$STAGING" -ov -format UDZO "$DMG" > /dev/null

if [ "$IDENTITY" != "-" ]; then
    codesign --force --timestamp --sign "$IDENTITY" "$DMG"

    if [ -n "${MACOS_NOTARY_KEY_PATH:-}" ]; then
        echo "Notarizing"
        xcrun notarytool submit "$DMG" --key "$MACOS_NOTARY_KEY_PATH" --key-id "$MACOS_NOTARY_KEY_ID" \
            --issuer "$MACOS_NOTARY_ISSUER" --wait
        xcrun stapler staple "$DMG"
    fi
fi

echo "Done: $DMG"
