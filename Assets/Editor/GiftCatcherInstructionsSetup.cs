using DDF.GiftCatcher;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class GiftCatcherInstructionsSetup
{
    static readonly Color Wine = new Color32(102, 9, 24, 255);
    static readonly Color Body = new Color32(66, 49, 44, 255);

    public static bool Configure(GiftCatcherGame game)
    {
        var root = game.instructions.transform;
        if (root.Find("Instructions Pearl Background")) return false;
        foreach (string name in new[] { "Brand Header Gold Border", "Brand Header", "Dubai Duty Free", "Game Name", "Instructions Card Gold Border", "Instructions Card" })
        { var child = root.Find(name); if (child) child.gameObject.SetActive(false); }

        var backdrop = Panel(root, "Instructions Pearl Background", 0, 0, 1080, 1920, new Color32(248, 243, 235, 255));
        backdrop.transform.SetAsFirstSibling();
        Panel(root, "Left Wine Edge", -520, 0, 40, 1920, Wine);
        Panel(root, "Right Wine Edge", 520, 0, 40, 1920, Wine);
        var gold = new Color32(198, 161, 91, 255);
        Panel(root, "Left Gold Line", -499, 0, 2, 1920, gold);
        Panel(root, "Right Gold Line", 499, 0, 2, 1920, gold);
        var logo = Rect(root, "Instructions DDF Logo", 0, 730, 320, 139.48f).gameObject.AddComponent<RawImage>();
        logo.texture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Media/UI/ddf-logo-black.png");
        logo.raycastTarget = false;
        Text(root, "Instructions Title", "HOW TO PLAY", 0, 520, 860, 90, 48, true, true);

        Row(root, "Catch gifts", "CATCH GIFTS", "Drag the bag left and right.\nCatch the falling gifts.", "UI/bag", 300, 130, 139);
        Row(root, "Avoid rocks", "AVOID ROCKS", "Each rock costs one life.\nMissed gifts cost nothing.", "Items/rock", 40, 140, 140);
        Row(root, "Win your surprise", "WIN YOUR SURPRISE", "Catch " + game.targetGifts + " gifts in " + game.roundSeconds + " seconds.\nYou have " + game.startingLives + " lives.", "Items/teddy", -220, 150, 150);

        var ready = game.readyButton;
        Place((RectTransform)ready.transform, 0, -620, 700, 110);
        ready.GetComponent<Image>().color = Wine;
        var outline = ready.GetComponent<Outline>();
        if (outline) outline.enabled = false;
        var label = ready.GetComponentInChildren<TMP_Text>(true);
        Place(label.rectTransform, 0, 0, 660, 95);
        label.text = "LET'S PLAY"; label.fontSize = 36; label.characterSpacing = 3;
        label.color = Color.white;
        ready.transform.SetAsLastSibling();
        return true;
    }

    static void Row(Transform parent, string name, string title, string copy, string texture, float y, float width, float height)
    {
        var art = parent.Find(name + " Art").GetComponent<RawImage>();
        art.texture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Media/" + texture + ".png");
        Place(art.rectTransform, -325, y, width, height);
        Text(parent, name + " Title", title, 95, y + 45, 560, 70, 34, true, false);
        Text(parent, name + " Copy", copy, 95, y - 35, 560, 110, 29, false, false);
    }

    static void Text(Transform parent, string name, string value, float x, float y, float width, float height, float size, bool heading, bool centred)
    {
        var text = parent.Find(name).GetComponent<TMP_Text>();
        Place(text.rectTransform, x, y, width, height);
        text.text = value; text.fontSize = size;
        text.fontStyle = heading ? FontStyles.Bold : FontStyles.Normal;
        text.characterSpacing = heading ? 1.5f : 0;
        text.color = heading ? Wine : Body;
        text.alignment = centred ? TextAlignmentOptions.Center : TextAlignmentOptions.MidlineLeft;
        text.raycastTarget = false;
    }

    static Image Panel(Transform parent, string name, float x, float y, float width, float height, Color color)
    {
        var image = Rect(parent, name, x, y, width, height).gameObject.AddComponent<Image>();
        image.color = color; image.raycastTarget = false; return image;
    }

    static RectTransform Rect(Transform parent, string name, float x, float y, float width, float height)
    {
        var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false); Place(rect, x, y, width, height); return rect;
    }

    static void Place(RectTransform rect, float x, float y, float width, float height)
    {
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
        rect.anchoredPosition = new Vector2(x, y); rect.sizeDelta = new Vector2(width, height);
    }
}
