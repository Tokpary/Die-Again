using System;
using Code.Scripts.Roulette;
using Code.Scripts.UI.Screens;
using UnityEngine;

namespace Code.Scripts.States.State
{
    public class RouletteState : AState
    {
        private readonly System.Random _random = new System.Random();
        private RouletteScreen _screen;
        private bool _active;
        private bool _spinning;

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
            if (_context.RouletteView != null)
            {
                _context.RouletteView.SpinCompleted += OnSpinCompleted;
                _context.RouletteView.SpinCancelled += OnSpinCancelled;
            }

            if (!TryValidate(out string error))
            {
                _screen.ShowError(error);
                return;
            }

            int probability = _context.RouletteSettings.GetBossProbability(_context.Progress.Day);
            _context.RouletteView.Show(probability);
            _screen.ShowPreview(_context.Progress, probability);
            if (_context.Progress.HasResult)
            {
                _context.RouletteView.ShowResult(_context.Progress.CurrentResult);
                _screen.ShowResult(_context.Progress.CurrentResult, !_context.Progress.IsRoundStarted);
            }
        }

        private bool TryValidate(out string error)
        {
            error = null;
            if (_context.RouletteSettings == null || _context.RouletteView == null || _context.Progress == null)
                error = "Faltan referencias de la ruleta en GameManager.";
            else if (!_context.RouletteSettings.TryValidate(out error)) return false;
            else if (!_context.RouletteView.TryValidate(out error)) return false;
            else if (_context.Progress.IsComplete) error = "Partida completada: los tres jefes han sido derrotados.";
            else if (!_context.Progress.IsRoundPrepared)
                error = "La progresión debe preparar el rango antes de anunciarlo y entrar en la ruleta.";
            return error == null;
        }

        // The test button and future automatic entry both use this single operation.
        public void RequestSpin()
        {
            if (!_active || _spinning) return;
            if (!TryValidate(out string error))
            {
                _screen.ShowError(error);
                return;
            }
            if (_context.Progress.IsRoundStarted) return;

            try
            {
                int probability = _context.RouletteSettings.GetBossProbability(_context.Progress.Day);
                RouletteResult result = RouletteRules.Roll(_context.Progress, probability, _random);
                _context.Progress.ClearTestResult();
                _screen.ShowPreview(_context.Progress, probability);
                _screen.ShowSpinning();
                _spinning = true;
                _context.RouletteView.Spin(result);
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
                _context.Progress.RecordResult(result);
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
            if (!_active || _spinning || !_context.Progress.HasResult) return;
            _context.Progress.StartRound();
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
            if (_context.RouletteView != null)
            {
                _context.RouletteView.SpinCompleted -= OnSpinCompleted;
                _context.RouletteView.SpinCancelled -= OnSpinCancelled;
                _context.RouletteView.Hide();
            }
            base.Exit();
        }
    }
}
