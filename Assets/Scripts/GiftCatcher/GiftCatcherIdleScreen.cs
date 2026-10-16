using TMPro;
using UnityEngine;
using DG.Tweening;

namespace DDF.GiftCatcher
{
    // The scene is a still image; only the native Unity branding and prompt animate.
    public sealed class GiftCatcherIdleScreen : MonoBehaviour
    {
        public GiftCatcherGame game;
        public RectTransform logo;
        public TMP_Text prompt;
        public CanvasGroup logoFade, promptFade;
        public int layoutVersion = 2;
        Vector2 logoPosition, promptPosition;
        Vector3 logoScale, promptScale;
        Sequence entrance;
        Tween logoFloat, promptPulse, promptBreath;
        bool cached, paused;

        void OnEnable()
        {
            if (!logo || !prompt || !logoFade || !promptFade) return;
            if (!cached)
            {
                logoPosition = logo.anchoredPosition;
                promptPosition = prompt.rectTransform.anchoredPosition;
                logoScale = logo.localScale;
                promptScale = prompt.rectTransform.localScale;
                cached = true;
            }
            paused = false;
            logoFade.alpha = promptFade.alpha = 0;
            logo.anchoredPosition = logoPosition + Vector2.down * 18;
            logo.localScale = logoScale * .97f;
            prompt.rectTransform.anchoredPosition = promptPosition + Vector2.down * 10;
            entrance = DOTween.Sequence().SetUpdate(true);
            entrance.Append(logoFade.DOFade(1, .85f));
            entrance.Join(logo.DOAnchorPos(logoPosition, .85f).SetEase(Ease.OutCubic));
            entrance.Join(logo.DOScale(logoScale, .85f).SetEase(Ease.OutCubic));
            entrance.Insert(.4f, promptFade.DOFade(1, .7f));
            entrance.Insert(.4f, prompt.rectTransform.DOAnchorPos(promptPosition, .7f).SetEase(Ease.OutCubic));
            entrance.OnComplete(StartIdleLoops);
            Update();
        }

        void StartIdleLoops()
        {
            logoFloat = logo.DOAnchorPosY(logoPosition.y + 4, 2.8f).SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo).SetUpdate(true);
            promptPulse = promptFade.DOFade(.65f, 1.7f).SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo).SetUpdate(true);
            promptBreath = prompt.rectTransform.DOScale(promptScale * 1.025f, 1.7f).SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo).SetUpdate(true);
        }

        void Update()
        {
            if (!game || paused == game.OperatorOpen) return;
            paused = game.OperatorOpen;
            SetPaused(entrance); SetPaused(logoFloat); SetPaused(promptPulse); SetPaused(promptBreath);
        }

        void SetPaused(Tween tween)
        {
            if (tween == null || !tween.IsActive()) return;
            if (paused) tween.Pause(); else tween.Play();
        }

        void OnDisable()
        {
            entrance?.Kill(); logoFloat?.Kill(); promptPulse?.Kill(); promptBreath?.Kill();
            entrance = null; logoFloat = promptPulse = promptBreath = null;
            if (!cached) return;
            if (logo)
            {
                logo.anchoredPosition = logoPosition;
                logo.localScale = logoScale;
            }
            if (prompt) { prompt.rectTransform.anchoredPosition = promptPosition; prompt.rectTransform.localScale = promptScale; }
            if (logoFade) logoFade.alpha = 1;
            if (promptFade) promptFade.alpha = 1;
        }
    }
}
