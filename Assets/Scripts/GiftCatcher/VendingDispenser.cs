using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
using System.IO.Ports;
#endif

namespace DDF.GiftCatcher
{
    [Serializable]
    public sealed class VendingSettings
    {
        public bool Enabled, Dtr = true, Rts;
        public bool Debug;
        public int NumMotors = 4, InventoryRevision = 1;
        public int[] Turns = { 3, 3, 3, 3 };
        public string Port = "COM3";
        public int Motor = 1, StartupMs = 2000, TimeoutMs = 6000;

        // Select a mode before loading inventory; a missing port must never consume real stock.
        // Keep that choice for the session. Never turn a failed in-flight motor request into success.
        public VendingSettings ForAvailableHardware(out string mode)
        {
            var effective = (VendingSettings)MemberwiseClone();
            effective.Turns = (int[])Turns.Clone();
            mode = "DEMO — physical vending disabled";
            if (!Enabled) return effective;
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
            foreach (string available in SerialPort.GetPortNames())
                if (string.Equals(available, Port, StringComparison.OrdinalIgnoreCase))
                { mode = "Hardware ready (" + Port + ")"; return effective; }
            mode = "SIMULATION — " + Port + " not connected";
#else
            mode = "SIMULATION — serial hardware unavailable on this device";
#endif
            effective.Enabled = false;
            return effective;
        }

        public static VendingSettings Parse(string text)
        {
            var ini = new INIParser(); ini.OpenFromString(text);
            var settings = new VendingSettings
            {
                Enabled = ini.ReadValue("vending", "enabled", 0) == 1,
                Port = ini.ReadValue("vending", "port", "COM3"),
                Motor = ini.ReadValue("vending", "motor", 1),
                StartupMs = ini.ReadValue("vending", "startup_delay_ms", 2000),
                TimeoutMs = ini.ReadValue("vending", "response_timeout_ms", 6000),
                Dtr = ini.ReadValue("vending", "dtr", 1) == 1,
                Rts = ini.ReadValue("vending", "rts", 0) == 1
            };
            settings.Debug = ini.ReadValue("general", "debug", 0) == 1;
            settings.NumMotors = ini.ReadValue("vending", "num_motors", 4);
            settings.InventoryRevision = ini.ReadValue("vending", "inventory_revision", 1);
            if (settings.NumMotors < 1 || settings.NumMotors > 4 || settings.InventoryRevision < 1)
                throw new FormatException("num_motors must be 1–4; inventory_revision must be positive.");
            for (int i = 0; i < 4; i++)
            {
                settings.Turns[i] = ini.ReadValue("vending", "motor_" + (i + 1) + "_turns", 3);
                if (settings.Turns[i] < 0 || settings.Turns[i] > 100000) throw new FormatException("Invalid per-motor turn count.");
            }
            if (settings.Motor < 1 || settings.Motor > 4) throw new FormatException("vending.motor must be 1–4.");
            if (string.IsNullOrWhiteSpace(settings.Port)) throw new FormatException("vending.port is empty.");
            if (settings.StartupMs < 0 || settings.StartupMs > 10000 || settings.TimeoutMs < 4000 || settings.TimeoutMs > 60000)
                throw new FormatException("Invalid vending startup/response timeout.");
            return settings;
        }

        public void Validate()
        {
            if (NumMotors < 1 || NumMotors > 4 || InventoryRevision < 1)
                throw new FormatException("Active trays must be 1–4; inventory revision must be positive.");
            if (Turns == null || Turns.Length != 4) throw new FormatException("Four tray counts are required.");
            foreach (int count in Turns)
                if (count < 0 || count > 100000) throw new FormatException("Tray counts must be 0–100000.");
            if (Motor < 1 || Motor > 4) throw new FormatException("Fallback motor must be 1–4.");
            if (string.IsNullOrWhiteSpace(Port) || Port.Length > 32 || Port.IndexOfAny(new[] { '\r', '\n' }) >= 0)
                throw new FormatException("Enter a valid COM port name.");
            if (StartupMs < 0 || StartupMs > 10000 || TimeoutMs < 4000 || TimeoutMs > 60000)
                throw new FormatException("Startup delay must be 0–10000 ms; reply timeout must be 4000–60000 ms.");
        }
    }

    // Exact replies from VendingMachine_Targetted_1-4.ino. No commands are retried.
    public sealed class VendingProtocol
    {
        public enum Phase { Enabling, AwaitingMotor, Running, Complete }
        public Phase State { get; private set; }
        readonly int motor;
        public bool SensorStop { get; private set; }
        public VendingProtocol(int motor) { if (motor < 1 || motor > 4) throw new ArgumentOutOfRangeException(nameof(motor)); this.motor = motor; }
        public string Receive(string line)
        {
            line = line.Trim();
            if (State == Phase.Enabling && line == "Motor control enabled.")
            { State = Phase.AwaitingMotor; return motor.ToString(System.Globalization.CultureInfo.InvariantCulture); }
            if (State == Phase.AwaitingMotor && line == "Starting Motor " + motor) State = Phase.Running;
            else if (State == Phase.Running)
            {
                SensorStop = line == "Motor " + motor + " stopped due to LOW signal after 0.15s delay.";
                if (SensorStop || line == "Motor " + motor + " stopped after 3 seconds.") State = Phase.Complete;
            }
            return null;
        }
    }

    public interface IVendingConnection : IDisposable
    {
        void Open();
        void DiscardInput();
        void WriteLine(string line);
        string ReadLine(); // null on read timeout
    }

