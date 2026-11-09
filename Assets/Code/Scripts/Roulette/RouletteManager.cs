using System;
using Code.Scripts.Core;
using UnityEngine;

namespace Code.Scripts.Roulette
{
    public class RouletteManager : MonoBehaviour
    {
        [SerializeField] private RouletteSO _settings;
        [SerializeField] private RouletteView _view;
        
        private readonly System.Random _random = new System.Random();
        
        public event Action<RouletteResult> OnSpinCompleted;
        public event Action OnSpinCancelled;
        public bool IsSpinning => _view.IsSpinning;

        private void Awake()
        {
            _view.SpinCompleted += r => OnSpinCompleted?.Invoke(r);
            _view.SpinCancelled += () => OnSpinCancelled?.Invoke();
            _view.Hide();
        }
        
        public void PrepareRound(GameProgress progress) => progress.PrepareNextRound(_settings.luckyNumberMinimum, _settings.luckyNumberMaximum);
        public int GetBossProbability(int day) => _settings.GetBossProbability(day);

        public bool TryValidate(GameProgress gameProgress, out string error)
        {
            error = null;
            if (_settings == null) error = "Falta el RouletteSO.";
            else if (_view == null) error = "Falta la RouletteView.";
            else if (gameProgress == null) error = "Falta el GameProgress.";
            else if (_settings.TryValidate(out error)) _view.TryValidate(out error);
            return error == null;
        }
        
        public void Show(GameProgress progress) => _view.Show(GetBossProbability(progress.Day));
        public void ShowResult(RouletteResult result) => _view.ShowResult(result);
        public void Hide() => _view.Hide();

        public RouletteResult Spin(GameProgress progress)
        {
            int probability = GetBossProbability(progress.Day);
            RouletteResult result = RouletteRules.Roll(progress, probability, _random);
            progress.ClearTestResult();
            _view.Spin(result);
            return result;
        }
    }
}