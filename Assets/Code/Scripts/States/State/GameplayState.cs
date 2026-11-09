using System.Collections.Generic;
using Code.Scripts.UI.Screens;
using UnityEngine;

namespace Code.Scripts.States.State
{
    public class GameplayState : AState
    {
        private GameplayScreen _screen;
        private readonly List<Dice.Dice> _outOfDrawer = new List<Dice.Dice>();
        private bool _ending;
        public GameplayState(GameContext context, IStateMachine stateMachine) : base(context, stateMachine)
        {
        }
        
        public override void Enter()
        {
            base.Enter();
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
        
        private void OnDiceDropped(Dice.Dice dice)
        {
            _context.DiceDrawer.Release(dice);
            _context.DiceThrower.LoadDice(dice);
            _outOfDrawer.Add(dice);
        }
    }
}