    public sealed class VendingDispenser : IDisposable
    {
        public enum ResultKind { Demo, Completed, Failed, Empty }
        public sealed class Result { public ResultKind Kind; public string Detail; }
        public bool Busy { get; private set; }
        public bool RequiresRecovery { get; private set; }
        readonly VendingSettings settings;
        readonly VendingInventory inventory;
        readonly string journal;
        readonly Func<IVendingConnection> factory;
        readonly ConcurrentQueue<Result> results = new ConcurrentQueue<Result>();
        readonly CancellationTokenSource cancellation = new CancellationTokenSource();
        Thread worker;
        bool used;
        public VendingDispenser(VendingSettings settings, string journal, Func<IVendingConnection> factory = null, VendingInventory inventory = null)
        {
            this.settings = settings; this.journal = journal;
            this.inventory = inventory;
            this.factory = factory ?? (() => new DesktopSerialConnection(settings));
            RequiresRecovery = settings.Enabled && File.Exists(journal);
        }
        public bool TryTakeResult(out Result result)
        {
            if (!results.TryDequeue(out result)) return false;
            Busy = false;
            if (result.Kind == ResultKind.Failed || result.Kind == ResultKind.Empty) RequiresRecovery = true;
            return true;
        }
        public void Begin(string roundId)
        {
            if (Busy || RequiresRecovery) return;
            // Each service instance permits one request; the game disposes it before the next round.
            if (used) return;
            used = true; Busy = true;
            int motor = inventory == null ? settings.Motor : inventory.NextMotor();
            if (motor == 0) { results.Enqueue(new Result { Kind = ResultKind.Empty, Detail = "All trays are empty." }); return; }
            if (!settings.Enabled)
            {
                try { inventory?.Complete(roundId, motor); results.Enqueue(new Result { Kind = ResultKind.Demo, Detail = "motor=" + motor }); }
                catch (Exception exception) { results.Enqueue(new Result { Kind = ResultKind.Failed, Detail = exception.Message }); }
                return;
            }
            worker = new Thread(() => Run(roundId, motor)) { IsBackground = true, Name = "Gift Catcher serial" };
            worker.Start();
        }
        void Run(string roundId, int motor)
        {
            try
            {
                using (var connection = factory())
                {
                    connection.Open();
                    if (cancellation.Token.WaitHandle.WaitOne(settings.StartupMs)) throw new OperationCanceledException();
                    connection.DiscardInput();
                    connection.WriteLine("start");
                    var protocol = new VendingProtocol(motor);
                    var clock = System.Diagnostics.Stopwatch.StartNew();
                    while (protocol.State != VendingProtocol.Phase.Complete)
                    {
                        cancellation.Token.ThrowIfCancellationRequested();
                        if (clock.ElapsedMilliseconds > settings.TimeoutMs) throw new TimeoutException("Arduino acknowledgement timed out; no automatic retry.");
                        string line = connection.ReadLine();
                        if (line == null) continue;
                        string command = protocol.Receive(line);
                        if (command != null)
                        {
                            cancellation.Token.ThrowIfCancellationRequested();
                            // Persist BEFORE the motor command: a lost response/restart must not resend it.
                            using (var stream = new FileStream(journal, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                            using (var writer = new StreamWriter(stream)) { writer.WriteLine(roundId); writer.WriteLine(motor); writer.Flush(); stream.Flush(true); }
                            cancellation.Token.ThrowIfCancellationRequested();
                            connection.WriteLine(command);
                        }
                    }
                    // stop disables new commands; it is NOT an emergency motor stop in this firmware.
                    try { connection.WriteLine("stop"); } catch { /* Motor stop was already acknowledged. */ }
                    inventory?.Complete(roundId, motor);
                    File.Delete(journal);
                    results.Enqueue(new Result { Kind = ResultKind.Completed, Detail = "motor=" + motor + " " + (protocol.SensorStop ? "sensor-stop" : "firmware-timer-stop") });
                }
            }
            catch (Exception exception) { results.Enqueue(new Result { Kind = ResultKind.Failed, Detail = exception.Message }); }
        }
        public void Dispose()
        {
            cancellation.Cancel();
            if (worker != null && worker.IsAlive) worker.Join(1000);
            // An unresolved journal remains for staff inspection; closing a port does not confirm delivery.
            if (worker == null || !worker.IsAlive) cancellation.Dispose();
        }
    }

    sealed class DesktopSerialConnection : IVendingConnection
    {
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
        readonly SerialPort port;
        string partial = "";
        public DesktopSerialConnection(VendingSettings settings)
        {
            port = new SerialPort(settings.Port, 9600, Parity.None, 8, StopBits.One)
            { NewLine = "\n", DtrEnable = settings.Dtr, RtsEnable = settings.Rts, ReadTimeout = 100, WriteTimeout = 1000, Handshake = Handshake.None };
        }
        public void Open() { port.Open(); }
        public void DiscardInput() { port.DiscardInBuffer(); partial = ""; }
        public void WriteLine(string line) { port.WriteLine(line); }
        public string ReadLine()
        {
            try
            {
                // Preserve partial lines across timeouts; Arduino replies end with CRLF.
                while (true)
                {
                    int c = port.ReadChar();
                    if (c == '\n') { string line = partial.TrimEnd('\r'); partial = ""; return line; }
                    if (partial.Length >= 1024) throw new IOException("Serial reply exceeded 1024 characters.");
                    partial += (char)c;
                }
            }
            catch (TimeoutException) { return null; }
        }
        public void Dispose() { port.Dispose(); }
#else
        public DesktopSerialConnection(VendingSettings settings) { }
        public void Open() { throw new PlatformNotSupportedException("Arduino COM serial transport requires Windows. Android needs a native USB serial bridge."); }
        public void DiscardInput() { }
        public void WriteLine(string line) { }
        public string ReadLine() { return null; }
        public void Dispose() { }
#endif
    }
}
