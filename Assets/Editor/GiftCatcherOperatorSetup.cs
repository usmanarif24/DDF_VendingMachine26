using System.Collections.Generic;
using DDF.GiftCatcher;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

// Author the panel once on import; all controls remain editable saved prefab objects.
public static class GiftCatcherOperatorSetup
{
    const string PrefabPath = "Assets/Prefabs/GiftCatcher.prefab";
    static readonly Color Ink = new Color32(35, 31, 32, 255);
    static readonly Color Red = new Color32(200, 25, 36, 255);

    [InitializeOnLoadMethod]
    static void OnImport()
    {
        EditorApplication.delayCall += Wire;
        EditorApplication.playModeStateChanged += state =>
        {
            if (state == PlayModeStateChange.EnteredEditMode) EditorApplication.delayCall += Wire;
        };
    }

    [MenuItem("Gift Catcher/Add Operator Panel")]
    public static void Wire()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
        { EditorApplication.delayCall += Wire; return; }
        if (EditorApplication.isPlayingOrWillChangePlaymode || !AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath)) return;
        var root = PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            var existing = root.GetComponent<GiftCatcherOperatorPanel>();
            if (existing)
            {
                bool changed = existing.RepairLayout();
                if (!existing.testDispenseButton)
                {
                    existing.testDispenseButton = Button(existing.panel.transform, "Test Dispense", "TEST DISPENSE", 380, 610, 230, 80, 25);
                    changed = true;
                }
                if (changed) PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                return;
            }
            var controller = root.AddComponent<GiftCatcherOperatorPanel>();
            controller.game = root.GetComponent<GiftCatcherGame>();
            controller.openButton = Button(root.transform, "Operator Access", "OPERATOR", 410, 925, 220, 60, 26);
            var panel = Rect(root.transform, "Operator Panel", 0, 0, 1080, 1920);
            controller.panel = panel.gameObject;
            var background = panel.gameObject.AddComponent<Image>(); background.color = new Color32(242, 240, 236, 255);
            Label(panel, "Title", "OPERATOR PANEL", 0, 890, 990, 80, 52);
            Label(panel, "Subtitle", "Configure the machine • F10 to open or close", 0, 820, 990, 60, 28);
            controller.closeButton = Button(panel, "Close", "CLOSE", 380, 720, 230, 80, 28);
            controller.testDispenseButton = Button(panel, "Test Dispense", "TEST DISPENSE", 380, 610, 230, 80, 25);
            controller.status = Label(panel, "Machine Status", "Machine status", -125, 665, 730, 230, 23);
            controller.status.alignment = TextAlignmentOptions.TopLeft;

            var scrollRect = Rect(panel, "Settings Scroll", 0, -5, 1000, 1100);
            var scroll = scrollRect.gameObject.AddComponent<ScrollRect>();
            var scrollBackground = scrollRect.gameObject.AddComponent<Image>(); scrollBackground.color = Color.white;
            var viewport = Rect(scrollRect, "Viewport", 0, 0, 980, 1100);
            viewport.gameObject.AddComponent<RectMask2D>();
            var content = Rect(viewport, "Content", 0, 0, 960, 2400);
            content.anchorMin = content.anchorMax = new Vector2(.5f, 1);
            content.pivot = new Vector2(.5f, 1);
            scroll.viewport = viewport; scroll.content = content;
            scroll.horizontal = false; scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 45;

            var fields = new List<GiftCatcherOperatorPanel.Field>();
            float y = -45;
            Heading(content, "GAME RULES", ref y);
            Field(fields, content, "game", "target_gifts", "Gifts to win", "10", 1, 1000, ref y);
            Field(fields, content, "game", "starting_lives", "Starting lives", "3", 1, 100, ref y);
            Field(fields, content, "game", "round_seconds", "Round duration (seconds)", "60", 1, 3600, ref y);
            Field(fields, content, "game", "result_seconds", "Result return delay (seconds)", "12", 1, 600, ref y);
            Field(fields, content, "game", "instruction_idle_seconds", "Instructions idle delay (seconds)", "30", 1, 600, ref y);
            Heading(content, "VENDING & DEBUG", ref y);
            Field(fields, content, "vending", "enabled", "Physical vending (0 demo / 1 real)", "0", 0, 1, ref y);
            Field(fields, content, "general", "debug", "Space debug dispensing (0 off / 1 on)", "0", 0, 1, ref y);
            Field(fields, content, "vending", "port", "Arduino COM port", "COM3", 0, 0, ref y);
            Field(fields, content, "vending", "num_motors", "Active trays (1–4)", "4", 1, 4, ref y);
            Heading(content, "COIL TURNS WHEN FULL", ref y);
            for (int i = 1; i <= 4; i++)
                Field(fields, content, "vending", "motor_" + i + "_turns", "Tray " + i + " turns when full", "3", 0, 100000, ref y);
            controller.refillButton = Button(content, "Refill", "MACHINE REFILLED", 0, y, 910, 85, 28); y -= 105;
            Label(content, "Stock Help", "Fill the coils, then press Machine Refilled.\nThis saves settings and restarts at motor 1, turn 1.", 0, y - 30, 920, 100, 25); y -= 125;
            Heading(content, "SERIAL CONNECTION", ref y);
            Field(fields, content, "vending", "startup_delay_ms", "Board startup delay (ms)", "2000", 0, 10000, ref y);
            Field(fields, content, "vending", "response_timeout_ms", "Reply timeout (ms)", "6000", 4000, 60000, ref y);
            Field(fields, content, "vending", "dtr", "DTR (0 off / 1 on)", "1", 0, 1, ref y);
            Field(fields, content, "vending", "rts", "RTS (0 off / 1 on)", "0", 0, 1, ref y);
            content.sizeDelta = new Vector2(960, -y + 40);
            controller.fields = fields.ToArray();

            var recoveryRoot = Rect(panel, "Recovery Confirmation", 0, -640, 990, 110);
            controller.recovery = recoveryRoot.gameObject.AddComponent<Toggle>();
            var toggleHit = recoveryRoot.gameObject.AddComponent<Image>(); toggleHit.color = new Color32(255, 233, 215, 255);
            var checkBox = Rect(recoveryRoot, "Check Box", -450, 0, 55, 55).gameObject.AddComponent<Image>(); checkBox.color = Ink;
            var check = Label(checkBox.transform, "Check", "✓", 0, 0, 55, 55, 42); check.color = Color.white;
            controller.recovery.targetGraphic = checkBox; controller.recovery.graphic = check;
            var recoveryText = Label(recoveryRoot, "Explanation", "I inspected the motor/tray and reconciled the last prize.\nClear the unresolved request when saving.", 35, 0, 860, 110, 25);
            recoveryText.alignment = TextAlignmentOptions.MidlineLeft;
            controller.message = Label(panel, "Message", "", 0, -760, 990, 140, 27);
            controller.saveButton = Button(panel, "Save and Restart", "SAVE AND RESTART", 0, -885, 990, 100, 36);
            controller.RepairLayout();
            panel.gameObject.SetActive(false);
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    static void Heading(Transform parent, string title, ref float y)
    {
        var label = Label(parent, title, title, 0, y, 920, 65, 28);
        label.color = Red; label.alignment = TextAlignmentOptions.MidlineLeft; y -= 80;
    }

    static void Field(List<GiftCatcherOperatorPanel.Field> fields, Transform parent, string section, string key,
        string title, string fallback, int minimum, int maximum, ref float y)
    {
        var label = Label(parent, title, title, -150, y, 610, 80, 27);
        label.alignment = TextAlignmentOptions.MidlineLeft;
        var rect = Rect(parent, key, 325, y, 260, 78);
        var image = rect.gameObject.AddComponent<Image>(); image.color = new Color32(232, 232, 232, 255);
        var input = rect.gameObject.AddComponent<TMP_InputField>();
        input.targetGraphic = image;
        var viewport = Rect(rect, "Text Area", 0, 0, 230, 70);
        viewport.gameObject.AddComponent<RectMask2D>();
        var text = Label(viewport, "Text", "", 0, 0, 230, 70, 30);
        text.fontStyle = FontStyles.Normal;
        input.textViewport = viewport; input.textComponent = text;
        input.contentType = key == "port" ? TMP_InputField.ContentType.Standard : TMP_InputField.ContentType.IntegerNumber;
        input.lineType = TMP_InputField.LineType.SingleLine;
        input.characterLimit = key == "port" ? 32 : 10;
        input.text = fallback;
        fields.Add(new GiftCatcherOperatorPanel.Field { section = section, key = key, label = title,
            fallback = fallback, minimum = minimum, maximum = maximum, input = input });
        y -= 100;
    }

    static RectTransform Rect(Transform parent, string name, float x, float y, float width, float height)
    {
        var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false); rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
        rect.anchoredPosition = new Vector2(x, y); rect.sizeDelta = new Vector2(width, height); return rect;
    }

    static TMP_Text Label(Transform parent, string name, string value, float x, float y, float width, float height, float size)
    {
        var label = Rect(parent, name, x, y, width, height).gameObject.AddComponent<TextMeshProUGUI>();
        label.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
        label.text = value; label.fontSize = size; label.fontStyle = FontStyles.Bold;
        label.color = Ink; label.alignment = TextAlignmentOptions.Center; label.raycastTarget = false; return label;
    }

    static Button Button(Transform parent, string name, string title, float x, float y, float width, float height, float size)
    {
        var rect = Rect(parent, name, x, y, width, height);
        var image = rect.gameObject.AddComponent<Image>(); image.color = Red;
        var button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = image;
        button.navigation = new Navigation { mode = Navigation.Mode.None };
        Label(rect, "Label", title, 0, 0, width - 20, height - 10, size).color = Color.white;
        return button;
    }
}
