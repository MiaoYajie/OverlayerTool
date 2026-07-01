# OverlayerTool 安装包构建

## 说明

| 平台 | 工具 | 产物 |
|------|------|------|
| **Windows** | [Inno Setup 6](https://jrsoftware.org/isinfo.php) | `dist/OverlayerTool-Setup-0.0.1-win-x64.exe` |
| **macOS** | 内置 `build-installer.sh` + `hdiutil` | `dist/OverlayerTool-Setup-0.0.1-osx-arm64.dmg` |

> **Inno Setup 仅支持 Windows。** macOS 无法使用 `.iss` 脚本，请使用 `installer/macos/build-installer.sh` 生成 `.app` 与 `.dmg`。

---

## Windows

### 前置条件

1. [.NET 8 SDK](https://dotnet.microsoft.com/download)
2. [Inno Setup 6](https://jrsoftware.org/isinfo.php)（安装后包含 `ISCC.exe`）

### 一键构建

```powershell
cd installer/windows
.\build-installer.ps1
```

脚本会：

1. `dotnet publish`（`win-x64`、自带运行时、单文件）
2. 调用 Inno Setup 编译 `OverlayerTool.iss`

### 手动构建

```powershell
# 1. 发布
dotnet publish src/OverlayerTool.App `
  -c Release -r win-x64 --self-contained true `
  /p:PublishSingleFile=true `
  /p:IncludeNativeLibrariesForSelfExtract=true

# 2. 用 Inno Setup Compiler 打开并编译
#    installer/windows/OverlayerTool.iss
```

安装包输出到仓库根目录 `dist/`。

### 自定义

- 修改版本号：同步更新 `OverlayerTool.iss` 中的 `AppVersion` 与 csproj 中的 `<Version>`
- 应用图标：添加 `src/OverlayerTool.App/Assets/app.ico`，并取消 `OverlayerTool.iss` 里 `SetupIconFile` 的注释

---

## macOS

### 前置条件

- macOS 系统
- .NET 8 SDK
- Xcode Command Line Tools（提供 `hdiutil`）

### 一键构建

```bash
chmod +x installer/macos/build-installer.sh
./installer/macos/build-installer.sh          # Apple Silicon (osx-arm64)
./installer/macos/build-installer.sh osx-x64  # Intel Mac
```

产物：

- `dist/OverlayerTool.app` — 可直接双击运行
- `dist/OverlayerTool-Setup-0.0.1-osx-arm64.dmg` — 拖拽安装镜像

用户安装：打开 DMG，将 OverlayerTool 拖到「应用程序」文件夹。

### 对外分发（可选）

公开下载时建议对 `.app` 进行代码签名与公证，否则可能触发 Gatekeeper 拦截：

```bash
codesign --deep --force --sign "Developer ID Application: YOUR NAME" dist/OverlayerTool.app
xcrun notarytool submit dist/OverlayerTool-Setup-0.0.1-osx-arm64.dmg --wait
```

---

## 版本号

发布前请统一修改：

- `src/OverlayerTool.App/OverlayerTool.App.csproj` → `<Version>`
- `installer/windows/OverlayerTool.iss` → `#define AppVersion`
- `installer/macos/build-installer.sh` → `VERSION`
- `installer/macos/Info.plist.template` → `CFBundleVersion` / `CFBundleShortVersionString`
