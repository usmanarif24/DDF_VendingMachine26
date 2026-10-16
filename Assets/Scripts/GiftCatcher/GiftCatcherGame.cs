using System;
using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

namespace DDF.GiftCatcher
{
    public sealed class GiftCatcherGame : MonoBehaviour
    {
        [Header("Round rules")]
        [Min(1)] public int targetGifts = 10;
        [Min(1)] public int startingLives = 3;
        [Min(1)] public float roundSeconds = 60;
        public float resultSeconds = 12;
        public float instructionIdleSeconds = 30;
        [Header("Authored screens")]
        public GameObject welcome, instructions, gameplay, result, pause;
        public TMP_Text giftsLabel, timeLabel, livesLabel, countdownLabel;
        public TMP_Text resultTitle, resultBody, returnLabel, feedbackLabel;
        public Image progress;
        public RectTransform playfield, bag;
        public GiftCatcherItem[] itemPool;
        public Texture2D[] giftTextures;
        public Texture2D rockTexture;
        public Button startButton, readyButton, homeButton, resumeButton;
        public GiftCatcherInput touchInput;
        [Header("Vending")]
        public TextAsset vendingDefaults;
        public TMP_Text vendingStatusLabel;
        VendingDispenser dispenser;
        VendingSettings vendingConfig;
        VendingInventory inventory;
        string vendingError;
        string vendingMode = "Machine ready";
        bool dispensingPending;
        bool operatorDispense;
        public string OperatorDispenseMessage { get; private set; }
        public bool OperatorOpen { get; private set; }
        public bool CanOpenOperator => !dispensingPending && (dispenser == null || !dispenser.Busy);
        public bool PendingRecovery => File.Exists(Path.Combine(Application.persistentDataPath, "vending-pending.txt"));
        public bool CanTestDispense => OperatorOpen && CanOpenOperator && !PendingRecovery &&
            string.IsNullOrEmpty(vendingError) && inventory != null && inventory.NextMotor() != 0 && vendingConfig != null;
        public string OperatorStatus
        {
            get
            {
                string status = string.IsNullOrEmpty(vendingError) ? vendingMode : vendingError;
                if (inventory != null && vendingConfig != null)
                {
                    int motor = inventory.NextMotor();
                    status += motor == 0 ? "\nAll trays empty — refill the machine" :
                        "\nNext: motor " + motor + ", turn " + inventory.NextTurn() + " / " + inventory.Capacity(motor);
                    for (int i = 1; i <= vendingConfig.NumMotors; i++)
                        status += "\nTray " + i + ": " + inventory.CompletedTurns(i) + " / " + inventory.Capacity(i) + " turns used, " + inventory.Remaining(i) + " left";
                }
                return status;
            }
        }
        public void SetOperatorOpen(bool open) { OperatorOpen = open; touchInput.Release(); }
        public void ReleaseDispenser() { dispenser?.Dispose(); dispenser = null; }

        public void TestDispense()
        {
            if (!CanTestDispense) return;
            try
            {
                ReleaseDispenser();
                PrepareDispenser();
                if (dispenser.RequiresRecovery) throw new InvalidOperationException("Resolve the previous dispense first.");
                int motor = inventory.NextMotor(), turn = inventory.NextTurn();
                OperatorDispenseMessage = (vendingConfig.Enabled ? "Dispensing" : "Simulating") +
                    " motor " + motor + ", turn " + turn + "…";
                operatorDispense = true;
                dispensingPending = true;
                dispenser.Begin(Guid.NewGuid().ToString("N"));
                PumpVending();
            }
            catch (Exception exception)
            {
                operatorDispense = false; dispensingPending = false;
                vendingError = exception.Message;
                OperatorDispenseMessage = "Test dispense failed: " + exception.Message;
                ClearItems(); ShowVendingFault();
            }
        }

        enum State { Welcome, Instructions, Countdown, Playing, Result }
        State state;
        int gifts, lives;
        float timeLeft, countdown, nextSpawn, returnTime, idleTime, feedbackTime;
        bool isPaused;
        readonly System.Random random = new System.Random();

        void Awake()
        {
            Application.targetFrameRate = 60;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            startButton.onClick.AddListener(ShowInstructions);
            readyButton.onClick.AddListener(StartRound);
            homeButton.onClick.AddListener(ShowWelcome);
            resumeButton.onClick.AddListener(Resume);
            touchInput.game = this;
            LoadVending();
            if (!string.IsNullOrEmpty(vendingError)) ShowVendingFault();
            else ShowWelcome();
        }

