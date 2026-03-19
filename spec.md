# Technical Specification - ClickTool

## 1. 技术栈
- 平台：Windows
- 框架：.NET 8 + WPF
- 类型：`WinExe`
- 发布方式：`PublishSingleFile=true` + `SelfContained=true` + `RuntimeIdentifier=win-x64`
- 安装包：WiX Toolset MSI

## 2. 主要模块
### 2.1 UI
- `MainWindow.xaml`
  - 定义圆形悬浮态和矩形展开态。
  - 主面板只保留执行控制、方案摘要、步骤列表入口、方案列表入口和热键区。
- `MainWindow.xaml.cs`
  - 负责窗口初始化、方案加载、状态恢复和独立窗口生命周期。
- `MainWindow.Actions.cs`
  - 负责步骤摘要刷新、步骤编辑结果回写和方案摘要生成。
- `MainWindow.Hotkeys.cs`
  - 负责全局热键注册、双键组合解析、重复校验、保存与 `Esc` 清空。
- `MainWindow.Interactions.cs`
  - 负责录制、播放全部、循环播放、按步播放、循环次数解析和状态联动。
- `MainWindow.WindowChrome.cs`
  - 负责展开态拖动、边缘缩放和窗口置底/置顶切换。
- `RecordEditorWindow.xaml` / `RecordEditorWindow.xaml.cs`
  - 独立步骤列表窗口，负责步骤查看、逐条编辑和批量编辑。
- `SchemeManagerWindow.xaml` / `SchemeManagerWindow.xaml.cs`
  - 独立方案列表窗口，负责方案切换和方案管理。

### 2.2 模型
- `Models/MouseAction.cs`
  - 表示单条录制动作。
  - 提供动作摘要、等待时间摘要和按钮/状态文本。
- `Models/RecordingSession.cs`
  - 表示一次录制会话。
- `Models/RecordedActionListItem.cs`
  - 步骤窗口中的单条记录显示模型。
  - 暴露是否使用黄色高亮的属性。
- `Models/RecordedActionGroup.cs`
  - 步骤窗口中的步骤分组显示模型。
- `Models/RecordingSchemeSummary.cs`
  - 方案列表显示模型。
- `Models/AppSettings.cs`
  - 持久化窗口位置、不透明度、录制采样配置和热键设置。
  - 每个热键包含 `VirtualKey` 和 `Modifiers` 两部分。

### 2.3 服务
- `RecordingService`
  - 安装全局鼠标 Hook 并记录动作。
  - 新录制动作默认 `IsStepEnd=false`。
- `PlaybackService`
  - 负责播放全部、循环播放和按步播放。
  - 支持有限次数循环和无限循环。
- `RecordingEditService`
  - 负责单条记录编辑、批量修改等待时间、单步复制和批量复制步骤。
- `StepDefinitionService`
  - 负责步骤统计、默认步骤归集、合并、拆分、步尾切换和分组展示数据生成。
- `HotkeyService`
  - 负责把按键输入解析为单键或“一个修饰键 + 一个主键”的热键绑定，并生成显示文本。
- `StorageService`
  - 负责设置和录制方案的持久化。
- `GlobalMouseHook`
  - 基于 `SetWindowsHookEx` 的全局鼠标 Hook。
- `MouseSimulator`
  - 基于 Win32 API 的鼠标移动与点击模拟。

## 3. 步骤系统规则
- `MouseAction.IsStepEnd=true` 表示显式步尾。
- 如果当前会话存在显式步尾，`StepDefinitionService` 会把从上一个步尾之后到当前步尾之间的连续记录视为同一步。
- 如果当前会话没有任何显式步尾，系统会把全部记录归入步骤 1。
- 该默认步骤中的所有记录都保持 `IsStepEnd=false`，界面统一显示为“步骤中”。
- 这样可以同时满足：
  - 新录制默认全部归集在步骤 1
  - 用户仍可手动定义步尾并继续拆分/合并
  - 按步播放会把默认步骤视为一个完整步骤执行

## 4. 批量编辑规则
- 步骤窗口使用勾选记录作为批量操作输入。
- “复制选中步骤”会按当前勾选项所属的步骤去重后复制整个步骤。
- “应用到选中记录”会把输入的等待时间批量写入选中的记录。
- 删除、合并、拆分也统一基于当前勾选记录执行。

## 5. 热键规则
- 支持五类热键：窗口查看、录制、播放、按步播放、停止。
- 支持两种输入形式：
  - 单键
  - 一个修饰键 + 一个主键
- 不支持多个修饰键叠加。
- `Esc` 表示清空当前热键。
- 注册时如果与系统或其他程序冲突，设置会回滚到上一个有效值。

## 6. 持久化约束
- 设置文件：`data/settings.json`
- 方案目录：`data/recordings/*.json`
- 编辑已加载方案时，必须覆盖保存回源文件。
- `settings.json` 额外持久化各热键的修饰键字段。
- 方案列表直接从 `data/recordings/*.json` 扫描生成。

## 7. 交互约束
- 主功能面板不承载步骤编辑列表。
- 主功能面板不承载方案切换列表。
- 步骤编辑统一通过独立窗口完成。
- 方案切换和方案管理统一通过独立窗口完成。
- 非鼠标移动记录在步骤窗口中必须使用黄色文字高亮。
- 主面板循环播放输入框必须接受 `1-*`，其中 `*` 为无限循环。

## 8. 图标与资源
- 应用图标：`Assets/icon.ico`
- 图标预览：`Assets/icon-preview.png`
- `ClickTool.csproj` 通过 `ApplicationIcon` 引用 `Assets/icon.ico`
- MSI 产物路径：`Installer/bin/x64/Release/ClickTool.Installer.msi`
