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
        int[] remaining;
        string lastRequest = "";
        public VendingInventory(VendingSettings settings, string path)
        {
            this.path = path; revision = settings.InventoryRevision;
            remaining = settings.Turns.Take(settings.NumMotors).ToArray();
            if (path != null && File.Exists(path))
            {
                var lines = File.ReadAllLines(path);
                if (lines.Length != 3 || !int.TryParse(lines[0], out int storedRevision)) throw new IOException("Invalid vending inventory file; staff recovery required.");
                if (storedRevision == revision)
                {
                    var values = lines[1].Split(',');
                    if (values.Length != settings.NumMotors) throw new IOException("Motor count changed; increment inventory_revision after restocking.");
                    remaining = values.Select(value => int.Parse(value, CultureInfo.InvariantCulture)).ToArray();
                    if (remaining.Any(value => value < 0)) throw new IOException("Invalid negative inventory.");
                    lastRequest = lines[2];
                }
            }
        }
        public int NextMotor()
        {
            for (int i = 0; i < remaining.Length; i++) if (remaining[i] > 0) return i + 1;
            return 0;
        }
        public int Remaining(int motor) { return remaining[motor - 1]; }
        public void Complete(string request, int motor)
        {
            if (request == lastRequest) return;
            if (motor != NextMotor() || remaining[motor - 1] <= 0) throw new InvalidOperationException("Inventory does not match completed motor.");
            var updated = (int[])remaining.Clone(); updated[motor - 1]--;
            if (path != null)
            {
                string temporary = path + ".new";
                using (var stream = new FileStream(temporary, FileMode.Create, FileAccess.Write, FileShare.None))
                using (var writer = new StreamWriter(stream))
                {
                    writer.WriteLine(revision.ToString(CultureInfo.InvariantCulture));
                    writer.WriteLine(string.Join(",", updated.Select(value => value.ToString(CultureInfo.InvariantCulture))));
                    writer.WriteLine(request); writer.Flush(); stream.Flush(true);
                }
                if (File.Exists(path)) File.Replace(temporary, path, null);
                else File.Move(temporary, path);
            }
            remaining = updated; lastRequest = request;
        }
    }
}
