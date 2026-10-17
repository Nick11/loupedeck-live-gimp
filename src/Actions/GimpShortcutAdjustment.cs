namespace Loupedeck.GimpPlugin
{
    using System;

    // Turn = one GIMP default shortcut per tick (zoom, layer, undo step), press = reset shortcut. No acceleration on purpose.
    public class GimpShortcutAdjustment : PluginDynamicAdjustment
    {
        public GimpShortcutAdjustment()
            : base(hasReset: true)
        {
            this.DisplayName = "GIMP Shortcut Dial";
            this.GroupName = "Shortcuts";

            foreach (var dial in GimpShortcutDials.Table.All)
            {
                this.AddParameter(dial.Id, $"{dial.Group}: {dial.Label}", dial.Group);
            }
        }

        protected override void ApplyAdjustment(String actionParameter, Int32 diff)
        {
            if (GimpShortcutDials.Table.Find(actionParameter) is { } dial && diff != 0)
            {
                GimpPlugin.KeySender?.EnqueueSteps(dial.Decrease, dial.Increase, diff);
            }
        }

        protected override void RunCommand(String actionParameter)
        {
            if (GimpShortcutDials.Table.Find(actionParameter)?.Reset is { } reset)
            {
                GimpPlugin.KeySender?.EnqueueShortcut(reset);
            }
        }

        protected override String GetAdjustmentDisplayName(String actionParameter, PluginImageSize imageSize) =>
            GimpShortcutDials.Table.Find(actionParameter)?.Short;

        protected override BitmapImage GetAdjustmentImage(String actionParameter, PluginImageSize imageSize) =>
            GimpShortcutDials.Table.Find(actionParameter) is { } dial ? GimpIcons.Tile(dial) : null;
    }
}
