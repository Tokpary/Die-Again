using System.Collections.Generic;
using System.Linq;
using Code.Scripts.Camera;
using Code.Scripts.Core;
using Code.Scripts.Utils;
using Code.Scripts.Dice;
using Code.Scripts.Rolling;
using Code.Scripts.Rolls;
using Code.Scripts.Roulette;
using Code.Scripts.Rounds;
using Code.Scripts.UI.Screens;
using DG.Tweening;
using UnityEngine;
using UnityEngine.SocialPlatforms.Impl;

namespace Code.Scripts.States.State
{
    public class GameplayState : AState
    {
        
        private GameplayScreen _screen;
        private RouletteResult _result;
        private bool _ending;
        private RollSession _session;
        
        public GameplayState(GameContext context, IStateMachine stateMachine) : base(context, stateMachine)
        {
        }
        
        public override void Enter()
        {
            base.Enter();
            _result = _context.RunData.Progress.CurrentResult;
            
            _context.CameraController.MoveTo(CameraShotId.Gameplay, () =>
            {
                
                _screen = _context.UIManager.ShowScreen<GameplayScreen>();
                _screen.ShowWinningScore(_result.LuckyNumber, _result.VictoryTolerance);
                _context.DiceDrawer.Open(_context.RunData.DiceCase);
                
                _session = new RollSession(_context, new RollSessionSettings
                {
                    Shots = _context.GameConfig.shotsPerDay,
                    Rerolls = _context.GameConfig.rerollsPerDay,
                    RestShot = CameraShotId.Gameplay,
                });
                _screen.TableControls.Bind(_session);

                _session.AllSettled += OnTableSettled;
                _session.BudgetChanged += RefreshBudget;
                _session.RerollModeChanged += _screen.ShowRerollMode;
                
                _screen.NextButtonClicked += OnCheckClicked;
                _screen.RerollButtonClicked += _session.ToggleReroll;
                
                _session.Start();
            });
        }

        public override void Update()
        {
            base.Update();
            _session?.Tick();
        }


        public override void Exit()
        {
            base.Exit();
            
            _screen.TableControls.Unbind();
            _session.Stop();
            _session.AllSettled -= OnTableSettled;
            _session.BudgetChanged -= RefreshBudget;
            _session.RerollModeChanged -= _screen.ShowRerollMode;
            _screen.NextButtonClicked -= OnCheckClicked;
            _screen.RerollButtonClicked -= _session.ToggleReroll;
        }
        


        private void RefreshBudget()
        {
            _screen.ShowBudget(_session.Budget.Shots, _session.Budget.Rerolls);
            _screen.SetRerollAvailable(_session.CanReroll);
        }

        private void OnTableSettled() => _screen.ShowScore(CalculateScore().TotalScore);
        private ScoreResult CalculateScore() => DiceCalculator.Calculate(_session.ThrownDice, _context.RunData.CursedObject, _result.LuckyNumber, _result.VictoryTolerance, _result.Day);
        
        private void OnCheckClicked()
        {
            if(_ending || _session.IsAnimating) return;
            ScoreResult score = CalculateScore();
            if (score.IsSuccess) EndDay(true);
            else if (_session.Budget.Shots <= 0) EndDay(false);
            _screen.ShowScore(score.TotalScore);
        }
        
        private void EndDay(bool success)
        {
            _ending = true;
            _session.Stop();
            if(success) _context.RunData.Progress.CompleteRound();
            
            _session.ReturnAllToDrawer(() =>
            {
                if (!success) _context.DiceDrawer.Close();   
                _stateMachine.ChangeState(NextStateAfterDay(success));
            });
        }
        private IState NextStateAfterDay(bool success)
        {
            if (!success) return new GameOverState(_context, _stateMachine);
            if (_result.IsBoss) return new ShopState(_context, _stateMachine);
            return new DiceCoinRewardState(_context, _stateMachine);
        }
    }
}