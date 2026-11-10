using Code.Scripts.Dice;
using UnityEngine;

namespace Code.Scripts.UI
{
    [RequireComponent(typeof(RectTransform))]
    public class DiceDropSlot : MonoBehaviour, IDiceDropTarget
    {
        [SerializeField] private GameObject _hoverHighlight;

        private RectTransform _rect;
        private Canvas _canvas;

        private void Awake()
        {
            _rect = (RectTransform)transform;
            _canvas = GetComponentInParent<Canvas>();
            SetHover(false);
        }

        public bool ContainsScreenPoint(Vector2 screenPoint)
        {
            if (!isActiveAndEnabled) return false;
            UnityEngine.Camera uiCamera = _canvas != null && _canvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? _canvas.worldCamera
                : null;
            return RectTransformUtility.RectangleContainsScreenPoint(_rect, screenPoint, uiCamera);
        }

        public void SetHover(bool hover)
        {
            if (_hoverHighlight != null) _hoverHighlight.SetActive(hover);
        }
    }
}