namespace Loupedeck.GimpPlugin
{
    using System;

    public class GimpPlugin : Plugin
    {
        private readonly Object _statusLock = new();
        private String _shownBlockReason = String.Empty;

        public GimpPlugin() => PluginLog.Init(this.Log);

        public override Boolean UsesApplicationApiOnly => false;

        public override Boolean HasNoApplication => false;

        internal static KeySender KeySender { get; private set; }

        internal static GimpBridge Bridge { get; private set; }

        public override void Load()
        {
            this.RefreshInputStatus();
            KeySender = new KeySender(this);
            Bridge = new GimpBridge();
        }

        public override void Unload()
        {
            KeySender?.Dispose();
            KeySender = null;
            Bridge?.Dispose();
            Bridge = null;
        }

        // True if the OS lets us inject keys; otherwise shows the reason as plugin warning in the Loupedeck software.
        // (Plugin has a PluginStatus property, hence the namespace-qualified enum below.)
        internal Boolean RefreshInputStatus()
        {
            var reason = InputGuard.BlockReason();
            lock (this._statusLock)
            {
                if (reason != this._shownBlockReason)
                {
                    this._shownBlockReason = reason;
                    this.OnPluginStatusChanged(reason == null ? Loupedeck.PluginStatus.Normal : Loupedeck.PluginStatus.Warning, reason);
                }
            }

            return reason == null;
        }
    }
}
