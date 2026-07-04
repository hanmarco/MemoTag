using System.Windows;
using Color = System.Windows.Media.Color;

namespace MemoTag;

public sealed class MarkedWindow : IDisposable
{
    private const double BorderThickness = 6;
    private const double ScreenEdgeInnerOverlap = 6;
    private const double FullScreenBottomLift = 0;
    private const double NoteGap = 10;
    private const double TitleBarNoteInset = 8;
    private const double TitleButtonReserveWidth = 140;
    private const double DefaultNoteLift = 9;
    private const double DefaultNoteLeftShift = 100;

    private readonly BorderSegmentWindow _topBorder;
    private readonly BorderSegmentWindow _bottomBorder;
    private readonly BorderSegmentWindow _leftBorder;
    private readonly BorderSegmentWindow _rightBorder;
    private readonly NoteWindow _noteWindow;

    public IntPtr Handle { get; }
    public string Title { get; private set; }
    public string Note { get; private set; }
    public Color Color { get; private set; }

    public event EventHandler? EditRequested;
    public event EventHandler? RemoveRequested;

    public MarkedWindow(IntPtr handle, string title, string note, Color color)
    {
        Handle = handle;
        Title = title;
        Note = string.IsNullOrWhiteSpace(note) ? "(메모 없음)" : note;
        Color = color;

        _topBorder = new BorderSegmentWindow(color);
        _bottomBorder = new BorderSegmentWindow(color);
        _leftBorder = new BorderSegmentWindow(color);
        _rightBorder = new BorderSegmentWindow(color);
        _noteWindow = new NoteWindow(Note, color);
        _noteWindow.EditRequested += (_, _) => EditRequested?.Invoke(this, EventArgs.Empty);
        _noteWindow.RemoveRequested += (_, _) => RemoveRequested?.Invoke(this, EventArgs.Empty);
    }

    public void ApplyEdit(string note, Color color)
    {
        Note = string.IsNullOrWhiteSpace(note) ? "(메모 없음)" : note;
        Color = color;

        _topBorder.SetColor(color);
        _bottomBorder.SetColor(color);
        _leftBorder.SetColor(color);
        _rightBorder.SetColor(color);
        _noteWindow.UpdateContent(Note, color);
    }

    public void Update()
    {
        if (!NativeMethods.TryGetFrameBounds(Handle, out var rect))
        {
            return;
        }

        Title = NativeMethods.GetWindowTitle(Handle);
        ShowAll();

        var screenBounds = GetTargetScreenBounds();
        var edgeState = GetScreenEdgeState(rect, screenBounds);

        var borderLeft = edgeState.Left
            ? rect.Left - BorderThickness + ScreenEdgeInnerOverlap
            : rect.Left - BorderThickness;
        var borderTop = edgeState.Top
            ? screenBounds.Top
            : rect.Top - BorderThickness;
        var borderRight = edgeState.Right
            ? rect.Right - ScreenEdgeInnerOverlap
            : rect.Right;
        var borderBottom = edgeState.Bottom
            ? Math.Min(rect.Bottom, screenBounds.WorkingBottom) - BorderThickness - FullScreenBottomLift
            : rect.Bottom;
        var borderOuterRight = borderRight + BorderThickness;

        _topBorder.SetBounds(borderLeft, borderTop, borderOuterRight - borderLeft, BorderThickness);
        _bottomBorder.SetBounds(borderLeft, borderBottom, borderOuterRight - borderLeft, BorderThickness);
        _leftBorder.SetBounds(borderLeft, borderTop + BorderThickness, BorderThickness, borderBottom - borderTop - BorderThickness);
        _rightBorder.SetBounds(borderRight, borderTop + BorderThickness, BorderThickness, borderBottom - borderTop - BorderThickness);

        _noteWindow.UpdateLayout();
        var noteWidth = Math.Max(_noteWindow.ActualWidth, _noteWindow.Width);
        var noteHeight = Math.Max(_noteWindow.ActualHeight, _noteWindow.Height);

        var notePlacement = ChooseVerticalNotePosition(rect.Top, rect.Bottom, noteHeight, screenBounds, edgeState);
        var noteLeft = ChooseHorizontalNotePosition(rect.Left, rect.Right, noteWidth, screenBounds);

        _noteWindow.SetAttachedToTopEdge(notePlacement.AttachedToTopEdge);
        _noteWindow.SetPosition(noteLeft, notePlacement.Top);
        ApplyDepth(IsFullScreen(edgeState), edgeState.Top);
    }

    public void SetHidden(bool hidden)
    {
        if (hidden)
        {
            HideAll();
        }
        else
        {
            ShowAll();
        }
    }

    public void Dispose()
    {
        _topBorder.Close();
        _bottomBorder.Close();
        _leftBorder.Close();
        _rightBorder.Close();
        _noteWindow.Close();
    }

    private void ShowAll()
    {
        ShowIfNeeded(_topBorder);
        ShowIfNeeded(_bottomBorder);
        ShowIfNeeded(_leftBorder);
        ShowIfNeeded(_rightBorder);
        ShowIfNeeded(_noteWindow);
    }

    private void HideAll()
    {
        _topBorder.Hide();
        _bottomBorder.Hide();
        _leftBorder.Hide();
        _rightBorder.Hide();
        _noteWindow.Hide();
    }

