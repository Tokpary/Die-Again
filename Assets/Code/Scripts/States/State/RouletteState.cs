using System;
using Code.Scripts.Camera;
using Code.Scripts.Core;
using Code.Scripts.Roulette;
using Code.Scripts.UI;
using Code.Scripts.UI.Screens;
using UnityEngine;

namespace Code.Scripts.States.State
{
    public class RouletteState : AState
    {
        private RouletteScreen _screen;
        private bool _active;
        private bool _spinning;
        private bool _transitioning;
        private bool _entered;
        
        private RouletteManager _rouletteManager => _context.RouletteManager;
        private GameProgress _progress => _context.RunData.Progress;
        public RouletteState(GameContext context, IStateMachine stateMachine) : base(context, stateMachine)
        {
        }

        public override void Enter()
        {
            base.Enter();
            _entered = true;
            Debug.Log("Entering Roulette State");
            _context.CameraController.MoveTo(CameraShotId.Roulette, () =>
            {
                if (!_entered) return;
                _active = true;
                _screen = _context.UIManager.ShowScreen<RouletteScreen>();
                _screen.SpinButtonClicked += OnSpinButtonClicked;
                _screen.NextButtonClicked += OnPlayButtonClicked;
                _rouletteManager.OnSpinCompleted += OnSpinCompleted;
                _rouletteManager.OnSpinCancelled += OnSpinCancelled;
                _rouletteManager.SpinRequested += OnRouletteClicked;
            

                if (!TryValidate(out string error))
                {
                    _screen.ShowError(error);
                    return;
                }
            
                if (!_progress.IsRoundPrepared) _rouletteManager.PrepareRound(_progress);

                _transitioning = true;
                _screen.ShowTransition("Preparando la ruleta...");
                _rouletteManager.Show(_progress, _context.RunData.CursedObject, () =>
                {
                    if (!_active) return;
                    _transitioning = false;
                    WeekDayBanner.Refresh(_progress);
                    _screen.ShowPreview(_progress, _rouletteManager.GetBossProbability(_progress.Day, _context.RunData.CursedObject));
                    if (_progress.HasResult)
                    {
                        _rouletteManager.ShowResult(_progress.CurrentResult);
                        _screen.ShowResult(_progress.CurrentResult, false);
                    }
                });
            });
            
        }

        private bool TryValidate(out string error)
        {
            if (!_rouletteManager.TryValidate(_progress, out error)) return false;
            if(_progress.IsComplete) error = "El juego ha finalizado. No se puede continuar.";
            return error == null;
        }

        // Both the UI button and clicking the model use this single operation.
        public void RequestSpin()
        {
            if (!_active || _transitioning || _spinning || _progress.IsRoundStarted || _progress.HasResult) return;
            if (!TryValidate(out string error))
            {
                _screen.ShowError(error);
                return;
            }

            try
            {
                _screen.ShowPreview(_progress, _rouletteManager.GetBossProbability(_progress.Day, _context.RunData.CursedObject));
                _screen.ShowSpinning();
                _spinning = true;
                _rouletteManager.Spin(_progress, _context.RunData.CursedObject);
            }
            catch (Exception exception) when (exception is ArgumentException || exception is InvalidOperationException)
            {
                _spinning = false;
                _screen.ShowError(exception.Message, true);
                Debug.LogException(exception);
            }
        }

        private void OnSpinButtonClicked() => RequestSpin();

        private void OnRouletteClicked()
        {
            if (!_active || _transitioning || _spinning) return;
            if (_progress.HasResult) OnPlayButtonClicked();
            else RequestSpin();
        }

        private void OnSpinCompleted(RouletteResult result)
        {
            if (!_active || !_spinning) return;
            _spinning = false;
            try
            {
                _progress.RecordResult(result);
                _screen.ShowResult(result, false);
                ScoreResult preview = DiceCalculator.PreviewTolerance(_context.RunData.CursedObject,
                    result.LuckyNumber, result.VictoryTolerance, result.Day);
                _rouletteManager.GetBossProbability(result.Day, _context.RunData.CursedObject, true);
                Debug.Log($"[Ruleta] Semana {result.Week}, día {result.Day} | Lucky Number: {result.LuckyNumber} | " +
                    $"Tolerancia base: +/-{result.VictoryTolerance} | " +
                    $"Rango para ganar: [{preview.MinimumScore}, {preview.MaximumScore}] (inclusive) | " +
                    $"Jefe: {_rouletteManager.GetBossProbability(result.Day)}% base -> {result.BossProbability}% aplicado | " +
                    $"Encuentro: {(result.IsBoss ? "JEFE" : "NORMAL")}");
            }
            catch (InvalidOperationException exception)
            {
                _screen.ShowError(exception.Message);
                Debug.LogException(exception);
            }
        }

        private void OnSpinCancelled()
        {
            if (!_active || !_spinning) return;
            _spinning = false;
            _screen.ShowError("Giro cancelado. Puedes volver a intentarlo.", true);
        }

        private void OnPlayButtonClicked()
        {
            if (!_active || _transitioning || _spinning || !_progress.HasResult) return;
            _transitioning = true;
            _screen.ShowTransition("Continuando...");
            _rouletteManager.Dismiss(() =>
            {
                if (!_active) return;
                _progress.StartRound();
                _stateMachine.ChangeState(new GameplayState(_context, _stateMachine));
            });
        }

        public override void Exit()
        {
            _entered = false;
            if (!_active) return;
            _active = false;
            _transitioning = false;
            _spinning = false;
            if (_screen != null)
            {
                _screen.SpinButtonClicked -= OnSpinButtonClicked;
                _screen.NextButtonClicked -= OnPlayButtonClicked;
            }
            _rouletteManager.OnSpinCompleted -= OnSpinCompleted;
            _rouletteManager.OnSpinCancelled -= OnSpinCancelled;
            _rouletteManager.SpinRequested -= OnRouletteClicked;
            _rouletteManager.Hide();
            base.Exit();
        }
    }
}
