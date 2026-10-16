using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Reflection;
using DDF.GiftCatcher;
using UnityEditor;
using UnityEngine;

public static class GiftCatcherVendingValidation
{
    [MenuItem("Gift Catcher/Validate Arduino Integration (No Hardware)")]
    public static void Validate()
    {
        string directory = Path.Combine(Path.GetTempPath(), "DDF-Vending-Validation-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        string journal = Path.Combine(directory, "pending.txt");
        try
        {
            for (int motor = 1; motor <= 4; motor++)
            {
                var protocol = new VendingProtocol(motor);
                protocol.Receive("Motor " + motor + " stopped after 3 seconds.");
                Check(protocol.State == VendingProtocol.Phase.Enabling, "Ignore stale stop reply");
                Check(protocol.Receive("Motor control enabled.") == motor.ToString(), "Choose configured motor");
                Check(protocol.Receive("Motor control enabled.") == null, "No duplicate motor command");
                protocol.Receive("Starting Motor " + motor);
                protocol.Receive("Motor " + (motor == 4 ? 1 : motor + 1) + " stopped after 3 seconds.");
                Check(protocol.State == VendingProtocol.Phase.Running, "Ignore another motor's stop");
                protocol.Receive("Motor " + motor + " stopped after 3 seconds.");
                Check(protocol.State == VendingProtocol.Phase.Complete, "Timer stop acknowledged");
            }
            var settings = new VendingSettings { Enabled = true, Motor = 2, StartupMs = 0, TimeoutMs = 6000 };
            var fake = new Fake("Motor control enabled.", "Motor control enabled.", "Starting Motor 2", "LOW signal detected. Delaying stop by 0.15 seconds...", "Motor 2 stopped due to LOW signal after 0.15s delay.");
            using (var service = new VendingDispenser(settings, journal, () => fake))
            {
                service.Begin("round-success"); service.Begin("duplicate");
                Check(Await(service).Kind == VendingDispenser.ResultKind.Completed, "Sensor stop success");
                Check(string.Join(",", fake.Writes) == "start,2,stop", "Exact commands, one motor request");
                Check(!File.Exists(journal), "Clear resolved journal");
            }
            fake = new Fake("Motor control enabled.", "Starting Motor 2");
            using (var service = new VendingDispenser(settings, journal, () => fake))
            {
                service.Begin("round-disconnect");
                Check(Await(service).Kind == VendingDispenser.ResultKind.Failed, "Disconnect produces failure");
                Check(service.RequiresRecovery && File.Exists(journal), "Persist ambiguous request");
                service.Begin("retry");
                Check(fake.Writes.Count == 2, "Never retry ambiguous motor request");
            }
            using (var service = new VendingDispenser(settings, journal, () => { throw new Exception("Must not open hardware"); }))
            {
                Check(service.RequiresRecovery, "Restart retains recovery lock"); service.Begin("restart");
                Check(!service.Busy, "Blocked request cannot start");
            }
            File.Delete(journal);
            settings.TimeoutMs = 0;
            fake = new Fake { WaitWhenEmpty = true };
            using (var service = new VendingDispenser(settings, journal, () => fake))
            {
                service.Begin("silent-board");
                Check(Await(service).Kind == VendingDispenser.ResultKind.Failed, "Silent board expires response deadline");
                Check(string.Join(",", fake.Writes) == "start", "No motor command before enable acknowledgement");
            }
            settings.TimeoutMs = 6000;
            settings.Enabled = false;
            using (var service = new VendingDispenser(settings, journal, () => { throw new Exception("Demo opened port"); }))
            { service.Begin("demo"); Check(Await(service).Kind == VendingDispenser.ResultKind.Demo, "Demo never opens hardware"); }
            bool rejected = false;
            try { VendingSettings.Parse("[vending]\nmotor=5"); } catch (FormatException) { rejected = true; }
            Check(rejected, "Reject invalid motor");
            ValidateInventory(directory);
            ValidateDebugSpace();
            ValidateResultGating(journal);
            Debug.Log("Arduino integration validation PASSED: protocol, failures, recovery, ordered inventory, persistence, refill, empty trays and debug dispensing. No hardware was accessed.");
            GiftCatcherValidation.Validate();
        }
        finally
        {
            if (File.Exists(journal)) File.Delete(journal);
            foreach (string file in Directory.GetFiles(directory)) File.Delete(file);
            Directory.Delete(directory);
        }
    }
    static void ValidateInventory(string directory)
    {
        string path = Path.Combine(directory, "stock.txt");
        var settings = VendingSettings.Parse("[general]\ndebug=1\n[vending]\nnum_motors=4\nmotor_1_turns=3\nmotor_2_turns=1\nmotor_3_turns=0\nmotor_4_turns=2\ninventory_revision=1");
        Check(settings.Debug && settings.NumMotors == 4, "Parse debug and tray count");
        var inventory = new VendingInventory(settings, path);
        int[] expected = { 1, 1, 1, 2, 4, 4 };
        for (int i = 0; i < expected.Length; i++)
        {
            Check(inventory.NextMotor() == expected[i], "Use trays in configured order");
            inventory.Complete("stock-" + i, expected[i]);
            inventory.Complete("stock-" + i, expected[i]); // duplicate completion must not decrement twice
            inventory = new VendingInventory(settings, path); // restart must retain remaining cycles
        }
        Check(inventory.NextMotor() == 0, "Exhausted inventory is empty");
        using (var service = new VendingDispenser(settings, Path.Combine(directory, "empty-pending.txt"), () => { throw new Exception("Empty tray opened a port"); }, inventory))
        { service.Begin("empty"); Check(Await(service).Kind == VendingDispenser.ResultKind.Empty, "Empty trays never send commands"); }
        settings.InventoryRevision++;
        inventory = new VendingInventory(settings, path);
        Check(inventory.NextMotor() == 1 && inventory.Remaining(1) == 3, "Explicit refill revision restores configured counts");
        settings.Enabled = true; settings.StartupMs = 0;
        var fake = new Fake("Motor control enabled.", "Starting Motor 1");
        using (var service = new VendingDispenser(settings, Path.Combine(directory, "failed-pending.txt"), () => fake, inventory))
        { service.Begin("failed-stock"); Check(Await(service).Kind == VendingDispenser.ResultKind.Failed && inventory.Remaining(1) == 3, "Failure does not consume confirmed inventory"); }
    }
    static void ValidateDebugSpace()
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
        var root = PrefabUtility.LoadPrefabContents("Assets/Prefabs/GiftCatcher.prefab");
        try
        {
            var game = root.GetComponent<GiftCatcherGame>();
            var settings = new VendingSettings { Enabled = false, Debug = false };
            var inventory = new VendingInventory(settings, null);
            typeof(GiftCatcherGame).GetField("vendingConfig", flags).SetValue(game, settings);
            typeof(GiftCatcherGame).GetField("inventory", flags).SetValue(game, inventory);
            game.DebugDispense(); Check(inventory.Remaining(1) == 3, "Debug disabled ignores Space action");
            settings.Debug = true;
            for (int i = 0; i < 3; i++) game.DebugDispense();
            Check(inventory.NextMotor() == 2 && inventory.Remaining(1) == 0, "Three debug requests use up tray one");
            game.DebugDispense(); Check(inventory.Remaining(2) == 2, "Next debug request uses tray two");
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }
    static void ValidateResultGating(string journal)
    {
        const BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Instance;
        var root = PrefabUtility.LoadPrefabContents("Assets/Prefabs/GiftCatcher.prefab");
        try
        {
            var game = root.GetComponent<GiftCatcherGame>();
            var type = typeof(GiftCatcherGame);
            type.GetField("vendingConfig", flags).SetValue(game, new VendingSettings { Enabled = false });
            type.GetField("inventory", flags).SetValue(game, new VendingInventory(new VendingSettings(), null));
            game.StartRound();
            type.GetMethod("Tick", flags).Invoke(game, new object[] { 3.5f });
            ((VendingDispenser)type.GetField("dispenser", flags).GetValue(game)).Dispose();
            var fake = new Fake { WaitWhenEmpty = true };
            var service = new VendingDispenser(new VendingSettings { Enabled = true, StartupMs = 0 }, journal, () => fake);
            type.GetField("dispenser", flags).SetValue(game, service);
            type.GetMethod("Finish", flags).Invoke(game, new object[] { true });
            type.GetMethod("Tick", flags).Invoke(game, new object[] { 60f });
            game.ShowWelcome();
            Check(game.result.activeSelf && !game.homeButton.interactable, "Pending request blocks auto/home reset");
            fake.WaitWhenEmpty = false; // Release the fake with a simulated disconnect.
            var clock = System.Diagnostics.Stopwatch.StartNew();
            while (clock.ElapsedMilliseconds < 2000 && (bool)type.GetField("dispensingPending", flags).GetValue(game))
            { type.GetMethod("PumpVending", flags).Invoke(game, null); Thread.Sleep(5); }
            Check(!(bool)type.GetField("dispensingPending", flags).GetValue(game), "Disconnect reaches result UI");
            game.StartRound(); game.ShowWelcome();
            Check(game.result.activeSelf && !game.homeButton.interactable, "Failure blocks new rounds and needs staff");
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }
    static VendingDispenser.Result Await(VendingDispenser service)
    {
        var clock = System.Diagnostics.Stopwatch.StartNew();
        while (clock.ElapsedMilliseconds < 2000)
        { if (service.TryTakeResult(out var result)) return result; Thread.Sleep(5); }
        throw new Exception("Fake serial worker did not finish.");
    }
    static void Check(bool condition, string reason) { if (!condition) throw new Exception("Vending validation failed: " + reason); }
    sealed class Fake : IVendingConnection
    {
        readonly Queue<string> replies;
        public readonly List<string> Writes = new List<string>();
        public volatile bool WaitWhenEmpty;
        public Fake(params string[] lines) { replies = new Queue<string>(lines); }
        public void Open() { }
        public void DiscardInput() { }
        public void WriteLine(string line) { Writes.Add(line); }
        public string ReadLine() { if (replies.Count == 0) { if (WaitWhenEmpty) { Thread.Sleep(5); return null; } throw new IOException("Simulated serial disconnect"); } return replies.Dequeue(); }
        public void Dispose() { }
    }
}
