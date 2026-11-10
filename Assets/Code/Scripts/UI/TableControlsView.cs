using Code.Scripts.Rolling;
using UnityEngine;
using UnityEngine.UI;

namespace Code.Scripts.UI
{
    public class TableControlsView : MonoBehaviour
    {
        [SerializeField] private Button _backButton;
        [SerializeField] private GameObject _placePanel;
        [SerializeField] private DiceDropSlot _placeSlot;
        [Tooltip("Opcional: atenúa el hueco cuando no se puede colocar (sin tiros, animación en marcha...).")]
        [SerializeField] private CanvasGroup _placeGroup;

        private RollSession _session;

        public void Bind(RollSession session)
        {
            Unbind();
            _session = session;
            _session.ViewChanged += Refresh;
            _session.BudgetChanged += Refresh;
            _session.SetPlaceTarget(_placeSlot);
            _backButton.onClick.AddListener(_session.GoBack);
            Refresh();
        }

        public void Unbind()
        {
            if (_session != null)
            {
                _session.ViewChanged -= Refresh;
                _session.BudgetChanged -= Refresh;
                _session.SetPlaceTarget(null);
                _backButton.onClick.RemoveListener(_session.GoBack);
                _session = null;
            }
            Refresh();
        }

        private void Refresh()
        {
            bool bound = _session != null;
            _backButton.gameObject.SetActive(bound && !_session.IsInHub);
            _placePanel.SetActive(bound && _session.IsInDrawer);
            if (_placeGroup != null) _placeGroup.alpha = bound && _session.CanPlace ? 1f : 0.4f;
        }
    }
}