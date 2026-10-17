namespace Loupedeck.GimpPlugin
{
    using System;

    // A button that sends one GIMP default shortcut.
    internal sealed record GimpCommand(String Id, String Group, String Label, String Short, String Icon, GimpAction Action) : IGimpTile;

    internal static class GimpCommands
    {
        public static readonly GimpTable<GimpCommand> Table = new(
            new GimpCommand("tool.paintbrush", "Tools", "Paintbrush", "Brush", "gimp-tool-paintbrush", GimpActions.Paintbrush),
            new GimpCommand("tool.pencil", "Tools", "Pencil", "Pencil", "gimp-tool-pencil", GimpActions.Pencil),
            new GimpCommand("tool.airbrush", "Tools", "Airbrush", "Airbrush", "gimp-tool-airbrush", GimpActions.Airbrush),
            new GimpCommand("tool.eraser", "Tools", "Eraser", "Eraser", "gimp-tool-eraser", GimpActions.Eraser),
            new GimpCommand("tool.smudge", "Tools", "Smudge", "Smudge", "gimp-tool-smudge", GimpActions.Smudge),
            new GimpCommand("tool.clone", "Tools", "Clone", "Clone", "gimp-tool-clone", GimpActions.Clone),
            new GimpCommand("tool.heal", "Tools", "Heal", "Heal", "gimp-tool-heal", GimpActions.Heal),
            new GimpCommand("tool.bucketfill", "Tools", "Bucket Fill", "Bucket", "gimp-tool-bucket-fill", GimpActions.BucketFill),
            new GimpCommand("tool.gradient", "Tools", "Gradient", "Gradient", "gimp-tool-gradient", GimpActions.Gradient),
            new GimpCommand("tool.colorpicker", "Tools", "Color Picker", "Picker", "gimp-tool-color-picker", GimpActions.ColorPicker),
            new GimpCommand("tool.move", "Tools", "Move", "Move", "gimp-tool-move", GimpActions.Move),
            new GimpCommand("tool.transform", "Tools", "Unified Transform", "Transfrm", "gimp-tool-unified-transform", GimpActions.UnifiedTransform),
            new GimpCommand("tool.warp", "Tools", "Warp Transform", "Warp", "gimp-tool-warp", GimpActions.Warp),
            new GimpCommand("tool.crop", "Tools", "Crop", "Crop", "gimp-tool-crop", GimpActions.Crop),
            new GimpCommand("tool.text", "Tools", "Text", "Text", "gimp-tool-text", GimpActions.Text),
            new GimpCommand("tool.path", "Tools", "Paths", "Paths", "gimp-tool-path", GimpActions.Path),
            new GimpCommand("tool.zoom", "Tools", "Zoom Tool", "Zoom T.", "gimp-tool-zoom", GimpActions.ZoomTool),

            new GimpCommand("select.rect", "Selection", "Rectangle Select", "Rect Sel", "gimp-tool-rect-select", GimpActions.RectSelect),
            new GimpCommand("select.ellipse", "Selection", "Ellipse Select", "Ell Sel", "gimp-tool-ellipse-select", GimpActions.EllipseSelect),
            new GimpCommand("select.free", "Selection", "Free Select", "Free Sel", "gimp-tool-free-select", GimpActions.FreeSelect),
            new GimpCommand("select.fuzzy", "Selection", "Fuzzy Select", "Fuzzy", "gimp-tool-fuzzy-select", GimpActions.FuzzySelect),
            new GimpCommand("select.all", "Selection", "Select All", "Sel All", "gimp-selection-all", GimpActions.SelectAll),
            new GimpCommand("select.none", "Selection", "Select None", "Sel None", "gimp-selection-none", GimpActions.SelectNone),
            new GimpCommand("select.invert", "Selection", "Invert Selection", "Invert", null, GimpActions.SelectInvert),
            new GimpCommand("select.quickmask", "Selection", "Toggle Quick Mask", "Q. Mask", "gimp-quick-mask-on", GimpActions.QuickMask),

            new GimpCommand("color.swap", "Colors", "Swap Colors", "Swap", "gimp-swap-colors", GimpActions.SwapColors),
            new GimpCommand("color.default", "Colors", "Default Colors", "Default", "gimp-default-colors", GimpActions.DefaultColors),

            new GimpCommand("history.undo", "History", "Undo", "Undo", "edit-undo", GimpActions.Undo),
            new GimpCommand("history.redo", "History", "Redo", "Redo", "edit-redo", GimpActions.Redo),

            new GimpCommand("edit.cut", "Edit", "Cut", "Cut", "edit-cut", GimpActions.Cut),
            new GimpCommand("edit.copy", "Edit", "Copy", "Copy", "edit-copy", GimpActions.Copy),
            new GimpCommand("edit.paste", "Edit", "Paste", "Paste", "edit-paste", GimpActions.Paste),

            new GimpCommand("layer.new", "Layers", "New Layer", "New Lay", "gimp-layer", GimpActions.NewLayer),
            new GimpCommand("layer.duplicate", "Layers", "Duplicate Layer", "Dup Lay", "gimp-duplicate", GimpActions.DuplicateLayer),
            new GimpCommand("layer.anchor", "Layers", "Anchor Layer", "Anchor", "gimp-anchor", GimpActions.AnchorLayer),
            new GimpCommand("layer.previous", "Layers", "Layer Above", "Lay Up", "go-up", GimpActions.LayerPrevious),
            new GimpCommand("layer.next", "Layers", "Layer Below", "Lay Down", "go-down", GimpActions.LayerNext),

            new GimpCommand("view.zoomin", "View", "Zoom In", "Zoom +", "zoom-in", GimpActions.ZoomIn),
            new GimpCommand("view.zoomout", "View", "Zoom Out", "Zoom −", "zoom-out", GimpActions.ZoomOut),
            new GimpCommand("view.zoomfit", "View", "Fit Image in Window", "Zoom Fit", "zoom-fit-best", GimpActions.ZoomFit),
            new GimpCommand("view.zoom100", "View", "Zoom 100%", "Zoom 1:1", "zoom-original", GimpActions.Zoom100),

            new GimpCommand("file.save", "File", "Save", "Save", "document-save", GimpActions.Save),
            new GimpCommand("file.export", "File", "Export", "Export", "gimp-prefs-import-export", GimpActions.Export),
            new GimpCommand("file.exportas", "File", "Export As…", "Exp As", "gimp-prefs-import-export", GimpActions.ExportAs));
    }
}
