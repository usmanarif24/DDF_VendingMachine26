using UnityEngine;
using UnityEngine.EventSystems;
namespace DDF.GiftCatcher
{
    public sealed class GiftCatcherInput : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        public GiftCatcherGame game;
        int? pointer;
        public void OnPointerDown(PointerEventData e) { if (pointer.HasValue) return; pointer = e.pointerId; Move(e); }
        public void OnDrag(PointerEventData e) { if (pointer == e.pointerId) Move(e); }
        public void OnPointerUp(PointerEventData e) { if (pointer == e.pointerId) Release(); }
        void Move(PointerEventData e)
        {
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(game.playfield, e.position, e.pressEventCamera, out var point)) game.MoveBag(point.x);
        }
        public void Release() { pointer = null; }
        void OnDisable() { Release(); }
    }
}
