using DDF.GiftCatcher;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class GiftCatcherGameplaySetup
{
    static readonly Color Wine = new Color32(102, 9, 24, 255);
    static readonly Color Gold = new Color32(198, 161, 91, 255);
    static readonly Color Muted = new Color32(104, 89, 79, 255);

    public static bool Configure(GiftCatcherGame game)
    {
        var root = game.gameplay.transform;
        bool changed = false;
        foreach (string name in new[] { "Gameplay Header Wash", "Gameplay Footer Wash" })
        {
            var panel = root.Find(name);
            if (panel && panel.gameObject.activeSelf)
            {
                panel.gameObject.SetActive(false);
                changed = true;
            }
        }
        if (root.Find("Gameplay Skyline Background")) return changed;

        foreach (string name in new[] { "HUD", "HUD Gold Border", "Touch Hint", "Touch Hint Gold Border", "Gameplay Pearl Background", "Gameplay Left Wine Edge", "Gameplay Right Wine Edge" })
        {
            var old = root.Find(name);
            if (old) old.gameObject.SetActive(false);
        }
        var background = Rect(root, "Gameplay Skyline Background", 0, 0, 1080, 1920).gameObject.AddComponent<RawImage>();
        background.texture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Media/Backgrounds/background.png");
        background.raycastTarget = false;
        background.transform.SetAsFirstSibling();
        var logoRect = Rect(root, "Gameplay DDF Logo", 0, 835, 260, 113.33f);
        var logo = logoRect.GetComponent<RawImage>();
        if (!logo) logo = logoRect.gameObject.AddComponent<RawImage>();
        logo.texture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Media/UI/ddf-logo-black.png");
        logo.raycastTarget = false;

        Caption(root, "Gifts Caption", "COLLECTED", -320);
        Caption(root, "Time Caption", "TIME", 0);
        Caption(root, "Lives Caption", "LIVES", 320);
        Style(game.giftsLabel, -320, 685, 280, 80, 52, Wine, FontStyles.Normal);
        Style(game.timeLabel, 0, 685, 280, 80, 58, Wine, FontStyles.Normal);
        Style(game.livesLabel, 320, 685, 280, 80, 52, Wine, FontStyles.Normal);
        game.livesLabel.text = game.startingLives.ToString();

        var track = root.Find("Progress Track").GetComponent<Image>();
        Place(track.rectTransform, -320, 630, 210, 3);
        track.color = new Color32(225, 215, 200, 255);
        track.raycastTarget = false;
        Place(game.progress.rectTransform, -320, 630, 210, 3);
        game.progress.color = Gold;
        game.progress.raycastTarget = false;

        // Leave the playfield and bag dimensions intact: their bounds drive input and catches.
        Panel(root, "Gameplay Footer Accent", 0, -700, 90, 3, Gold);
        var hint = root.Find("Touch Hint Text").GetComponent<TMP_Text>();
        Style(hint, 0, -770, 900, 55, 27, Wine, FontStyles.Normal);
        hint.text = "DRAG THE BAG TO COLLECT";
        Style(root.Find("Target Hint").GetComponent<TMP_Text>(), 0, -825, 900, 55, 24, Muted, FontStyles.Normal);
        Style(game.feedbackLabel, 0, -300, 850, 80, 32, Wine, FontStyles.Normal);
        Style(game.countdownLabel, 0, 200, 900, 260, 150, Wine, FontStyles.Normal);
        return true;
    }

    static void Caption(Transform root, string name, string value, float x)
    {
        var text = root.Find(name).GetComponent<TMP_Text>();
        Style(text, x, 750, 280, 45, 23, Muted, FontStyles.Normal);
        text.text = value;
        text.characterSpacing = 3;
    }
    static void Style(TMP_Text text, float x, float y, float w, float h, float size, Color color, FontStyles style)
    {
        Place(text.rectTransform, x, y, w, h);
        text.fontSize = size; text.fontStyle = style; text.color = color;
        text.characterSpacing = 0; text.alignment = TextAlignmentOptions.Center;
        text.raycastTarget = false;
    }
    static Image Panel(Transform parent, string name, float x, float y, float w, float h, Color color)
    {
        var rect = Rect(parent, name, x, y, w, h);
        var image = rect.GetComponent<Image>();
        if (!image) image = rect.gameObject.AddComponent<Image>();
        image.color = color; image.raycastTarget = false; return image;
    }
    static RectTransform Rect(Transform parent, string name, float x, float y, float w, float h)
    {
        var existing = parent.Find(name);
        var rect = existing ? (RectTransform)existing : new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false); Place(rect, x, y, w, h); return rect;
    }
    static void Place(RectTransform rect, float x, float y, float w, float h)
    {
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
        rect.anchoredPosition = new Vector2(x, y); rect.sizeDelta = new Vector2(w, h);
    }
}
