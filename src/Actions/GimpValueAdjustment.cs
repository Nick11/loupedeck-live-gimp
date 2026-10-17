namespace Loupedeck.GimpPlugin
{
    using System;

    // Turn = step a GIMP value through the bridge plug-in, press = GIMP default. The dial shows the value GIMP reports.
    public class GimpValueAdjustment : PluginDynamicAdjustment
    {
        public GimpValueAdjustment()
            : base(hasReset: true)
        {
            this.DisplayName = "GIMP Value";
            this.GroupName = "Values";

            foreach (var value in GimpValues.Table.All)
            {
                this.AddParameter(value.Id, $"{value.Group}: {value.Label}", value.Group);
            }
        }

        protected override Boolean OnLoad()
        {
            GimpBridge.ValueChanged += this.OnValueChanged;
            return true;
        }

        protected override Boolean OnUnload()
        {
            GimpBridge.ValueChanged -= this.OnValueChanged;
            return true;
        }

        protected override void ApplyAdjustment(String actionParameter, Int32 diff)
        {
            if (GimpValues.Table.Find(actionParameter) is { } value)
            {
                GimpPlugin.Bridge?.Step(value.BridgeName, diff);
            }
        }

        protected override void RunCommand(String actionParameter)
        {
            if (GimpValues.Table.Find(actionParameter) is { CanReset: true } value)
            {
                GimpPlugin.Bridge?.Reset(value.BridgeName);
            }
        }

        protected override String GetAdjustmentDisplayName(String actionParameter, PluginImageSize imageSize) =>
            GimpValues.Table.Find(actionParameter)?.Short;

        // The value is part of the image ("–" while GIMP or the bridge plug-in is not reachable), see GimpIcons.Value.
        protected override BitmapImage GetAdjustmentImage(String actionParameter, PluginImageSize imageSize) =>
            GimpValues.Table.Find(actionParameter) is { } value
                ? GimpIcons.Value(value, GimpPlugin.Bridge?.Get(value.BridgeName) is { } current ? value.Format(current) : "–")
                : null;

        private void OnValueChanged(String bridgeName)
        {
            foreach (var value in GimpValues.Table.All)
            {
                if (value.BridgeName == bridgeName)
                {
                    this.ActionImageChanged(value.Id);
                }
            }
        }
    }
}
