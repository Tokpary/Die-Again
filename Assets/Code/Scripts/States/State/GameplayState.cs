using System.Collections.Generic;
using Code.Scripts.Core;
using Code.Scripts.Roulette;
using Code.Scripts.UI.Screens;
using UnityEngine;

namespace Code.Scripts.States.State
{
    public class GameplayState : AState
    {
        private ScoreResult _scoreResult;
        private GameplayScreen _screen;
        private readonly List<Dice.Dice> _outOfDrawer = new List<Dice.Dice>();
        private RouletteResult _result;
        private bool _ending;
        public GameplayState(GameContext context, IStateMachine stateMachine) : base(context, stateMachine)
        {
        }
        
        public override void Enter()
        {
            base.Enter();
            _result = _context.RunData.Progress.CurrentResult;
            Debug.Log("Entering gameplay State");
            _screen = _context.UIManager.ShowScreen<GameplayScreen>();
            _screen.NextButtonClicked += OnPlayButtonClicked;
            _context.DiceDragController.DroppedOnCannon += OnDiceDropped;
            _context.GameInputs.Gameplay.Enable();
            _context.DiceDrawer.Open(_context.RunData.DiceCase);
        }
        
        public override void Exit()
        {
            base.Exit();
            _screen.NextButtonClicked -= OnPlayButtonClicked;
            _context.DiceDragController.DroppedOnCannon -= OnDiceDropped;
        }

        private void OnPlayButtonClicked()
        {
            if(CheckIfWinnerValue())
            {
                if(_ending) return;
                _ending = true;
                _screen.NextButtonClicked -= OnPlayButtonClicked;
                _context.GameInputs.Gameplay.Disable();
                _context.RunData.Progress.CompleteRound();
                _context.DiceThrower.UnloadDice();
                _context.DiceAnimator.ArcTo(_outOfDrawer, _context.DiceDrawer.GetReturnPoint, dice => _context.DiceDrawer.Add(dice),
                    () =>
                    {
                        _context.DiceDrawer.Close();
                        _stateMachine.ChangeState(new ShopState(_context, _stateMachine));
                    });
            }
            else
            {
                // Aqui hay que comprobar contra el sistema de tiradas y reroll para ver si todavia puede hacer algo o no
                Debug.Log("Dice Values not equals to target value: " + _scoreResult.TotalScore + " vs " + (_scoreResult.TargetScore - _scoreResult.Tolerance) + "-" + (_scoreResult.TargetScore + _scoreResult.Tolerance));
            }
        }
        
        private bool CheckIfWinnerValue()
        {
            _scoreResult = DiceCalculator.Calculate(_outOfDrawer, _context.RunData.CursedObject, _result.LuckyNumber, _result.VictoryTolerance);
            return _scoreResult.IsSuccess;
        }
        
        private void OnDiceDropped(Dice.Dice dice)
        {
            _context.DiceDrawer.Release(dice);
            _context.DiceThrower.LoadDice(dice);
            _outOfDrawer.Add(dice);
        }
    }
}