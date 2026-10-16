using System;
using System.Globalization;
using System.IO;
using UnityEngine;

namespace DDF.GiftCatcher
{
    [Serializable]
    public sealed class OperatorConfiguration
    {
        public int version = 1;
        public int targetGifts = 10, startingLives = 3, roundSeconds = 60;
        public int resultSeconds = 12, instructionIdleSeconds = 30;
        public VendingSettings vending = new VendingSettings();

        public string Get(string key)
        {
            switch (key)
            {
                case "port": return vending.Port;
                case "target_gifts": return Number(targetGifts);
                case "starting_lives": return Number(startingLives);
                case "round_seconds": return Number(roundSeconds);
                case "result_seconds": return Number(resultSeconds);
                case "instruction_idle_seconds": return Number(instructionIdleSeconds);
                case "enabled": return vending.Enabled ? "1" : "0";
                case "debug": return vending.Debug ? "1" : "0";
                case "num_motors": return Number(vending.NumMotors);
                case "inventory_revision": return Number(vending.InventoryRevision);
                case "startup_delay_ms": return Number(vending.StartupMs);
                case "response_timeout_ms": return Number(vending.TimeoutMs);
                case "dtr": return vending.Dtr ? "1" : "0";
                case "rts": return vending.Rts ? "1" : "0";
                case "motor_1_turns": return Number(vending.Turns[0]);
                case "motor_2_turns": return Number(vending.Turns[1]);
                case "motor_3_turns": return Number(vending.Turns[2]);
                case "motor_4_turns": return Number(vending.Turns[3]);
                default: throw new ArgumentException("Unknown operator setting: " + key);
            }
        }

        public void Set(string key, string value)
        {
            if (key == "port") { vending.Port = value; return; }
            int number = int.Parse(value, NumberStyles.Integer, CultureInfo.InvariantCulture);
            switch (key)
            {
                case "target_gifts": targetGifts = number; break;
                case "starting_lives": startingLives = number; break;
                case "round_seconds": roundSeconds = number; break;
                case "result_seconds": resultSeconds = number; break;
                case "instruction_idle_seconds": instructionIdleSeconds = number; break;
                case "enabled": vending.Enabled = number == 1; break;
                case "debug": vending.Debug = number == 1; break;
                case "num_motors": vending.NumMotors = number; break;
                case "inventory_revision": vending.InventoryRevision = number; break;
                case "startup_delay_ms": vending.StartupMs = number; break;
                case "response_timeout_ms": vending.TimeoutMs = number; break;
                case "dtr": vending.Dtr = number == 1; break;
                case "rts": vending.Rts = number == 1; break;
                case "motor_1_turns": vending.Turns[0] = number; break;
                case "motor_2_turns": vending.Turns[1] = number; break;
                case "motor_3_turns": vending.Turns[2] = number; break;
                case "motor_4_turns": vending.Turns[3] = number; break;
                default: throw new ArgumentException("Unknown operator setting: " + key);
            }
        }

        public void Validate()
        {
            if (version != 1) throw new FormatException("Unsupported operator settings version.");
            Range(targetGifts, 1, 1000, "Gifts to win");
            Range(startingLives, 1, 100, "Starting lives");
            Range(roundSeconds, 1, 3600, "Round duration");
            Range(resultSeconds, 1, 600, "Result delay");
            Range(instructionIdleSeconds, 1, 600, "Instructions delay");
            if (vending == null) throw new FormatException("Vending settings are missing.");
            vending.Validate();
        }

        public void Apply(GiftCatcherGame game)
        {
            Validate();
            game.targetGifts = targetGifts; game.startingLives = startingLives;
            game.roundSeconds = roundSeconds; game.resultSeconds = resultSeconds;
            game.instructionIdleSeconds = instructionIdleSeconds;
        }

        static void Range(int value, int minimum, int maximum, string name)
        {
            if (value < minimum || value > maximum) throw new FormatException(name + " must be " + minimum + "–" + maximum + ".");
        }
        static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);
    }

    public static class OperatorSettings
    {
        const string Key = "DDF.GiftCatcher.OperatorSettings.v1";

        public static OperatorConfiguration Read(GiftCatcherGame game)
        {
            if (PlayerPrefs.HasKey(Key))
            {
                var saved = JsonUtility.FromJson<OperatorConfiguration>(PlayerPrefs.GetString(Key));
                if (saved == null) throw new FormatException("Saved operator settings are invalid.");
                saved.Validate(); return saved;
            }
            var initial = Defaults(game);
            // One-time upgrade preserves existing installations. INI is never read once preferences exist.
            string previous = Path.Combine(Application.persistentDataPath, "settings.ini");
            string packaged = Path.Combine(Application.streamingAssetsPath, "settings.ini");
            string legacy = File.Exists(previous) ? File.ReadAllText(previous) :
                File.Exists(packaged) ? File.ReadAllText(packaged) : game.vendingDefaults ? game.vendingDefaults.text : null;
            if (!string.IsNullOrEmpty(legacy))
            {
                initial.vending = VendingSettings.Parse(legacy);
                var ini = new INIParser(); ini.OpenFromString(legacy);
                foreach (string key in new[] { "target_gifts", "starting_lives", "round_seconds", "result_seconds", "instruction_idle_seconds" })
                    initial.Set(key, ini.ReadValue("game", key, initial.Get(key)));
            }
            Save(initial); return initial;
        }

        public static OperatorConfiguration Defaults(GiftCatcherGame game)
        {
            return new OperatorConfiguration
            {
                targetGifts = game.targetGifts, startingLives = game.startingLives,
                roundSeconds = Mathf.RoundToInt(game.roundSeconds), resultSeconds = Mathf.RoundToInt(game.resultSeconds),
                instructionIdleSeconds = Mathf.RoundToInt(game.instructionIdleSeconds)
            };
        }

        public static void Save(OperatorConfiguration configuration)
        {
            configuration.Validate();
            PlayerPrefs.SetString(Key, JsonUtility.ToJson(configuration));
            PlayerPrefs.Save();
        }
    }
}
