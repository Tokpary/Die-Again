using System;
using System.Collections.Generic;
using System.Linq;
using Code.Scripts.Camera;
using Code.Scripts.Core;
using Code.Scripts.Dice;
using Code.Scripts.Dice.DiceMaterials;
using Code.Scripts.Rolls;
using Code.Scripts.Rounds;
using DG.Tweening;
using UnityEngine;

namespace Code.Scripts.Rolling
{
    public class RollSession : IDiceHoverRules, IDiceTable
    {
        #region VARIABLES

        private readonly GameContext _context;
        private readonly RollSessionSettings _settings;
        private readonly ThrownDiceTracker _tracker = new ThrownDiceTracker();
        private readonly List<Dice.Dice> _outOfDrawer = new List<Dice.Dice>();
        private readonly HashSet<Dice.Dice> _freeShots = new HashSet<Dice.Dice>();
        private readonly Dictionary<Dice.Dice, DiceDrawer> _origins = new Dictionary<Dice.Dice, DiceDrawer>(); 
        private readonly List<Dice.Dice> _eliminated = new List<Dice.Dice>();  
        private readonly int _inFlightLayer;
        private readonly int _diceLayer;

        private bool _running;
        private bool _rerollSelecting;   
        private bool _placing;          
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
        public bool IsAnimating => IsRerolling || _placing;   
        public bool UnlimitedShots => _settings.Shots <= 0;
        public bool CanReroll => !IsAnimating && Budget.Rerolls > 0 && _tracker.Dice.Count > 0;
        public bool CanPlace => _running && !IsAnimating && (UnlimitedShots || Budget.Shots > 0);

        public CameraShotId CurrentView => _context.CameraController.CurrentShotId;
        public bool IsInHub => CurrentView == _settings.RestShot;
        public bool IsInDrawer => CurrentView == _settings.DrawerShot;
        public bool IsInChest => CurrentView == _settings.ChestShot;

        public event Action AllSettled;
        public event Action BudgetChanged;
        public event Action<bool> RerollModeChanged;
        public event Action ViewChanged;   
        
        private DiceDrawer ActiveContainer => IsInDrawer ? _context.DiceDrawer : IsInChest ? _context.BankChest : null;

        #endregion

        #region START/END

        public void Start()
        {
            if (_running) return;
            _running = true;
            DiceHover.HoverRules = this;
            _context.CameraViewInput.ViewRequested += OnViewRequested;
            _context.CameraViewInput.BackRequested += GoBack;
            _context.DiceDragController.DiceSelected += OnDiceSelected;
            _context.DiceDragController.DroppedOnTarget += OnDiceDroppedOnSlot;
            _context.DiceThrower.OnDiceShot += OnDiceShot;
            Dice.Dice.CollidedWithDice += OnDiceCollided;
            _tracker.AllSettled += OnAllSettled;
            Budget.OnBudgetChanged += NotifyBudgetChanged;

            _context.GameInputs.Gameplay.Enable();
            RefreshViewState();
            NotifyBudgetChanged();
        }

        public void Stop()
        {
            if (!_running) return;
            _running = false;

            CancelCameraReturn();
            SetRerollSelecting(false);
            UpdateDicePicking();   
            _context.GameInputs.Gameplay.Disable();
            _context.CameraViewInput.SetAvailableViews(null);

            if (DiceHover.HoverRules == this) DiceHover.HoverRules = null;
            _context.CameraViewInput.ViewRequested -= OnViewRequested;
            _context.CameraViewInput.BackRequested -= GoBack;
            _context.DiceDragController.DiceSelected -= OnDiceSelected;
            _context.DiceDragController.DroppedOnTarget -= OnDiceDroppedOnSlot;
            _context.DiceThrower.OnDiceShot -= OnDiceShot;
            Dice.Dice.CollidedWithDice -= OnDiceCollided;
            _tracker.AllSettled -= OnAllSettled;
            Budget.OnBudgetChanged -= NotifyBudgetChanged;
        }

