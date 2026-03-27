#!/usr/bin/env bash
set -euo pipefail

DOTNET="$HOME/.dotnet/dotnet"

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
DESKTOP_PROJ="$SCRIPT_DIR/LocoCalc.Desktop/LocoCalc.Desktop.csproj"
ANDROID_PROJ="$SCRIPT_DIR/LocoCalc.Android/LocoCalc.Android.csproj"

# ── Parse arguments ─────────────────────────────────────────────────────────
DIST="$SCRIPT_DIR/dist"
TARGETS=()

usage() {
    echo "Usage: $0 [-o|--output <folder>] [-r|--release <target>]..."
    echo "  Targets: win-x64, linux-x64, android (default: all)"
    echo "  Example: $0 -r win-x64 -r android -o /tmp/out"
    exit 1
}

while [[ $# -gt 0 ]]; do
    case "$1" in
        -o|--output)
            DIST="$2"
            shift 2
            ;;
        -r|--release)
            TARGETS+=("$2")
            shift 2
            ;;
        *)
            usage
            ;;
    esac
done

# Default to all targets if none specified
if [[ ${#TARGETS[@]} -eq 0 ]]; then
    TARGETS=(win-x64 linux-x64 android)
fi

# Validate targets
for t in "${TARGETS[@]}"; do
    case "$t" in
        win-x64|linux-x64|android) ;;
        *) echo "Unknown target: $t" >&2; usage ;;
    esac
done

VERSION=$(grep -m1 '<AppVersion>' "$SCRIPT_DIR/Directory.Build.props" | sed 's/.*<AppVersion>\(.*\)<\/AppVersion>.*/\1/' | tr -d '[:space:]')

rm -rf "$DIST"
mkdir -p "$DIST"

ok()  { echo "  ✓ $*"; }
fail(){ echo "  ✗ $*" >&2; exit 1; }
step(){ echo; echo "▶ $*"; }

for TARGET in "${TARGETS[@]}"; do
    case "$TARGET" in

        win-x64)
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
            ;;

        linux-x64)
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
            ;;

        android)
            step "Publishing Android (APK)..."
            "$DOTNET" publish "$ANDROID_PROJ" \
                -c Release \
                -o "$DIST/android" \
                || fail "Android publish failed"
            APK=$(find "$DIST/android" -name "*.apk" | head -1)
            [[ -z "$APK" ]] && fail "No APK found in dist/android"
            cp "$APK" "$DIST/LocoCalc-android-$VERSION.apk"
            ok "LocoCalc-android-$VERSION.apk"
            ;;

    esac
done

# ── Summary ──────────────────────────────────────────────────────────────────
echo
echo "Done. Output in $DIST:"
ls -lh "$DIST"/*.zip "$DIST"/*.apk 2>/dev/null || ls -lh "$DIST"
