using System.Collections.Generic;
using System.Linq;
using Code.Scripts.Core;
using Code.Scripts.Dice;
using Code.Scripts.Rolling;
using Code.Scripts.Rolls;
using Code.Scripts.UI.Screens;
using UnityEngine;

namespace Code.Scripts.States.State
{
    public class ShopState : AState
    {
        private ShopScreen _screen;
        private RollSession _session;
        private int _sum;
        private bool _busy;
        public ShopState(GameContext context, IStateMachine stateMachine) : base(context, stateMachine)
        {
        }
        
        public override void Enter()
        {
            base.Enter();
            _context.RunData.ShopOffer.RefreshShopOffer(_context.RunData.Progress.Week);
            
            _context.CameraController.MoveTo(CameraShotId.Shop, () =>
            {
                _screen = _context.UIManager.ShowScreen<ShopScreen>();
                _context.DiceDrawer.Open(_context.RunData.DiceCase);
                _session = new RollSession(_context, new RollSessionSettings
                {
                    Shots = 0,
                    Rerolls = 0,
                    RestShot = CameraShotId.Shop
                });
                _session.AllSettled += OnTableSettled;
                _session.BudgetChanged += RefreshScreen;
                _screen.ClaimButtonClicked += OnClaimClicked;
                _screen.NextButtonClicked += OnLeaveButtonClicked;
                
                RefreshScreen();
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
            if(_session == null) return;
            
            _session.Stop();
            _session.AllSettled -= OnTableSettled;
            _session.BudgetChanged -= RefreshScreen;
            _screen.ClaimButtonClicked -= OnClaimClicked;
            _screen.NextButtonClicked -= OnLeaveButtonClicked;
        }

        private void OnTableSettled()
        {
            _sum = DiceCalculator.Calculate(_session.ThrownDice, _context.RunData.CursedObject,0,0).TotalScore;
            RefreshScreen();
        }
        
        private void RefreshScreen()
        {
            bool canClaim = !_busy && TableIsStill() && _context.RunData.ShopOffer.Get(_sum) != null;
            
            _screen.ShowOffer(_context.RunData.ShopOffer.Dice, _sum);
            _screen.ShowSum(_sum);
            _screen.SetClaimAvailable(canClaim);
            _screen.SetLeaveAvailable(!_busy && TableIsStill());
        }
        
        private bool TableIsStill() => _session.ThrownDice.Count > 0 && _session.ThrownDice.All(d => d.Motion.IsSettled);
        
        private void OnClaimClicked()
        {
            if(_busy || !TableIsStill()) return;
            DiceSO offered = _context.RunData.ShopOffer.Get(_sum);
            if(offered == null) return;
            
            _busy = true;
            _session.Stop();
            RefreshScreen();

            List<Dice.Dice> spent = _session.TakeThrownDice();
            foreach (Dice.Dice d in spent) _context.RunData.DiceCase.Remove(d.Data);
            var bought = new DiceInstance(offered);
            if(!_context.RunData.DiceCase.TryAdd(bought))  
                Debug.LogWarning(("[ShopState] No se pudo añadir el dado comprado al inventario"));
            
            _context.PurchaseEffect.Play(spent, bought, FinishPurchase);
        }
        
        private void FinishPurchase()
        {
            _busy = false;
            _sum = 0;
            _context.CameraController.MoveTo(CameraShotId.Shop);
            _session.Start();
            RefreshScreen();
        }
        
        private void OnLeaveButtonClicked()
        {
            if(_busy) return;
            _busy = true;
            _session.Stop();
            RefreshScreen();
            
            List<Dice.Dice> spent = _session.TakeThrownDice();
            _context.DiceAnimator.Vanish(spent, d => _context.DiceDrawer.Discard(d, _context.RunData.DiceCase),
                () =>
                {
                    _context.DiceDrawer.Close();
                    _stateMachine.ChangeState(new RouletteState(_context, _stateMachine));
                });
        }
        
    }
}