using UnityEngine;

namespace Code.Scripts.UI
{
    [RequireComponent(typeof(CanvasGroup))]
    public abstract class UIScreen : MonoBehaviour
    {
        private CanvasGroup _canvasGroup;
        private CanvasGroup CanvasGroup => _canvasGroup != null ? _canvasGroup : (_canvasGroup = GetComponent<CanvasGroup>());
        
        protected virtual void Awake()
        {
        }

        public virtual void Show() => SetVisible(true);
        public virtual void Hide() => SetVisible(false);
        
        private void SetVisible(bool isVisible)
        {
            CanvasGroup.alpha = isVisible ? 1 : 0;
            CanvasGroup.interactable = isVisible;
            CanvasGroup.blocksRaycasts = isVisible;
        }
    }
}