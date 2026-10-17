namespace Loupedeck.GimpPlugin
{
    using System;

    // A button that sends one GIMP default shortcut.
    public class GimpShortcutCommand : PluginDynamicCommand
    {
        public GimpShortcutCommand()
        {
            this.DisplayName = "GIMP Shortcut";
            this.GroupName = "Shortcuts";

            foreach (var command in GimpCommands.Table.All)
            {
                this.AddParameter(command.Id, command.Label, command.Group);
            }
        }

        protected override void RunCommand(String actionParameter)
        {
            if (GimpCommands.Table.Find(actionParameter) is { } command)
            {
                GimpPlugin.KeySender?.EnqueueShortcut(command.Action);
            }
        }

        protected override BitmapImage GetCommandImage(String actionParameter, PluginImageSize imageSize) =>
            GimpCommands.Table.Find(actionParameter) is { } command ? GimpIcons.Tile(command) : null;
    }
}
