
using System.Collections.Generic;
using System.Linq;
using Code.Scripts.Ambient;
using Code.Scripts.Bosses;
using Code.Scripts.Camera;
using Code.Scripts.Core;
using Code.Scripts.Utils;
using Code.Scripts.Dice;
using Code.Scripts.Rolling;
using Code.Scripts.Rolls;
using Code.Scripts.Roulette;
using Code.Scripts.Rounds;
using Code.Scripts.UI;
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
        private bool _wasWithinWinningRange;
        private BossSO _boss;
        private TableMood DayMood => _boss != null ? TableMood.Boss : TableMood.Normal;
        public GameplayState(
            GameContext context,
            IStateMachine stateMachine
        ) : base(context, stateMachine)
        {
        }

        public override void Enter()
        {
            base.Enter();

            _result = _context.RunData.Progress.CurrentResult;
            _boss = _result.IsBoss ? _context.RunData.BossOfWeek(_result.Week) : null;

            _context.CameraController.MoveTo(
                CameraShotId.Gameplay,
                () =>
                {
                    _screen = _context.UIManager.ShowScreen<GameplayScreen>();
                    _context.HoverController?.SetGameplayActive(true);

                    ScoreResult range = DiceCalculator.PreviewTolerance(
                        _context.RunData.CursedObject,
                        _result.LuckyNumber,
                        _result.VictoryTolerance,
                        _result.Day
                    );

                    _screen.ShowWinningScore(
                        range.MinimumScore,
                        range.MaximumScore
                    );

                    _screen.ShowBoss(_boss);
                    _context.SetTableMood(DayMood);
                    _context.DiceDrawer.Open(
                        _context.RunData.DiceCase
                    );

                    // ============================================
                    // CURSED OBJECTS - SHOTS AND REROLLS
                    // ============================================

                    // Presupuesto base de la ronda.
                    int shots = _context.GameConfig.shotsPerDay;
                    int rerolls = _context.GameConfig.rerollsPerDay;

                    // Indica si la ronda actual es de jefe.
                    bool isBoss = _result.IsBoss;

                    // RollSession interpreta Shots <= 0
                    // como lanzamientos ilimitados.
                    bool unlimitedShots = shots <= 0;

                    // Aplicar los efectos de todos los
                    // objetos malditos equipados.
                    foreach (
                        var cursedObject in
                        _context.RunData.CursedObject
                    )
                    {
                        if (cursedObject == null)
                            continue;

                        // Herradura invertida y futuros
                        // modificadores de lanzamientos.
                        if (!unlimitedShots)
                        {
                            shots = cursedObject.ModifyShots(
                                shots,
                                isBoss
                            );
                        }

                        // Daruma, Maneki-neko y futuros
                        // modificadores de rerolls.
                        rerolls = cursedObject.ModifyRerolls(
                            rerolls,
                            isBoss
                        );
                    }
                    
                    if (_boss != null)
                    {
                        if (!unlimitedShots)
                            shots = _boss.ModifyShots(shots, _context.RunData.DiceCase.Dice.Count);
                        rerolls = _boss.ModifyRerolls(rerolls);
                    }

                    // Evitar presupuestos negativos.
                    if (!unlimitedShots)
                    {
                        shots = Mathf.Max(1, shots);
                    }

                    rerolls = Mathf.Max(0, rerolls);

                    Debug.Log(
                        "[Gameplay] Recursos de la ronda | " +
                        $"Jefe: {isBoss} | " +
                        $"Lanzamientos: {(unlimitedShots ? "Ilimitados" : shots.ToString())} | " +
                        $"Rerolls: {rerolls}"
                    );

                    // ============================================
                    // CREATE ROLL SESSION
                    // ============================================

                    if (_boss != null) _boss.OnFightStart(_context);
                        
                    _session = new RollSession(
                        _context,
                        new RollSessionSettings
                        {
                            Shots = shots,
                            Rerolls = rerolls,
                            RestShot = CameraShotId.Gameplay,
                        }
                    );

                    _screen.TableControls.Bind(_session);

                    _session.AllSettled += OnTableSettled;
                    _session.DiceEliminated += OnDiceEliminated;
                    _session.BudgetChanged += RefreshBudget;
                    _session.RerollModeChanged += _screen.ShowRerollMode;

                    _screen.NextButtonClicked += OnCheckClicked;
                    _screen.RerollButtonClicked += _session.ToggleReroll;

                    _session.Start();
                }
            );
        }

        public override void Update()
        {
            base.Update();
            _session?.Tick();
            if (_session != null) _screen.SetCheckAvailable(!_ending && CanCheck());
        }
        
        private bool CanCheck() => _boss == null || _boss.CanCheck(_context, _session);

        public override void Exit()
        {
            base.Exit();
            _context.HoverController?.SetGameplayActive(false);

            _context.RouletteManager.SetWinningRangeHighlighted(false);
            _screen.TableControls.Unbind();

            _session.Stop();

            _session.AllSettled -= OnTableSettled;
            _session.DiceEliminated -= OnDiceEliminated;
            _session.BudgetChanged -= RefreshBudget;
            _session.RerollModeChanged -= _screen.ShowRerollMode;

            _screen.NextButtonClicked -= OnCheckClicked;
            _screen.RerollButtonClicked -= _session.ToggleReroll;
        }

        private void RefreshBudget()
        {
            _screen.ShowBudget(
                _session.Budget.Shots,
                _session.Budget.Rerolls
            );

            _screen.SetRerollAvailable(
                _session.CanReroll
            );
        }

        private void OnTableSettled()
        {
            UpdateScoreDisplay(CalculateScore());
        }

        private void OnDiceEliminated()
        {
            UpdateScoreDisplay(CalculateScore());
        }

        private void UpdateScoreDisplay(ScoreResult score)
        {
            if (score.IsSuccess && !_wasWithinWinningRange && AudioManager.Instance != null)
                AudioManager.Instance.PlayWinningScoreSFX();

            _wasWithinWinningRange = score.IsSuccess;
            _context.SetTableMood(score.IsSuccess ? TableMood.CanWin : DayMood);
            _screen.ShowScore(score.TotalScore);
            _context.RouletteManager.SetWinningRangeHighlighted(score.IsSuccess);
        }

        private ScoreResult CalculateScore()
        {
            return DiceCalculator.Calculate(
                _session.ThrownDice,
                _context.RunData.CursedObject,
                _result.LuckyNumber,
                _result.VictoryTolerance,
                _result.Day
            );
        }

        private void OnCheckClicked()
        {
            if (_ending || _session.IsAnimating)
                return;

            ScoreResult score = CalculateScore();

            if (score.IsSuccess)
            {
                EndDay(true);
            }
            else if (!_session.HasMovesLeft)  
            {
                EndDay(false);
            }

            UpdateScoreDisplay(score);
        }

        private void EndDay(bool success)
        {
            _ending = true;

            _session.Stop();

            if (success)
            {
                _context.RunData.Progress.CompleteRound();
                WeekDayBanner.Refresh(_context.RunData.Progress);
            }

            _session.ReturnAllToDrawer(() =>
            {
                if (_boss != null) _boss.OnFightEnd(_context);
                if (success && !_context.RunData.Progress.IsComplete) _context.SetTableMood(TableMood.Normal);   
                if (!success || _context.RunData.Progress.IsComplete)
                {
                    _context.DiceDrawer.Close();   
                }

                _stateMachine.ChangeState(
                    NextStateAfterDay(success)
                );
            });
        }

        private IState NextStateAfterDay(bool success)
        {
            if (!success)
            {
                return new GameOverState(
                    _context,
                    _stateMachine
                );
            }

            if (_context.RunData.Progress.IsComplete)
            {
                return new GameOverState(
                    _context,
                    _stateMachine,
                    victory: true
                );
            }
            
            if (_result.IsBoss)
            {
                return new CursedRewardState(
                    _context,
                    _stateMachine
                );
            }

            return new DiceCoinRewardState(
                _context,
                _stateMachine
            );
        }
    }
}
