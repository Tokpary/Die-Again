using System.Collections.Generic;
using System.Linq;
using Code.Scripts.Ambient;
using Code.Scripts.Camera;
using Code.Scripts.Core;
using Code.Scripts.Dice;
using Code.Scripts.Rolling;
using Code.Scripts.Rolls;
using Code.Scripts.Shop;
using Code.Scripts.UI.Screens;
using UnityEngine;

namespace Code.Scripts.States.State
{
    public class ShopState : AState
    {
        private ShopScreen _screen;
        private RollSession _session;
        private ShopDisplayRotation _shopDisplay;
        private int _sum;
        private bool _busy;
        public ShopState(GameContext context, IStateMachine stateMachine) : base(context, stateMachine)
        {
        }
        
        public override void Enter()
        {
            base.Enter();
            _context.RunData.ShopOffer.RefreshShopOffer(_context.RunData.Progress.Week);
            _screen = _context.UIManager.Get<ShopScreen>();
            _screen.ShowOffer(_context.RunData.ShopOffer.Entries, _sum);

            _shopDisplay = UnityEngine.Object.FindFirstObjectByType<ShopDisplayRotation>();
            if (_shopDisplay == null)
            {
                Debug.LogError("ShopDisplayRotation is missing from the shop root. The shop will open without its entrance animation.");
                StartShop();
                return;
            }

            _shopDisplay.Show(StartShop);
        }

        private void StartShop()
        {
            _context.CameraController.MoveTo(CameraShotId.Gameplay, () =>
            {
                _screen = _context.UIManager.ShowScreen<ShopScreen>();
                _context.SetTableMood(TableMood.Shop);
                _context.DiceDrawer.Open();   
                _context.BankChest.Open();
                _session = new RollSession(_context, new RollSessionSettings
                {
                    Shots = 0,
                    Rerolls = 0,
                    RestShot = CameraShotId.Gameplay,
                    Views = new[] {CameraShotId.Drawer, CameraShotId.Chest, CameraShotId.Throwing, CameraShotId.CursedShelf, CameraShotId.Shop},
                });
                
                _screen.TableControls.Bind(_session);
                _session.AllSettled += OnTableSettled;
                _session.BudgetChanged += RefreshScreen;
                _screen.NextButtonClicked += OnNextClicked;
                
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
            _shopDisplay?.Hide();
            if(_session == null) return;
            
            _session.Stop();
            _screen.TableControls.Unbind();
            _session.AllSettled -= OnTableSettled;
            _session.BudgetChanged -= RefreshScreen;
            _screen.NextButtonClicked -= OnNextClicked;
        }

        private void OnTableSettled()
        {
            _sum = DiceCalculator.Calculate(_session.ThrownDice, _context.RunData.CursedObject,0,0).TotalScore;
            RefreshScreen();
        }
        
        private void RefreshScreen()
        {
            bool claim = _session.ThrownDice.Count > 0;
            bool hasPrize = _context.RunData.ShopOffer.Get(_sum) != null;

            _screen.ShowOffer(_context.RunData.ShopOffer.Entries, _sum);
            _screen.SetNextButton(claim, hasPrize, !_busy && NothingRolling());
        }

        private bool NothingRolling() => _session.ThrownDice.All(d => d.Motion.IsSettled);

        private void OnNextClicked()
        {
            if (_busy || !NothingRolling()) return;
            if (_session.ThrownDice.Count > 0) Claim();
            else Leave();
        }
        
        private bool TableIsStill() => _session.ThrownDice.Count > 0 && _session.ThrownDice.All(d => d.Motion.IsSettled);
        private void Claim()
        {
            DiceSO offered = _context.RunData.ShopOffer.Get(_sum); 

            _busy = true;
            _session.Stop();
            RefreshScreen();

            List<Dice.Dice> spent = _session.TakeThrownDice();
            foreach (Dice.Dice d in spent) _context.RunData.RemoveDice(d.Data);

            if (offered == null)
            {
                _context.CameraController.MoveTo(CameraShotId.Throwing, () =>
                    _context.PurchaseEffect.PlayFailed(spent, FinishPurchase, FlickerLight));
                return;
            }

            var bought = new DiceInstance(offered);
            if(!_context.RunData.DiceCase.TryAdd(bought))  
                Debug.LogWarning(("[ShopState] No se pudo añadir el dado comprado al inventario"));

            _context.CameraController.MoveTo(CameraShotId.Throwing, () =>
            {
                _context.PurchaseEffect.Play(spent, bought, FinishPurchase, FlashLight);
            });
        }
        
        private void FlashLight()
        {
            if (_context.TableLight != null) _context.TableLight.FlashClaim();
        }

        private void FlickerLight()
        {
            if (_context.TableLight != null) _context.TableLight.FlickerFail();
        }
        
        private void FinishPurchase()
        {
            _busy = false;
            _sum = 0;
            _session.Start();
            _session.GoBack();
            RefreshScreen();
        }
        
        private void Leave()
        {
            if(_busy) return;
            _busy = true;
            _session.Stop();
            RefreshScreen();
            
            _session.DiscardThrown(() =>
            {
                _context.DiceDrawer.Close();
                _context.BankChest.Close();
                if (_shopDisplay != null)
                    _shopDisplay.Hide(() => _stateMachine.ChangeState(new RouletteState(_context, _stateMachine)));
                else
                    _stateMachine.ChangeState(new RouletteState(_context, _stateMachine));
            });
        }
        
    }
}