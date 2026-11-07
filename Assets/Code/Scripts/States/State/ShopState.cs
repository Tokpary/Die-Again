using Code.Scripts.UI.Screens;
using UnityEngine;

namespace Code.Scripts.States.State
{
    public class ShopState : AState
    {
        private ShopScreen _screen;
        public ShopState(GameContext context, IStateMachine stateMachine) : base(context, stateMachine)
        {
        }
        
        public override void Enter()
        {
            base.Enter();
            Debug.Log("Entering Shop State");
            _screen = _context.UIManager.ShowScreen<ShopScreen>();
            _screen.NextButtonClicked += OnPlayButtonClicked;
        }
        
        public override void Exit()
        {
            base.Exit();
            _screen.NextButtonClicked -= OnPlayButtonClicked;
        }
        
        private void OnPlayButtonClicked()
        {
            _screen.NextButtonClicked -= OnPlayButtonClicked;
            _stateMachine.ChangeState(new RouletteState(_context, _stateMachine));
        }
    }
}