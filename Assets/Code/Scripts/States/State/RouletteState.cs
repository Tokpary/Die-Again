using Code.Scripts.UI.Screens;
using UnityEngine;

namespace Code.Scripts.States.State
{
    public class RouletteState : AState
    {
        
        private RouletteScreen _screen;
        
        public RouletteState(GameContext context, IStateMachine stateMachine) : base(context, stateMachine)
        {
        }
        
        public override void Enter()
        {
            base.Enter();
            Debug.Log("Entering Roulette State");
            _screen = _context.UIManager.ShowScreen<RouletteScreen>();
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
            _stateMachine.ChangeState(new GameplayState(_context, _stateMachine));
        }
    }
}