        public void Tick()
        {
            if (!_running) return;
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

        #region VIEWS

        private void OnViewRequested(CameraShotId view)
        {
            if (!IsInHub || IsAnimating || Array.IndexOf(_settings.Views, view) < 0) return;
            CancelCameraReturn();
            GoTo(view);
        }

        public void GoBack()
        {
            if (!_running || IsInHub || IsAnimating) return;
            CancelCameraReturn();
            GoTo(_settings.RestShot);
        }

        private void GoTo(CameraShotId view, Action onArrived = null)
        {
            if (view != _settings.AimShot)
            {
                SetRerollSelecting(false);
                ReturnLoadedDiceToDrawer();   
            }

            _context.CameraController.MoveTo(view, onArrived);
            RefreshViewState();
        }

        private void RefreshViewState()
        {
            _context.CameraViewInput.SetAvailableViews(IsInHub ? _settings.Views : null);
            UpdateDicePicking();
            ViewChanged?.Invoke();
        }

        private void UpdateDicePicking()
        {
            _context.DiceDragController.SelectionMode = _running && _rerollSelecting; 
            _context.DiceDragController.ActiveContainer = _running ? ActiveContainer : null;
            _context.DiceDragController.DragEnabled = _running && ActiveContainer != null && CanPlace;
        }

        #endregion

        #region DRAWER: PLACE
        
        private void OnDiceDroppedOnSlot(Dice.Dice dice)
        {
            DiceDrawer container = ActiveContainer;
            if (!CanPlace || container == null || !container.Contains(dice))
            {
                dice.GetComponent<Rigidbody>().isKinematic = false;   
                return;
            }

            _placing = true;
            CancelCameraReturn();
            NotifyBudgetChanged();   

            container.Release(dice);
            _origins[dice] = container;
            ReturnLoadedDiceToDrawer();   
            _outOfDrawer.Add(dice);

            _context.DiceAnimator.Vanish(new[] { dice }, vanished =>
            {
                _context.DiceThrower.LoadDice(vanished);   
                GoTo(_settings.AimShot, () =>
                    _context.DiceAnimator.Appear(new[] { vanished }, null, () =>
                    {
                        _placing = false;
                        NotifyBudgetChanged();
                    }));
            }, null);
        }

        #endregion

        #region SHOT

        private void OnDiceShot(Dice.Dice dice)
        {
            if (!_freeShots.Remove(dice) && !UnlimitedShots) Budget.TryUseShot();
            _tracker.Add(dice);
            NotifyBudgetChanged();
        }

        private void OnAllSettled()
        {
            AllSettled?.Invoke();
            CancelCameraReturn();

            if (IsAnimating || _rerollSelecting || _context.DiceThrower.HasLoadedDice) return;
            if (CurrentView != _settings.AimShot) return;

            _cameraReturn = DOVirtual.DelayedCall(_settings.ResultViewDelay, () =>
            {
                _cameraReturn = null;
                GoTo(_settings.RestShot);
            });
        }

        private void OnDiceEscaped(Dice.Dice dice)
        {
            _tracker.Remove(dice);
            _outOfDrawer.Remove(dice);
            NotifyBudgetChanged();

            _context.DiceAnimator.Vanish(new[] { dice }, onVanish =>
            {
                DiceDrawer home = ContainerOf(onVanish);
                onVanish.transform.position = home.DropPoint;
                _context.DiceAnimator.Appear(new[] { onVanish }, d => home.Add(d), null);
            }, null);
        }

        #endregion

        #region COLLISIONS

        private void OnDiceCollided(Dice.Dice self, Dice.Dice other, Collision collision)
        {
            if (!IsInPlay(self) || !IsInPlay(other)) return;
            DiceMaterial material = self.Data.DiceMaterial;
            if (material == null) return;

            Vector3 point = collision.contactCount > 0 ? collision.GetContact(0).point : self.transform.position;
            material.OnCollision(new DiceCollision(self, other, point, collision.relativeVelocity.magnitude, this));
        }

        public bool IsInPlay(Dice.Dice dice) => _running && _tracker.Dice.Contains(dice);

        public void Eliminate(Dice.Dice dice)
        {
            if (!IsInPlay(dice)) return;
            _tracker.Remove(dice);   
            _outOfDrawer.Remove(dice);
            _eliminated.Add(dice);
            NotifyBudgetChanged();

            Rigidbody rb = dice.GetComponent<Rigidbody>();
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
            dice.transform.DOScale(Vector3.zero, _settings.EliminateDuration).SetEase(Ease.InBack)
                .SetLink(dice.gameObject)
                .OnComplete(() => dice.gameObject.SetActive(false));
        }

        private void BringBack(List<Dice.Dice> dice, List<DiceDrawer> homes, int firstReturnIndex, Action onDone)
        {
            for (int i = 0; i < dice.Count; i++)
            {
                dice[i].transform.DOKill();   
                dice[i].transform.position = homes[i].GetReturnPoint(firstReturnIndex + i);
                dice[i].gameObject.SetActive(true);
            }
            _context.DiceAnimator.Appear(dice, d => homes[dice.IndexOf(d)].Add(d), onDone);
        }

        private void ConsumeEliminated()
        {
            foreach (Dice.Dice dice in _eliminated)
            {
                _context.RunData.RemoveDice(dice.Data);
                UnityEngine.Object.Destroy(dice.gameObject);
            }
            _eliminated.Clear();
        }

        #endregion
        
        #region REROLL

        public void ToggleReroll()
        {
            if (IsAnimating) return;

            if (_rerollSelecting)
            {
                SetRerollSelecting(false);   
                if (!_context.DiceThrower.HasLoadedDice) GoTo(_settings.RestShot);
                return;
            }

            if (!CanReroll) return;
            CancelCameraReturn();
            _context.GameInputs.Gameplay.Disable();   

            GoTo(_settings.AimShot, () =>
            {
                if (!_running) return;
                _context.GameInputs.Gameplay.Enable();
                SetRerollSelecting(true);
            });
        }

        private void OnDiceSelected(Dice.Dice dice)
        {
            if (_rerollSelecting) TryReroll(dice);
        }

        private void TryReroll(Dice.Dice dice)
        {
            if (!_tracker.Dice.Contains(dice)) return;   
            if (!Budget.TryUseReroll()) return;

            CancelCameraReturn();
            SetRerollSelecting(false);
            IsRerolling = true;
            _context.GameInputs.Gameplay.Disable();

            ReturnLoadedDiceToDrawer();
            _tracker.Remove(dice);

            _context.DiceAnimator.Vanish(new[] { dice }, onVanish =>
            {
                _context.DiceThrower.LoadDice(onVanish);
                _freeShots.Add(onVanish);  
                _context.DiceAnimator.Appear(new[] { onVanish }, null, () =>
                {
                    IsRerolling = false;
                    if (_running) _context.GameInputs.Gameplay.Enable();
                    NotifyBudgetChanged();
                });
            }, null);
        }

        private void SetRerollSelecting(bool active)
        {
            if (_rerollSelecting == active) return;
            _rerollSelecting = active;
            UpdateDicePicking();
            RerollModeChanged?.Invoke(active);
        }

        private void ReturnLoadedDiceToDrawer()
        {
            Dice.Dice loadedDice = _context.DiceThrower.UnloadDice();
            if (loadedDice == null) return;
            _outOfDrawer.Remove(loadedDice);
            _freeShots.Remove(loadedDice);
            DiceDrawer home = ContainerOf(loadedDice);
            _context.DiceAnimator.ArcTo(new[] { loadedDice }, home.GetReturnPoint, d => home.Add(d), null);
        }

        #endregion

        #region END_SESSION

        public void ReturnAllToDrawer(Action onDone)
        {
            _context.DiceThrower.UnloadDice();
            var dice = new List<Dice.Dice>(_outOfDrawer);
            var homes = dice.Select(ContainerOf).ToList();
            var eliminated = new List<Dice.Dice>(_eliminated);
            var eliminatedHomes = eliminated.Select(ContainerOf).ToList();
            ClearTable();

            int pending = 2;
            void Done()
            {
                if (--pending == 0) onDone?.Invoke();
            }

            _context.DiceAnimator.ArcTo(dice, _context.DiceDrawer.GetReturnPoint,
                d => _context.DiceDrawer.Add(d), Done);
            BringBack(eliminated, eliminatedHomes, dice.Count, Done);
        }

        public void DiscardThrown(Action onDone)
        {
            List<Dice.Dice> thrown = TakeThrownDice();
            _context.DiceAnimator.Vanish(thrown, d =>
            {
                _context.RunData.RemoveDice(d.Data);   
                UnityEngine.Object.Destroy(d.gameObject);
            }, onDone);
        }

        public List<Dice.Dice> TakeThrownDice()
        {
            ReturnLoadedDiceToDrawer();
            var thrown = new List<Dice.Dice>(_tracker.Dice);
            ClearTable();
            return thrown;
        }

        public void ClearTable()
        {
            _origins.Clear();
            _tracker.Clear();
            _outOfDrawer.Clear();
            _freeShots.Clear();
            NotifyBudgetChanged();
        }

        #endregion

        #region UTILS

        private void CancelCameraReturn()
        {
            if (_cameraReturn != null && _cameraReturn.IsActive()) _cameraReturn.Kill();
            _cameraReturn = null;
        }
        private DiceDrawer ContainerOf(Dice.Dice dice) =>
            _origins.TryGetValue(dice, out DiceDrawer home) ? home : _context.DiceDrawer;
        

        private void NotifyBudgetChanged()
        {
            UpdateDicePicking();
            BudgetChanged?.Invoke();
        }

        #endregion

        public bool CanHover(Dice.Dice dice, CameraShotId view)
        {
            if (!_running || Array.IndexOf(_settings.HoverViews, view) < 0) return false;
            if (_context.DiceDrawer.Contains(dice) || (_context.BankChest != null && _context.BankChest.Contains(dice))) return true;
            return _tracker.Dice.Contains(dice) && dice.Motion.IsSettled;
        }

        public IReadOnlyList<Dice.Dice> ThrownDices { get; }

    }
}