        void ShowVendingFault()
        {
            state = State.Result; Screens(result);
            bool empty = vendingError == "All trays are empty.";
            resultTitle.text = empty ? "MACHINE EMPTY" : "MACHINE UNAVAILABLE";
            resultBody.text = empty ? "All trays are empty.\nPlease ask a team member to refill the machine." :
                "Please ask a team member for help.\nAn earlier dispense or configuration needs attention.";
            returnLabel.text = "Waiting for a team member";
            homeButton.interactable = false;
            if (vendingStatusLabel) vendingStatusLabel.text = "Machine needs staff attention";
            Debug.LogError("Vending needs staff attention: " + vendingError);
        }

        void LoadVending()
        {
            try
            {
                var configuration = OperatorSettings.Read(this);
                configuration.Apply(this);
                UpdateRuleCopy();
                vendingConfig = configuration.vending.ForAvailableHardware(out vendingMode);
                PrepareDispenser();
                if (PendingRecovery || dispenser.RequiresRecovery) vendingError = "An earlier motor request is unresolved. Please ask a team member for help.";
            }
            catch (Exception exception) { vendingError = exception.Message; Debug.LogError("Vending configuration: " + exception.Message); }
        }

        void UpdateRuleCopy()
        {
            SetCopy(welcome.transform, "Round Promise", targetGifts + " gifts  •  " + roundSeconds + " seconds  •  " + startingLives + " lives");
            SetCopy(instructions.transform, "Win your surprise Copy", "Catch " + targetGifts + " gifts in " + roundSeconds + " seconds.\nYou have " + startingLives + " lives.");
            SetCopy(gameplay.transform, "Target Hint", "Catch " + targetGifts + " gifts to win your surprise");
        }

        static void SetCopy(Transform parent, string child, string value)
        {
            var target = parent.Find(child);
            if (target && target.TryGetComponent<TMP_Text>(out var label)) label.text = value;
        }

        void PrepareDispenser()
        {
            if (inventory == null)
                inventory = new VendingInventory(vendingConfig, Path.Combine(Application.persistentDataPath,
                    vendingConfig.Enabled ? "vending-inventory.txt" : "vending-inventory-demo.txt"));
            dispenser = new VendingDispenser(vendingConfig, Path.Combine(Application.persistentDataPath, "vending-pending.txt"), inventory: inventory);
        }

        public void DebugDispense()
        {
            if (OperatorOpen || vendingConfig == null || !vendingConfig.Debug || dispensingPending || isPaused || !string.IsNullOrEmpty(vendingError)) return;
            StartRound();
            if (state != State.Countdown) return;
            Debug.Log("Debug Space dispense: motor " + inventory.NextMotor());
            Finish(true);
        }

        void PumpVending()
        {
            if (dispenser == null || !dispenser.TryTakeResult(out var outcome)) return;
            dispensingPending = false;
            if (operatorDispense)
            {
                operatorDispense = false;
                bool failed = outcome.Kind == VendingDispenser.ResultKind.Failed || outcome.Kind == VendingDispenser.ResultKind.Empty;
                OperatorDispenseMessage = failed ? "Test dispense failed: " + outcome.Detail :
                    outcome.Kind == VendingDispenser.ResultKind.Demo ? "Demo dispense complete. Demo progress saved; no motor moved." :
                    "Dispense cycle complete. Progress saved. Check the collection tray.";
                if (failed) vendingError = outcome.Detail;
                else if (inventory.NextMotor() == 0) vendingError = "All trays are empty.";
                if (!string.IsNullOrEmpty(vendingError)) { ClearItems(); ShowVendingFault(); }
                Debug.Log("Operator test dispense: " + outcome.Kind + " " + outcome.Detail);
                return;
            }
            if (outcome.Kind == VendingDispenser.ResultKind.Failed || outcome.Kind == VendingDispenser.ResultKind.Empty)
            {
                vendingError = outcome.Detail;
                resultBody.text = outcome.Kind == VendingDispenser.ResultKind.Empty ? "All trays are empty.\nPlease ask a team member to refill the machine." : "We could not confirm the dispense cycle.\nPlease ask a team member for help.";
                if (vendingStatusLabel) vendingStatusLabel.text = "Machine needs staff attention";
                Debug.LogError("Vending failed: " + outcome.Detail);
                returnLabel.text = "Waiting for a team member";
                homeButton.interactable = false;
                return;
            }
            homeButton.interactable = true;
            returnTime = resultSeconds;
            resultBody.text = outcome.Kind == VendingDispenser.ResultKind.Demo ?
                "Demo round complete.\nNo physical gift was dispensed." :
                "Dispense cycle complete.\nPlease check the collection tray.";
            if (vendingStatusLabel) vendingStatusLabel.text = outcome.Kind == VendingDispenser.ResultKind.Demo ? vendingMode : "Motor cycle acknowledged by Arduino";
            Debug.Log("Vending result: " + outcome.Kind + " " + outcome.Detail);
        }

