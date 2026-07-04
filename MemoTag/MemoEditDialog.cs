using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Markup;
using System.Windows.Media;
using Button = System.Windows.Controls.Button;
using Color = System.Windows.Media.Color;
using ComboBox = System.Windows.Controls.ComboBox;
using TextBox = System.Windows.Controls.TextBox;
using WpfBrushes = System.Windows.Media.Brushes;

namespace MemoTag;

public sealed class MemoEditDialog : Window
{
    private static readonly MessagePreset[] Presets =
    [
        new("테스트중", "테스트중...", Color.FromRgb(255, 193, 7)),
        new("연락처", "연락 주세요", Color.FromRgb(33, 150, 243)),
        new("자리비움", "자리비움", Color.FromRgb(156, 39, 176)),
        new("회의중", "회의중", Color.FromRgb(244, 67, 54)),
        new("닫지 마세요", "닫지 마세요.", Color.FromRgb(255, 193, 7)),
        new("직접 입력", string.Empty, Color.FromRgb(255, 193, 7))
    ];

    private readonly TextBox _messageBox;
    private readonly ComboBox _presetBox;
    private readonly Color _initialColor;
    private readonly IntPtr _targetHandle;

    public MarkerEditResult Result { get; private set; } = new(MarkerDialogAction.Cancel, string.Empty, Presets[0].Color);

    public MemoEditDialog(IntPtr targetHandle, string windowTitle, string currentNote, Color currentColor, bool allowRemove)
    {
        _targetHandle = targetHandle;
        _initialColor = currentColor;

        Title = allowRemove ? "MemoTag 메시지 수정" : "MemoTag 메시지 설정";
        Width = 360;
        SizeToContent = SizeToContent.Height;
        ResizeMode = ResizeMode.NoResize;
        ShowInTaskbar = false;
        Topmost = true;
        WindowStartupLocation = WindowStartupLocation.Manual;
        WindowStyle = WindowStyle.None;
        Background = WpfBrushes.Transparent;
        AllowsTransparency = true;

        var rootBorder = new Border
        {
            Background = Brush("#F7F5F1"),
            BorderBrush = Brush("#DCD8D1"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(9),
            SnapsToDevicePixels = true
        };
        rootBorder.Resources.MergedDictionaries.Add(CreateControlStyles());
        rootBorder.SizeChanged += (_, _) =>
        {
            rootBorder.Clip = new RectangleGeometry(
                new Rect(0, 0, rootBorder.ActualWidth, rootBorder.ActualHeight),
                9,
                9);
        };

        var root = new Grid();
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(32) });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        root.Children.Add(CreateTitleBar());

        var body = new Grid
        {
            Margin = new Thickness(18, 16, 18, 18)
        };
        body.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        body.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        body.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        body.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        Grid.SetRow(body, 1);

        body.Children.Add(CreateTargetBlock(windowTitle));

        _presetBox = CreatePresetBox(currentNote, currentColor);
        _presetBox.SelectionChanged += (_, _) => ApplySelectedPreset();
        var presetField = CreateField("사전 정의된 메시지", _presetBox);
        presetField.Margin = new Thickness(0, 12, 0, 0);
        Grid.SetRow(presetField, 1);
        body.Children.Add(presetField);

        _messageBox = CreateMessageBox(currentNote);
        var messageField = CreateField("메시지", _messageBox);
        messageField.Margin = new Thickness(0, 12, 0, 0);
        Grid.SetRow(messageField, 2);
        body.Children.Add(messageField);

        var actions = CreateActions(allowRemove);
        Grid.SetRow(actions, 3);
        body.Children.Add(actions);

        root.Children.Add(body);
        rootBorder.Child = root;
        Content = rootBorder;

