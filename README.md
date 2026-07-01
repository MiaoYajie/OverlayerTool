# OverlayerTool

跨平台（Windows / macOS）桌面工具：在底板图片上定义文字绘制区域，导入表格数据后按行批量生成最终图片。

## 功能

- 导入底板图片（PNG / JPEG / WebP）
- 在底板上框选多个命名区域，分别设置字体、字号、颜色、旋转角度与对齐方式
- 上传自定义字体文件（.ttf / .otf / .ttc 等），在区域中选用
- 可选导入示例图，半透明叠加辅助定位
- 导入表格数据：粘贴 TSV、CSV 文件、Excel (.xlsx)
- 表头与区域名称匹配后，每行数据生成一张 PNG
- 表格每行支持「预览」，在主区域查看生成效果，并与底板/示例图对比
- 保存 / 加载 `.overlayer` 项目文件夹

## 技术栈

- .NET 8
- Avalonia UI 12
- SkiaSharp（图片渲染）
- ClosedXML（Excel 解析）

## 开发

```bash
# 还原依赖并编译
dotnet restore
dotnet build

# 运行
dotnet run --project src/OverlayerTool.App
```

## 发布

```bash
# Windows x64
dotnet publish src/OverlayerTool.App -c Release -r win-x64 --self-contained false

# macOS x64
dotnet publish src/OverlayerTool.App -c Release -r osx-x64 --self-contained false

# macOS Apple Silicon
dotnet publish src/OverlayerTool.App -c Release -r osx-arm64 --self-contained false
```

发布产物位于 `src/OverlayerTool.App/bin/Release/net8.0/<runtime>/publish/`。

## 项目文件格式

```
my-project.overlayer/
├── project.json      # 区域配置与元数据
├── base.png          # 底板图片
├── reference.png     # 可选示例图
└── fonts/            # 自定义字体文件
    └── {id}.ttf
```

## 使用流程

1. **新建项目** → **导入底板**
2. 在画布上拖拽框选区域，在右侧设置名称（需与表格表头一致）、字体样式
3. 可选：在左侧「字体管理」上传 `.ttf` / `.otf` 字体，并在区域属性中选择
4. 可选：**导入示例图** 辅助对齐
5. **粘贴导入** / **CSV** / **Excel** 导入表格，确认匹配状态
6. 点击表格行末 **预览**，在主区域查看单张效果，可切换对比模式
7. **选择输出目录** → **开始生成**

## 快捷键

| 快捷键 | 功能 |
|--------|------|
| Ctrl+S | 保存项目（每次可选择保存目录） |
| Delete | 删除选中区域 |

区域编辑画布支持 **滚轮缩放**、**缩小/放大/重置视图** 按钮，以及 **右键拖拽平移**（放大后便于精准框选）。

## 注意事项

- Excel 首版仅读取第一个工作表的矩形数据，不支持合并单元格
- 表头与区域名称匹配规则：忽略大小写、去除首尾空格
- 生成前会校验匹配状态，存在未匹配项时需修正后再生成
