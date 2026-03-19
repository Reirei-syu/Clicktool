using System.Threading;
using System.Windows;

namespace ClickTool;

public partial class App : Application
{
    private static Mutex? _mutex;

    protected override void OnStartup(StartupEventArgs e)
    {
        const string mutexName = "ClickTool_SingleInstance_Mutex";
        bool isNewInstance;

        _mutex = new Mutex(true, mutexName, out isNewInstance);

        if (!isNewInstance)
        {
            MessageBox.Show("ClickTool 已经在运行中！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            Current.Shutdown();
            return;
        }

        base.OnStartup(e);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        if (_mutex != null)
        {
            _mutex.ReleaseMutex();
            _mutex.Dispose();
        }

        base.OnExit(e);
    }

    private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
    {
        System.Diagnostics.Debug.WriteLine($"Unhandled exception: {e.Exception}");
        MessageBox.Show(
            "程序发生未处理异常，当前操作已被中断。请重新打开应用后重试。",
            "错误",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
        e.Handled = true;
    }
}
