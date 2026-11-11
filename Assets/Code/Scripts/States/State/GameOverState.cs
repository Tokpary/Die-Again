using Code.Scripts.Camera;
using Code.Scripts.Core;
using Code.Scripts.UI;
using Code.Scripts.UI.Screens;
using UnityEngine;

namespace Code.Scripts.States.State
{
    public class GameOverState : AState
    {
        private GameOverScreen _screen;

        public GameOverState(GameContext context, IStateMachine stateMachine) : base(context, stateMachine)
        {
        }

        public override void Enter()
        {
            base.Enter();
            Debug.Log("Entering Main Menu State");
            _screen = _context.UIManager.ShowScreen<GameOverScreen>();
            _screen.ReturnToMainMenuClicked += OnReturnToMainMenuClicked;
        }

        public override void Exit()
        {
            base.Exit();
            _screen.ReturnToMainMenuClicked -= OnReturnToMainMenuClicked;
            _context.CameraController.MoveTo(CameraShotId.MainMenu);
        }

        private void OnReturnToMainMenuClicked()
        {
            _screen.ReturnToMainMenuClicked -= OnReturnToMainMenuClicked;
            _context.RunData = new RunData(_context.GameConfig); 
            _context.DiceDrawer.Clear();
            _stateMachine.ChangeState(new MainMenuState(_context, _stateMachine));
        }
    }
}
