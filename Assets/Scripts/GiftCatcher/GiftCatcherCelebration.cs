using DG.Tweening;
using UnityEngine;

namespace DDF.GiftCatcher
{
    public sealed class GiftCatcherCelebration : MonoBehaviour
    {
        public RectTransform[] confetti;
        public RectTransform merchandise;
        Sequence celebration;

        public void Show(bool won)
        {
            Stop();
            merchandise.gameObject.SetActive(won);
            if (!won) return;
            merchandise.localScale = Vector3.one * .85f;
            celebration = DOTween.Sequence().SetUpdate(true);
            celebration.Append(merchandise.DOScale(1, .8f).SetEase(Ease.OutBack));
            for (int i = 0; i < confetti.Length; i++)
            {
                var piece = confetti[i];
                piece.gameObject.SetActive(true);
                float x = -495 + (i * 137 % 990);
                float delay = .15f + (i % 12) * .09f;
                piece.anchoredPosition = new Vector2(x, 1050 + i % 5 * 55);
                piece.localRotation = Quaternion.Euler(0, 0, i * 37);
                celebration.Insert(delay, piece.DOAnchorPos(new Vector2(x + (i % 2 == 0 ? 90 : -90), -1080), 4.8f + i % 4 * .3f).SetEase(Ease.InQuad));
                celebration.Insert(delay, piece.DOLocalRotate(new Vector3(0, 0, i * 37 + 540), 5.7f, RotateMode.FastBeyond360).SetEase(Ease.Linear));
            }
            celebration.OnComplete(() => { foreach (var piece in confetti) piece.gameObject.SetActive(false); });
        }

        void Stop()
        {
            celebration?.Kill();
            celebration = null;
            foreach (var piece in confetti) piece.gameObject.SetActive(false);
            if (merchandise) merchandise.localScale = Vector3.one;
        }

        void OnDisable() { Stop(); }
    }
}
