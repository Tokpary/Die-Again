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
            if (_nextButton != null) _nextButton.onClick.AddListener(OnNextClicked);
            if (_spinButton != null) _spinButton.onClick.AddListener(OnSpinClicked);
        }

        private void OnNextClicked() => NextButtonClicked?.Invoke();
        private void OnSpinClicked() => SpinButtonClicked?.Invoke();

        public void ShowPreview(GameProgress progress, int probability)
        {
            if (_previewText != null)
                _previewText.text = $"SEMANA {progress.Week} · DÍA {progress.Day}\n" +
                    $"Rango de prueba: {progress.NextLuckyNumberMinimum}-{progress.NextLuckyNumberMaximum}" +
                    $"     |     Jefe: {probability}%\nVERDE: NORMAL     ·     ROJO: JEFE";
            if (_resultText != null) _resultText.text = "Pulsa GIRAR para revelar el resultado";
            SetButtons(true, false);
        }

        public void ShowSpinning()
        {
            if (_resultText != null) _resultText.text = "GIRANDO…";
            SetButtons(false, false);
        }

        public void ShowResult(RouletteResult result, bool canRepeat)
        {
            if (_resultText != null)
                _resultText.text = $"<b>LUCKY NUMBER: {result.LuckyNumber}</b>\n" +
                    (result.IsBoss ? "<color=#F27E85>ENCUENTRO: JEFE</color>" :
                        "<color=#8ED6B2>ENCUENTRO: NORMAL</color>");
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
            if (_spinButton != null) _spinButton.interactable = canSpin;
            if (_nextButton != null) _nextButton.interactable = canContinue;
        }

        private void OnDestroy()
        {
            if (_nextButton != null) _nextButton.onClick.RemoveListener(OnNextClicked);
            if (_spinButton != null) _spinButton.onClick.RemoveListener(OnSpinClicked);
        }
    }
}