        Loaded += (_, _) =>
        {
            CenterOverTarget();
            _messageBox.Focus();
            _messageBox.SelectAll();
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

    private UIElement CreateTitleBar()
    {
        var titleBarShell = new Border
        {
            Background = Brush("#F0EEEA"),
            CornerRadius = new CornerRadius(8, 8, 0, 0)
        };
        var titleBar = new Grid
        {
            ClipToBounds = true
        };
        titleBarShell.Child = titleBar;
        titleBar.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        titleBar.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        titleBar.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        titleBar.MouseLeftButtonDown += (_, args) =>
        {
            if (args.ButtonState == MouseButtonState.Pressed)
            {
                DragMove();
            }
        };

        var icon = new Border
        {
            Width = 16,
            Height = 16,
            Margin = new Thickness(10, 0, 7, 0),
            CornerRadius = new CornerRadius(4),
            Background = new LinearGradientBrush(Color.FromRgb(242, 166, 90), Color.FromRgb(232, 97, 63), 135)
        };
        Grid.SetColumn(icon, 0);

        var name = new TextBlock
        {
            Text = "MemoTag",
            FontSize = 12,
            FontWeight = FontWeights.SemiBold,
            Foreground = Brush("#4A4550"),
            VerticalAlignment = VerticalAlignment.Center
        };
        Grid.SetColumn(name, 1);

        var controls = new StackPanel
        {
            Orientation = System.Windows.Controls.Orientation.Horizontal,
            HorizontalAlignment = System.Windows.HorizontalAlignment.Right
        };
        controls.Children.Add(CreateWindowButton("─", (_, _) => WindowState = WindowState.Minimized));
        controls.Children.Add(CreateWindowButton("□", (_, _) => { }));
        controls.Children.Add(CreateWindowButton("✕", (_, _) => CloseWith(MarkerDialogAction.Cancel), isClose: true));
        Grid.SetColumn(controls, 2);

        titleBar.Children.Add(icon);
        titleBar.Children.Add(name);
        titleBar.Children.Add(controls);
        return titleBarShell;
    }

    private void CenterOverTarget()
    {
        UpdateLayout();

        if (!NativeMethods.TryGetFrameBounds(_targetHandle, out var rect))
        {
            CenterOverScreen();
            return;
        }

        var width = ActualWidth > 0 ? ActualWidth : Width;
        var height = ActualHeight > 0 ? ActualHeight : Height;
        var screen = System.Windows.Forms.Screen.FromHandle(_targetHandle).WorkingArea;
        var targetTopLeft = DeviceToDip(rect.Left, rect.Top);
        var targetSize = DeviceToDip(rect.Width, rect.Height);
        var screenTopLeft = DeviceToDip(screen.Left, screen.Top);
        var screenBottomRight = DeviceToDip(screen.Right, screen.Bottom);

        var left = targetTopLeft.X + ((targetSize.X - width) / 2);
        var top = targetTopLeft.Y + ((targetSize.Y - height) / 2);

        Left = Clamp(left, screenTopLeft.X, screenBottomRight.X - width);
        Top = Clamp(top, screenTopLeft.Y, screenBottomRight.Y - height);
    }

    private void CenterOverScreen()
    {
        var width = ActualWidth > 0 ? ActualWidth : Width;
        var height = ActualHeight > 0 ? ActualHeight : Height;
        var screen = System.Windows.Forms.Screen.PrimaryScreen?.WorkingArea ?? System.Windows.Forms.Screen.FromPoint(System.Windows.Forms.Cursor.Position).WorkingArea;
        var screenTopLeft = DeviceToDip(screen.Left, screen.Top);
        var screenSize = DeviceToDip(screen.Width, screen.Height);

        Left = screenTopLeft.X + ((screenSize.X - width) / 2);
        Top = screenTopLeft.Y + ((screenSize.Y - height) / 2);
    }

    private System.Windows.Point DeviceToDip(double x, double y)
    {
        var source = PresentationSource.FromVisual(this) as HwndSource;
        var transform = source?.CompositionTarget?.TransformFromDevice ?? Matrix.Identity;
        return transform.Transform(new System.Windows.Point(x, y));
    }

    private static UIElement CreateTargetBlock(string windowTitle)
    {
        var stack = new StackPanel();
        stack.Children.Add(new TextBlock
        {
            Text = "대상",
            FontSize = 11,
            Foreground = Brush("#8A8590"),
            Margin = new Thickness(0, 0, 0, 2)
        });
        stack.Children.Add(new TextBlock
        {
            Text = windowTitle,
            FontSize = 13.5,
            FontWeight = FontWeights.SemiBold,
            Foreground = Brush("#2A2730"),
            TextTrimming = TextTrimming.CharacterEllipsis,
            TextWrapping = TextWrapping.NoWrap
        });
        return stack;
    }

    private static StackPanel CreateField(string label, System.Windows.Controls.Control control)
    {
        var stack = new StackPanel();
        stack.Children.Add(new TextBlock
        {
            Text = label,
            FontSize = 10.5,
            Foreground = Brush("#8A8590"),
            Margin = new Thickness(0, 0, 0, 5)
        });
        stack.Children.Add(control);
        return stack;
    }

    private ComboBox CreatePresetBox(string currentNote, Color currentColor)
    {
        var comboBox = new ComboBox
        {
            ItemsSource = Presets,
            SelectedItem = FindInitialPreset(currentNote, currentColor),
            Height = 34,
            Padding = new Thickness(8, 3, 8, 3),
            FontSize = 12.5,
            Foreground = Brush("#2A2730"),
            Background = WpfBrushes.White,
            BorderBrush = Brush("#DCD8D1")
        };

        return comboBox;
    }

    private TextBox CreateMessageBox(string currentNote)
    {
        return new TextBox
        {
            Height = 34,
            Text = currentNote,
            Padding = new Thickness(8, 6, 8, 6),
            FontSize = 12.5,
            Foreground = Brush("#2A2730"),
            Background = WpfBrushes.White,
            BorderBrush = Brush("#DCD8D1"),
            SelectionBrush = Brush("#F2A65A"),
            VerticalContentAlignment = VerticalAlignment.Center
        };
    }

    private StackPanel CreateActions(bool allowRemove)
    {
        var actions = new StackPanel
        {
            Orientation = System.Windows.Controls.Orientation.Horizontal,
            HorizontalAlignment = System.Windows.HorizontalAlignment.Right,
            Margin = new Thickness(0, 16, 0, 0)
        };

        if (allowRemove)
        {
            actions.Children.Add(CreateActionButton("표시 해제", (_, _) => CloseWith(MarkerDialogAction.Remove), secondary: true));
        }

        var cancelButton = CreateActionButton("취소", (_, _) => CloseWith(MarkerDialogAction.Cancel), secondary: true);
        cancelButton.Margin = new Thickness(8, 0, 0, 0);
        actions.Children.Add(cancelButton);

        var saveButton = CreateActionButton("설정", (_, _) => CloseWith(MarkerDialogAction.Save), secondary: false);
        saveButton.Margin = new Thickness(8, 0, 0, 0);
        saveButton.IsDefault = true;
        actions.Children.Add(saveButton);

        return actions;
    }

    private void ApplySelectedPreset()
    {
        if (_presetBox.SelectedItem is not MessagePreset preset || string.IsNullOrEmpty(preset.Message))
        {
            return;
        }

        _messageBox.Text = preset.Message;
        _messageBox.CaretIndex = _messageBox.Text.Length;
    }

    private MessagePreset FindInitialPreset(string currentNote, Color currentColor)
    {
        return Presets.FirstOrDefault(preset =>
            string.Equals(preset.Message, currentNote, StringComparison.Ordinal) &&
            preset.Color == currentColor) ?? Presets[^1];
    }

    private void CloseWith(MarkerDialogAction action)
    {
        var preset = (MessagePreset?)_presetBox.SelectedItem ?? Presets[^1];
        var color = preset.Name == "직접 입력" ? _initialColor : preset.Color;
        Result = new MarkerEditResult(action, _messageBox.Text.Trim(), color);
        DialogResult = action != MarkerDialogAction.Cancel;
        Close();
    }

    private static Button CreateWindowButton(string text, RoutedEventHandler click, bool isClose = false)
    {
        var button = new Button
        {
            Content = text,
            Width = 42,
            Height = 32,
            Padding = new Thickness(0),
            BorderThickness = new Thickness(0),
            Background = WpfBrushes.Transparent,
            Foreground = Brush("#6B6673"),
            FontSize = 11,
            Focusable = false,
            Style = (Style)CreateControlStyles()[isClose ? "CloseWindowButtonStyle" : "WindowButtonStyle"]
        };
        button.Click += click;
        return button;
    }

    private static Button CreateActionButton(string text, RoutedEventHandler click, bool secondary)
    {
        var button = new Button
        {
            Content = text,
            MinWidth = 64,
            Padding = new Thickness(14, 6, 14, 6),
            BorderThickness = secondary ? new Thickness(1) : new Thickness(0),
            BorderBrush = Brush("#DCD8D1"),
            Background = secondary ? WpfBrushes.Transparent : Brush("#F2A65A"),
            Foreground = secondary ? Brush("#6B6673") : Brush("#2B1A0E"),
            FontSize = 12,
            FontWeight = FontWeights.SemiBold,
            Style = (Style)CreateControlStyles()[secondary ? "SecondaryButtonStyle" : "PrimaryButtonStyle"]
        };
        button.Click += click;
        return button;
    }

    private static ResourceDictionary CreateControlStyles()
    {
        const string xaml = """
<ResourceDictionary
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
    <SolidColorBrush x:Key="MemoAccent" Color="#F2A65A"/>
    <SolidColorBrush x:Key="MemoAccentHover" Color="#E89543"/>
    <SolidColorBrush x:Key="MemoAccentSoft" Color="#FFF0DA"/>
    <SolidColorBrush x:Key="MemoBorder" Color="#DCD8D1"/>
    <SolidColorBrush x:Key="MemoText" Color="#2A2730"/>
    <SolidColorBrush x:Key="MemoMuted" Color="#6B6673"/>

    <Style TargetType="{x:Type TextBox}">
        <Setter Property="BorderThickness" Value="1"/>
        <Setter Property="BorderBrush" Value="{StaticResource MemoBorder}"/>
        <Setter Property="Background" Value="White"/>
        <Setter Property="Template">
            <Setter.Value>
                <ControlTemplate TargetType="{x:Type TextBox}">
                    <Border x:Name="Chrome"
                            Background="{TemplateBinding Background}"
                            BorderBrush="{TemplateBinding BorderBrush}"
                            BorderThickness="{TemplateBinding BorderThickness}"
                            CornerRadius="2">
                        <ScrollViewer x:Name="PART_ContentHost"/>
                    </Border>
                    <ControlTemplate.Triggers>
                        <Trigger Property="IsKeyboardFocused" Value="True">
                            <Setter TargetName="Chrome" Property="BorderBrush" Value="{StaticResource MemoAccent}"/>
                        </Trigger>
                        <Trigger Property="IsMouseOver" Value="True">
                            <Setter TargetName="Chrome" Property="BorderBrush" Value="{StaticResource MemoAccent}"/>
                        </Trigger>
                    </ControlTemplate.Triggers>
                </ControlTemplate>
            </Setter.Value>
        </Setter>
    </Style>

    <Style TargetType="{x:Type ComboBoxItem}">
        <Setter Property="Padding" Value="8,6"/>
        <Setter Property="Foreground" Value="{StaticResource MemoText}"/>
        <Setter Property="Template">
            <Setter.Value>
                <ControlTemplate TargetType="{x:Type ComboBoxItem}">
                    <Border x:Name="ItemChrome" Background="White" Padding="{TemplateBinding Padding}">
                        <ContentPresenter/>
                    </Border>
                    <ControlTemplate.Triggers>
                        <Trigger Property="IsHighlighted" Value="True">
                            <Setter TargetName="ItemChrome" Property="Background" Value="{StaticResource MemoAccentSoft}"/>
                        </Trigger>
                        <Trigger Property="IsSelected" Value="True">
                            <Setter TargetName="ItemChrome" Property="Background" Value="{StaticResource MemoAccentSoft}"/>
                        </Trigger>
                    </ControlTemplate.Triggers>
                </ControlTemplate>
            </Setter.Value>
        </Setter>
    </Style>

    <Style TargetType="{x:Type ComboBox}">
        <Setter Property="SnapsToDevicePixels" Value="True"/>
        <Setter Property="Foreground" Value="{StaticResource MemoText}"/>
        <Setter Property="Background" Value="White"/>
        <Setter Property="BorderBrush" Value="{StaticResource MemoBorder}"/>
        <Setter Property="BorderThickness" Value="1"/>
        <Setter Property="Template">
            <Setter.Value>
                <ControlTemplate TargetType="{x:Type ComboBox}">
                    <Grid>
                        <ToggleButton x:Name="ToggleButton"
                                      Background="{TemplateBinding Background}"
                                      BorderBrush="{TemplateBinding BorderBrush}"
                                      Focusable="False"
                                      IsChecked="{Binding IsDropDownOpen, Mode=TwoWay, RelativeSource={RelativeSource TemplatedParent}}"
                                      ClickMode="Press">
                            <ToggleButton.Template>
                                <ControlTemplate TargetType="{x:Type ToggleButton}">
                                    <Border x:Name="Chrome"
                                            Background="{TemplateBinding Background}"
                                            BorderBrush="{TemplateBinding BorderBrush}"
                                            BorderThickness="1"
                                            CornerRadius="2">
                                        <Grid>
                                            <Grid.ColumnDefinitions>
                                                <ColumnDefinition/>
                                                <ColumnDefinition Width="28"/>
                                            </Grid.ColumnDefinitions>
                                            <ContentPresenter Grid.Column="0"
                                                              Margin="8,0,4,0"
                                                              VerticalAlignment="Center"
                                                              HorizontalAlignment="Left"
                                                              RecognizesAccessKey="True"/>
                                            <Path Grid.Column="1"
                                                  Width="8"
                                                  Height="5"
                                                  HorizontalAlignment="Center"
                                                  VerticalAlignment="Center"
                                                  Fill="#6B6673"
                                                  Data="M 0 0 L 4 4 L 8 0 Z"/>
                                        </Grid>
                                    </Border>
                                    <ControlTemplate.Triggers>
                                        <Trigger Property="IsMouseOver" Value="True">
                                            <Setter TargetName="Chrome" Property="BorderBrush" Value="{StaticResource MemoAccent}"/>
                                            <Setter TargetName="Chrome" Property="Background" Value="White"/>
                                        </Trigger>
                                        <Trigger Property="IsChecked" Value="True">
                                            <Setter TargetName="Chrome" Property="BorderBrush" Value="{StaticResource MemoAccent}"/>
                                        </Trigger>
                                    </ControlTemplate.Triggers>
                                </ControlTemplate>
                            </ToggleButton.Template>
                        </ToggleButton>
                        <ContentPresenter x:Name="ContentSite"
                                          IsHitTestVisible="False"
                                          Content="{TemplateBinding SelectionBoxItem}"
                                          ContentTemplate="{TemplateBinding SelectionBoxItemTemplate}"
                                          ContentStringFormat="{TemplateBinding SelectionBoxItemStringFormat}"
                                          Margin="8,0,28,0"
                                          VerticalAlignment="Center"
                                          HorizontalAlignment="Left"/>
                        <Popup x:Name="PART_Popup"
                               AllowsTransparency="True"
                               IsOpen="{TemplateBinding IsDropDownOpen}"
                               Placement="Bottom"
                               PopupAnimation="Fade">
                            <Border Background="White"
                                    BorderBrush="{StaticResource MemoBorder}"
                                    BorderThickness="1"
                                    CornerRadius="2"
                                    MinWidth="{TemplateBinding ActualWidth}">
                                <ScrollViewer MaxHeight="220">
                                    <ItemsPresenter/>
                                </ScrollViewer>
                            </Border>
                        </Popup>
                    </Grid>
                    <ControlTemplate.Triggers>
                        <Trigger Property="IsKeyboardFocusWithin" Value="True">
                            <Setter Property="BorderBrush" Value="{StaticResource MemoAccent}"/>
                        </Trigger>
                    </ControlTemplate.Triggers>
                </ControlTemplate>
            </Setter.Value>
        </Setter>
    </Style>

    <Style x:Key="PrimaryButtonStyle" TargetType="{x:Type Button}">
        <Setter Property="Background" Value="{StaticResource MemoAccent}"/>
        <Setter Property="Foreground" Value="#2B1A0E"/>
        <Setter Property="BorderBrush" Value="{StaticResource MemoAccent}"/>
        <Setter Property="BorderThickness" Value="1"/>
        <Setter Property="Template">
            <Setter.Value>
                <ControlTemplate TargetType="{x:Type Button}">
                    <Border x:Name="Chrome"
                            Background="{TemplateBinding Background}"
                            BorderBrush="{TemplateBinding BorderBrush}"
                            BorderThickness="{TemplateBinding BorderThickness}"
                            CornerRadius="2"
                            Padding="{TemplateBinding Padding}">
                        <ContentPresenter HorizontalAlignment="Center" VerticalAlignment="Center"/>
                    </Border>
                    <ControlTemplate.Triggers>
                        <Trigger Property="IsMouseOver" Value="True">
                            <Setter TargetName="Chrome" Property="Background" Value="{StaticResource MemoAccentHover}"/>
                            <Setter TargetName="Chrome" Property="BorderBrush" Value="{StaticResource MemoAccentHover}"/>
                        </Trigger>
                        <Trigger Property="IsPressed" Value="True">
                            <Setter TargetName="Chrome" Property="Background" Value="#D88434"/>
                            <Setter TargetName="Chrome" Property="BorderBrush" Value="#D88434"/>
                        </Trigger>
                    </ControlTemplate.Triggers>
                </ControlTemplate>
            </Setter.Value>
        </Setter>
    </Style>

    <Style x:Key="WindowButtonStyle" TargetType="{x:Type Button}">
        <Setter Property="Background" Value="Transparent"/>
        <Setter Property="Foreground" Value="{StaticResource MemoMuted}"/>
        <Setter Property="BorderThickness" Value="0"/>
        <Setter Property="Template">
            <Setter.Value>
                <ControlTemplate TargetType="{x:Type Button}">
                    <Border x:Name="Chrome" Background="{TemplateBinding Background}">
                        <ContentPresenter HorizontalAlignment="Center" VerticalAlignment="Center"/>
                    </Border>
                    <ControlTemplate.Triggers>
                        <Trigger Property="IsMouseOver" Value="True">
                            <Setter TargetName="Chrome" Property="Background" Value="#10000000"/>
                        </Trigger>
                        <Trigger Property="IsPressed" Value="True">
                            <Setter TargetName="Chrome" Property="Background" Value="#18000000"/>
                        </Trigger>
                    </ControlTemplate.Triggers>
                </ControlTemplate>
            </Setter.Value>
        </Setter>
    </Style>

    <Style x:Key="CloseWindowButtonStyle" TargetType="{x:Type Button}">
        <Setter Property="Background" Value="Transparent"/>
        <Setter Property="Foreground" Value="{StaticResource MemoMuted}"/>
        <Setter Property="BorderThickness" Value="0"/>
        <Setter Property="Template">
            <Setter.Value>
                <ControlTemplate TargetType="{x:Type Button}">
                    <Border x:Name="Chrome" Background="{TemplateBinding Background}">
                        <ContentPresenter HorizontalAlignment="Center" VerticalAlignment="Center"/>
                    </Border>
                    <ControlTemplate.Triggers>
                        <Trigger Property="IsMouseOver" Value="True">
                            <Setter TargetName="Chrome" Property="Background" Value="#E81123"/>
                            <Setter Property="Foreground" Value="White"/>
                        </Trigger>
                        <Trigger Property="IsPressed" Value="True">
                            <Setter TargetName="Chrome" Property="Background" Value="#C50F1F"/>
                            <Setter Property="Foreground" Value="White"/>
                        </Trigger>
                    </ControlTemplate.Triggers>
                </ControlTemplate>
            </Setter.Value>
        </Setter>
    </Style>

    <Style x:Key="SecondaryButtonStyle" TargetType="{x:Type Button}">
        <Setter Property="Background" Value="Transparent"/>
        <Setter Property="Foreground" Value="{StaticResource MemoMuted}"/>
        <Setter Property="BorderBrush" Value="{StaticResource MemoBorder}"/>
        <Setter Property="BorderThickness" Value="1"/>
        <Setter Property="Template">
            <Setter.Value>
                <ControlTemplate TargetType="{x:Type Button}">
                    <Border x:Name="Chrome"
                            Background="{TemplateBinding Background}"
                            BorderBrush="{TemplateBinding BorderBrush}"
                            BorderThickness="{TemplateBinding BorderThickness}"
                            CornerRadius="2"
                            Padding="{TemplateBinding Padding}">
                        <ContentPresenter HorizontalAlignment="Center" VerticalAlignment="Center"/>
                    </Border>
                    <ControlTemplate.Triggers>
                        <Trigger Property="IsMouseOver" Value="True">
                            <Setter TargetName="Chrome" Property="Background" Value="{StaticResource MemoAccentSoft}"/>
                            <Setter TargetName="Chrome" Property="BorderBrush" Value="{StaticResource MemoAccent}"/>
                        </Trigger>
                        <Trigger Property="IsPressed" Value="True">
                            <Setter TargetName="Chrome" Property="Background" Value="#FFE4BD"/>
                        </Trigger>
                    </ControlTemplate.Triggers>
                </ControlTemplate>
            </Setter.Value>
        </Setter>
    </Style>
</ResourceDictionary>
""";

        return (ResourceDictionary)XamlReader.Parse(xaml);
    }

    private static SolidColorBrush Brush(string hex)
    {
        return (SolidColorBrush)new BrushConverter().ConvertFromString(hex)!;
    }

    private static double Clamp(double value, double min, double max)
    {
        if (max < min)
        {
            return min;
        }

        return Math.Min(Math.Max(value, min), max);
    }

    private sealed record MessagePreset(string Name, string Message, Color Color)
    {
        public override string ToString() => Name;
    }
}
