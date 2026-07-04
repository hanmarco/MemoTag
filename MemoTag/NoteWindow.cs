using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using Button = System.Windows.Controls.Button;
using Color = System.Windows.Media.Color;

namespace MemoTag;

public sealed class NoteWindow : Window
{
    private readonly TextBlock _noteText;
    private readonly Border _rootBorder;

    public IntPtr Handle => new WindowInteropHelper(this).Handle;

    public event EventHandler? EditRequested;
    public event EventHandler? RemoveRequested;

    public NoteWindow(string note, Color color)
    {
        MinWidth = 220;
        MaxWidth = 420;
        SizeToContent = SizeToContent.WidthAndHeight;
        ResizeMode = ResizeMode.NoResize;
        ShowActivated = false;
        ShowInTaskbar = false;
        Topmost = false;
        WindowStyle = WindowStyle.None;

        _rootBorder = new Border
        {
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(12),
            BorderBrush = new SolidColorBrush(Darken(color, 0.62)),
            Background = new SolidColorBrush(Soften(color))
        };

        var panel = new DockPanel { LastChildFill = true };
        var buttons = new StackPanel
        {
            Orientation = System.Windows.Controls.Orientation.Horizontal,
            HorizontalAlignment = System.Windows.HorizontalAlignment.Right,
            Margin = new Thickness(0, 10, 0, 0)
        };

        var editButton = CreateButton("수정");
        editButton.Click += (_, _) => EditRequested?.Invoke(this, EventArgs.Empty);

        var removeButton = CreateButton("해제");
        removeButton.Margin = new Thickness(8, 0, 0, 0);
        removeButton.Click += (_, _) => RemoveRequested?.Invoke(this, EventArgs.Empty);

        buttons.Children.Add(editButton);
        buttons.Children.Add(removeButton);
        DockPanel.SetDock(buttons, Dock.Bottom);

        _noteText = new TextBlock
        {
            TextWrapping = TextWrapping.Wrap,
            FontSize = 14,
            Foreground = System.Windows.Media.Brushes.Black,
            Text = note
        };

        panel.Children.Add(buttons);
        panel.Children.Add(_noteText);
        _rootBorder.Child = panel;
        Content = _rootBorder;

        SourceInitialized += (_, _) =>
        {
            var handle = new WindowInteropHelper(this).Handle;
            NativeMethods.MakeToolWindow(handle);
        };
    }

    public void UpdateContent(string note, Color color)
    {
        _noteText.Text = note;
        _rootBorder.Background = new SolidColorBrush(Soften(color));
        _rootBorder.BorderBrush = new SolidColorBrush(Darken(color, 0.62));
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
        return new Button
        {
            Content = text,
            MinWidth = 48,
            Padding = new Thickness(10, 4, 10, 4)
        };
    }

    private static Color Soften(Color color)
    {
        return Color.FromRgb(
            (byte)((color.R * 0.18) + 209),
            (byte)((color.G * 0.18) + 209),
            (byte)((color.B * 0.18) + 209));
    }

    private static Color Darken(Color color, double amount)
    {
        return Color.FromRgb(
            (byte)(color.R * amount),
            (byte)(color.G * amount),
            (byte)(color.B * amount));
    }
}
