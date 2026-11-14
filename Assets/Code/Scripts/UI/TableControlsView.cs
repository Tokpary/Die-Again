using Code.Scripts.Rolling;
using UnityEngine;
using UnityEngine.UI;

namespace Code.Scripts.UI
{
    public class TableControlsView : MonoBehaviour
    {
        [SerializeField] private Button _backButton;

        private RollSession _session;

        public void Bind(RollSession session)
        {
            Unbind();
            _session = session;
            _session.ViewChanged += Refresh;
            _session.BudgetChanged += Refresh;
            _backButton.onClick.AddListener(OnBackClicked);
            Refresh();
        }

        public void Unbind()
        {
            if (_session != null)
            {
                _session.ViewChanged -= Refresh;
                _session.BudgetChanged -= Refresh;
                _backButton.onClick.RemoveListener(OnBackClicked);
                _session = null;
            }
            Refresh();
        }

        private void OnBackClicked()
        {
            if (!InGameSettingsCard.BlocksGameplayInput)
                _session?.GoBack();
        }

        private void Refresh()
        {
            if(_backButton != null)
                _backButton.gameObject.SetActive(_session != null && !_session.IsInHub && !_session.IsRerollPending);
        }
    }
}