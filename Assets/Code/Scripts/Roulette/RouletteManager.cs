
using System;
using System.Collections.Generic;
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
        public bool IsSpinning => _view != null && _view.IsSpinning;

        private void Awake()
        {
            if (_view == null) { Debug.LogError("RouletteView is missing.", this); return; }
            _view.SpinCompleted += HandleSpinCompleted;
            _view.SpinCancelled += HandleSpinCancelled;
            _view.Hide();
        }

        private void HandleSpinCompleted(RouletteResult result) => OnSpinCompleted?.Invoke(result);
        private void HandleSpinCancelled() => OnSpinCancelled?.Invoke();

        private void OnDestroy()
        {
            if (_view == null) return;
            _view.SpinCompleted -= HandleSpinCompleted;
            _view.SpinCancelled -= HandleSpinCancelled;
        }


        public void PrepareRound(GameProgress progress)
        {
            if (progress == null)
                throw new ArgumentNullException(nameof(progress));

            if (_settings == null)
                throw new InvalidOperationException("Missing RouletteSO.");

            if (!_settings.TryValidate(out string error))
                throw new InvalidOperationException(error);

            progress.PrepareNextRound(
                _settings.luckyNumberMinimum,
                _settings.luckyNumberMaximum,
                _settings.maximumSlices,
                _random
            );
        }


        public int GetBossProbability(int day, IReadOnlyList<Code.Scripts.CursedObject.CursedObject> passives = null, bool logEffects = false)
        {
            int probability = _settings.GetBossProbability(day);
            if (passives != null)
                foreach (var passive in passives)
                    {
                    if (passive == null) continue;
                    int before = probability;
                    probability = passive.ModifyBossProbability(day, probability);
                    if (logEffects)
                        Debug.Log($"[Objetos/Jefe] {passive.displayName} | Día {day} | {before}% -> {probability}%", passive);
                }
            return day == 4 ? 100 : Mathf.Clamp(probability, 0, 100);
        }

        public bool TryValidate(GameProgress progress, out string error)
        {
            error = null;
            if (_settings == null) error = "Falta el RouletteSO.";
            else if (_view == null) error = "Falta la RouletteView.";
            else if (progress == null) error = "Falta el GameProgress.";
            else if (_settings.TryValidate(out error)) _view.TryValidate(out error);
            return error == null;
        }

        public void Show(GameProgress progress, IReadOnlyList<Code.Scripts.CursedObject.CursedObject> passives = null)
        {
            if (progress == null || !progress.IsRoundPrepared)
                throw new InvalidOperationException("Prepare the round before showing the roulette.");
            _view.Show(GetBossProbability(progress.Day, passives), progress.SelectedLuckyNumbers);
        }

        public void ShowResult(RouletteResult result) => _view.ShowResult(result);
        public void Hide() => _view.Hide();

        public RouletteResult Spin(GameProgress progress, IReadOnlyList<Code.Scripts.CursedObject.CursedObject> passives = null)
        {
            if (!TryValidate(progress, out string error)) throw new InvalidOperationException(error);
            if (_view.IsSpinning) throw new InvalidOperationException("The roulette is already spinning.");
            RouletteResult result = RouletteRules.Roll(progress, GetBossProbability(progress.Day, passives), _random);
            progress.ClearTestResult();
            _view.Show(result.BossProbability, progress.SelectedLuckyNumbers);
            _view.Spin(result);
            return result;
        }
    }
}
