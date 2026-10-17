namespace Loupedeck.GimpPlugin
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Threading;

    // Sends shortcuts to GIMP on one worker thread, only while GIMP is in front and no modifier is held.
    // Dial steps are accumulated and clamped instead of queued, so fast turns never lag behind.
    internal sealed class KeySender : IDisposable
    {
        private const Int32 ShortcutDelayMs = 10;
        private const Int32 MaxPendingSteps = 5;
        private static readonly TimeSpan MaxCommandAge = TimeSpan.FromMilliseconds(500);

        private readonly GimpPlugin _plugin;
        private readonly Queue<(GimpKey Key, Int64 Timestamp)> _commands = new();
        private readonly Thread _worker;
        private (GimpKey Decrease, GimpKey Increase)? _stepKeys;
        private Int32 _steps;
        private Boolean _stopping;

        public KeySender(GimpPlugin plugin)
        {
            this._plugin = plugin;
            this._worker = new Thread(this.Run) { IsBackground = true, Name = "GIMP KeySender" };
            this._worker.Start();
        }

        // Positive steps send increase, negative steps decrease. Switching keys drops steps still owed to the old pair.
        public void EnqueueSteps(GimpAction decrease, GimpAction increase, Int32 steps)
        {
            lock (this._commands)
            {
                var keys = (decrease.Key, increase.Key);
                if (this._stepKeys != keys)
                {
                    this._stepKeys = keys;
                    this._steps = 0;
                }

                this._steps = Math.Clamp(this._steps + steps, -MaxPendingSteps, MaxPendingSteps);
                Monitor.Pulse(this._commands);
            }
        }

        // A command (e.g. a dial reset) cancels owed dial steps, so they can't undo it afterwards.
        public void EnqueueShortcut(GimpAction action)
        {
            lock (this._commands)
            {
                this._steps = 0;
                this._commands.Enqueue((action.Key, Stopwatch.GetTimestamp()));
                Monitor.Pulse(this._commands);
            }
        }

        public void Dispose()
        {
            lock (this._commands)
            {
                this._stopping = true;
                Monitor.Pulse(this._commands);
            }

            this._worker.Join(TimeSpan.FromSeconds(2));
        }

        private void Run()
        {
            while (true)
            {
                GimpKey? key;
                lock (this._commands)
                {
                    while (this._commands.Count == 0 && this._steps == 0 && !this._stopping)
                    {
                        Monitor.Wait(this._commands);
                    }

                    if (this._stopping)
                    {
                        break;
                    }

                    key = this.NextKey();
                }

                if (key is { } send)
                {
                    this.TrySend(send);
                }
            }
        }

        // Called under lock: a fresh queued command, else one dial step.
        private GimpKey? NextKey()
        {
            if (this._commands.TryDequeue(out var command))
            {
                return Stopwatch.GetElapsedTime(command.Timestamp) <= MaxCommandAge ? command.Key : null;
            }

            if (this._steps != 0 && this._stepKeys is { } keys)
            {
                var direction = Math.Sign(this._steps);
                this._steps -= direction;
                return direction > 0 ? keys.Increase : keys.Decrease;
            }

            return null;
        }

        // Checked per shortcut: GIMP may lose focus or a modifier may go down while a dial is still turning.
        private Boolean CanSend() =>
            this._plugin.RefreshInputStatus() && this._plugin.ClientApplication.IsActive() && !InputGuard.AnyModifierPressed();

        private void TrySend(GimpKey key)
        {
            try
            {
                if (this.CanSend())
                {
                    // ClientApplication.SendKeyboardShortcut maps keys by position (Ctrl+Z arrives as Ctrl+Y on QWERTZ);
                    // SendKeyboardEvent with the active layout sends the right key on every layout.
                    var keyboard = this._plugin.NativeApi.GetNativeKeyboard();
                    var layout = keyboard.GetActiveKeyboardLayout();
                    keyboard.SendKeyboardEvent(key.Key, key.Modifiers, layout, true);
                    keyboard.SendKeyboardEvent(key.Key, key.Modifiers, layout, false);
                    Thread.Sleep(ShortcutDelayMs);
                }
                else
                {
                    lock (this._commands)
                    {
                        this._steps = 0;
                    }
                }
            }
            catch (Exception ex)
            {
                PluginLog.Error(ex, "Failed to send shortcut to GIMP");
            }
        }
    }
}
