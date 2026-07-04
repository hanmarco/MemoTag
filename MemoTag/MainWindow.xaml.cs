using System.Drawing.Drawing2D;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;

namespace MemoTag;

public partial class MainWindow : Window
{
    private const int MarkHotkeyId = 0x4D01;
    private const int ClearHotkeyId = 0x4D02;
    private const int WmHotkey = 0x0312;

    private readonly WindowMarkerService _markerService = new();
    private HwndSource? _source;
    private System.Windows.Forms.NotifyIcon? _trayIcon;
    private System.Drawing.Icon? _trayIconImage;

    public MainWindow()
    {
        InitializeComponent();
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);

        var handle = new WindowInteropHelper(this).Handle;
        _source = HwndSource.FromHwnd(handle);
        _source?.AddHook(WndProc);

        NativeMethods.RegisterHotKey(handle, MarkHotkeyId, HotkeyModifiers.Control | HotkeyModifiers.Alt, (uint)KeyInterop.VirtualKeyFromKey(Key.M));
        NativeMethods.RegisterHotKey(handle, ClearHotkeyId, HotkeyModifiers.Control | HotkeyModifiers.Alt | HotkeyModifiers.Shift, (uint)KeyInterop.VirtualKeyFromKey(Key.M));

        SetupTrayIcon();
        Hide();
    }

    protected override void OnClosed(EventArgs e)
    {
        var handle = new WindowInteropHelper(this).Handle;
        NativeMethods.UnregisterHotKey(handle, MarkHotkeyId);
        NativeMethods.UnregisterHotKey(handle, ClearHotkeyId);

        _source?.RemoveHook(WndProc);
        _markerService.Dispose();
        _trayIcon?.Dispose();
        _trayIconImage?.Dispose();

        base.OnClosed(e);
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg != WmHotkey)
        {
            return IntPtr.Zero;
        }

        var hotkeyId = wParam.ToInt32();
        if (hotkeyId == MarkHotkeyId)
        {
            _markerService.MarkOrEditForegroundWindow();
            handled = true;
        }
        else if (hotkeyId == ClearHotkeyId)
        {
            _markerService.ClearForegroundWindowMarker();
            handled = true;
        }

        return IntPtr.Zero;
    }

    private void SetupTrayIcon()
    {
        var menu = new System.Windows.Forms.ContextMenuStrip();
        menu.Items.Add("현재 창에 메모 표시 (Ctrl+Alt+M)", null, (_, _) => _markerService.MarkOrEditForegroundWindow());
        menu.Items.Add("현재 창 표시 해제 (Ctrl+Alt+Shift+M)", null, (_, _) => _markerService.ClearForegroundWindowMarker());
        menu.Items.Add("모든 표시 해제", null, (_, _) => _markerService.ClearAll());
        menu.Items.Add(new System.Windows.Forms.ToolStripSeparator());
        menu.Items.Add("정보", null, (_, _) => ShowAboutDialog());
        menu.Items.Add(new System.Windows.Forms.ToolStripSeparator());
        menu.Items.Add("종료", null, (_, _) => Close());

        _trayIconImage = CreateTrayIcon();
        _trayIcon = new System.Windows.Forms.NotifyIcon
        {
            Icon = _trayIconImage,
            Text = "MemoTag",
            ContextMenuStrip = menu,
            Visible = true
        };

        _trayIcon.DoubleClick += (_, _) => _markerService.MarkOrEditForegroundWindow();
        if (!App.IsStartupLaunch)
        {
            _trayIcon.ShowBalloonTip(
                3000,
                "MemoTag",
                "Ctrl+Alt+M: 현재 앱에 테두리와 메모를 남깁니다.\nCtrl+Alt+Shift+M: 현재 앱 표시를 해제합니다.",
                System.Windows.Forms.ToolTipIcon.Info);
        }
    }

    private void ShowAboutDialog()
    {
        System.Windows.MessageBox.Show(
            "MemoTag\n\nEmail: hsn103@gmail.com\nGitHub: github.com/hanmarco/MemoTag",
            "MemoTag 정보",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    private static System.Drawing.Icon CreateTrayIcon()
    {
        using var bitmap = new System.Drawing.Bitmap(32, 32);
        using var graphics = System.Drawing.Graphics.FromImage(bitmap);
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.Clear(System.Drawing.Color.Transparent);

        using var tagPath = new GraphicsPath();
        tagPath.AddPolygon(
        new System.Drawing.Point[]
        {
            new System.Drawing.Point(7, 6),
            new System.Drawing.Point(22, 6),
            new System.Drawing.Point(29, 13),
            new System.Drawing.Point(15, 27),
            new System.Drawing.Point(4, 16)
        });

        using var shadowBrush = new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb(70, 90, 70, 0));
        using var fillBrush = new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb(255, 255, 205, 52));
        using var borderPen = new System.Drawing.Pen(System.Drawing.Color.FromArgb(255, 160, 118, 0), 2);
        using var holeBrush = new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb(255, 255, 246, 185));

        graphics.TranslateTransform(1, 1);
        graphics.FillPath(shadowBrush, tagPath);
        graphics.ResetTransform();
        graphics.FillPath(fillBrush, tagPath);
        graphics.DrawPath(borderPen, tagPath);
        graphics.FillEllipse(holeBrush, 19, 9, 5, 5);

        var handle = bitmap.GetHicon();
        try
        {
            using var icon = System.Drawing.Icon.FromHandle(handle);
            return (System.Drawing.Icon)icon.Clone();
        }
        finally
        {
            NativeMethods.DestroyIcon(handle);
        }
    }
}
