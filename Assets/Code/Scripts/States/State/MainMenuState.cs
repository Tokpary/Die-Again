using Code.Scripts.UI;
using Code.Scripts.UI.Screens;
using UnityEngine;

namespace Code.Scripts.States.State
{
    public class MainMenuState : AState
    {
        private MainMenuScreen _screen;
        
        public MainMenuState(GameContext context, IStateMachine stateMachine) : base(context, stateMachine)
        {
        }
        
        public override void Enter()
        {
            base.Enter();
            Debug.Log("Entering Main Menu State");
            _screen = _context.UIManager.ShowScreen<MainMenuScreen>();
            _screen.PlayButtonClicked += OnPlayButtonClicked;
        }

        public override void Exit()
        {
            base.Exit();
            _screen.PlayButtonClicked -= OnPlayButtonClicked;
        }

        private void OnPlayButtonClicked()
        {
            _screen.PlayButtonClicked -= OnPlayButtonClicked;
            _stateMachine.ChangeState(new RouletteState(_context, _stateMachine));
        }
    }
}