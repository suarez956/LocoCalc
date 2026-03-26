#!/usr/bin/env bash
set -euo pipefail

DOTNET="$HOME/.dotnet/dotnet"

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
DIST="$SCRIPT_DIR/dist"
DESKTOP_PROJ="$SCRIPT_DIR/LocoCalc.Desktop/LocoCalc.Desktop.csproj"
ANDROID_PROJ="$SCRIPT_DIR/LocoCalc.Android/LocoCalc.Android.csproj"

VERSION=$(grep -m1 '<Version>' "$DESKTOP_PROJ" | sed 's/.*<Version>\(.*\)<\/Version>.*/\1/' | tr -d '[:space:]')

rm -rf "$DIST"
mkdir -p "$DIST"

ok()  { echo "  ✓ $*"; }
fail(){ echo "  ✗ $*" >&2; exit 1; }
step(){ echo; echo "▶ $*"; }

# ── Windows x64 ────────────────────────────────────────────────────────────
step "Publishing Windows x64..."
"$DOTNET" publish "$DESKTOP_PROJ" \
    -c Release -r win-x64 \
    --self-contained true \
    -p:PublishSingleFile=true \
    -o "$DIST/win-x64" \
    -p:DebugType=none -p:DebugSymbols=false \
    || fail "Windows x64 publish failed"

cd "$DIST"
zip -r "LocoCalc-win-x64-$VERSION.zip" "win-x64/" > /dev/null
ok "LocoCalc-win-x64-$VERSION.zip"

# ── Linux x64 ──────────────────────────────────────────────────────────────
step "Publishing Linux x64..."
"$DOTNET" publish "$DESKTOP_PROJ" \
    -c Release -r linux-x64 \
    --self-contained true \
    -p:PublishSingleFile=true \
    -o "$DIST/linux-x64" \
    -p:DebugType=none -p:DebugSymbols=false \
    || fail "Linux x64 publish failed"

cd "$DIST"
zip -r "LocoCalc-linux-x64-$VERSION.zip" "linux-x64/" > /dev/null
ok "LocoCalc-linux-x64-$VERSION.zip"

# ── Android APK ────────────────────────────────────────────────────────────
step "Publishing Android (APK)..."
"$DOTNET" publish "$ANDROID_PROJ" \
    -c Release \
    -o "$DIST/android" \
    || fail "Android publish failed"

APK=$(find "$DIST/android" -name "*.apk" | head -1)
if [[ -z "$APK" ]]; then
    fail "No APK found in dist/android"
fi

cd "$DIST"
zip "LocoCalc-android-$VERSION.zip" "android/$(basename "$APK")" > /dev/null
ok "LocoCalc-android-$VERSION.zip  ($(basename "$APK"))"

# ── Summary ────────────────────────────────────────────────────────────────
echo
echo "Done. Archives in $DIST:"
ls -lh "$DIST"/*.zip
