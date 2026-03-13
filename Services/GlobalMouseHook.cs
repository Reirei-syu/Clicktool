using System.Diagnostics;
using System.Runtime.InteropServices;

namespace ClickTool.Services;

/// <summary>
/// 全局鼠标 Hook 服务
/// 通过 P/Invoke 调用 user32.dll 的 SetWindowsHookEx 实现全局鼠标事件监听
/// </summary>
public class GlobalMouseHook : IDisposable
{
    #region Win32 API 声明

    private const int WH_MOUSE_LL = 14;

    private const int WM_MOUSEMOVE = 0x0200;
    private const int WM_LBUTTONDOWN = 0x0201;
    private const int WM_LBUTTONUP = 0x0202;
    private const int WM_RBUTTONDOWN = 0x0204;
    private const int WM_RBUTTONUP = 0x0205;
    private const int WM_MBUTTONDOWN = 0x0207;
    private const int WM_MBUTTONUP = 0x0208;

    private delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelMouseProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr GetModuleHandle(string lpModuleName);

    [StructLayout(LayoutKind.Sequential)]
    private struct MSLLHOOKSTRUCT
    {
        public POINT pt;
        public uint mouseData;
        public uint flags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int x;
        public int y;
    }

    #endregion

    #region 事件

    /// <summary>
    /// 鼠标移动事件 (x, y)
    /// </summary>
    public event Action<int, int>? OnMouseMove;

    /// <summary>
    /// 鼠标按下事件 (x, y, button)
    /// </summary>
    public event Action<int, int, Models.MouseButton>? OnMouseDown;

    /// <summary>
    /// 鼠标抬起事件 (x, y, button)
    /// </summary>
    public event Action<int, int, Models.MouseButton>? OnMouseUp;

    #endregion

    private IntPtr _hookId = IntPtr.Zero;
    private readonly LowLevelMouseProc _proc;
    private bool _disposed = false;

    public bool IsHooked => _hookId != IntPtr.Zero;

    public GlobalMouseHook()
    {
        // 必须保持委托引用，防止 GC 回收导致崩溃
        _proc = HookCallback;
    }

    /// <summary>
    /// 安装全局鼠标 Hook
    /// </summary>
    public void Install()
    {
        if (_hookId != IntPtr.Zero) return;

        using var curProcess = Process.GetCurrentProcess();
        using var curModule = curProcess.MainModule!;
        _hookId = SetWindowsHookEx(
            WH_MOUSE_LL,
            _proc,
            GetModuleHandle(curModule.ModuleName!),
            0);

        if (_hookId == IntPtr.Zero)
        {
            throw new InvalidOperationException($"安装鼠标 Hook 失败，错误码: {Marshal.GetLastWin32Error()}");
        }
    }

    /// <summary>
    /// 卸载全局鼠标 Hook
    /// </summary>
    public void Uninstall()
    {
        if (_hookId != IntPtr.Zero)
        {
            UnhookWindowsHookEx(_hookId);
            _hookId = IntPtr.Zero;
        }
    }

    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0)
        {
            var hookStruct = Marshal.PtrToStructure<MSLLHOOKSTRUCT>(lParam);
            int x = hookStruct.pt.x;
            int y = hookStruct.pt.y;
            int msg = (int)wParam;

            switch (msg)
            {
                case WM_MOUSEMOVE:
                    OnMouseMove?.Invoke(x, y);
                    break;
                case WM_LBUTTONDOWN:
                    OnMouseDown?.Invoke(x, y, Models.MouseButton.Left);
                    break;
                case WM_LBUTTONUP:
                    OnMouseUp?.Invoke(x, y, Models.MouseButton.Left);
                    break;
                case WM_RBUTTONDOWN:
                    OnMouseDown?.Invoke(x, y, Models.MouseButton.Right);
                    break;
                case WM_RBUTTONUP:
                    OnMouseUp?.Invoke(x, y, Models.MouseButton.Right);
                    break;
                case WM_MBUTTONDOWN:
                    OnMouseDown?.Invoke(x, y, Models.MouseButton.Middle);
                    break;
                case WM_MBUTTONUP:
                    OnMouseUp?.Invoke(x, y, Models.MouseButton.Middle);
                    break;
            }
        }

        return CallNextHookEx(_hookId, nCode, wParam, lParam);
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            Uninstall();
            _disposed = true;
        }
        GC.SuppressFinalize(this);
    }

    ~GlobalMouseHook()
    {
        Dispose();
    }
}
