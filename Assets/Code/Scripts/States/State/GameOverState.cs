using Code.Scripts.Ambient;
using Code.Scripts.Core;
using Code.Scripts.Roulette;
using Code.Scripts.UI;
using Code.Scripts.UI.Screens;
using UnityEngine;

namespace Code.Scripts.States.State
{
    public class GameOverState : AState
    {
        private GameOverScreen _screen;
        private readonly bool _victory;
        public GameOverState(GameContext context, IStateMachine stateMachine, bool victory = false) : base(context, stateMachine)
        {
            _victory = victory;
        }

        public override void Enter()
        {
            base.Enter();
            _screen = _context.UIManager.ShowScreen<GameOverScreen>();
            _screen.ReturnToMainMenuClicked += OnReturnToMainMenuClicked;
            _screen.ReplayClicked += OnReplayClicked;

            RouletteResult result = _context.RunData.Progress.CurrentResult;
            var boss = result.IsBoss ? _context.RunData.BossOfWeek(result.Week) : null;
            if (_victory) _screen.ShowVictory(result, boss);
            else _screen.ShowDefeat(result, boss);
            _screen.ShowDice(_context.RunData.DiceCase.Dice);
            _context.SetTableMood(_victory ? TableMood.Victory : TableMood.GameOver);
        }

        public override void Exit()
        {
            base.Exit();
            Unsubscribe();
        }

        private void OnReplayClicked()
        {
            Unsubscribe();
            RunSetup.StartNewRun(_context);
            _stateMachine.ChangeState(new RouletteState(_context, _stateMachine));
        }

        private void OnReturnToMainMenuClicked()
        {
            Unsubscribe();
            ReturnToMainMenu(_context, _stateMachine);
        }

        public static void ReturnToMainMenu(GameContext context, IStateMachine stateMachine)
        {
            context.RunData = new RunData(context.GameConfig);
            WeekDayBanner.Refresh(context.RunData.Progress);
            context.DiceDrawer.Clear();
            context.SetTableMood(TableMood.Normal);
            stateMachine.ChangeState(new MainMenuState(context, stateMachine));
        }

        private void Unsubscribe()
        {
            _screen.ReturnToMainMenuClicked -= OnReturnToMainMenuClicked;
            _screen.ReplayClicked -= OnReplayClicked;
        }
    }
}