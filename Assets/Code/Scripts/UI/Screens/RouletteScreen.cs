using System;
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

        public event Action NextButtonClicked;
        public event Action SpinButtonClicked;

        protected override void Awake()
        {
            base.Awake();
            SetButtons(false, false);
            if (_nextButton != null) _nextButton.onClick.AddListener(OnNextClicked);
            if (_spinButton != null) _spinButton.onClick.AddListener(OnSpinClicked);
        }

        private void OnNextClicked() => NextButtonClicked?.Invoke();
        private void OnSpinClicked() => SpinButtonClicked?.Invoke();

        public void ShowTransition(string message)
        {
            if (_resultText != null) _resultText.text = message;
            SetButtons(false, false);
        }

        public void ShowPreview(GameProgress progress, int probability)
        {
            if (_previewText != null)
                _previewText.text = "";
            if (_resultText != null) _resultText.text = "";
            SetButtons(true, false);
        }

        public void ShowSpinning()
        {
            if (_resultText != null) _resultText.text = "";
            SetButtons(false, false);
        }

        public void ShowResult(RouletteResult result, bool canRepeat)
        {
            if (_resultText != null)
                _resultText.text = "";
            SetButtons(canRepeat, true);
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
        }
    }
}
