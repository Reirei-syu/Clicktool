# Technical Specification - ClickTool

## 1. 技术栈
- 平台：Windows
- 框架：.NET 8 + WPF
- 类型：`WinExe`
- 发布方式：`PublishSingleFile=true` + `SelfContained=true` + `RuntimeIdentifier=win-x64`

## 2. 主要模块
### 2.1 UI
- `MainWindow.xaml`
  - 定义圆形悬浮态和矩形展开态。
  - 主面板只保留执行控制、方案摘要、步骤列表入口、方案列表入口和统一热键区。
- `MainWindow.xaml.cs`
  - 负责窗口初始化、方案加载、状态恢复、独立窗口生命周期。
- `MainWindow.Actions.cs`
  - 负责步骤摘要刷新、步骤编辑结果回写和方案摘要生成。
- `MainWindow.Hotkeys.cs`
  - 负责全局热键注册、冲突校验、热键保存与 `Esc` 清空热键。
- `MainWindow.Interactions.cs`
  - 负责录制/播放按钮、圆形按钮交互、状态联动。
- `MainWindow.WindowChrome.cs`
  - 负责展开态空白区拖动、边缘缩放和窗口置底/置顶切换。
- `RecordEditorWindow.xaml` / `RecordEditorWindow.xaml.cs`
  - 独立步骤列表窗口。
- `SchemeManagerWindow.xaml` / `SchemeManagerWindow.xaml.cs`
  - 独立方案列表窗口。

### 2.2 模型
- `Models/MouseAction.cs`
  - 单条录制动作。
- `Models/RecordingSession.cs`
  - 一次录制会话。
- `Models/RecordedActionListItem.cs`
  - 单条步骤动作展示模型。
- `Models/RecordedActionGroup.cs`
  - 步骤分组展示模型。
- `Models/RecordingSchemeSummary.cs`
  - 方案列表展示模型。
- `Models/AppSettings.cs`
  - 持久化热键、窗口位置、透明度和录制采样配置。

### 2.3 服务
- `RecordingService`
  - 负责安装全局鼠标 Hook 并记录动作。
- `PlaybackService`
  - 负责整段回放、循环回放与按步回放。
- `RecordingEditService`
  - 负责记录编辑，包括修改坐标/等待时间与复制步骤。
- `StepDefinitionService`
  - 负责步骤规范化、统计、合并、拆分、步尾切换和分组展示数据生成。
- `StorageService`
  - 负责设置和录制文件持久化。
- `GlobalMouseHook`
  - 基于 `SetWindowsHookEx` 的全局鼠标 Hook。
- `MouseSimulator`
  - 基于 Win32 API 的鼠标移动和点击模拟。

## 3. 持久化约束
- 设置文件：`data/settings.json`
- 方案目录：`data/recordings/*.json`
- 编辑已加载录制时，必须覆盖回源文件。
- `settings.json` 包含 `hotkey_editor_window` 字段。
- 方案列表直接从 `data/recordings/*.json` 扫描生成。

## 4. 交互约束
- 主功能面板不承载步骤编辑列表。
- 主功能面板不承载方案切换列表。
- 步骤编辑统一通过独立窗口完成。
- 方案切换与方案管理统一通过独立窗口完成。
- 循环播放会持续执行当前方案，直到用户主动停止。
- 展开态窗口允许通过透明 `Thumb` 边缘区域调整大小。
- 展开态如果鼠标左键按在无功能控件的空白区域，会触发 `DragMove()`。
- “置底”按钮使用 Win32 `SetWindowPos(HWND_BOTTOM, ...)` 实现，再次点击恢复 `Topmost=true`。
- 透明度滑条使用 5%-100% 的连续值，不再使用阶梯式吸附。
- 步骤列表窗口顶部负责说明 X/Y/等待时间输入框的含义。

## 5. 图标与资源
- 应用图标：`Assets/icon.ico`
- 图标预览：`Assets/icon-preview.png`
- `ClickTool.csproj` 通过 `ApplicationIcon` 引用 `Assets/icon.ico`
