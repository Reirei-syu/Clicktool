# Security Review 2026-03-19

## 修改内容
- 限制录制文件的加载、保存、删除范围，只允许落在 `data/recordings` 目录中。
- 对从 JSON 读取的录制数据做安全清洗，避免异常数据直接进入播放链路。
- 对设置值做边界钳制，避免异常配置直接进入 UI 和录制参数。

## 影响范围
- `Services/StorageService.cs`
- `ClickTool.Tests/StorageServiceTests.cs`

## 风险与兼容性
- 兼容性影响：
  - 旧版本如果曾经把方案文件路径指向 `data/recordings` 之外，现版本将不再接受。
- 风险控制：
  - 没有调整播放、录制、步骤定义主流程。
  - 仅在存储边界和加载入口增加约束与清洗。

## 具体安全规则
- 方案文件必须位于 `data/recordings`，否则拒绝加载/删除，并在保存时重新落到受控目录。
- `delay_ms` 会被限制在 `0..2147483647`，避免进入 `Task.Delay((int)delay)` 时产生溢出或异常。
- 空名称、默认时间戳、无效动作集合会在加载时修正为可用值。
- 非点击动作会清空 `button/state`，避免脏数据混入运行态。
- `opacity` 限制为 `0.05..1.0`，`move_sample_interval_ms` 限制为 `0..5000`。
