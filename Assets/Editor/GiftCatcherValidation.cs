using System;
using System.Reflection;
using DDF.GiftCatcher;
using UnityEditor;
using UnityEngine;

// Exercise the saved prefab in an isolated editor scene, without entering Play mode or touching Main.
public static class GiftCatcherValidation
{
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    [InitializeOnLoadMethod]
    static void OnImport()
    {
        EditorApplication.delayCall += () =>
        {
            if (SessionState.GetBool("DDF.GiftCatcher.ValidatedV1", false) || EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (!AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/GiftCatcher.prefab")) return;
            SessionState.SetBool("DDF.GiftCatcher.ValidatedV1", true);
            Validate();
        };
    }

    [MenuItem("Gift Catcher/Validate Round Rules")]
    public static void Validate()
    {
        var root = PrefabUtility.LoadPrefabContents("Assets/Prefabs/GiftCatcher.prefab");
        int dispensingLogs = 0;
        Application.LogCallback logger = (message, trace, kind) => { if (message == "dispensing gift") dispensingLogs++; };
        Application.logMessageReceived += logger;
        try
        {
            var game = root.GetComponent<GiftCatcherGame>();
            // Rule checks must never actuate hardware, even when the operator enabled settings.ini.
            typeof(GiftCatcherGame).GetField("vendingConfig", Private).SetValue(game, new VendingSettings { Enabled = false });
            typeof(GiftCatcherGame).GetField("inventory", Private).SetValue(game, new VendingInventory(new VendingSettings { Turns = new[] { 1000, 1000, 1000, 1000 } }, null));
            Assert(game.itemPool.Length == 20 && game.giftTextures.Length == 8, "Authored artwork and pool");
            Begin(game);
            Assert(game.timeLabel.text == "1:00" && game.giftsLabel.text == "0 / 10", "Initial round HUD");
            game.MoveBag(10000);
            Assert(game.bag.anchoredPosition.x <= (game.playfield.rect.width - game.bag.sizeDelta.x) / 2, "Bag right edge");
            game.MoveBag(-10000);
            Assert(game.bag.anchoredPosition.x >= -(game.playfield.rect.width - game.bag.sizeDelta.x) / 2, "Bag left edge");
            game.MoveBag(0);
            Invoke(game, "PauseRound"); Tick(game, 5);
            Assert(game.timeLabel.text == "1:00" && game.pause.activeSelf, "Pause preserves timer");
            game.Resume();
            // A missed gift must not consume a life.
            var missed = game.itemPool[0]; missed.isRock = false;
            missed.rect.anchoredPosition = new Vector2(0, -1000); missed.gameObject.SetActive(true); Tick(game, .05f);
            Assert(game.livesLabel.text == "3 / 3" && game.giftsLabel.text == "0 / 10", "Missed gifts preserve lives");
            for (int i = 0; i < 10; i++) Catch(game, false);
            Assert(game.result.activeSelf && game.resultTitle.text == "YOU WON!", "Ten catches win immediately");
            Assert(dispensingLogs == 1, "One dispensing log per winning round");
            Tick(game, game.resultSeconds + 1);
            Assert(game.welcome.activeSelf && !game.result.activeSelf, "Result returns to welcome");
            Begin(game);
            for (int i = 0; i < 3; i++) Catch(game, true);
            Assert(game.result.activeSelf && game.resultTitle.text == "NICE TRY!", "Three rocks end round");
            Assert(dispensingLogs == 1, "Losing round never dispenses");
            Begin(game); Tick(game, 60);
            Assert(game.result.activeSelf && game.resultBody.text.StartsWith("Time is up!"), "Sixty-second timeout");
            Assert(dispensingLogs == 1, "Timeout never dispenses");
            game.ShowInstructions(); Tick(game, 31);
            Assert(game.welcome.activeSelf, "Abandoned instructions reset");
            Debug.Log("Gift Catcher validation PASSED: authored pool, HUD, drag bounds, pause, missed gifts, 10-gift win, exactly-once dispensing log, 3-rock loss, 60-second timeout and exhibition resets.");
        }
        finally
        {
            Application.logMessageReceived -= logger;
            PrefabUtility.UnloadPrefabContents(root);
        }
    }
    static void Begin(GiftCatcherGame game)
    {
        game.StartRound(); Tick(game, 3.5f);
        typeof(GiftCatcherGame).GetField("nextSpawn", Private).SetValue(game, 1000f);
    }
    static void Catch(GiftCatcherGame game, bool rock)
    {
        var item = game.itemPool[0]; item.isRock = rock; item.bottomOffset = 0; item.catchHalfWidth = 20;
        float catchY = game.bag.anchoredPosition.y + game.bag.sizeDelta.y * .36f;
        item.rect.anchoredPosition = new Vector2(game.bag.anchoredPosition.x, catchY + 8);
        item.gameObject.SetActive(true); Tick(game, .05f);
    }
    static void Tick(GiftCatcherGame game, float dt) { Invoke(game, "Tick", dt); }
    static void Invoke(GiftCatcherGame game, string name, params object[] arguments) { typeof(GiftCatcherGame).GetMethod(name, Private).Invoke(game, arguments); }
    static void Assert(bool condition, string message) { if (!condition) throw new InvalidOperationException("Gift Catcher validation failed: " + message); }
}
