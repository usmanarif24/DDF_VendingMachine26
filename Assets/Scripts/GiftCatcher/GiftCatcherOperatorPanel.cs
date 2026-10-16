using System;
using System.Globalization;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DDF.GiftCatcher
{
    public sealed class GiftCatcherOperatorPanel : MonoBehaviour
    {
        [Serializable]
        public sealed class Field
        {
            public string section, key, label, fallback;
            public int minimum, maximum;
            public TMP_InputField input;
        }
        public GiftCatcherGame game;
        public GameObject panel;
        public Button openButton, closeButton, saveButton, refillButton, testDispenseButton;
        public TMP_Text status, message;
        public Toggle recovery;
        public Field[] fields;
        OperatorConfiguration source;
        bool watchingTest;

        void Awake()
        {
            RepairLayout();
            panel.SetActive(false);
            openButton.onClick.AddListener(Open);
            closeButton.onClick.AddListener(Close);
            saveButton.onClick.AddListener(() => SaveAndRestart(false));
            refillButton.onClick.AddListener(Refill);
            if (testDispenseButton) testDispenseButton.onClick.AddListener(TestDispense);
        }

        void Update()
        {
            openButton.interactable = game.CanOpenOperator;
            if (panel.activeSelf)
            {
                bool idle = game.CanOpenOperator;
                closeButton.interactable = saveButton.interactable = refillButton.interactable = idle;
                if (testDispenseButton) testDispenseButton.interactable = game.CanTestDispense;
                if (watchingTest)
                {
                    message.text = game.OperatorDispenseMessage;
                    status.text = game.OperatorStatus;
                    if (idle)
                    {
                        watchingTest = false;
                        recovery.gameObject.SetActive(game.PendingRecovery);
                        recovery.isOn = false;
                    }
                }
            }
            if (Keyboard.current != null && Keyboard.current.f10Key.wasPressedThisFrame)
            { if (panel.activeSelf) Close(); else Open(); }
        }

        public void Open()
        {
            if (!game.CanOpenOperator) return;
            game.SetOperatorOpen(true); panel.SetActive(true); panel.transform.SetAsLastSibling();
            RepairLayout();
            var scroll = panel.GetComponentInChildren<ScrollRect>(true);
            if (scroll)
            {
                scroll.StopMovement();
                Canvas.ForceUpdateCanvases();
                scroll.verticalNormalizedPosition = 1;
            }
            recovery.isOn = false;
            recovery.gameObject.SetActive(game.PendingRecovery);
            status.text = game.OperatorStatus + (game.PendingRecovery ? "\nUnresolved dispense: inspect the machine before clearing." : "");
            message.text = "Changes apply after Save and Restart. The current round will be discarded.";
            try
            {
                source = OperatorSettings.Read(game);
                foreach (var field in fields) field.input.text = source.Get(field.key);
                saveButton.interactable = true;
            }
            catch (Exception exception)
            {
                // Keep a faulted installation configurable rather than trapping staff on a read error.
                source = OperatorSettings.Defaults(game);
                foreach (var field in fields) field.input.text = source.Get(field.key);
                message.text = "Settings could not be loaded. Defaults are shown; review and save to replace them. " + exception.Message;
                saveButton.interactable = true;
            }
        }

        public void Close()
        {
            if (!game.CanOpenOperator) return;
            panel.SetActive(false); game.SetOperatorOpen(false);
        }

        void TestDispense()
        {
            if (!game.CanTestDispense) return;
            // Use active settings, never silently apply edits or refill stock during a test.
            foreach (var field in fields)
                if (field.key != "inventory_revision" && field.input.text.Trim() != source.Get(field.key))
                { message.text = "Save and Restart to apply your settings before using Test Dispense."; return; }
            watchingTest = true;
            game.TestDispense();
            message.text = game.OperatorDispenseMessage;
            status.text = game.OperatorStatus;
        }

        // Rows use negative offsets measured from the top, never the centre of the long content.
        // Also repairs the first panel revision without requiring it to be rebuilt.
        public bool RepairLayout()
        {
            var scroll = panel.GetComponentInChildren<ScrollRect>(true);
            if (!scroll || !scroll.content) return false;
            bool changed = false;
            // The refill action owns this internal generation; staff should not edit it by hand.
            foreach (var field in fields)
            {
                if (field.key == "inventory_revision")
                {
                    if (field.input.gameObject.activeSelf) { field.input.gameObject.SetActive(false); changed = true; }
                    var label = scroll.content.Find(field.label);
                    if (label && label.gameObject.activeSelf) { label.gameObject.SetActive(false); changed = true; }
                }
                if (field.key.StartsWith("motor_") && field.key.EndsWith("_turns"))
                {
                    string title = "Tray " + field.key[6] + " turns when full";
                    var label = scroll.content.Find(field.label);
                    if (field.label != title && label && label.TryGetComponent<TMP_Text>(out var text))
                    { label.name = title; text.text = title; field.label = title; changed = true; }
                }
            }
            var refillLabel = refillButton.GetComponentInChildren<TMP_Text>(true);
            if (refillLabel && refillLabel.text != "MACHINE REFILLED") { refillLabel.text = "MACHINE REFILLED"; changed = true; }
            var help = scroll.content.Find("Stock Help");
            const string stockHelp = "Fill the coils, then press Machine Refilled.\nThis saves settings and restarts at motor 1, turn 1.";
            if (help && help.TryGetComponent<TMP_Text>(out var helpText) && helpText.text != stockHelp)
            { helpText.text = stockHelp; changed = true; }
            var stockHeading = scroll.content.Find("STOCK • COUNTS LOAD ON A NEW REVISION");
            if (stockHeading && stockHeading.TryGetComponent<TMP_Text>(out var heading) && heading.text != "COIL TURNS WHEN FULL")
            { heading.text = "COIL TURNS WHEN FULL"; changed = true; }
            if (status.rectTransform.sizeDelta.y != 230 || status.rectTransform.anchoredPosition.y != 665)
            {
                status.rectTransform.sizeDelta = new Vector2(status.rectTransform.sizeDelta.x, 230);
                status.rectTransform.anchoredPosition = new Vector2(status.rectTransform.anchoredPosition.x, 665);
                status.fontSize = 23; changed = true;
            }
            foreach (RectTransform row in scroll.content)
            {
                var top = new Vector2(.5f, 1);
                if (row.anchorMin == top && row.anchorMax == top) continue;
                Vector2 position = row.anchoredPosition;
                row.anchorMin = row.anchorMax = top;
                row.anchoredPosition = position;
                changed = true;
            }
            return changed;
        }

        void Refill()
        {
            SaveAndRestart(true);
        }

        void SaveAndRestart(bool refilled)
        {
            if (!game.CanOpenOperator) { message.text = "Wait for the dispense to finish."; return; }
            try
            {
                var updated = JsonUtility.FromJson<OperatorConfiguration>(JsonUtility.ToJson(source));
                foreach (var field in fields)
                {
                    if (field.key == "inventory_revision") continue;
                    string value = field.input.text.Trim();
                    if (field.key == "port")
                    {
                        if (string.IsNullOrWhiteSpace(value) || value.IndexOfAny(new[] { '\r', '\n', '[', ']', ';', '=' }) >= 0)
                            throw new FormatException("Enter a valid COM port name.");
                    }
                    else if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int number) || number < field.minimum || number > field.maximum)
                        throw new FormatException(field.label + " must be " + field.minimum + "–" + field.maximum + ".");
                    updated.Set(field.key, value);
                }
                // Only Machine Refilled starts a new stock generation. Normal saves preserve progress.
                if (refilled)
                {
                    if (game.PendingRecovery && !recovery.isOn)
                        throw new InvalidOperationException("Reconcile the unresolved dispense and tick its acknowledgement before refilling.");
                    updated.vending.InventoryRevision = checked(source.vending.InventoryRevision + 1);
                }
                updated.Validate();
                var settings = updated.vending.ForAvailableHardware(out _);
                var scene = SceneManager.GetActiveScene();
                if (string.IsNullOrEmpty(scene.path) || !Application.CanStreamedLevelBeLoaded(scene.path))
                    throw new InvalidOperationException("The active scene is not available to restart. Include Main in the build scene list.");
                // Preflight inventory before saving; changing tray count requires an explicit refill revision.
                string inventoryPath = Path.Combine(Application.persistentDataPath,
                    settings.Enabled ? "vending-inventory.txt" : "vending-inventory-demo.txt");
                // A normal settings save may not reset stock by changing the tray topology.
                if (!refilled && source.vending.NumMotors != settings.NumMotors)
                    throw new InvalidOperationException("After changing the active trays, fill the coils and press Machine Refilled.");
                // Check the current generation first. Persist the new generation only after preferences.
                if (!refilled) new VendingInventory(settings, inventoryPath);
                OperatorSettings.Save(updated);
                game.ReleaseDispenser();
                if (refilled) new VendingInventory(settings, inventoryPath);
                if (game.PendingRecovery && recovery.isOn)
                    File.Delete(Path.Combine(Application.persistentDataPath, "vending-pending.txt"));
                saveButton.interactable = false;
                SceneManager.LoadScene(scene.path);
            }
            catch (Exception exception) { message.text = "Could not apply settings: " + exception.Message; saveButton.interactable = true; }
        }
    }
}
