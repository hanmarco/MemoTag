using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

namespace MemoTag;

public sealed class TargetOverlayWindow : Window
{
    public IntPtr Handle => new WindowInteropHelper(this).Handle;

    public TargetOverlayWindow()
    {
        AllowsTransparency = true;
        Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(130, 247, 245, 241));
        ResizeMode = ResizeMode.NoResize;
        ShowActivated = false;
        ShowInTaskbar = false;
        Topmost = true;
        WindowStyle = WindowStyle.None;

        SourceInitialized += (_, _) =>
        {
            var handle = new WindowInteropHelper(this).Handle;
            NativeMethods.MakeToolWindow(handle);
        };
    }

    public void SetBounds(NativeMethods.Rect rect)
    {
        NativeMethods.SetWindowBounds(Handle, rect.Left, rect.Top, rect.Width, rect.Height);
    }
}
