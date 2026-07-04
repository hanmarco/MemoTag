using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Interop;
using System.Windows.Media;
using Button = System.Windows.Controls.Button;
using Color = System.Windows.Media.Color;

namespace MemoTag;

public sealed class NoteWindow : Window
{
    private static readonly SolidColorBrush CalloutTextBrush = new(Color.FromRgb(43, 26, 14));
    private static readonly SolidColorBrush HoverBrush = new(Color.FromArgb(34, 43, 26, 14));
    private static readonly SolidColorBrush RecBrush = new(Color.FromRgb(240, 113, 120));

    private readonly TextBlock _noteText;
    private readonly Border _rootBorder;
    private readonly Ellipse _recDot;

    public IntPtr Handle => new WindowInteropHelper(this).Handle;

    public event EventHandler? EditRequested;
    public event EventHandler? RemoveRequested;

    public NoteWindow(string note, Color color)
    {
        AllowsTransparency = true;
        Background = System.Windows.Media.Brushes.Transparent;
        MinWidth = 120;
        MaxWidth = 420;
        SizeToContent = SizeToContent.WidthAndHeight;
        ResizeMode = ResizeMode.NoResize;
        ShowActivated = false;
        ShowInTaskbar = false;
        Topmost = false;
        WindowStyle = WindowStyle.None;

        _rootBorder = new Border
        {
            CornerRadius = new CornerRadius(8, 8, 0, 0),
            Padding = new Thickness(10, 7, 8, 7),
            Background = new SolidColorBrush(color)
        };

        var panel = new StackPanel
        {
            Orientation = System.Windows.Controls.Orientation.Horizontal,
            VerticalAlignment = System.Windows.VerticalAlignment.Center
        };

        var label = new StackPanel
        {
            Orientation = System.Windows.Controls.Orientation.Horizontal,
            VerticalAlignment = System.Windows.VerticalAlignment.Center
        };

        _recDot = new Ellipse
        {
            Width = 7,
            Height = 7,
            Fill = RecBrush,
            Margin = new Thickness(0, 0, 6, 0),
            VerticalAlignment = System.Windows.VerticalAlignment.Center
        };

        _noteText = new TextBlock
        {
            FontSize = 12.5,
            FontWeight = FontWeights.SemiBold,
            Foreground = CalloutTextBrush,
            MaxWidth = 270,
            Text = note,
            TextTrimming = TextTrimming.CharacterEllipsis,
            TextWrapping = TextWrapping.NoWrap,
            VerticalAlignment = System.Windows.VerticalAlignment.Center
        };

        label.Children.Add(_recDot);
        label.Children.Add(_noteText);
        panel.Children.Add(label);

        var buttons = new StackPanel
        {
            Orientation = System.Windows.Controls.Orientation.Horizontal,
            HorizontalAlignment = System.Windows.HorizontalAlignment.Right,
            Margin = new Thickness(8, 0, 0, 0),
            VerticalAlignment = System.Windows.VerticalAlignment.Center
        };

        var editButton = CreateButton("✎");
        editButton.ToolTip = "편집";
        editButton.Click += (_, _) => EditRequested?.Invoke(this, EventArgs.Empty);

        var removeButton = CreateButton("×");
        removeButton.ToolTip = "닫기";
        removeButton.Margin = new Thickness(5, 0, 0, 0);
        removeButton.Click += (_, _) => RemoveRequested?.Invoke(this, EventArgs.Empty);

        buttons.Children.Add(editButton);
        buttons.Children.Add(removeButton);
        panel.Children.Add(buttons);
        _rootBorder.Child = panel;
        Content = _rootBorder;

        SourceInitialized += (_, _) =>
        {
            var handle = new WindowInteropHelper(this).Handle;
            NativeMethods.MakeToolWindow(handle);
        };

        Loaded += (_, _) => StartPulseAnimation();
    }

    public void UpdateContent(string note, Color color)
    {
        _noteText.Text = note;
        _rootBorder.Background = new SolidColorBrush(color);
    }

    public void SetAttachedToTopEdge(bool attached)
    {
        _rootBorder.CornerRadius = attached
            ? new CornerRadius(0, 0, 8, 8)
            : new CornerRadius(8, 8, 0, 0);
    }

    public void SetPosition(double left, double top)
    {
        NativeMethods.SetWindowBounds(
            Handle,
            (int)Math.Round(left),
            (int)Math.Round(top),
            (int)Math.Round(Math.Max(ActualWidth, Width)),
            (int)Math.Round(Math.Max(ActualHeight, Height)));
    }

    private static Button CreateButton(string text)
    {
        var button = new Button
        {
            Content = text,
            Width = 20,
            Height = 20,
            Padding = new Thickness(0),
            BorderThickness = new Thickness(0),
            Background = System.Windows.Media.Brushes.Transparent,
            Foreground = CalloutTextBrush,
            FontSize = 13,
            FontWeight = FontWeights.SemiBold,
            Focusable = false,
            VerticalContentAlignment = System.Windows.VerticalAlignment.Center,
            HorizontalContentAlignment = System.Windows.HorizontalAlignment.Center
        };

        button.MouseEnter += (_, _) => button.Background = HoverBrush;
        button.MouseLeave += (_, _) => button.Background = System.Windows.Media.Brushes.Transparent;
        return button;
    }

    private void StartPulseAnimation()
    {
        var animation = new DoubleAnimation
        {
            From = 1,
            To = 0.25,
            AutoReverse = true,
            Duration = TimeSpan.FromMilliseconds(600),
            RepeatBehavior = RepeatBehavior.Forever
        };

        _recDot.BeginAnimation(OpacityProperty, animation);
    }
}
