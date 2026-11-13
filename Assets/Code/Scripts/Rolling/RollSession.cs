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
        private int _returning;
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

        public bool HasMovesLeft
        {
            get
            {
                bool hasShots = UnlimitedShots || Budget.Shots > 0;
                bool hasDice = _context.DiceThrower.HasLoadedDice || _returning > 0 || DiceInContainers() > 0;
                bool canReroll = Budget.Rerolls > 0 && _tracker.Dice.Count > 0;
                return (hasShots && hasDice) || canReroll;
            }
        }
        
        public CameraShotId CurrentView => _context.CameraController.CurrentShotId;
        public bool IsInHub => CurrentView == _settings.RestShot;
        public bool IsInDrawer => CurrentView == _settings.DrawerShot;
        public bool IsInChest => CurrentView == _settings.ChestShot;
        public int EliminatedCount => _eliminated.Count;
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
            _context.DiceThrower.ShootEnabled = _running && !IsAnimating;
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
            _returning++;
            NotifyBudgetChanged();

            _context.DiceAnimator.Vanish(new[] { dice }, onVanish =>
            {
                DiceDrawer home = ContainerOf(onVanish);
                onVanish.transform.position = home.DropPoint;
                _context.DiceAnimator.Appear(new[] { onVanish }, d => home.Add(d), () => _returning--);
            }, null);
        }

        #endregion

        #region COLLISIONS

        private void OnDiceCollided(Dice.Dice a, Dice.Dice b, Collision collision)
        {
            if (!IsInPlay(a) || !IsInPlay(b)) return;

            Vector3 point = collision.contactCount > 0 ? collision.GetContact(0).point : a.transform.position;
            float impact = collision.relativeVelocity.magnitude;

            var hits = EffectsOf(a, b, point, impact).Concat(EffectsOf(b, a, point, impact))
                .OrderByDescending(h => h.Effect.Priority)
                .ToList();

            foreach ((DiceCollisionEffect effect, DiceCollision hit) in hits)
                if (IsInPlay(hit.Self)) effect.Apply(hit); 
        }
        
        private IEnumerable<(DiceCollisionEffect Effect, DiceCollision Hit)> EffectsOf(Dice.Dice self, Dice.Dice other, Vector3 point, float impact)
        {
            DiceMaterial material = self.Data.DiceMaterial;
            if (material == null || material.collisionEffects == null) yield break;

            var hit = new DiceCollision(self, other, point, impact, this);
            foreach (DiceCollisionEffect effect in material.collisionEffects)
                if (effect != null) yield return (effect, hit);
        }


        public bool IsInPlay(Dice.Dice dice) => _running && _tracker.Dice.Contains(dice);

        public void Eliminate(Dice.Dice dice, Func<Transform, Tween> vanish = null)
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

            Transform t = dice.transform;
            Tween tween = vanish != null
                ? vanish(t)
                : t.DOScale(Vector3.zero, _settings.EliminateDuration).SetEase(Ease.InBack);
            tween.SetTarget(t)
                .SetLink(dice.gameObject)
                .OnComplete(() => dice.gameObject.SetActive(false));
        }
        
        public Dice.Dice Convert(Dice.Dice target, Dice.Dice model)
        {
            if (!IsInPlay(target)) return null;

            DiceInstance data = model.Data.Clone();
            if (!_context.RunData.ReplaceDice(target.Data, data))
                Debug.LogWarning("[RollSession] El dado convertido no estaba ni en el estuche ni en el cofre.", target);

            Transform from = target.transform;
            Dice.Dice converted = UnityEngine.Object.Instantiate(data.DiceData.prefab, from.position, from.rotation);
            converted.Init(data);
            Utils.Utils.SetLayer(converted.gameObject, target.gameObject.layer);   // si aún iba volando, sigue igual
            Rigidbody oldBody = target.GetComponent<Rigidbody>();
            Rigidbody newBody = converted.GetComponent<Rigidbody>();
            newBody.isKinematic = false;
            newBody.linearVelocity = oldBody.linearVelocity;
            newBody.angularVelocity = oldBody.angularVelocity;

            _tracker.Add(converted);
            _tracker.Remove(target);
            int index = _outOfDrawer.IndexOf(target);
            if (index >= 0) _outOfDrawer[index] = converted;
            else _outOfDrawer.Add(converted);
            if (_origins.TryGetValue(target, out DiceDrawer home))
            {
                _origins.Remove(target);
                _origins[converted] = home;
            }
            NotifyBudgetChanged();

            UnityEngine.Object.Destroy(target.gameObject);
            return converted;
        }

        public void Blast(Vector3 center, float force, float radius, float upwards = 0.5f)
        {
            foreach (Rigidbody rb in PhysicalDiceInPlay())
                rb.AddExplosionForce(force, center, radius, upwards, ForceMode.Impulse);
        }

        public void Pull(Vector3 center, float acceleration, float radius, float duration)
        {
            DOVirtual.Float(0f, 1f, duration, _ =>
            {
                foreach (Rigidbody rb in PhysicalDiceInPlay())
                {
                    Vector3 toCenter = center - rb.position;
                    float distance = toCenter.magnitude;
                    if (distance > radius || distance < 0.01f) continue;
                    rb.AddForce(toCenter / distance * (acceleration * (1f - distance / radius)), ForceMode.Acceleration);
                }
            }).SetUpdate(UpdateType.Fixed);   
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

        private IEnumerable<Rigidbody> PhysicalDiceInPlay()
        {
            if (!_running) yield break;
            foreach (Dice.Dice dice in _tracker.Dice.ToList())
            {
                Rigidbody rb = dice.GetComponent<Rigidbody>();
                if (!rb.isKinematic) yield return rb;
            }
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
            _returning++;
            _context.DiceAnimator.ArcTo(new[] { loadedDice }, home.GetReturnPoint, d => home.Add(d), () => _returning--);
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
            _returning = 0;
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
        
        private int DiceInContainers()
        {
            int count = _context.DiceDrawer.Count;
            if (_context.BankChest != null && Array.IndexOf(_settings.Views, _settings.ChestShot) >= 0)
                count += _context.BankChest.Count;
            return count;
        }

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


    }
}