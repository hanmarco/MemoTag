using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Button = System.Windows.Controls.Button;
using Color = System.Windows.Media.Color;
using ComboBox = System.Windows.Controls.ComboBox;
using TextBox = System.Windows.Controls.TextBox;

namespace MemoTag;

public sealed class MemoEditDialog : Window
{
    private static readonly ColorOption[] Colors =
    [
        new("노랑", Color.FromRgb(255, 193, 7)),
        new("빨강", Color.FromRgb(244, 67, 54)),
        new("초록", Color.FromRgb(76, 175, 80)),
        new("파랑", Color.FromRgb(33, 150, 243)),
        new("보라", Color.FromRgb(156, 39, 176))
    ];

    private readonly TextBox _noteBox;
    private readonly ComboBox _colorBox;
    private readonly bool _allowRemove;

    public MarkerEditResult Result { get; private set; } = new(MarkerDialogAction.Cancel, string.Empty, Colors[0].Color);

    public MemoEditDialog(string windowTitle, string currentNote, Color currentColor, bool allowRemove)
    {
        _allowRemove = allowRemove;

        Title = allowRemove ? "메모 수정" : "창에 메모 남기기";
        Width = 440;
        MinHeight = 300;
        ResizeMode = ResizeMode.NoResize;
        ShowInTaskbar = false;
        Topmost = true;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;

        var root = new Grid { Margin = new Thickness(16) };
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var targetLabel = new TextBlock
        {
            Text = $"대상: {windowTitle}",
            TextWrapping = TextWrapping.Wrap,
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, 0, 0, 12)
        };
        Grid.SetRow(targetLabel, 0);

        _colorBox = new ComboBox
        {
            ItemsSource = Colors,
            SelectedItem = Colors.FirstOrDefault(option => option.Color == currentColor) ?? Colors[0],
            Margin = new Thickness(0, 0, 0, 12)
        };
        Grid.SetRow(_colorBox, 1);

        _noteBox = new TextBox
        {
            AcceptsReturn = true,
            MinHeight = 130,
            Text = currentNote,
            TextWrapping = TextWrapping.Wrap,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto
        };
        Grid.SetRow(_noteBox, 2);

        var buttons = new StackPanel
        {
            Orientation = System.Windows.Controls.Orientation.Horizontal,
            HorizontalAlignment = System.Windows.HorizontalAlignment.Right,
            Margin = new Thickness(0, 14, 0, 0)
        };

        if (allowRemove)
        {
            var removeButton = CreateButton("표시 해제");
            removeButton.Click += (_, _) => CloseWith(MarkerDialogAction.Remove);
            buttons.Children.Add(removeButton);
        }

        var cancelButton = CreateButton("취소");
        cancelButton.Margin = new Thickness(8, 0, 0, 0);
        cancelButton.Click += (_, _) => CloseWith(MarkerDialogAction.Cancel);

        var saveButton = CreateButton("저장");
        saveButton.Margin = new Thickness(8, 0, 0, 0);
        saveButton.IsDefault = true;
        saveButton.Click += (_, _) => CloseWith(MarkerDialogAction.Save);

        buttons.Children.Add(cancelButton);
        buttons.Children.Add(saveButton);
        Grid.SetRow(buttons, 3);

        root.Children.Add(targetLabel);
        root.Children.Add(_colorBox);
        root.Children.Add(_noteBox);
        root.Children.Add(buttons);
        Content = root;

        Loaded += (_, _) =>
        {
            _noteBox.Focus();
            _noteBox.SelectAll();
        };

        KeyDown += (_, args) =>
        {
            if (args.Key == Key.Escape)
            {
                CloseWith(MarkerDialogAction.Cancel);
            }
            else if (args.Key == Key.Enter && Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
            {
                CloseWith(MarkerDialogAction.Save);
            }
        };
    }

    private void CloseWith(MarkerDialogAction action)
    {
        var selected = (ColorOption?)_colorBox.SelectedItem ?? Colors[0];
        Result = new MarkerEditResult(action, _noteBox.Text.Trim(), selected.Color);
        DialogResult = action != MarkerDialogAction.Cancel;
        Close();
    }

    private static Button CreateButton(string text)
    {
        return new Button
        {
            Content = text,
            MinWidth = 82,
            Padding = new Thickness(12, 6, 12, 6)
        };
    }
}
