using System;
using System.Collections.Generic;
using System.Linq;
using Code.Scripts.Core;
using Code.Scripts.Dice;
using Code.Scripts.Rolls;
using Code.Scripts.Rounds;
using DG.Tweening;
using UnityEngine;

namespace Code.Scripts.Rolling
{
    public class RollSession
    {
        #region VARIABLES

        private readonly GameContext _context;
        private readonly RollSessionSettings _settings;
        private readonly ThrownDiceTracker _tracker = new ThrownDiceTracker();
        private readonly List<Dice.Dice> _outOfDrawer = new List<Dice.Dice>();
        private readonly HashSet<Dice.Dice> _freeShots = new HashSet<Dice.Dice>();
        private readonly int _inFlightLayer;
        private readonly int _diceLayer;
        
        private bool _running;
        private Tween _cameraReturn;
        
        public RollSession(GameContext context, RollSessionSettings settings)
        {
            _context = context;
            _settings = settings;
            Budget = new RoundBudget(settings.Shots, settings.Rerolls);
            _inFlightLayer = LayerMask.NameToLayer("DiceInFlight");
            _diceLayer = LayerMask.NameToLayer("DiceLayer");
        }
        
        public RoundBudget Budget { get; }
        public IReadOnlyList<Dice.Dice> ThrownDice => _tracker.Dice;
        public bool IsRerolling { get; private set; }
        public bool UnlimitedShots => _settings.Shots <= 0;
        public bool CanReroll => !IsRerolling && Budget.Rerolls > 0 && _tracker.Dice.Count > 0;

        public event Action AllSettled;
        public event Action BudgetChanged;
        public event Action<bool> RerollModeChanged;
        #endregion

        #region START/END
        
        public void Start()
        {
            if (_running) return;
            _running = true;

            _context.DiceDragController.DroppedOnCannon += OnDiceDropped;
            _context.DiceDragController.DiceSelected += OnDiceSelected;
            _context.DiceThrower.OnDiceShot += OnDiceShot;
            _tracker.AllSettled += OnAllSettled;
            Budget.OnBudgetChanged += NotifyBudgetChanged;
            
            _context.GameInputs.Gameplay.Enable();
            NotifyBudgetChanged();
        }

        public void Stop()
        {
            if(!_running) return;
            _running = false;
            
            CancelCameraReturn();
            SetSelectionMode(false);
            _context.GameInputs.Gameplay.Disable();

            _context.DiceDragController.DroppedOnCannon -= OnDiceDropped;
            _context.DiceDragController.DiceSelected -= OnDiceSelected;
            _context.DiceThrower.OnDiceShot -= OnDiceShot;
            _tracker.AllSettled -= OnAllSettled;
            Budget.OnBudgetChanged -= NotifyBudgetChanged;
        }

        public void Tick()
        {
            if(!_running) return;
            foreach (Dice.Dice dice in _tracker.Dice.ToList())
            {
                Vector3 p = dice.transform.position;

                if (_context.PlayArea.HasFallen(p))
                {
                    OnDiceEscaped(dice);
                    continue;
                }

                if (dice.gameObject.layer == _inFlightLayer && _context.PlayArea.IsInside(p, 0.5f))
                    Utils.Utils.SetLayer(dice.gameObject, _diceLayer);
            }
        }
        
        #endregion

        #region LOAD/SHOT

        
        private void OnDiceDropped(Dice.Dice dice)
        {
            CancelCameraReturn();
            _context.DiceDrawer.Release(dice);
            if (!UnlimitedShots && Budget.Shots <= 0)
            {
                _context.DiceAnimator.ArcTo(new[] {dice}, _context.DiceDrawer.GetReturnPoint, d => _context.DiceDrawer.Add(d), null);
                return;
            }
            ReturnLoadedDiceToDrawer();
            
            _context.DiceThrower.LoadDice(dice);
            _outOfDrawer.Add(dice);
            _context.CameraController.MoveTo(CameraShotId.Throwing);
        }

        private void OnDiceShot(Dice.Dice obj)
        {
            if(!_freeShots.Remove(obj) && !UnlimitedShots) Budget.TryUseShot();
            _tracker.Add(obj);
            NotifyBudgetChanged();
        }
        
