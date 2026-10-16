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
        if (root.Find("Instructions Layout V2")) return false;
        Rect(root, "Instructions Layout V2", 0, 0, 0, 0);
        foreach (string name in new[] { "Brand Header Gold Border", "Brand Header", "Dubai Duty Free", "Game Name", "Instructions Card Gold Border", "Instructions Card" })
        { var child = root.Find(name); if (child) child.gameObject.SetActive(false); }

        var backdrop = Panel(root, "Instructions Pearl Background", 0, 0, 1080, 1920, new Color32(248, 243, 235, 255));
        backdrop.transform.SetAsFirstSibling();
        Panel(root, "Left Wine Edge", -530, 0, 20, 1920, Wine);
        Panel(root, "Right Wine Edge", 530, 0, 20, 1920, Wine);
        var gold = new Color32(198, 161, 91, 255);
        Panel(root, "Left Gold Line", -519, 0, 1, 1920, gold);
        Panel(root, "Right Gold Line", 519, 0, 1, 1920, gold);
        var logoRect = Rect(root, "Instructions DDF Logo", 0, 795, 270, 117.69f);
        var logo = logoRect.GetComponent<RawImage>();
        if (!logo) logo = logoRect.gameObject.AddComponent<RawImage>();
        logo.texture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Media/UI/ddf-logo-black.png");
        logo.raycastTarget = false;
        Text(root, "Instructions Title", "READY TO PLAY?", 0, 615, 880, 90, 54, true, true);
        Text(root, "Instructions Subtitle", "THREE SIMPLE STEPS. ONE LOVELY SURPRISE.", 0, 535, 880, 50, 23, false, true);
        Panel(root, "Instructions Heading Accent", 0, 475, 80, 3, gold);

        Row(root, "Catch gifts", "MOVE & COLLECT", "Slide the bag left or right\nto catch the falling gifts.", "UI/bag", 315, 145, 155, "01");
        Row(root, "Avoid rocks", "WATCH FOR ROCKS", "Each rock takes one life.\nMissed gifts cost nothing.", "Items/rock", 45, 145, 145, "02");
        Row(root, "Win your surprise", "UNLOCK YOUR SURPRISE", "Reach " + game.targetGifts + " gifts to unlock\nyour surprise.", "Items/teddy", -225, 165, 165, "03");
        Panel(root, "Round Details Rule", 0, -425, 780, 1, new Color32(220, 207, 185, 255));
        Text(root, "Round Details", game.targetGifts + " GIFTS     /     " + game.roundSeconds + "s     /     " + game.startingLives + " LIVES", 0, -495, 860, 70, 28, true, true);

        var ready = game.readyButton;
        Place((RectTransform)ready.transform, 0, -690, 800, 115);
        ready.GetComponent<Image>().color = Wine;
        var outline = ready.GetComponent<Outline>();
        if (outline) outline.enabled = false;
        var label = ready.GetComponentInChildren<TMP_Text>(true);
        Place(label.rectTransform, 0, 0, 760, 95);
        label.text = "LET'S PLAY"; label.fontSize = 36; label.characterSpacing = 3;
        label.color = Color.white;
        ready.transform.SetAsLastSibling();
        return true;
    }

    static void Row(Transform parent, string name, string title, string copy, string texture, float y, float width, float height, string number)
    {
        var card = Panel(parent, name + " Step Card", 0, y, 880, 235, Color.white);
        card.transform.SetSiblingIndex(1);
        Text(parent, name + " Step Number", number, -390, y + 76, 65, 40, 22, true, true);
        parent.Find(name + " Step Number").GetComponent<TMP_Text>().color = new Color32(160, 126, 65, 255);
        var art = parent.Find(name + " Art").GetComponent<RawImage>();
        art.texture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Media/" + texture + ".png");
        Place(art.rectTransform, -275, y - 6, width, height);
        art.raycastTarget = false;
        Text(parent, name + " Title", title, 105, y + 48, 530, 60, 30, true, false);
        Text(parent, name + " Copy", copy, 105, y - 32, 530, 105, 28, false, false);
    }

    static void Text(Transform parent, string name, string value, float x, float y, float width, float height, float size, bool heading, bool centred)
    {
        var child = parent.Find(name);
        var text = child ? child.GetComponent<TMP_Text>() : Rect(parent, name, x, y, width, height).gameObject.AddComponent<TextMeshProUGUI>();
        text.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
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
        var rect = Rect(parent, name, x, y, width, height);
        var image = rect.GetComponent<Image>();
        if (!image) image = rect.gameObject.AddComponent<Image>();
        image.color = color; image.raycastTarget = false; return image;
    }

    static RectTransform Rect(Transform parent, string name, float x, float y, float width, float height)
    {
        var existing = parent.Find(name);
        var rect = existing ? (RectTransform)existing : new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false); Place(rect, x, y, width, height); return rect;
    }

    static void Place(RectTransform rect, float x, float y, float width, float height)
    {
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
        rect.anchoredPosition = new Vector2(x, y); rect.sizeDelta = new Vector2(width, height);
    }
}
