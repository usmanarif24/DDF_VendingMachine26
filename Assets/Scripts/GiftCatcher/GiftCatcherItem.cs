using UnityEngine;
using UnityEngine.UI;
namespace DDF.GiftCatcher
{
    public sealed class GiftCatcherItem : MonoBehaviour
    {
        public RectTransform rect;
        public RawImage art;
        [System.NonSerialized] public bool isRock;
        [System.NonSerialized] public float bottomOffset, catchHalfWidth;
    }
}
