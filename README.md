# ClickTool

ClickTool 是一个基于 .NET 8 WPF 的 Windows 鼠标录制与回放工具。

## 下载

### 最新版本直链

下面这个链接始终指向当前最新 Release 中的单文件可执行程序：

[下载最新版本 ClickTool.exe](https://github.com/Reirei-syu/Clicktool/releases/latest/download/ClickTool-win-x64.exe)

说明：

- 这是 GitHub 支持的固定格式 `releases/latest/download/<asset-name>`
- 只要每个新版本 Release 继续上传同名资产 `ClickTool-win-x64.exe`，该链接就会自动指向最新版本

### 当前已发布版本

- Release 页面：
  [v1.0.1 Release](https://github.com/Reirei-syu/Clicktool/releases/tag/v1.0.1)
- 当前版本 exe：
  [ClickTool-win-x64.exe](https://github.com/Reirei-syu/Clicktool/releases/download/v1.0.1/ClickTool-win-x64.exe)
- 当前版本校验文件：
  [ClickTool-win-x64.exe.sha256](https://github.com/Reirei-syu/Clicktool/releases/download/v1.0.1/ClickTool-win-x64.exe.sha256)

### 历史版本规则

如果以后要定位某个固定版本，可以使用下面这种格式：

```text
https://github.com/Reirei-syu/Clicktool/releases/download/版本号/ClickTool-win-x64.exe
```

例如：

```text
https://github.com/Reirei-syu/Clicktool/releases/download/v1.0.1/ClickTool-win-x64.exe
```

## 发布说明

仓库已配置 GitHub Actions 自动发布流程：

- 推送 `v*` 标签会自动运行测试
- 自动构建单文件自包含 `exe`
- 自动创建 GitHub Release
- 自动上传 `ClickTool-win-x64.exe` 和 `.sha256` 文件

## 本地构建

### 构建单文件 exe

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\build.ps1
```

### 运行测试

```powershell
dotnet test .\ClickTool.Tests\ClickTool.Tests.csproj -c Release --nologo
```
