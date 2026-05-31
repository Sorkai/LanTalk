using Avalonia.Media;

namespace LanTalk.App.Services;

public static class AvatarService
{
    private static readonly Color[] Palette =
    [
        Color.Parse("#2A8CFF"),
        Color.Parse("#16A34A"),
        Color.Parse("#9333EA"),
        Color.Parse("#EA580C"),
        Color.Parse("#0F766E"),
        Color.Parse("#DB2777"),
        Color.Parse("#4F46E5"),
        Color.Parse("#64748B")
    ];

    public static string GetInitial(string value)
    {
        var trimmed = value.Trim();
        return string.IsNullOrWhiteSpace(trimmed)
            ? "?"
            : trimmed[..1].ToUpperInvariant();
    }

    public static IBrush CreateBrush(string seed)
    {
        var hash = 0;
        foreach (var ch in seed)
        {
            hash = unchecked((hash * 31) + ch);
        }

        var index = (int)((uint)hash % Palette.Length);
        return new SolidColorBrush(Palette[index]);
    }
}
