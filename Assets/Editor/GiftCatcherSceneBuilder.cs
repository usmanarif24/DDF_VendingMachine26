using System;
using DDF.GiftCatcher;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// This tool authors and saves objects in edit mode. The player never builds UI or instantiates items.
public static class GiftCatcherSceneBuilder
{
    const string PrefabPath = "Assets/Prefabs/GiftCatcher.prefab";
    static readonly Color Ink = new Color32(35, 31, 32, 255);
    static readonly Color Red = new Color32(237, 28, 36, 255);
    static readonly Color Gold = new Color32(198, 161, 91, 255);

    [InitializeOnLoadMethod]
    static void FirstImport()
    {
        EditorApplication.delayCall += () =>
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null) return;
            if (SceneManager.GetActiveScene().path == "Assets/Scenes/Main.unity") Build();
        };
    }

    [MenuItem("Gift Catcher/Build First Iteration")]
    public static void Build()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        var scene = SceneManager.GetActiveScene();
        if (scene.path != "Assets/Scenes/Main.unity") throw new InvalidOperationException("Open Assets/Scenes/Main.unity before building.");
        if (GameObject.Find("GiftCatcher") != null) { Debug.Log("Gift Catcher already exists; edit the saved objects in the Hierarchy."); return; }
        var canvas = UnityEngine.Object.FindAnyObjectByType<Canvas>();
        if (canvas == null) throw new InvalidOperationException("Main scene must contain a Canvas.");
        Undo.RecordObject(canvas.transform, "Configure Gift Catcher Canvas");
        canvas.transform.localScale = Vector3.one;
        var scaler = canvas.GetComponent<CanvasScaler>();
        Undo.RecordObject(scaler, "Configure portrait layout");
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
        var root = Rect("GiftCatcher", canvas.transform, 0, 0, 1080, 1920);
        Undo.RegisterCreatedObjectUndo(root.gameObject, "Build Gift Catcher");
        var game = root.gameObject.AddComponent<GiftCatcherGame>();
        var background = Art("Dubai Waterfront", root, Load("Backgrounds/background"), 0, 0, 1080, 1920);
        background.color = Color.white;
        game.welcome = Screen("Welcome", root);
        game.instructions = Screen("Instructions", root);
        game.gameplay = Screen("Gameplay", root);
        game.result = Screen("Result", root);
        game.pause = Screen("Pause", root);

        var w = game.welcome.transform;
        Brand(w);
        Panel("Welcome Card", w, 0, 490, 960, 340);
        Text("Title", w, "GIFT CATCHER", 0, 560, 900, 100, 76);
        Text("Tagline", w, "Win a surprise", 0, 450, 900, 100, 58);
        Text("Welcome Copy", w, "A world of surprises is falling your way.", 0, 350, 850, 70, 30);
        Art("Hero Bag", w, Load("UI/bag"), 0, -100, 580, 605);
        Art("Hero Teddy", w, Load("Items/teddy"), 250, 80, 300, 450);
        Art("Hero Gift", w, Load("Items/gift"), -270, 100, 230, 230);
        Text("Hero Bag Branding", w, "Dubai\nDuty Free", 0, -85, 300, 140, 45, Color.white);
        Text("Round Promise", w, "10 gifts  •  60 seconds  •  3 lives", 0, -570, 940, 80, 37);
        game.startButton = Button("Start", w, "LET’S CATCH GIFTS", 0, -730, 940, 150);

        var ins = game.instructions.transform;
        Brand(ins);
        Panel("Instructions Card", ins, 0, 40, 960, 1160);
        Text("Instructions Title", ins, "HOW TO PLAY", 0, 510, 850, 90, 66);
        Instruction(ins, "Catch gifts", "Drag the bag left and right\nto catch the falling gifts.", Load("Items/gift"), 280);
        Instruction(ins, "Avoid rocks", "Each rock you catch costs one life.\nMissing a gift does not cost a life.", Load("Items/rock"), 20);
        Instruction(ins, "Win your surprise", "Catch 10 gifts in 60 seconds.\nYou have 3 lives. Good luck!", Load("Items/teddy"), -260);
        game.readyButton = Button("Ready", ins, "I’M READY — LET’S PLAY", 0, -730, 940, 150);

        var play = game.gameplay.transform;
        Panel("HUD", play, 0, 790, 1000, 250);
        Text("Gifts Caption", play, "GIFTS", -320, 855, 290, 60, 32);
        Text("Time Caption", play, "TIME LEFT", 0, 855, 300, 60, 32);
        Text("Lives Caption", play, "LIVES", 320, 855, 290, 60, 32);
        game.giftsLabel = Text("Gift Count", play, "0 / 10", -320, 778, 300, 85, 60);
        game.timeLabel = Text("Time", play, "1:00", 0, 778, 300, 85, 68);
        game.livesLabel = Text("Lives", play, "3 / 3", 320, 778, 280, 85, 60);
        Image("Progress Track", play, -320, 712, 270, 15, new Color32(225, 221, 215, 255));
        game.progress = Image("Progress Fill", play, -320, 712, 270, 15, Red);
        game.progress.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        game.progress.type = UnityEngine.UI.Image.Type.Filled;
        game.progress.fillMethod = UnityEngine.UI.Image.FillMethod.Horizontal;
        game.progress.fillOrigin = 0; game.progress.fillAmount = 0;
        game.playfield = Rect("Playfield", play, 0, -15, 1080, 1260);
        game.playfield.gameObject.AddComponent<RectMask2D>();
        var surface = game.playfield.gameObject.AddComponent<Image>();
        surface.color = Color.clear; surface.raycastTarget = true;
        game.touchInput = game.playfield.gameObject.AddComponent<GiftCatcherInput>();
        game.touchInput.game = game;
        game.bag = Art("Player Bag", game.playfield, Load("UI/bag"), 0, -490, 330, 345).rectTransform;
        Text("Bag Branding", game.bag, "Dubai\nDuty Free", 0, 0, 190, 95, 28, Color.white);
        game.giftTextures = new[] { Load("Items/gift"), Load("Items/teddy"), Load("Items/perfume"), Load("Items/chocolate"), Load("Items/watch"), Load("Items/sunglasses"), Load("Items/headphones"), Load("Items/suitcase") };
        game.rockTexture = Load("Items/rock");
        game.itemPool = new GiftCatcherItem[20];
        for (int i = 0; i < game.itemPool.Length; i++)
        {
            var art = Art("Falling Item " + (i + 1).ToString("00"), game.playfield, game.giftTextures[0], 0, 700, 160, 160);
            var item = art.gameObject.AddComponent<GiftCatcherItem>(); item.rect = art.rectTransform; item.art = art;
            game.itemPool[i] = item; art.gameObject.SetActive(false);
        }
        game.feedbackLabel = Text("Catch Feedback", play, "", 0, -345, 850, 90, 46, Red);
        game.countdownLabel = Text("Countdown", play, "3", 0, 200, 900, 260, 190, Red);
        Panel("Touch Hint", play, 0, -775, 1000, 200);
        Text("Touch Hint Text", play, "←  DRAG TO MOVE  →", 0, -735, 940, 80, 44);
        Text("Target Hint", play, "Catch 10 gifts to win your surprise", 0, -815, 940, 65, 32);

        var res = game.result.transform;
        Brand(res);
        Panel("Result Card", res, 0, 60, 960, 1150);
        game.resultTitle = Text("Result Title", res, "YOU WON!", 0, 480, 900, 110, 82, Red);
        Art("Result Gift", res, Load("Items/gift"), 0, 190, 340, 340);
        game.resultBody = Text("Result Message", res, "Dispensing your gift…\nPlease collect it from the tray below.", 0, -145, 840, 290, 44);
        Text("Demo Label", res, "DEMO — machine connection coming soon", 0, -410, 850, 80, 25);
        game.homeButton = Button("Back to Start", res, "BACK TO START", 0, -730, 940, 150);
        game.returnLabel = Text("Auto Return", res, "Returning to start in 12 seconds", 0, -870, 940, 80, 30);

        Image("Pause Dimmer", game.pause.transform, 0, 0, 1080, 1920, new Color(0, 0, 0, .7f), true);
        Panel("Pause Card", game.pause.transform, 0, 0, 900, 600);
        Text("Pause Title", game.pause.transform, "GAME PAUSED", 0, 150, 850, 100, 66);
        Text("Pause Copy", game.pause.transform, "Your time and gifts are saved.", 0, 10, 820, 100, 36);
        game.resumeButton = Button("Resume", game.pause.transform, "RESUME", 0, -160, 760, 130);
        game.welcome.SetActive(true); game.instructions.SetActive(false); game.gameplay.SetActive(false); game.result.SetActive(false); game.pause.SetActive(false);
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs")) AssetDatabase.CreateFolder("Assets", "Prefabs");
        PrefabUtility.SaveAsPrefabAssetAndConnect(root.gameObject, PrefabPath, InteractionMode.AutomatedAction);
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Selection.activeGameObject = root.gameObject;
        Debug.Log("Gift Catcher: saved editor-authored UI, 20 pooled items, prefab and Main scene. Ready to play.");
    }

    static Texture2D Load(string path)
    {
        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Media/" + path + ".png");
        if (!texture) throw new InvalidOperationException("Missing artwork: " + path);
        return texture;
    }
    static RectTransform Rect(string name, Transform parent, float x, float y, float width, float height)
    {
        var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false); rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
        rect.anchoredPosition = new Vector2(x, y); rect.sizeDelta = new Vector2(width, height); return rect;
    }
    static GameObject Screen(string name, Transform parent) { return Rect(name, parent, 0, 0, 1080, 1920).gameObject; }
    static Image Image(string name, Transform parent, float x, float y, float w, float h, Color color, bool raycast = false)
    {
        var image = Rect(name, parent, x, y, w, h).gameObject.AddComponent<Image>(); image.color = color; image.raycastTarget = raycast; return image;
    }
    static void Panel(string name, Transform parent, float x, float y, float w, float h)
    {
        Image(name + " Gold Border", parent, x, y, w, h, Gold);
        Image(name, parent, x, y, w - 8, h - 8, Color.white);
    }
    static RawImage Art(string name, Transform parent, Texture texture, float x, float y, float w, float h)
    {
        var art = Rect(name, parent, x, y, w, h).gameObject.AddComponent<RawImage>(); art.texture = texture; art.raycastTarget = false; return art;
    }
    static TMP_Text Text(string name, Transform parent, string value, float x, float y, float w, float h, float size, Color? color = null)
    {
        var text = Rect(name, parent, x, y, w, h).gameObject.AddComponent<TextMeshProUGUI>();
        text.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
        text.text = value; text.fontSize = size; text.color = color ?? Ink; text.alignment = TextAlignmentOptions.Center;
        text.fontStyle = FontStyles.Bold; text.raycastTarget = false; return text;
    }
    static Button Button(string name, Transform parent, string label, float x, float y, float w, float h)
    {
        var image = Image(name, parent, x, y, w, h, Red, true);
        var outline = image.gameObject.AddComponent<Outline>(); outline.effectColor = Gold; outline.effectDistance = new Vector2(4, -4);
        var button = image.gameObject.AddComponent<Button>(); button.targetGraphic = image;
        var colors = button.colors; colors.highlightedColor = new Color(.95f, .95f, .95f); colors.pressedColor = new Color(.8f, .8f, .8f); button.colors = colors;
        button.navigation = new Navigation { mode = Navigation.Mode.None };
        Text("Label", image.transform, label, 0, 0, w - 35, h - 20, 42, Color.white); return button;
    }
    static void Brand(Transform parent)
    {
        Panel("Brand Header", parent, 0, 795, 960, 180);
        Text("Dubai Duty Free", parent, "Dubai\nDuty Free", -270, 795, 390, 145, 49, Red);
        Text("Game Name", parent, "GIFT CATCHER", 190, 795, 490, 120, 45);
    }
    static void Instruction(Transform parent, string title, string body, Texture texture, float y)
    {
        Art(title + " Art", parent, texture, -320, y, 150, 180);
        Text(title + " Title", parent, title.ToUpperInvariant(), 90, y + 65, 610, 75, 42);
        Text(title + " Copy", parent, body, 90, y - 30, 620, 130, 31);
    }
}
