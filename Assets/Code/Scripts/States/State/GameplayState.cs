using Code.Scripts.UI.Screens;
using UnityEngine;

namespace Code.Scripts.States.State
{
    public class GameplayState : AState
    {
        private GameplayScreen _screen;
        public GameplayState(GameContext context, IStateMachine stateMachine) : base(context, stateMachine)
        {
        }
        
        public override void Enter()
        {
            base.Enter();
            Debug.Log("Entering gameplay State");
            _screen = _context.UIManager.ShowScreen<GameplayScreen>();
            _screen.NextButtonClicked += OnPlayButtonClicked;
            _context.GameInputs.Gameplay.Enable();
        }
        
        public override void Exit()
        {
            base.Exit();
            _screen.NextButtonClicked -= OnPlayButtonClicked;
            _context.GameInputs.Gameplay.Disable();
        }

        private void OnPlayButtonClicked()
        {
            _screen.NextButtonClicked -= OnPlayButtonClicked;
            _stateMachine.ChangeState(new ShopState(_context, _stateMachine));
        }
    }
}