        private void OnAllSettled()
        {
            AllSettled.Invoke();
            CancelCameraReturn();
            
            if(IsRerolling || _context.DiceThrower.HasLoadedDice || _context.DiceDragController.SelectionMode) return;
            
            _cameraReturn = DOVirtual.DelayedCall(_settings.ResultViewDelay, () =>
            {
                _cameraReturn = null;
                _context.CameraController.MoveTo(_settings.RestShot);
            });
            
        }
        
        private void OnDiceEscaped(Dice.Dice dice)
        {
            _tracker.Remove(dice);
            _outOfDrawer.Remove(dice);
            NotifyBudgetChanged();
            
            _context.DiceAnimator.Vanish(new[] {dice}, onVanish =>
            {
                onVanish.transform.position = _context.DiceDrawer.DropPoint;
                _context.DiceAnimator.Appear(new[] {onVanish}, d => _context.DiceDrawer.Add(d), null);
                
            }, null);
        }
        
        #endregion

        #region REROLL

        public void ToggleReroll()
        {
            if (IsRerolling) return;
            if (_context.DiceDragController.SelectionMode)
            {
                SetSelectionMode(false);
                if(!_context.DiceThrower.HasLoadedDice)
                    _context.CameraController.MoveTo(_settings.RestShot);
                return;
            }
            
            if(!CanReroll)return;
            CancelCameraReturn();
            _context.GameInputs.Gameplay.Disable();
            
            _context.CameraController.MoveTo(_settings.AimShot, () =>
            {
                if (!_running) return;
                _context.GameInputs.Gameplay.Enable();
                SetSelectionMode(true);
            });
        }
        
        private void OnDiceSelected(Dice.Dice obj)
        {
            if(!_tracker.Dice.Contains(obj)) return;
            if(!Budget.TryUseReroll()) return;
            
            CancelCameraReturn();
            SetSelectionMode(false);
            IsRerolling = true;
            _context.GameInputs.Gameplay.Disable();

            ReturnLoadedDiceToDrawer();
            _tracker.Remove(obj);
            
            _context.DiceAnimator.Vanish(new[] {obj}, onVanish =>
            {
                _context.DiceThrower.LoadDice(onVanish);
                _freeShots.Add(onVanish);
                _context.DiceAnimator.Appear(new[] {onVanish}, null,  () =>
                {
                    IsRerolling = false;
                    _context.GameInputs.Gameplay.Enable();
                    NotifyBudgetChanged();
                });
            }, null);
        }
        
        
        private void ReturnLoadedDiceToDrawer()
        {
            Dice.Dice loadedDice = _context.DiceThrower.UnloadDice();
            if (loadedDice == null) return;
            _outOfDrawer.Remove(loadedDice);
            _freeShots.Remove(loadedDice);
            _context.DiceAnimator.ArcTo(new[] {loadedDice}, _context.DiceDrawer.GetReturnPoint, d => _context.DiceDrawer.Add(d), null);
        }
        
        private void SetSelectionMode(bool active)
        {
            if (_context.DiceDragController.SelectionMode == active) return;
            _context.DiceDragController.SelectionMode = active;
            RerollModeChanged?.Invoke(active);
        }

        #endregion
        
        #region END_SESSION

       public void ReturnAllToDrawer(Action onDone)
       {
           _context.DiceThrower.UnloadDice();
           var dice = new List<Dice.Dice>(_outOfDrawer);
           ClearTable();
           _context.DiceAnimator.ArcTo(dice, _context.DiceDrawer.GetReturnPoint,
               d => _context.DiceDrawer.Add(d), onDone);
       }

       public void DiscardThrown(Action onDone)
       {
           ReturnLoadedDiceToDrawer();
           var thrown = new List<Dice.Dice>(_tracker.Dice);
           ClearTable();
           _context.DiceAnimator.Vanish(thrown, d => _context.DiceDrawer.Discard(d, _context.RunData.DiceCase), onDone);
       }

       public void ClearTable()
       {
           _tracker.Clear();
           _outOfDrawer.Clear();
           _freeShots.Clear();
           NotifyBudgetChanged();
       }
       
       #endregion

        #region UTILS

       private void CancelCameraReturn()
       {
           if(_cameraReturn != null && _cameraReturn.IsActive()) _cameraReturn.Kill();
           _cameraReturn = null;
       }
        
       private void NotifyBudgetChanged()
       {
           BudgetChanged?.Invoke();
       }

       #endregion
       
    }
}