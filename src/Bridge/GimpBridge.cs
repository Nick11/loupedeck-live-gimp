namespace Loupedeck.GimpPlugin
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Net.Sockets;
    using System.Text;
    using System.Threading;

    // Client for the GIMP bridge plug-in (gimp-plugin/loupedeck-bridge): TCP on 127.0.0.1, authenticated with the token GIMP
    // writes on every start, one text line per request and reply. All I/O runs on one worker thread; dial steps per value
    // are summed until sent, so fast turns never queue up. Values shown on the device are polled every second, so the display
    // follows changes made in GIMP and shows "–" as soon as GIMP is gone.
    internal sealed class GimpBridge : IDisposable
    {
        private const Int32 Port = 47862;
        private const String Protocol = "lgb1";
        private const Int32 MaxLine = 128;
        private const Int32 MaxPendingSteps = 50;
        private static readonly TimeSpan ConnectTimeout = TimeSpan.FromMilliseconds(300);
        private static readonly TimeSpan ReplyTimeout = TimeSpan.FromSeconds(1);
        private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(2);
        private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(1);
        private static readonly TimeSpan WatchTimeout = TimeSpan.FromSeconds(5);

        private readonly Object _lock = new();
        private readonly Dictionary<String, Int32> _pendingSteps = new();
        private readonly HashSet<String> _pendingResets = new();
        private readonly HashSet<String> _pendingReads = new();
        private readonly Dictionary<String, Double> _values = new();
        private readonly Dictionary<String, Int64> _watched = new();
        private readonly Thread _worker;
        private TcpClient _client;
        private NetworkStream _stream;
        private Int64 _lastFailure;
        private Boolean _stopping;

        public GimpBridge()
        {
            this._worker = new Thread(this.Run) { IsBackground = true, Name = "GIMP Bridge" };
            this._worker.Start();
        }

        // Raised on the worker thread when a value changed or became unknown. Static, so actions can subscribe before the plugin loads.
        public static event Action<String> ValueChanged;

        public void Step(String name, Int32 steps)
        {
            lock (this._lock)
            {
                this._pendingSteps[name] = Math.Clamp(this._pendingSteps.GetValueOrDefault(name) + steps, -MaxPendingSteps, MaxPendingSteps);
                Monitor.Pulse(this._lock);
            }
        }

        // Back to GIMP's default; drops steps still owed to the value.
        public void Reset(String name)
        {
            lock (this._lock)
            {
                this._pendingSteps.Remove(name);
                this._pendingResets.Add(name);
                Monitor.Pulse(this._lock);
            }
        }

        // Last value reported by GIMP, null while GIMP or the bridge is not reachable. Keeps the value polled while it is shown.
        public Double? Get(String name)
        {
            lock (this._lock)
            {
                if (!this._watched.ContainsKey(name))
                {
                    this._pendingReads.Add(name);
                    Monitor.Pulse(this._lock);
                }

                this._watched[name] = Stopwatch.GetTimestamp();
                return this._values.TryGetValue(name, out var value) ? value : null;
            }
        }

        public void Dispose()
        {
            lock (this._lock)
            {
                this._stopping = true;
                Monitor.Pulse(this._lock);
            }

            this._worker.Join(TimeSpan.FromSeconds(2));
        }

        private void Run()
        {
            while (this.NextRequest() is { } request)
            {
                try
                {
                    this.Send(request.Name, request.Line);
                }
                catch (Exception ex)
                {
                    // Nothing may escape this thread: an unhandled exception would take down the Logi Plugin Service.
                    PluginLog.Error(ex, "GIMP bridge request failed");
                    this.Disconnect();
                }
            }

            this.Disconnect();
        }

        // Blocks until there is work; null when stopping. Resets and steps first (they also return the new value), then reads.
        private (String Name, String Line)? NextRequest()
        {
            lock (this._lock)
            {
                while (!this._stopping)
                {
                    if (this._pendingResets.FirstOrDefault() is { } reset)
                    {
                        this._pendingResets.Remove(reset);
                        this._pendingReads.Remove(reset);
                        return (reset, $"{reset} reset");
                    }

                    if (this._pendingSteps.FirstOrDefault(s => s.Value != 0) is { Key: { } name, Value: var steps })
                    {
                        this._pendingSteps.Remove(name);
                        this._pendingReads.Remove(name);
                        return (name, $"{name} {steps.ToString("+0;-0", CultureInfo.InvariantCulture)}");
                    }

                    if (this._pendingReads.FirstOrDefault() is { } read)
                    {
                        this._pendingReads.Remove(read);
                        return (read, $"{read} ?");
                    }

                    if (!Monitor.Wait(this._lock, PollInterval))
                    {
                        this.QueuePolls();
                    }
                }

                return null;
            }
        }

        // Called under lock: re-read values shown recently, forget the others.
        private void QueuePolls()
        {
            foreach (var (name, lastShown) in this._watched.ToList())
            {
                if (Stopwatch.GetElapsedTime(lastShown) > WatchTimeout)
                {
                    this._watched.Remove(name);
                }
                else
                {
                    this._pendingReads.Add(name);
                }
            }
        }

        private void Send(String name, String line)
        {
            if (!this.EnsureConnected())
            {
                return;
            }

            try
            {
                this.WriteLine(line);
                if (!TryParseReply(this.ReadLine(), name, out var value))
                {
                    PluginLog.Warning($"GIMP bridge: unexpected reply to '{line}'");
                    this.Disconnect();
                    return;
                }

                Boolean changed;
                lock (this._lock)
                {
                    changed = !this._values.TryGetValue(name, out var previous) || previous != value;
                    this._values[name] = value;
                }

                if (changed)
                {
                    Notify(name);
                }
            }
            catch (IOException ex)
            {
                PluginLog.Warning($"GIMP bridge connection lost: {ex.Message}");
                this.Disconnect();
            }
        }

        // "<name> <finite number>" for the requested name only.
        private static Boolean TryParseReply(String reply, String name, out Double value)
        {
            value = 0;
            var parts = reply.Split(' ');
            return parts.Length == 2 && parts[0] == name
                && Double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out value)
                && Double.IsFinite(value);
        }

        // GIMP not running, extension not installed or token missing: retry at most every RetryDelay.
        private Boolean EnsureConnected()
        {
            if (this._client != null)
            {
                return true;
            }

            if (this._lastFailure != 0 && Stopwatch.GetElapsedTime(this._lastFailure) < RetryDelay)
            {
                return false;
            }

            var firstFailure = this._lastFailure == 0;
            var client = new TcpClient { NoDelay = true };
            try
            {
                var token = GimpBridgeToken.Read();
                if (!client.ConnectAsync("127.0.0.1", Port).Wait(ConnectTimeout))
                {
                    throw new IOException("connect timed out");
                }

                this._client = client;
                this._stream = client.GetStream();
                this.WriteLine($"{Protocol} {token}");
                this._lastFailure = 0;
                PluginLog.Info("GIMP bridge connected");
                return true;
            }
            catch (Exception ex) when (ex is IOException or SocketException or AggregateException or UnauthorizedAccessException)
            {
                if (firstFailure)
                {
                    PluginLog.Info($"GIMP bridge not available (GIMP not running or extension not installed): {ex.GetBaseException().Message}");
                }

                client.Dispose();
                this.Disconnect();
                return false;
            }
        }

        private void WriteLine(String line)
        {
            this._stream.WriteTimeout = (Int32)ReplyTimeout.TotalMilliseconds;
            this._stream.Write(Encoding.ASCII.GetBytes(line + "\n"));
        }

        // At most MaxLine bytes within ReplyTimeout, so a misbehaving peer can neither flood nor stall us.
        private String ReadLine()
        {
            var deadline = Stopwatch.GetTimestamp() + (Int64)(ReplyTimeout.TotalSeconds * Stopwatch.Frequency);
            var line = new List<Byte>(32);
            while (true)
            {
                var remainingMs = (deadline - Stopwatch.GetTimestamp()) * 1000 / Stopwatch.Frequency;
                if (remainingMs <= 0 || line.Count > MaxLine)
                {
                    throw new IOException("no valid reply");
                }

                this._stream.ReadTimeout = (Int32)remainingMs;
                var b = this._stream.ReadByte();
                if (b < 0)
                {
                    throw new IOException("connection closed by GIMP");
                }

                if (b == '\n')
                {
                    return Encoding.ASCII.GetString(line.ToArray());
                }

                line.Add((Byte)b);
            }
        }

        // Also forgets all values, so the device shows "–" instead of stale ones.
        private void Disconnect()
        {
            this._client?.Dispose();
            this._client = null;
            this._stream = null;
            this._lastFailure = Stopwatch.GetTimestamp();

            List<String> forgotten;
            lock (this._lock)
            {
                forgotten = this._values.Keys.ToList();
                this._values.Clear();
            }

            forgotten.ForEach(Notify);
        }

        private static void Notify(String name)
        {
            try
            {
                ValueChanged?.Invoke(name);
            }
            catch (Exception ex)
            {
                PluginLog.Error(ex, "Updating the device failed");
            }
        }
    }
}
