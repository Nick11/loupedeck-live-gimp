namespace Loupedeck.GimpPlugin
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Security;
    using System.Text.RegularExpressions;

    // Button images as SVG on an 80×80 canvas (scales to every device): group color, GIMP icon, label, and optionally a badge
    // or a value. The value is drawn here (not by the Loupedeck software) to control its size and weight.
    internal static class GimpIcons
    {
        private const Int32 Size = 80;
        private const Int32 IconX = 22, IconY = 8, IconSize = 36;
        private const Int32 LabelY = 64, LabelYWithoutIcon = 44, LabelSize = 14;
        private const Int32 ValueIconX = 23, ValueIconY = 4, ValueIconSize = 34, ValueLabelY = 54, ValueY = 74, ValueSize = 16;
        private const String Font = "font-family='Helvetica,Arial,sans-serif'";
        private const String DefaultColor = "#333333";

        // Explicit, not transparent: the Loupedeck software recolors single-color images.
        private const String ValueBackground = "#000000";

        private static readonly Dictionary<String, String> GroupColors = new()
        {
            ["Brush"] = "#3D6E8A",
            ["Colors"] = "#8A2F5E",
            ["Edit"] = "#5A5A2F",
            ["File"] = "#2F5A8A",
            ["History"] = "#7A3D3D",
            ["Layers"] = "#2F8A6B",
            ["Selection"] = "#8A6B2F",
            ["Tools"] = "#6B4FA0",
            ["View"] = "#4A4A4A",
        };

        private static readonly Dictionary<String, String> IconCache = new();

        public static BitmapImage Tile(IGimpTile tile, String badge = null)
        {
            var icon = GimpIcon(tile.Icon, IconX, IconY, IconSize);
            var labelY = icon.Length == 0 ? LabelYWithoutIcon : LabelY;
            var badgeSvg = badge == null ? "" : Text(Size - 10, 20, 20, "end", badge, "font-weight='bold'");
            return Svg(GroupColors.GetValueOrDefault(tile.Group, DefaultColor), icon + badgeSvg + Text(Size / 2, labelY, LabelSize, "middle", tile.Short));
        }

        // Smaller icon and label, the value large below, on black for readability.
        public static BitmapImage Value(IGimpTile tile, String value) =>
            Svg(ValueBackground,
                GimpIcon(tile.Icon, ValueIconX, ValueIconY, ValueIconSize) +
                Text(Size / 2, ValueLabelY, LabelSize, "middle", tile.Short) +
                Text(Size / 2, ValueY, ValueSize, "middle", value));

        private static BitmapImage Svg(String background, String content) => BitmapImage.FromSvg(
            $"<svg xmlns='http://www.w3.org/2000/svg' xmlns:xlink='http://www.w3.org/1999/xlink' viewBox='0 0 {Size} {Size}' width='{Size}' height='{Size}'>" +
            $"<rect width='{Size}' height='{Size}' rx='8' fill='{background}'/>" +
            content +
            "</svg>");

        private static String Text(Int32 x, Int32 y, Int32 size, String anchor, String text, String extra = "") =>
            $"<text x='{x}' y='{y}' font-size='{size}' text-anchor='{anchor}' fill='#fff' {Font} {extra}>{SecurityElement.Escape(text)}</text>";

        // A GIMP symbolic icon (embedded from icons/gimp) as nested <svg> at the given square; "" if none, missing or malformed.
        private static String GimpIcon(String name, Int32 x, Int32 y, Int32 size)
        {
            if (name == null)
            {
                return "";
            }

            String icon;
            lock (IconCache)
            {
                if (!IconCache.TryGetValue(name, out icon))
                {
                    icon = IconCache[name] = LoadIcon(name);
                }
            }

            return icon.Length == 0 ? "" : $"<svg x='{x}' y='{y}' width='{size}' height='{size}' {icon}</svg>";
        }

        private static String LoadIcon(String name)
        {
            using var stream = typeof(GimpIcons).Assembly.GetManifestResourceStream($"icons.gimp.{name}.svg");
            if (stream == null)
            {
                PluginLog.Warning($"Icon '{name}' not found");
                return "";
            }

            var svg = new StreamReader(stream).ReadToEnd();
            var rootStart = svg.IndexOf("<svg", StringComparison.Ordinal);
            var rootEnd = rootStart < 0 ? -1 : svg.IndexOf('>', rootStart);
            var end = svg.LastIndexOf("</svg>", StringComparison.Ordinal);
            if (rootEnd < 0 || end < rootEnd)
            {
                PluginLog.Warning($"Icon '{name}' is not a valid SVG");
                return "";
            }

            var root = svg[rootStart..(rootEnd + 1)];
            var namespaces = String.Join(" ", Regex.Matches(root, "xmlns:\\w+=\"[^\"]*\"").Select(m => m.Value));
            var viewBox = Regex.Match(root, "viewBox=\"([^\"]*)\"") is { Success: true } match ? match.Groups[1].Value : "0 0 16 16";
            return $"viewBox='{viewBox}' {namespaces}>{svg[(rootEnd + 1)..end]}";
        }
    }
}
