using DDF.GiftCatcher;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public static class GiftCatcherIdleScreenSetup
{
    public static bool Configure(GiftCatcherGame game)
    {
        var welcome = game.welcome.transform;
        var animation = welcome.GetComponent<GiftCatcherIdleScreen>();
        if (animation && animation.layoutVersion >= 2) return false;
        var backgroundRect = welcome.Find("Idle Depth Background") ?? welcome.Find("Idle Pearl Ribbons");
        if (!backgroundRect)
        {
            backgroundRect = new GameObject("Idle Depth Background", typeof(RectTransform)).transform;
            backgroundRect.SetParent(welcome, false);
        }
        backgroundRect.name = "Idle Depth Background";
        var videoBackground = backgroundRect.GetComponent<GiftCatcherIdleBackground>();
        if (videoBackground) Object.DestroyImmediate(videoBackground);
        var player = backgroundRect.GetComponent<VideoPlayer>();
        if (player) Object.DestroyImmediate(player);
        var background = backgroundRect.GetComponent<RawImage>();
        if (!background) background = backgroundRect.gameObject.AddComponent<RawImage>();
        background.texture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Media/Backgrounds/idle-depth-room.png");
        background.raycastTarget = false; background.color = Color.white;
        var fullFrame = (RectTransform)backgroundRect;
        fullFrame.anchorMin = Vector2.zero; fullFrame.anchorMax = Vector2.one;
        fullFrame.offsetMin = fullFrame.offsetMax = Vector2.zero;
        fullFrame.SetAsFirstSibling();
        foreach (Transform child in welcome)
            if (child != backgroundRect && child.name != "Idle DDF Logo" && child != game.startButton.transform)
                child.gameObject.SetActive(false);

        var existingLogo = welcome.Find("Idle DDF Logo");
        var logoRect = existingLogo ? (RectTransform)existingLogo : new GameObject("Idle DDF Logo", typeof(RectTransform)).GetComponent<RectTransform>();
        logoRect.SetParent(welcome, false);
        logoRect.gameObject.SetActive(true);
        Position(logoRect, 0, 560, 430, 187.43f);
        var logo = logoRect.GetComponent<RawImage>();
        if (!logo) logo = logoRect.gameObject.AddComponent<RawImage>();
        logo.texture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Media/UI/ddf-logo-black.png");
        logo.raycastTarget = false;

        var start = game.startButton;
        var touch = (RectTransform)start.transform;
        touch.anchorMin = Vector2.zero; touch.anchorMax = Vector2.one;
        touch.offsetMin = touch.offsetMax = Vector2.zero;
        start.GetComponent<Image>().color = Color.clear;
        var outline = start.GetComponent<Outline>();
        if (outline) outline.enabled = false;
        start.transition = Selectable.Transition.None;
        start.navigation = new Navigation { mode = Navigation.Mode.None };
        var label = start.GetComponentInChildren<TMP_Text>(true);
        Position(label.rectTransform, 0, 220, 720, 110);
        label.text = "TOUCH TO START";
        label.fontSize = 42;
        label.characterSpacing = 4;
        label.color = new Color32(102, 9, 24, 255);
        label.raycastTarget = false;
        touch.SetAsLastSibling();
        if (!animation) animation = welcome.gameObject.AddComponent<GiftCatcherIdleScreen>();
        animation.game = game; animation.logo = logoRect; animation.prompt = label;
        animation.logoFade = Group(logoRect.gameObject);
        animation.promptFade = Group(label.gameObject);
        animation.layoutVersion = 2;
        return true;
    }

    static CanvasGroup Group(GameObject target)
    {
        var group = target.GetComponent<CanvasGroup>();
        if (!group) group = target.AddComponent<CanvasGroup>();
        group.alpha = 1; group.interactable = false; group.blocksRaycasts = false;
        return group;
    }

    static void Position(RectTransform rect, float x, float y, float width, float height)
    {
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
        rect.anchoredPosition = new Vector2(x, y);
        rect.sizeDelta = new Vector2(width, height);
    }
}
