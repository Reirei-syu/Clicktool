using System.Windows;
using System.Threading;

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
        MessageBox.Show($"发生了未处理的异常：\n{e.Exception.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        e.Handled = true; // 尽量阻止程序崩溃
    }
}
