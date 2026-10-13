using System;
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
            ShowWelcome();
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
            ClearItems(); state = State.Welcome; Screens(welcome);
        }

        public void ShowInstructions()
        {
            state = State.Instructions; idleTime = instructionIdleSeconds; Screens(instructions);
        }

        public void StartRound()
        {
            ClearItems(); gifts = 0; lives = startingLives; timeLeft = roundSeconds;
            countdown = 3; nextSpawn = .35f; feedbackTime = 0;
            bag.anchoredPosition = new Vector2(0, bag.anchoredPosition.y);
            feedbackLabel.text = ""; state = State.Countdown; Screens(gameplay);
            countdownLabel.gameObject.SetActive(true); countdownLabel.text = "3";
            RefreshHUD();
        }

        void Update()
        {
            Tick(Time.deltaTime);
        }

        void Tick(float dt)
        {
            if (isPaused) return;
            if (state == State.Instructions)
            {
                idleTime -= dt; if (idleTime <= 0) ShowWelcome(); return;
            }
            if (state == State.Result)
            {
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
                float catchY = bag.anchoredPosition.y + bag.sizeDelta.y * .36f;
                if (bottomBefore >= catchY && bottomAfter <= catchY &&
                    Mathf.Abs(position.x - bag.anchoredPosition.x) <= bag.sizeDelta.x * .36f + item.catchHalfWidth)
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
            if (state != State.Playing || isPaused) return;
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
                case 0: return .960925f; case 1: return .989583f; case 2: return .968900f;
                case 3: return .976874f; case 4: return .974482f; case 5: return .742424f;
                case 6: return .960925f; default: return .970494f;
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
            state = State.Result; ClearItems(); Screens(result); returnTime = resultSeconds;
            resultTitle.text = won ? "YOU WON!" : "NICE TRY!";
            resultBody.text = won ? "Dispensing your gift…\nPlease collect it from the tray below." :
                (lives <= 0 ? "You ran out of lives." : "Time is up!") + "\nYou caught " + gifts + " of " + targetGifts + " gifts.\nThank you for playing.";
            returnLabel.text = "Returning to start in " + Mathf.CeilToInt(returnTime) + " seconds";
            if (won) Debug.Log("dispensing gift"); // Replace with the vending machine integration later.
        }

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
