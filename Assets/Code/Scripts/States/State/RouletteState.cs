using System;
using Code.Scripts.Ambient;
using Code.Scripts.Bosses;
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
        private bool _bossCardPending;
        
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
                _screen.BossCardAcknowledged += OnBossCardAcknowledged;
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
                _screen.ShowTransition("Preparing the roulette...");
                _rouletteManager.Show(_progress, _context.RunData.CursedObject, () =>
                {
                    if (!_active) return;
                    _transitioning = false;
                    WeekDayBanner.Refresh(_progress);
                    _screen.ShowPreview(_progress, _rouletteManager.GetBossProbability(_progress.Day, _context.RunData.CursedObject));
                    _context.SetTableMood(_progress.HasResult && _progress.CurrentResult.IsBoss ? TableMood.Boss : TableMood.Normal);
                    if (_progress.HasResult)
                    {
                        _rouletteManager.ShowResult(_progress.CurrentResult);
                        PresentResult(_progress.CurrentResult);
                        ScoreResult range = CalculateWinningRange(_progress.CurrentResult);
                        _rouletteManager.ShowWinningRange(range.MinimumScore, range.MaximumScore);
                    }
                });
            });
            
        }

        private bool TryValidate(out string error)
        {
            if (!_rouletteManager.TryValidate(_progress, out error)) return false;
            if(_progress.IsComplete) error = "The run is over. You can't continue.";
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
            if (!_active || _transitioning || _spinning || _bossCardPending ||
                _screen.IsBossCardVisible) return;
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
                _context.SetTableMood(result.IsBoss ? TableMood.Boss : TableMood.Normal);
                if (AudioManager.Instance != null)
                {
                    if (result.IsBoss)
                        AudioManager.Instance.PlayBossRouletteResultSFX();
                    else
                        AudioManager.Instance.PlayNormalRouletteResultSFX();
                }

                PresentResult(result);
                ScoreResult preview = CalculateWinningRange(result);
                _rouletteManager.ShowWinningRange(preview.MinimumScore, preview.MaximumScore);
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

        private ScoreResult CalculateWinningRange(RouletteResult result)
        {
            return DiceCalculator.PreviewTolerance(_context.RunData.CursedObject,
                result.LuckyNumber, result.VictoryTolerance, result.Day);
        }

        private void PresentResult(RouletteResult result)
        {
            _screen.ShowResult(result, false);
            _bossCardPending = result.IsBoss;
            if (!result.IsBoss)
                return;

            BossSO boss = _context.RunData.BossOfWeek(result.Week);
            if (!_screen.ShowBossCard(boss))
                _screen.ShowError("No se pudo mostrar la tarjeta del jefe. Revisa la configuración en Unity.");
        }

        private void OnBossCardAcknowledged()
        {
            _bossCardPending = false;
            OnPlayButtonClicked();
        }

        private void OnSpinCancelled()
        {
            if (!_active || !_spinning) return;
            _spinning = false;
            _screen.ShowError("Spin cancelled. Try again.", true);
        }

        private void OnPlayButtonClicked()
        {
            if (!_active || _transitioning || _spinning || !_progress.HasResult ||
                _bossCardPending || _screen.IsBossCardVisible) return;
            _transitioning = true;
            _screen.ShowTransition("Continuing...");
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
                _screen.BossCardAcknowledged -= OnBossCardAcknowledged;
            }
            _rouletteManager.OnSpinCompleted -= OnSpinCompleted;
            _rouletteManager.OnSpinCancelled -= OnSpinCancelled;
            _rouletteManager.SpinRequested -= OnRouletteClicked;
            _rouletteManager.Hide();
            base.Exit();
        }
    }
}
