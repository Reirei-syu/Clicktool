using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;

namespace ClickTool;

public partial class MainWindow
{
    private const double MinExpandedWidth = 428;
    private const double MinExpandedHeight = 620;

    private static readonly IntPtr HwndBottom = new(1);

    private const uint SwpNoSize = 0x0001;
    private const uint SwpNoMove = 0x0002;
    private const uint SwpNoActivate = 0x0010;
    private const uint SwpShowWindow = 0x0040;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetWindowPos(
        IntPtr hWnd,
        IntPtr hWndInsertAfter,
        int x,
        int y,
        int cx,
        int cy,
        uint flags);

    private void BtnToggleBottomLayer_Click(object sender, RoutedEventArgs e)
    {
        if (_isBottomLayer)
        {
            _isBottomLayer = false;
            Topmost = true;
            BtnToggleBottomLayer.Content = "底";
            SetStatus("窗口已恢复置顶。");
            return;
        }

        Topmost = false;
        var handle = new WindowInteropHelper(this).Handle;
        SetWindowPos(handle, HwndBottom, 0, 0, 0, 0, SwpNoMove | SwpNoSize | SwpNoActivate | SwpShowWindow);
        _isBottomLayer = true;
        BtnToggleBottomLayer.Content = "顶";
        SetStatus("窗口已置于底层。");
    }

    private void ExpandedView_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed)
        {
            return;
        }

        if (IsInteractiveElement(e.OriginalSource as DependencyObject))
        {
            return;
        }

        DragMove();
        e.Handled = true;
    }

    private static bool IsInteractiveElement(DependencyObject? source)
    {
        while (source != null)
        {
            if (source is Button
                or TextBox
                or ComboBox
                or Slider
                or Thumb
                or ScrollBar
                or CheckBox
                or ToggleButton
                or Expander)
            {
                return true;
            }

            source = VisualTreeHelper.GetParent(source);
        }

        return false;
    }

    private void ResizeLeft_DragDelta(object sender, DragDeltaEventArgs e) => ResizeLeft(e.HorizontalChange);

    private void ResizeRight_DragDelta(object sender, DragDeltaEventArgs e) => ResizeRight(e.HorizontalChange);

    private void ResizeTop_DragDelta(object sender, DragDeltaEventArgs e) => ResizeTop(e.VerticalChange);

    private void ResizeBottom_DragDelta(object sender, DragDeltaEventArgs e) => ResizeBottom(e.VerticalChange);

    private void ResizeTopLeft_DragDelta(object sender, DragDeltaEventArgs e)
    {
        ResizeLeft(e.HorizontalChange);
        ResizeTop(e.VerticalChange);
    }

    private void ResizeTopRight_DragDelta(object sender, DragDeltaEventArgs e)
    {
        ResizeRight(e.HorizontalChange);
        ResizeTop(e.VerticalChange);
    }

    private void ResizeBottomLeft_DragDelta(object sender, DragDeltaEventArgs e)
    {
        ResizeLeft(e.HorizontalChange);
        ResizeBottom(e.VerticalChange);
    }

    private void ResizeBottomRight_DragDelta(object sender, DragDeltaEventArgs e)
    {
        ResizeRight(e.HorizontalChange);
        ResizeBottom(e.VerticalChange);
    }

    private void ResizeLeft(double horizontalChange)
    {
        if (ExpandedView.Visibility != Visibility.Visible)
        {
            return;
        }

        var targetWidth = Math.Max(MinExpandedWidth, Width - horizontalChange);
        var delta = Width - targetWidth;
        Width = targetWidth;
        Left += delta;
    }

    private void ResizeRight(double horizontalChange)
    {
        if (ExpandedView.Visibility != Visibility.Visible)
        {
            return;
        }

        Width = Math.Max(MinExpandedWidth, Width + horizontalChange);
    }

    private void ResizeTop(double verticalChange)
    {
        if (ExpandedView.Visibility != Visibility.Visible)
        {
            return;
        }

        var targetHeight = Math.Max(MinExpandedHeight, Height - verticalChange);
        var delta = Height - targetHeight;
        Height = targetHeight;
        Top += delta;
    }

    private void ResizeBottom(double verticalChange)
    {
        if (ExpandedView.Visibility != Visibility.Visible)
        {
            return;
        }

        Height = Math.Max(MinExpandedHeight, Height + verticalChange);
    }
}