    private static void ShowIfNeeded(Window window)
    {
        if (!window.IsVisible)
        {
            window.Show();
        }
    }

    private ScreenBounds GetTargetScreenBounds()
    {
        var screen = System.Windows.Forms.Screen.FromHandle(Handle);
        var bounds = screen.Bounds;
        var workingArea = screen.WorkingArea;
        return new ScreenBounds(bounds.Left, bounds.Top, bounds.Right, bounds.Bottom, workingArea.Bottom);
    }

    private static ScreenEdgeState GetScreenEdgeState(NativeMethods.Rect rect, ScreenBounds screenBounds)
    {
        const double tolerance = 2;

        return new ScreenEdgeState(
            rect.Left <= screenBounds.Left + tolerance,
            rect.Top <= screenBounds.Top + tolerance,
            rect.Right >= screenBounds.Right - tolerance,
            rect.Bottom >= screenBounds.Bottom - tolerance ||
            rect.Bottom >= screenBounds.WorkingBottom - tolerance);
    }

    private static bool IsFullScreen(ScreenEdgeState edgeState)
    {
        return edgeState is { Left: true, Top: true, Right: true, Bottom: true };
    }

    private static NotePlacement ChooseVerticalNotePosition(
        double targetTop,
        double targetBottom,
        double noteHeight,
        ScreenBounds screenBounds,
        ScreenEdgeState edgeState)
    {
        var visibleTop = screenBounds.Top;
        var visibleBottom = Math.Min(screenBounds.Bottom, screenBounds.WorkingBottom);
        var above = targetTop - noteHeight;
        var below = targetBottom;

        if (above >= visibleTop)
        {
            return new NotePlacement(above - DefaultNoteLift, AttachedToTopEdge: false, IsDefaultAbove: true);
        }

        if (below + noteHeight <= visibleBottom)
        {
            return new NotePlacement(below, AttachedToTopEdge: true, IsDefaultAbove: false);
        }

        if (edgeState.Top || !CanFitWithinScreen(noteHeight, visibleTop, visibleBottom))
        {
            return new NotePlacement(visibleTop, AttachedToTopEdge: true, IsDefaultAbove: false);
        }

        var insideTop = Clamp(targetTop, visibleTop, visibleBottom - noteHeight);
        return new NotePlacement(insideTop, AttachedToTopEdge: true, IsDefaultAbove: false);
    }

    private static bool CanFitWithinScreen(double noteHeight, double visibleTop, double visibleBottom)
    {
        return noteHeight <= visibleBottom - visibleTop;
    }

    private static double ChooseHorizontalNotePosition(
        double targetLeft,
        double targetRight,
        double noteWidth,
        ScreenBounds screenBounds)
    {
        var preferred = targetRight - TitleButtonReserveWidth - noteWidth - TitleBarNoteInset - DefaultNoteLeftShift;
        var min = Math.Max(targetLeft + NoteGap, screenBounds.Left + NoteGap);
        var max = Math.Min(targetRight - noteWidth - NoteGap, screenBounds.Right - noteWidth - NoteGap);

        return Clamp(preferred, min, max);
    }

    private static double Clamp(double value, double min, double max)
    {
        if (max < min)
        {
            return min;
        }

        return Math.Min(Math.Max(value, min), max);
    }

    private void ApplyDepth(bool topmost, bool keepTopBorderAbove)
    {
        var overlayHandles = GetOverlayHandles();

        if (overlayHandles.Length == 0)
        {
            return;
        }

        if (keepTopBorderAbove && _topBorder.Handle != IntPtr.Zero)
        {
            NativeMethods.SetWindowTopmost(_topBorder.Handle, true);
        }

        if (topmost)
        {
            foreach (var overlayHandle in overlayHandles)
            {
                NativeMethods.SetWindowTopmost(overlayHandle, true);
            }

            return;
        }

        foreach (var overlayHandle in overlayHandles)
        {
            NativeMethods.SetWindowTopmost(overlayHandle, false);
        }

        var overlaySet = overlayHandles.ToHashSet();
        var insertAfter = FindNearestNonOverlayWindowAboveTarget(overlaySet);

        foreach (var overlayHandle in overlayHandles)
        {
            NativeMethods.SetWindowZOrder(overlayHandle, insertAfter);
        }
    }

    private IntPtr[] GetOverlayHandles()
    {
        return new[]
        {
            _topBorder.Handle,
            _bottomBorder.Handle,
            _leftBorder.Handle,
            _rightBorder.Handle,
            _noteWindow.Handle
        }.Where(handle => handle != IntPtr.Zero).ToArray();
    }

    private IntPtr FindNearestNonOverlayWindowAboveTarget(HashSet<IntPtr> overlayHandles)
    {
        var current = Handle;
        while (true)
        {
            current = NativeMethods.GetWindow(current, NativeMethods.GwHwndPrev);
            if (current == IntPtr.Zero)
            {
                return NativeMethods.HwndTop;
            }

            if (overlayHandles.Contains(current) || !NativeMethods.IsWindowVisible(current))
            {
                continue;
            }

            return current;
        }
    }

    private readonly record struct ScreenBounds(double Left, double Top, double Right, double Bottom, double WorkingBottom);

    private readonly record struct ScreenEdgeState(bool Left, bool Top, bool Right, bool Bottom);

    private readonly record struct NotePlacement(double Top, bool AttachedToTopEdge, bool IsDefaultAbove);
}
