using Color = System.Windows.Media.Color;

namespace MemoTag;

public enum MarkerDialogAction
{
    Cancel,
    Save,
    Remove
}

public sealed record ColorOption(string Name, Color Color)
{
    public override string ToString() => Name;
}

public sealed record MarkerEditResult(MarkerDialogAction Action, string Note, Color Color);
