using System;
using Code.Scripts.Core;
using Code.Scripts.Roulette;
using Code.Scripts.UI.Screens;
using UnityEngine;

namespace Code.Scripts.States.State
{
    public class RouletteState : AState
    {
        private RouletteScreen _screen;
        private bool _active;
        private bool _spinning;
        
        private RouletteManager _rouletteManager => _context.RouletteManager;
        private GameProgress _progress => _context.RunData.Progress;
        public RouletteState(GameContext context, IStateMachine stateMachine) : base(context, stateMachine)
        {
        }

        public override void Enter()
        {
            base.Enter();
            Debug.Log("Entering Roulette State");
            _active = true;
            _screen = _context.UIManager.ShowScreen<RouletteScreen>();
            _screen.SpinButtonClicked += OnSpinButtonClicked;
            _screen.NextButtonClicked += OnPlayButtonClicked;
            _rouletteManager.OnSpinCompleted += OnSpinCompleted;
            _rouletteManager.OnSpinCancelled += OnSpinCancelled;

            if (!TryValidate(out string error))
            {
                _screen.ShowError(error);
                return;
            }
            
            if (!_progress.IsRoundPrepared) _rouletteManager.PrepareRound(_progress);

            _rouletteManager.Show(_progress);
            _screen.ShowPreview(_progress, _rouletteManager.GetBossProbability(_progress.Day));
            if (_progress.HasResult)
            {
               _rouletteManager.ShowResult(_progress.CurrentResult);
                _screen.ShowResult(_progress.CurrentResult, !_progress.IsRoundStarted);
            }
        }

        private bool TryValidate(out string error)
        {
            if (!_rouletteManager.TryValidate(_progress, out error)) return false;
            if(_progress.IsComplete) error = "El juego ha finalizado. No se puede continuar.";
            return error == null;
        }

        // The test button and future automatic entry both use this single operation.
        public void RequestSpin()
        {
            if (!_active || _spinning || _progress.IsRoundStarted) return;
            if (!TryValidate(out string error))
            {
                _screen.ShowError(error);
                return;
            }

            try
            {
                _screen.ShowPreview(_progress, _rouletteManager.GetBossProbability(_progress.Day));
                _screen.ShowSpinning();
                _spinning = true;
                _rouletteManager.Spin(_progress);
            }
            catch (Exception exception) when (exception is ArgumentException || exception is InvalidOperationException)
            {
                _spinning = false;
                _screen.ShowError(exception.Message, true);
                Debug.LogException(exception);
            }
        }

        private void OnSpinButtonClicked() => RequestSpin();

        private void OnSpinCompleted(RouletteResult result)
        {
            if (!_active || !_spinning) return;
            _spinning = false;
            try
            {
                _progress.RecordResult(result);
                _screen.ShowResult(result, true);
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
            if (!_active || _spinning || !_progress.HasResult) return;
            _progress.StartRound();
            _stateMachine.ChangeState(new GameplayState(_context, _stateMachine));
        }

        public override void Exit()
        {
            if (!_active) return;
            _active = false;
            _spinning = false;
            if (_screen != null)
            {
                _screen.SpinButtonClicked -= OnSpinButtonClicked;
                _screen.NextButtonClicked -= OnPlayButtonClicked;
            }
            _rouletteManager.OnSpinCompleted -= OnSpinCompleted;
            _rouletteManager.OnSpinCancelled -= OnSpinCancelled;
            _rouletteManager.Hide();
            base.Exit();
        }
    }
}
