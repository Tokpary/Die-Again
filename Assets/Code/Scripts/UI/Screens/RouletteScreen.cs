using System;
using Code.Scripts.Bosses;
using Code.Scripts.Core;
using Code.Scripts.Roulette;
using UnityEngine;
using UnityEngine.UI;

namespace Code.Scripts.UI.Screens
{
    public class RouletteScreen : UIScreen
    {
        [SerializeField] private Button _nextButton;
        [SerializeField] private Button _spinButton;
        [SerializeField] private Text _previewText;
        [SerializeField] private Text _resultText;
        [SerializeField] private GameObject _bossCard;
        [SerializeField] private BossInfoView _bossInfoView;
        [SerializeField] private Button _bossCardButton;

        public event Action NextButtonClicked;
        public event Action SpinButtonClicked;
        public event Action BossCardAcknowledged;
        public bool IsBossCardVisible => _bossCard != null && _bossCard.activeSelf;

        protected override void Awake()
        {
            base.Awake();
            SetButtons(false, false);
            if (_nextButton != null) _nextButton.onClick.AddListener(OnNextClicked);
            if (_spinButton != null) _spinButton.onClick.AddListener(OnSpinClicked);
            if (_bossCard != null) _bossCard.SetActive(false);
            if (_bossCardButton != null) _bossCardButton.onClick.AddListener(OnBossCardClicked);
        }

        private void OnNextClicked() => NextButtonClicked?.Invoke();
        private void OnSpinClicked() => SpinButtonClicked?.Invoke();

        public void ShowTransition(string message)
        {
            HideBossCard();
            if (_resultText != null) _resultText.text = message;
            SetButtons(false, false);
        }

        public void ShowPreview(GameProgress progress, int probability)
        {
            HideBossCard();
            if (_previewText != null)
                _previewText.text = "";
            if (_resultText != null) _resultText.text = "";
            SetButtons(true, false);
        }

        public void ShowSpinning()
        {
            HideBossCard();
            if (_resultText != null) _resultText.text = "";
            SetButtons(false, false);
        }

        public void ShowResult(RouletteResult result, bool canRepeat)
        {
            HideBossCard();
            if (_resultText != null)
                _resultText.text = "";
            SetButtons(canRepeat, true);
        }

        public bool ShowBossCard(BossSO boss)
        {
            if (boss == null)
            {
                Debug.LogError("Cannot show the boss card because the boss data is missing.", this);
                return false;
            }

            if (_bossCard == null || _bossInfoView == null || _bossCardButton == null)
            {
                Debug.LogError(
                    "Assign Boss Card, Boss Info View, and Boss Card Button in the RouletteScreen Inspector.",
                    this
                );
                return false;
            }

            _bossInfoView.Show(boss);
            _bossCard.SetActive(true);
            return true;
        }

        public void ShowError(string error, bool canRetry = false)
        {
            if (_resultText != null) _resultText.text = error;
            else Debug.LogError(error, this);
            SetButtons(canRetry, false);
        }

        private void SetButtons(bool canSpin, bool canContinue)
        {
            // Keep serialized controls for compatibility; interaction now belongs to the model.
            if (_spinButton != null) _spinButton.gameObject.SetActive(false);
            if (_nextButton != null) _nextButton.gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            if (_nextButton != null) _nextButton.onClick.RemoveListener(OnNextClicked);
            if (_spinButton != null) _spinButton.onClick.RemoveListener(OnSpinClicked);
            if (_bossCardButton != null) _bossCardButton.onClick.RemoveListener(OnBossCardClicked);
        }

        private void OnBossCardClicked()
        {
            HideBossCard();
            BossCardAcknowledged?.Invoke();
        }

        private void HideBossCard()
        {
            if (_bossCard != null)
                _bossCard.SetActive(false);
        }
    }
}
