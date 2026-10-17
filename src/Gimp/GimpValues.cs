namespace Loupedeck.GimpPlugin
{
    using System;
    using System.Globalization;

    // A GIMP value set through the bridge plug-in. BridgeName is its name in the bridge protocol (step sizes live there).
    internal sealed record GimpValue(
        String Id, String Group, String Label, String Short, String Icon,
        String BridgeName, String Unit, Int32 Decimals = 0, Boolean CanReset = true) : IGimpTile
    {
        public String Format(Double value) => value.ToString("F" + this.Decimals, CultureInfo.InvariantCulture) + this.Unit;
    }

    internal static class GimpValues
    {
        public static readonly GimpTable<GimpValue> Table = new(
            new GimpValue("brush.size", "Brush", "Size", "Size", "gimp-prefs-folders-brushes", "size", "px"),
            new GimpValue("brush.opacity", "Brush", "Opacity", "Opacity", "gimp-transparency", "opacity", "%"),
            new GimpValue("brush.hardness", "Brush", "Hardness", "Hardness", "gimp-dynamics", "hardness", "%"),
            new GimpValue("brush.angle", "Brush", "Angle", "Angle", "object-rotate-right", "angle", "°"),
            new GimpValue("brush.aspect", "Brush", "Aspect Ratio", "Aspect", "gimp-tool-options", "aspect", "", Decimals: 1),
            new GimpValue("brush.spacing", "Brush", "Spacing", "Spacing", "gimp-letter-spacing", "spacing", "%"),
            new GimpValue("brush.force", "Brush", "Force", "Force", "gimp-tool-mypaint-brush", "force", "%"),

            new GimpValue("color.hue", "Colors", "Foreground Hue", "FG Hue", "gimp-tool-hue-saturation", "fg-hue", "°", CanReset: false),
            new GimpValue("color.saturation", "Colors", "Foreground Saturation", "FG Sat", "gimp-color-triangle", "fg-saturation", "%", CanReset: false),
            new GimpValue("color.value", "Colors", "Foreground Value", "FG Value", "gimp-channel-gray", "fg-value", "%", CanReset: false));
    }
}
