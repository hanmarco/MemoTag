using System.Diagnostics;
using System.Windows.Threading;

namespace MemoTag;

public sealed class WindowMarkerService : IDisposable
{
    private readonly Dictionary<IntPtr, MarkedWindow> _markers = [];
    private readonly DispatcherTimer _timer;

    public WindowMarkerService()
    {
        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(120)
        };
        _timer.Tick += (_, _) => UpdateMarkers();
        _timer.Start();
    }

    public void MarkOrEditForegroundWindow()
    {
        var hwnd = GetUsableForegroundWindow();
        if (hwnd == IntPtr.Zero)
        {
            return;
        }

        if (_markers.TryGetValue(hwnd, out var existing))
        {
            ShowEditDialog(existing);
            return;
        }

        var title = NativeMethods.GetWindowTitle(hwnd);
        var dialog = new MemoEditDialog(title, "닫지 마세요.", System.Windows.Media.Color.FromRgb(255, 193, 7), allowRemove: false);
        var accepted = dialog.ShowDialog() == true;
        if (!accepted || dialog.Result.Action != MarkerDialogAction.Save)
        {
            return;
        }

        var marker = new MarkedWindow(hwnd, title, dialog.Result.Note, dialog.Result.Color);
        marker.EditRequested += (_, _) => ShowEditDialog(marker);
        marker.RemoveRequested += (_, _) => RemoveMarker(marker.Handle);
        _markers[hwnd] = marker;
        marker.Update();
    }

    public void ClearForegroundWindowMarker()
    {
        var hwnd = GetUsableForegroundWindow();
        if (hwnd != IntPtr.Zero)
        {
            RemoveMarker(hwnd);
        }
    }

    public void ClearAll()
    {
        foreach (var marker in _markers.Values.ToArray())
        {
            marker.Dispose();
        }

        _markers.Clear();
    }

    public void Dispose()
    {
        _timer.Stop();
        ClearAll();
    }

    private void ShowEditDialog(MarkedWindow marker)
    {
        var dialog = new MemoEditDialog(marker.Title, marker.Note, marker.Color, allowRemove: true);
        var accepted = dialog.ShowDialog() == true;
        if (!accepted)
        {
            return;
        }

        if (dialog.Result.Action == MarkerDialogAction.Remove)
        {
            RemoveMarker(marker.Handle);
        }
        else if (dialog.Result.Action == MarkerDialogAction.Save)
        {
            marker.ApplyEdit(dialog.Result.Note, dialog.Result.Color);
            marker.Update();
        }
    }

    private void UpdateMarkers()
    {
        foreach (var marker in _markers.Values.ToArray())
        {
            if (!NativeMethods.IsWindow(marker.Handle))
            {
                RemoveMarker(marker.Handle);
                continue;
            }

            marker.SetHidden(NativeMethods.IsIconic(marker.Handle));
            if (!NativeMethods.IsIconic(marker.Handle))
            {
                marker.Update();
            }
        }
    }

    private void RemoveMarker(IntPtr hwnd)
    {
        if (!_markers.Remove(hwnd, out var marker))
        {
            return;
        }

        marker.Dispose();
    }

    private static IntPtr GetUsableForegroundWindow()
    {
        var hwnd = NativeMethods.GetForegroundWindow();
        if (hwnd == IntPtr.Zero)
        {
            return IntPtr.Zero;
        }

        var root = NativeMethods.GetAncestor(hwnd, NativeMethods.GaRoot);
        if (root != IntPtr.Zero)
        {
            hwnd = root;
        }

        if (!NativeMethods.IsWindow(hwnd) || !NativeMethods.IsWindowVisible(hwnd))
        {
            return IntPtr.Zero;
        }

        NativeMethods.GetWindowThreadProcessId(hwnd, out var processId);
        if (processId == Environment.ProcessId)
        {
            return IntPtr.Zero;
        }

        try
        {
            using var process = Process.GetProcessById((int)processId);
            if (string.Equals(process.ProcessName, "explorer", StringComparison.OrdinalIgnoreCase) &&
                NativeMethods.GetWindowTitle(hwnd) == "Program Manager")
            {
                return IntPtr.Zero;
            }
        }
        catch
        {
            return IntPtr.Zero;
        }

        return NativeMethods.TryGetFrameBounds(hwnd, out var rect) && rect.Width >= 80 && rect.Height >= 60
            ? hwnd
            : IntPtr.Zero;
    }
}
