using System.Collections.Generic;
using System.Linq;
using Code.Scripts.Core;
using Code.Scripts.Dice;
using Code.Scripts.Roulette;
using Code.Scripts.Rounds;
using Code.Scripts.UI.Screens;
using UnityEngine;

namespace Code.Scripts.States.State
{
    public class GameplayState : AState
    {
        private ScoreResult _scoreResult;
        private RoundBudget _budget;
        private ThrownDiceTracker _thrownDiceTracker;
        private GameplayScreen _screen;
        private readonly List<Dice.Dice> _outOfDrawer = new List<Dice.Dice>();
        private RouletteResult _result;
        private bool _ending;
        private readonly HashSet<Dice.Dice> _freeShots = new HashSet<Dice.Dice>();
        private bool _rerolling;
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
            _screen.ShowWinningScore(_result.LuckyNumber, _result.VictoryTolerance);
            _context.DiceDragController.DroppedOnCannon += OnDiceDropped;
            _context.GameInputs.Gameplay.Enable();
            _context.DiceDrawer.Open(_context.RunData.DiceCase);
            _budget = new RoundBudget(_context.GameConfig.shotsPerDay, _context.GameConfig.rerollsPerDay);
            _budget.OnBudgetChanged += RefreshBudget;
            _thrownDiceTracker = new ThrownDiceTracker();
            _thrownDiceTracker.AllSettled += OnAllDiceSettled;
            _context.DiceThrower.OnDiceShot += OnDiceShot;
            _screen.RerollButtonClicked += OnRerollButtonClicked;
            _context.DiceDragController.DiceSelected += OnDiceSelected;
            RefreshBudget();
        }

        private void OnDiceSelected(Dice.Dice obj)
        {
            if(!_thrownDiceTracker.Dice.Contains(obj)) return;
            if(!_budget.TryUseReroll()) return;
            
            SetSelectionMode(false);
            _rerolling = true;
            _context.GameInputs.Gameplay.Disable();

            ReturnLoadedDiceToDrawer();
            _thrownDiceTracker.Remove(obj);
            
            _context.DiceAnimator.Vanish(new[] {obj}, onVanish =>
            {
                _context.DiceThrower.LoadDice(onVanish);
                _freeShots.Add(onVanish);
                _context.DiceAnimator.Appear(new[] {onVanish}, null,  () =>
                {
                    _rerolling = false;
                    _context.GameInputs.Gameplay.Enable();
                    RefreshBudget();
                });
            }, null);
        }

        private void ReturnLoadedDiceToDrawer()
        {
            Dice.Dice loadedDice = _context.DiceThrower.UnloadDice();
            if (loadedDice == null) return;
            _outOfDrawer.Remove(loadedDice);
            _freeShots.Remove(loadedDice);
            _context.DiceDrawer.Release(loadedDice);
            _context.DiceAnimator.ArcTo(new[] {loadedDice}, _context.DiceDrawer.GetReturnPoint, d => _context.DiceDrawer.Add(d), null);
        }

        private void OnRerollButtonClicked()
        {
            if (_rerolling) return;
            if (_context.DiceDragController.SelectionMode)
            {
                SetSelectionMode(false);   
                return;
            }
            if (_budget.Rerolls <= 0 || _thrownDiceTracker.Dice.Count == 0) return;
            SetSelectionMode(true);
        }

        private void SetSelectionMode(bool active)
        {
            _context.DiceDragController.SelectionMode = active;
            _screen.ShowRerollMode(active);
        }

        private void OnDiceShot(Dice.Dice obj)
        {
            if(!_freeShots.Remove(obj)) _budget.TryUseShot();
            _thrownDiceTracker.Add(obj);
            RefreshBudget();
        }

        private void OnAllDiceSettled()
        {
            _scoreResult = DiceCalculator.Calculate(_thrownDiceTracker.Dice, _context.RunData.CursedObject, _result.LuckyNumber, _result.VictoryTolerance);
            _screen.ShowScore(_scoreResult.TotalScore);
        }

        private void RefreshBudget()
        {
            _screen.ShowBudget(_budget.Shots, _budget.Rerolls);
            _screen.SetRerollAvailable(_budget.Rerolls > 0 && _outOfDrawer.Count > 0);
        }

        public override void Exit()
        {
            base.Exit();
            _screen.NextButtonClicked -= OnPlayButtonClicked;
            _context.DiceDragController.DroppedOnCannon -= OnDiceDropped;
            _budget.OnBudgetChanged -= RefreshBudget;
            _thrownDiceTracker.AllSettled -= OnAllDiceSettled;
            _thrownDiceTracker.Clear();
            _context.DiceThrower.OnDiceShot -= _thrownDiceTracker.Add;
            _screen.RerollButtonClicked -= OnRerollButtonClicked;
            _context.DiceDragController.DiceSelected -= OnDiceSelected;
            _context.DiceDragController.SelectionMode = false;
        }

        private void OnPlayButtonClicked()
        {
            if(_rerolling) return;
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
                if(_budget.Shots <= 0)
                {
                    _context.GameInputs.Gameplay.Disable();
                    _context.DiceThrower.UnloadDice();
                    _context.DiceAnimator.ArcTo(_outOfDrawer, _context.DiceDrawer.GetReturnPoint, dice => _context.DiceDrawer.Add(dice),
                        () =>
                        {
                            _context.DiceDrawer.Close();
                            _stateMachine.ChangeState(new GameOverState(_context, _stateMachine));
                        });
                    return;
                }
                Debug.Log("Dice Values not equals to target value: " + _scoreResult.TotalScore + " vs " + (_scoreResult.TargetScore - _scoreResult.Tolerance) + "-" + (_scoreResult.TargetScore + _scoreResult.Tolerance));
            }
        }
        
        private bool CheckIfWinnerValue()
        {
            _scoreResult = DiceCalculator.Calculate(_thrownDiceTracker.Dice, _context.RunData.CursedObject, _result.LuckyNumber, _result.VictoryTolerance);
            return _scoreResult.IsSuccess;
        }
        
        private void OnDiceDropped(Dice.Dice dice)
        {
            _context.DiceDrawer.Release(dice);
            if (_budget.Shots <= 0)
            {
                _context.DiceAnimator.ArcTo(new[] {dice}, _context.DiceDrawer.GetReturnPoint, d => _context.DiceDrawer.Add(d), null);
                return;
            }
            _context.DiceThrower.LoadDice(dice);
            _outOfDrawer.Add(dice);
        }
    }
}