        void Screens(GameObject visible)
        {
            welcome.SetActive(visible == welcome);
            instructions.SetActive(visible == instructions);
            gameplay.SetActive(visible == gameplay);
            result.SetActive(visible == result);
            pause.SetActive(false);
            isPaused = false;
            touchInput.Release();
        }

        public void ShowWelcome()
        {
            if (OperatorOpen || dispensingPending || !string.IsNullOrEmpty(vendingError)) return;
            if (inventory != null && inventory.NextMotor() == 0)
            { vendingError = "All trays are empty."; ShowVendingFault(); resultBody.text = "All trays are empty.\nPlease ask a team member to refill the machine."; return; }
            ClearItems(); state = State.Welcome; Screens(welcome);
        }

        public void ShowInstructions()
        {
            if (OperatorOpen || dispensingPending || !string.IsNullOrEmpty(vendingError)) return;
            state = State.Instructions; idleTime = instructionIdleSeconds; Screens(instructions);
        }

        public void StartRound()
        {
            if (OperatorOpen || dispensingPending || !string.IsNullOrEmpty(vendingError)) return;
            if (dispenser != null && !dispenser.Busy)
            {
                dispenser.Dispose(); dispenser = null;
            }
            if (vendingConfig == null) LoadVending();
            else PrepareDispenser();
            if (dispenser != null && dispenser.RequiresRecovery) vendingError = "Unresolved vending request.";
            if (inventory != null && inventory.NextMotor() == 0) vendingError = "All trays are empty.";
            if (!string.IsNullOrEmpty(vendingError)) { ShowVendingFault(); return; }
            ClearItems(); gifts = 0; lives = startingLives; timeLeft = roundSeconds;
            countdown = 3; nextSpawn = .35f; feedbackTime = 0;
            bag.anchoredPosition = new Vector2(0, bag.anchoredPosition.y);
            feedbackLabel.text = ""; state = State.Countdown; Screens(gameplay);
            countdownLabel.gameObject.SetActive(true); countdownLabel.text = "3";
            RefreshHUD();
        }

        void Update()
        {
            PumpVending();
            if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame) DebugDispense();
            Tick(Time.deltaTime);
        }

        void Tick(float dt)
        {
            if (isPaused || OperatorOpen) return;
            if (state == State.Instructions)
            {
                idleTime -= dt; if (idleTime <= 0) ShowWelcome(); return;
            }
            if (state == State.Result)
            {
                if (dispensingPending || !string.IsNullOrEmpty(vendingError)) return;
                returnTime -= dt;
                returnLabel.text = "Returning to start in " + Mathf.CeilToInt(Mathf.Max(0, returnTime)) + " seconds";
                if (returnTime <= 0) ShowWelcome(); return;
            }
            if (state == State.Countdown)
            {
                countdown -= dt;
                countdownLabel.text = countdown > 0 ? Mathf.CeilToInt(countdown).ToString() : "GO!";
                if (countdown <= -.4f) { state = State.Playing; countdownLabel.gameObject.SetActive(false); }
                return;
            }
            if (state != State.Playing) return;
            // Touch and editor keyboard share the same bounds. Only one touch owns the bag.
            float direction = 0;
            if (Keyboard.current != null)
            {
                if (Keyboard.current.leftArrowKey.isPressed || Keyboard.current.aKey.isPressed) direction -= 1;
                if (Keyboard.current.rightArrowKey.isPressed || Keyboard.current.dKey.isPressed) direction += 1;
            }
            if (direction != 0) MoveBag(bag.anchoredPosition.x + direction * 850 * dt);
            float step = Mathf.Min(dt, timeLeft);
            timeLeft = Mathf.Max(0, timeLeft - step);
            nextSpawn -= step;
            if (nextSpawn <= 0) { Spawn(); nextSpawn = .65f + (float)random.NextDouble() * .4f; }
            float speed = Mathf.Lerp(330, 500, 1 - timeLeft / roundSeconds);
            foreach (var item in itemPool)
            {
                if (!item.gameObject.activeSelf) continue;
                float before = item.rect.anchoredPosition.y;
                var position = item.rect.anchoredPosition; position.y -= speed * step;
                item.rect.anchoredPosition = position;
                // Sweep the bottom of visible artwork across the bag opening; no physics is needed.
                float bottomBefore = before + item.bottomOffset;
                float bottomAfter = position.y + item.bottomOffset;
                float catchY = bag.anchoredPosition.y + bag.sizeDelta.y * .30f;
                if (bottomBefore >= catchY && bottomAfter <= catchY &&
                    Mathf.Abs(position.x - bag.anchoredPosition.x) <= bag.sizeDelta.x * .42f + item.catchHalfWidth)
                {
                    item.gameObject.SetActive(false);
                    if (item.isRock) { lives--; feedbackLabel.text = "Ouch! Avoid the rocks"; }
                    else { gifts++; feedbackLabel.text = "+1 gift!"; }
                    feedbackTime = .7f;
                    if (gifts >= targetGifts) { Finish(true); return; }
                    if (lives <= 0) { Finish(false); return; }
                }
                else if (position.y + item.rect.sizeDelta.y / 2 < -playfield.rect.height / 2)
                    item.gameObject.SetActive(false);
            }
            feedbackTime -= dt; if (feedbackTime <= 0) feedbackLabel.text = "";
            RefreshHUD();
            if (timeLeft <= 0) Finish(false);
        }

