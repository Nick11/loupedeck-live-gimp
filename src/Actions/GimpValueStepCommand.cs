namespace Loupedeck.GimpPlugin
{
    using System;

    // GimpValueAdjustment as +, − and reset buttons, for devices or keys without a dial.
    public class GimpValueStepCommand : PluginDynamicCommand
    {
        // Parameter IDs end up in Windows file names in exported profiles: only letters, digits and dots.
        private const String Increase = "inc", Decrease = "dec", Reset = "reset";

        public GimpValueStepCommand()
        {
            this.DisplayName = "GIMP Value Step";
            this.GroupName = "Values";

            foreach (var value in GimpValues.Table.All)
            {
                this.AddParameter($"{value.Id}.{Increase}", $"{value.Group}: {value.Label} +", value.Group);
                this.AddParameter($"{value.Id}.{Decrease}", $"{value.Group}: {value.Label} −", value.Group);
                if (value.CanReset)
                {
                    this.AddParameter($"{value.Id}.{Reset}", $"{value.Group}: {value.Label} Reset", value.Group);
                }
            }
        }

        protected override void RunCommand(String actionParameter)
        {
            if (!TryParse(actionParameter, out var value, out var op))
            {
                return;
            }

            var bridge = GimpPlugin.Bridge;
            switch (op)
            {
                case Increase: bridge?.Step(value.BridgeName, 1); break;
                case Decrease: bridge?.Step(value.BridgeName, -1); break;
                case Reset when value.CanReset: bridge?.Reset(value.BridgeName); break;
            }
        }

        protected override BitmapImage GetCommandImage(String actionParameter, PluginImageSize imageSize) =>
            TryParse(actionParameter, out var value, out var op)
                ? GimpIcons.Tile(value, op == Reset ? "↺" : op == Decrease ? "−" : "+")
                : null;

        // Parameter format: "<value id>.<inc|dec|reset>".
        private static Boolean TryParse(String actionParameter, out GimpValue value, out String op)
        {
            var dot = actionParameter?.LastIndexOf('.') ?? -1;
            value = dot > 0 ? GimpValues.Table.Find(actionParameter[..dot]) : null;
            op = value != null ? actionParameter[(dot + 1)..] : null;
            return value != null;
        }
    }
}
