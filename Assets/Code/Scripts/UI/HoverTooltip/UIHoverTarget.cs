using UnityEngine;
using UnityEngine.EventSystems;

namespace Code.Scripts.UI
{
    public abstract class UIHoverTarget : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public static UIHoverTarget Current { get; private set; }

        public abstract object TooltipData { get; }

        public void OnPointerEnter(PointerEventData eventData) => Current = this;

        public void OnPointerExit(PointerEventData eventData)
        {
            if (Current == this) Current = null;
        }

        protected virtual void OnDisable()
        {
            if (Current == this) Current = null;
        }
    }
}