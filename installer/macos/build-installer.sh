#!/usr/bin/env bash
# OverlayerTool macOS 安装包构建脚本（.app + .dmg）
# 说明：Inno Setup 仅支持 Windows；macOS 使用本脚本生成 .dmg。
#
# 用法（在 macOS 上执行）：
#   chmod +x installer/macos/build-installer.sh
#   ./installer/macos/build-installer.sh
#   ./installer/macos/build-installer.sh osx-x64

set -euo pipefail

RID="${1:-osx-arm64}"
VERSION="0.0.1"
APP_NAME="OverlayerTool"
BUNDLE_NAME="${APP_NAME}.app"
EXEC_NAME="OverlayerTool.App"

ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
PROJECT="$ROOT/src/OverlayerTool.App/OverlayerTool.App.csproj"
PUBLISH_DIR="$ROOT/src/OverlayerTool.App/bin/Release/net8.0/${RID}/publish"
DIST_DIR="$ROOT/dist"
APP_DIR="$DIST_DIR/${BUNDLE_NAME}"
DMG_PATH="$DIST_DIR/${APP_NAME}-Setup-${VERSION}-${RID}.dmg"
TEMPLATE_PLIST="$ROOT/installer/macos/Info.plist.template"

echo "==> dotnet publish (${RID}, self-contained)"
dotnet publish "$PROJECT" \
  -c Release \
  -r "$RID" \
  --self-contained true \
  /p:PublishSingleFile=true \
  /p:IncludeNativeLibrariesForSelfExtract=true \
  /p:EnableCompressionInSingleFile=true \
  /p:PublishReadyToRun=true

if [[ ! -f "$PUBLISH_DIR/$EXEC_NAME" ]]; then
  echo "错误：未找到 $PUBLISH_DIR/$EXEC_NAME" >&2
  exit 1
fi

echo "==> 创建 ${BUNDLE_NAME}"
rm -rf "$APP_DIR"
mkdir -p "$APP_DIR/Contents/MacOS"
mkdir -p "$APP_DIR/Contents/Resources"

cp -R "$PUBLISH_DIR/"* "$APP_DIR/Contents/MacOS/"
chmod +x "$APP_DIR/Contents/MacOS/$EXEC_NAME"

if [[ -f "$ROOT/src/OverlayerTool.App/Assets/app.icns" ]]; then
  cp "$ROOT/src/OverlayerTool.App/Assets/app.icns" "$APP_DIR/Contents/Resources/app.icns"
fi

sed "s/0.0.1/${VERSION}/g" "$TEMPLATE_PLIST" > "$APP_DIR/Contents/Info.plist"

echo "==> 创建 DMG"
rm -f "$DMG_PATH"
mkdir -p "$DIST_DIR"
STAGING="$DIST_DIR/dmg-staging"
rm -rf "$STAGING"
mkdir -p "$STAGING"
cp -R "$APP_DIR" "$STAGING/"
ln -s /Applications "$STAGING/Applications"

hdiutil create \
  -volname "$APP_NAME" \
  -srcfolder "$STAGING" \
  -ov \
  -format UDZO \
  "$DMG_PATH"

rm -rf "$STAGING"

echo ""
echo "完成。"
echo "  应用程序：$APP_DIR"
echo "  安装镜像：  $DMG_PATH"
echo ""
echo "提示：对外分发建议在 macOS 上对 .app 进行 codesign 与 notarization。"
