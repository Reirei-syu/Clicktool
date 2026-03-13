using System.Runtime.InteropServices;

namespace ClickTool.Services;

/// <summary>
/// 鼠标模拟服务
/// 通过 P/Invoke 调用 user32.dll 的 SetCursorPos + mouse_event 实现鼠标操作模拟
/// </summary>
public static class MouseSimulator
{
    #region Win32 API 声明

    [DllImport("user32.dll")]
    private static extern bool SetCursorPos(int X, int Y);

    [DllImport("user32.dll")]
    private static extern void mouse_event(uint dwFlags, int dx, int dy, uint dwData, IntPtr dwExtraInfo);

    private const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
    private const uint MOUSEEVENTF_LEFTUP = 0x0004;
    private const uint MOUSEEVENTF_RIGHTDOWN = 0x0008;
    private const uint MOUSEEVENTF_RIGHTUP = 0x0010;
    private const uint MOUSEEVENTF_MIDDLEDOWN = 0x0020;
    private const uint MOUSEEVENTF_MIDDLEUP = 0x0040;
    private const uint MOUSEEVENTF_ABSOLUTE = 0x8000;

    #endregion

    /// <summary>
    /// 移动鼠标到指定屏幕坐标
    /// </summary>
    public static void MoveTo(int x, int y)
    {
        SetCursorPos(x, y);
    }

    /// <summary>
    /// 模拟鼠标按下
    /// </summary>
    public static void MouseDown(Models.MouseButton button)
    {
        uint flags = button switch
        {
            Models.MouseButton.Left => MOUSEEVENTF_LEFTDOWN,
            Models.MouseButton.Right => MOUSEEVENTF_RIGHTDOWN,
            Models.MouseButton.Middle => MOUSEEVENTF_MIDDLEDOWN,
            _ => MOUSEEVENTF_LEFTDOWN
        };
        mouse_event(flags, 0, 0, 0, IntPtr.Zero);
    }

    /// <summary>
    /// 模拟鼠标抬起
    /// </summary>
    public static void MouseUp(Models.MouseButton button)
    {
        uint flags = button switch
        {
            Models.MouseButton.Left => MOUSEEVENTF_LEFTUP,
            Models.MouseButton.Right => MOUSEEVENTF_RIGHTUP,
            Models.MouseButton.Middle => MOUSEEVENTF_MIDDLEUP,
            _ => MOUSEEVENTF_LEFTUP
        };
        mouse_event(flags, 0, 0, 0, IntPtr.Zero);
    }

    /// <summary>
    /// 模拟完整的鼠标单击 (按下+抬起)
    /// </summary>
    public static void Click(int x, int y, Models.MouseButton button = Models.MouseButton.Left)
    {
        MoveTo(x, y);
        Thread.Sleep(10); // 小延迟确保移动生效
        MouseDown(button);
        Thread.Sleep(10);
        MouseUp(button);
    }

    /// <summary>
    /// 执行单个录制的鼠标动作
    /// </summary>
    public static void ExecuteAction(Models.MouseAction action)
    {
        MoveTo(action.X, action.Y);

        if (action.Action == Models.MouseActionType.Click && action.Button.HasValue && action.State.HasValue)
        {
            if (action.State.Value == Models.MouseButtonState.Down)
            {
                MouseDown(action.Button.Value);
            }
            else
            {
                MouseUp(action.Button.Value);
            }
        }
    }
}
