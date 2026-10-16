using System;
using System.Globalization;
using System.IO;
using System.Linq;

namespace DDF.GiftCatcher
{
    // Counts acknowledged cycles, not sensed gifts or measured physical revolutions.
    public sealed class VendingInventory
    {
        readonly string path;
        readonly int revision;
        int[] capacity;
        int[] remaining;
        string lastRequest = "";
        public VendingInventory(VendingSettings settings, string path)
        {
            this.path = path; revision = settings.InventoryRevision;
            capacity = settings.Turns.Take(settings.NumMotors).ToArray();
            remaining = (int[])capacity.Clone();
            if (path != null && File.Exists(path))
            {
                var lines = File.ReadAllLines(path);
                if ((lines.Length != 3 && lines.Length != 4) || !int.TryParse(lines[0], out int storedRevision)) throw new IOException("Invalid vending inventory file; staff recovery required.");
                if (storedRevision == revision)
                {
                    var values = lines[1].Split(',');
                    if (values.Length != settings.NumMotors) throw new IOException("Tray count changed; fill the coils and press Machine Refilled.");
                    remaining = values.Select(value => int.Parse(value, CultureInfo.InvariantCulture)).ToArray();
                    if (remaining.Any(value => value < 0)) throw new IOException("Invalid negative inventory.");
                    lastRequest = lines[2];
                    // Original three-line saves retain their remaining counts during this upgrade.
                    capacity = lines.Length == 4 ? lines[3].Split(',').Select(value => int.Parse(value, CultureInfo.InvariantCulture)).ToArray() :
                        capacity.Select((value, index) => Math.Max(value, remaining[index])).ToArray();
                    if (capacity.Length != remaining.Length || capacity.Where((value, index) => value < remaining[index] || value > 100000).Any())
                        throw new IOException("Invalid saved tray capacity; staff recovery required.");
                }
            }
            // Persist a refill immediately, even if the app closes before the next dispense.
            Persist(remaining, lastRequest);
        }
        public int NextMotor()
        {
            for (int i = 0; i < remaining.Length; i++) if (remaining[i] > 0) return i + 1;
            return 0;
        }
        public int Remaining(int motor) { return remaining[motor - 1]; }
        public int Capacity(int motor) { return capacity[motor - 1]; }
        public int CompletedTurns(int motor) { return capacity[motor - 1] - remaining[motor - 1]; }
        public int NextTurn() { int motor = NextMotor(); return motor == 0 ? 0 : CompletedTurns(motor) + 1; }
        public void Complete(string request, int motor)
        {
            if (request == lastRequest) return;
            if (motor != NextMotor() || remaining[motor - 1] <= 0) throw new InvalidOperationException("Inventory does not match completed motor.");
            var updated = (int[])remaining.Clone(); updated[motor - 1]--;
            Persist(updated, request);
            remaining = updated; lastRequest = request;
        }

        void Persist(int[] counts, string request)
        {
            if (path != null)
            {
                string temporary = path + ".new";
                using (var stream = new FileStream(temporary, FileMode.Create, FileAccess.Write, FileShare.None))
                using (var writer = new StreamWriter(stream))
                {
                    writer.WriteLine(revision.ToString(CultureInfo.InvariantCulture));
                    writer.WriteLine(string.Join(",", counts.Select(value => value.ToString(CultureInfo.InvariantCulture))));
                    writer.WriteLine(request);
                    writer.WriteLine(string.Join(",", capacity.Select(value => value.ToString(CultureInfo.InvariantCulture))));
                    writer.Flush(); stream.Flush(true);
                }
                if (File.Exists(path)) File.Replace(temporary, path, null);
                else File.Move(temporary, path);
            }
        }
    }
}
