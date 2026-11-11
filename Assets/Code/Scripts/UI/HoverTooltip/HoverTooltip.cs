using UnityEngine;
using UnityEngine.UI;

namespace Code.Scripts.UI
{
    [RequireComponent(typeof(CanvasGroup))]
    public class HoverTooltip : MonoBehaviour
    {
        [SerializeField] private RectTransform _panel;
        [Tooltip("Una ficha por tipo. Cada una en su propio GameObject hijo de _panel: se activa solo la que toca.")]
        [SerializeField] private InfoView[] _views;
        [Tooltip("Distancia al cursor en píxeles de referencia: por defecto abajo a la derecha.")]
        [SerializeField] private Vector2 _offset = new Vector2(24f, -24f);

        private CanvasGroup _group;
        private Canvas _canvas;

        private void Awake()
        {
            _group = GetComponent<CanvasGroup>();
            _group.interactable = false;
            _group.blocksRaycasts = false;   
            _canvas = GetComponentInParent<Canvas>();
            Hide();
        }

        public void Show(object data)
        {
            InfoView shown = null;
            foreach (InfoView view in _views)
            {
                bool match = shown == null && view.CanShow(data);
                view.gameObject.SetActive(match);
                if (!match) continue;
                view.ShowData(data);
                shown = view;
            }

            if (shown == null)
            {
                Debug.LogWarning($"[HoverTooltip] No hay ficha para {data.GetType().Name}.", this);
                Hide();
                return;
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(_panel);   
            _group.alpha = 1f;
        }

        public void Hide() => _group.alpha = 0f;

        public void MoveTo(Vector2 pointer)
        {
            Vector2 size = Vector2.Scale(_panel.rect.size, _panel.lossyScale);
            Vector2 offset = _offset * (_canvas != null ? _canvas.scaleFactor : 1f);

            float x = pointer.x + offset.x;
            if (x + size.x > Screen.width) x = pointer.x - offset.x - size.x;
            float y = pointer.y + offset.y;
            if (y - size.y < 0f) y = pointer.y - offset.y + size.y;

            x = Mathf.Clamp(x, 0f, Mathf.Max(0f, Screen.width - size.x));
            y = Mathf.Clamp(y, Mathf.Min(size.y, Screen.height), Screen.height);
            _panel.position = new Vector3(x, y, 0f);
        }
    }
}