        public void MoveBag(float x)
        {
            if (state != State.Playing || isPaused || OperatorOpen) return;
            float limit = Mathf.Max(0, (playfield.rect.width - bag.sizeDelta.x) / 2 - 16);
            bag.anchoredPosition = new Vector2(Mathf.Clamp(x, -limit, limit), bag.anchoredPosition.y);
        }

        void Spawn()
        {
            foreach (var item in itemPool)
            {
                if (item.gameObject.activeSelf) continue;
                item.isRock = random.NextDouble() < .18;
                int index = random.Next(giftTextures.Length);
                item.art.texture = item.isRock ? rockTexture : giftTextures[index];
                float width = 160;
                float height = width * item.art.texture.height / item.art.texture.width;
                item.rect.sizeDelta = new Vector2(width, height);
                // Original image alpha bounds; transparent padding must not be the catch point.
                float visibleBottom = item.isRock ? .88756f : GiftBottom(index);
                item.bottomOffset = height * (.5f - visibleBottom);
                item.catchHalfWidth = width * .18f;
                float limit = playfield.rect.width / 2 - width / 2 - 24;
                item.rect.anchoredPosition = new Vector2(Mathf.Lerp(-limit, limit, (float)random.NextDouble()), playfield.rect.height / 2 + height / 2);
                item.gameObject.SetActive(true); item.rect.SetAsFirstSibling(); return;
            }
        }

        static float GiftBottom(int index)
        {
            switch (index)
            {
                // Teddy, perfume, chocolate, headphones, neck pillow, water bottle.
                case 0: return .968102f; case 1: return .970494f; case 2: return .995215f;
                case 3: return .970494f; case 4: return .848485f; case 5: return .969697f;
                default: return .970494f;
            }
        }

        void RefreshHUD()
        {
            giftsLabel.text = gifts + " / " + targetGifts;
            int seconds = Mathf.CeilToInt(timeLeft);
            timeLabel.text = (seconds / 60) + ":" + (seconds % 60).ToString("00");
            timeLabel.color = timeLeft <= 10 ? new Color32(237, 28, 36, 255) : new Color32(35, 31, 32, 255);
            livesLabel.text = lives + " / " + startingLives;
            progress.fillAmount = (float)gifts / targetGifts;
        }

        void Finish(bool won)
        {
            if (state == State.Result) return;
            state = State.Result; ClearItems(); Screens(result); returnTime = resultSeconds;
            resultTitle.text = won ? "YOU WON!" : "NICE TRY!";
            resultBody.text = won ? "Dispensing your gift…\nPlease collect it from the tray below." :
                (lives <= 0 ? "You ran out of lives." : "Time is up!") + "\nYou caught " + gifts + " of " + targetGifts + " gifts.\nThank you for playing.";
            returnLabel.text = "Returning to start in " + Mathf.CeilToInt(returnTime) + " seconds";
            if (won)
            {
                Debug.Log("dispensing gift");
                dispensingPending = true;
                resultBody.text = "Dispensing your gift…\nPlease wait for the motor cycle to finish.";
                returnLabel.text = "Please wait…"; homeButton.interactable = false;
                if (vendingStatusLabel) vendingStatusLabel.text = "Waiting for Arduino acknowledgement";
                dispenser.Begin(Guid.NewGuid().ToString("N"));
                PumpVending(); // Demo mode completes immediately; real serial replies are handled in Update.
            }
        }

        void OnDestroy() { dispenser?.Dispose(); }

        void ClearItems() { foreach (var item in itemPool) item.gameObject.SetActive(false); }
        void OnApplicationFocus(bool focus) { if (!focus) PauseRound(); }
        void OnApplicationPause(bool paused) { if (paused) PauseRound(); }
        void PauseRound()
        {
            touchInput.Release();
            if (state != State.Playing && state != State.Countdown) return;
            isPaused = true; pause.SetActive(true);
        }
        public void Resume() { touchInput.Release(); isPaused = false; pause.SetActive(false); }
    }
}
