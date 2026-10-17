namespace Loupedeck.GimpPlugin
{
    using System;

    internal readonly record struct GimpKey(VirtualKeyCode Key, ModifierKey Modifiers);

    // A GIMP action and its default shortcut in GIMP 3.0–3.2. Name is the action name in GIMP's app/actions/*.c,
    // kept for reference only. The plugin never changes GIMP's shortcuts.
    internal sealed record GimpAction(String Name, GimpKey Key);

    internal static class GimpActions
    {
        // GIMP's <Primary> is Cmd on macOS, Ctrl elsewhere.
        private static readonly ModifierKey Primary = OperatingSystem.IsMacOS() ? ModifierKey.Command : ModifierKey.Control;
        private static readonly ModifierKey PrimaryShift = Primary | ModifierKey.Shift;

        public static readonly GimpAction ZoomIn = Key("view-zoom-in", VirtualKeyCode.Add);
        public static readonly GimpAction ZoomOut = Key("view-zoom-out", VirtualKeyCode.Subtract);
        public static readonly GimpAction Zoom100 = Key("view-zoom-1-1", VirtualKeyCode.NumPad1);
        public static readonly GimpAction ZoomFit = Key("view-zoom-fit-in", VirtualKeyCode.KeyJ, PrimaryShift);
        public static readonly GimpAction LayerPrevious = Key("layers-select-previous", VirtualKeyCode.PageUp);
        public static readonly GimpAction LayerNext = Key("layers-select-next", VirtualKeyCode.PageDown);
        public static readonly GimpAction LayerTop = Key("layers-select-top", VirtualKeyCode.Home);
        public static readonly GimpAction Undo = Key("edit-undo", VirtualKeyCode.KeyZ, Primary);
        public static readonly GimpAction Redo = Key("edit-redo", VirtualKeyCode.KeyY, Primary);

        public static readonly GimpAction Paintbrush = Key("tools-paintbrush", VirtualKeyCode.KeyP);
        public static readonly GimpAction Pencil = Key("tools-pencil", VirtualKeyCode.KeyN);
        public static readonly GimpAction Airbrush = Key("tools-airbrush", VirtualKeyCode.KeyA);
        public static readonly GimpAction Eraser = Key("tools-eraser", VirtualKeyCode.KeyE, ModifierKey.Shift);
        public static readonly GimpAction Move = Key("tools-move", VirtualKeyCode.KeyM);
        public static readonly GimpAction RectSelect = Key("tools-rect-select", VirtualKeyCode.KeyR);
        public static readonly GimpAction EllipseSelect = Key("tools-ellipse-select", VirtualKeyCode.KeyE);
        public static readonly GimpAction FreeSelect = Key("tools-free-select", VirtualKeyCode.KeyF);
        public static readonly GimpAction FuzzySelect = Key("tools-fuzzy-select", VirtualKeyCode.KeyU);
        public static readonly GimpAction Crop = Key("tools-crop", VirtualKeyCode.KeyC, ModifierKey.Shift);
        public static readonly GimpAction UnifiedTransform = Key("tools-unified-transform", VirtualKeyCode.KeyT, ModifierKey.Shift);
        public static readonly GimpAction Text = Key("tools-text", VirtualKeyCode.KeyT);
        public static readonly GimpAction BucketFill = Key("tools-bucket-fill", VirtualKeyCode.KeyB, ModifierKey.Shift);
        public static readonly GimpAction Gradient = Key("tools-gradient", VirtualKeyCode.KeyG);
        public static readonly GimpAction Clone = Key("tools-clone", VirtualKeyCode.KeyC);
        public static readonly GimpAction Heal = Key("tools-heal", VirtualKeyCode.KeyH);
        public static readonly GimpAction Smudge = Key("tools-smudge", VirtualKeyCode.KeyS);
        public static readonly GimpAction ColorPicker = Key("tools-color-picker", VirtualKeyCode.KeyO);
        public static readonly GimpAction Warp = Key("tools-warp", VirtualKeyCode.KeyW);
        public static readonly GimpAction Path = Key("tools-path", VirtualKeyCode.KeyB);
        public static readonly GimpAction ZoomTool = Key("tools-zoom", VirtualKeyCode.KeyZ);
        public static readonly GimpAction SwapColors = Key("context-colors-swap", VirtualKeyCode.KeyX);
        public static readonly GimpAction DefaultColors = Key("context-colors-default", VirtualKeyCode.KeyD);
        public static readonly GimpAction QuickMask = Key("quick-mask-toggle", VirtualKeyCode.KeyQ, ModifierKey.Shift);
        public static readonly GimpAction Save = Key("file-save", VirtualKeyCode.KeyS, Primary);
        public static readonly GimpAction Export = Key("file-export", VirtualKeyCode.KeyE, Primary);
        public static readonly GimpAction ExportAs = Key("file-export-as", VirtualKeyCode.KeyE, PrimaryShift);
        public static readonly GimpAction SelectAll = Key("select-all", VirtualKeyCode.KeyA, Primary);
        public static readonly GimpAction SelectNone = Key("select-none", VirtualKeyCode.KeyA, PrimaryShift);
        public static readonly GimpAction SelectInvert = Key("select-invert", VirtualKeyCode.KeyI, Primary);
        public static readonly GimpAction Cut = Key("edit-cut", VirtualKeyCode.KeyX, Primary);
        public static readonly GimpAction Copy = Key("edit-copy", VirtualKeyCode.KeyC, Primary);
        public static readonly GimpAction Paste = Key("edit-paste", VirtualKeyCode.KeyV, Primary);
        public static readonly GimpAction NewLayer = Key("layers-new", VirtualKeyCode.KeyN, PrimaryShift);
        public static readonly GimpAction DuplicateLayer = Key("layers-duplicate", VirtualKeyCode.KeyD, PrimaryShift);
        public static readonly GimpAction AnchorLayer = Key("layers-anchor", VirtualKeyCode.KeyH, Primary);

        private static GimpAction Key(String name, VirtualKeyCode key, ModifierKey modifiers = ModifierKey.None) =>
            new(name, new(key, modifiers));
    }
}
