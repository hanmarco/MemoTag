using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using Color = System.Windows.Media.Color;

namespace MemoTag;

public sealed class BorderSegmentWindow : Window
{
    public IntPtr Handle => new WindowInteropHelper(this).Handle;

    public BorderSegmentWindow(Color color)
    {
        AllowsTransparency = false;
        Background = new SolidColorBrush(color);
        ResizeMode = ResizeMode.NoResize;
        ShowActivated = false;
        ShowInTaskbar = false;
        Topmost = false;
        WindowStyle = WindowStyle.None;

        SourceInitialized += (_, _) =>
        {
            var handle = new WindowInteropHelper(this).Handle;
            NativeMethods.MakeClickThroughToolWindow(handle);
        };
    }

    public void SetColor(Color color)
    {
        Background = new SolidColorBrush(color);
    }

    public void SetBounds(double left, double top, double width, double height)
    {
        NativeMethods.SetWindowBounds(
            Handle,
            (int)Math.Round(left),
            (int)Math.Round(top),
            (int)Math.Round(width),
            (int)Math.Round(height));
    }
}
