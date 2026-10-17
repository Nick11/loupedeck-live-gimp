namespace Loupedeck.GimpPlugin
{
    using System;

    // A dial driven by GIMP's default shortcuts, for what the bridge plug-in can't do: turn = Decrease/Increase, press = Reset.
    internal sealed record GimpShortcutDial(
        String Id, String Group, String Label, String Short, String Icon,
        GimpAction Decrease, GimpAction Increase, GimpAction Reset = null) : IGimpTile;

    internal static class GimpShortcutDials
    {
        public static readonly GimpTable<GimpShortcutDial> Table = new(
            new GimpShortcutDial("view.zoom", "View", "Zoom", "Zoom", "gimp-tool-zoom",
                GimpActions.ZoomOut, GimpActions.ZoomIn, GimpActions.ZoomFit),
            new GimpShortcutDial("layer.select", "Layers", "Select Layer", "Layer", "gimp-layers",
                GimpActions.LayerPrevious, GimpActions.LayerNext, GimpActions.LayerTop),
            new GimpShortcutDial("history.undoredo", "History", "Undo / Redo", "History", "gimp-undo-history",
                GimpActions.Undo, GimpActions.Redo));
    }
}
