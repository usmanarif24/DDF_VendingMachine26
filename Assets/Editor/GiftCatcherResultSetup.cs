using DDF.GiftCatcher;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class GiftCatcherResultSetup
{
    static readonly Color Wine = new Color32(102, 9, 24, 255);
    static readonly Color Gold = new Color32(198, 161, 91, 255);

    public static bool Configure(GiftCatcherGame game)
    {
        bool changed = false;
        foreach (var page in new[] { game.welcome, game.instructions, game.gameplay, game.result, game.pause })
        {
            if (!page.GetComponent<CanvasGroup>()) { page.AddComponent<CanvasGroup>(); changed = true; }
            var name = page.transform.Find("Game Name");
            if (name && name.gameObject.activeSelf) { name.gameObject.SetActive(false); changed = true; }
        }
        var root = game.result.transform;
        if (root.Find("Result Pearl Background")) return changed;
        foreach (var name in new[] { "Brand Header Gold Border", "Brand Header", "Dubai Duty Free", "Result Card Gold Border", "Result Card", "Demo Label", "Auto Return" })
        {
            var old = root.Find(name);
            if (old) old.gameObject.SetActive(false);
        }
        var background = Image(root, "Result Pearl Background", 0, 0, 1080, 1920, new Color32(248, 243, 235, 255));
        background.transform.SetAsFirstSibling();
        Image(root, "Left Wine Edge", -525, 0, 30, 1920, Wine);
        Image(root, "Right Wine Edge", 525, 0, 30, 1920, Wine);
        var logo = Rect(root, "Result DDF Logo", 0, 725, 340, 148).gameObject.AddComponent<RawImage>();
        logo.texture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Media/UI/ddf-logo-black.png");
        logo.raycastTarget = false;
        Image(root, "Result Gold Accent", 0, 550, 90, 3, Gold);
        Style(game.resultTitle, 0, 365, 860, 230, 62, Wine);
        game.resultTitle.text = "A LITTLE JOY,\nJUST FOR YOU.";
        var art = root.Find("Result Gift").GetComponent<RawImage>();
        art.texture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Media/Items/teddy.png");
        Place(art.rectTransform, 0, 0, 330, 330);
        art.raycastTarget = false;
        Style(game.resultBody, 0, -330, 820, 240, 34, new Color32(66, 49, 44, 255));
        game.resultBody.fontStyle = FontStyles.Normal;
        var button = game.homeButton;
        Place((RectTransform)button.transform, 0, -700, 700, 110);
        button.GetComponent<Image>().color = Wine;
        var outline = button.GetComponent<Outline>();
        if (outline) outline.enabled = false;
        var label = button.GetComponentInChildren<TMP_Text>(true);
        Style(label, 0, 0, 660, 95, 32, Color.white);
        label.text = "FINISH";
        var celebration = game.result.AddComponent<GiftCatcherCelebration>();
        celebration.merchandise = art.rectTransform;
        celebration.confetti = new RectTransform[48];
        var confettiRoot = Rect(root, "Celebration Confetti", 0, 0, 1080, 1920);
        for (int i = 0; i < celebration.confetti.Length; i++)
        {
            var piece = Image(confettiRoot, "Confetti " + i, 0, 1050, i % 3 == 0 ? 7 : 11, i % 3 == 0 ? 24 : 14, i % 3 == 0 ? Wine : Gold);
            celebration.confetti[i] = piece.rectTransform;
            piece.gameObject.SetActive(false);
        }
        return true;
    }

    static void Style(TMP_Text text, float x, float y, float w, float h, float size, Color color)
    {
        Place(text.rectTransform, x, y, w, h);
        text.fontSize = size; text.color = color; text.characterSpacing = 1.5f;
        text.alignment = TextAlignmentOptions.Center;
    }
    static Image Image(Transform root, string name, float x, float y, float w, float h, Color color)
    {
        var image = Rect(root, name, x, y, w, h).gameObject.AddComponent<Image>();
        image.color = color; image.raycastTarget = false; return image;
    }
    static RectTransform Rect(Transform root, string name, float x, float y, float w, float h)
    {
        var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(root, false); Place(rect, x, y, w, h); return rect;
    }
    static void Place(RectTransform rect, float x, float y, float w, float h)
    {
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
        rect.anchoredPosition = new Vector2(x, y); rect.sizeDelta = new Vector2(w, h);
    }
}
