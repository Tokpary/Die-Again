using System.Collections.Generic;
using System.Linq;
using Code.Scripts.Camera;
using Code.Scripts.UI.Screens;
using DG.Tweening;
using UnityEngine;

namespace Code.Scripts.States.State
{
    public class CursedRewardState : AState
    {
        private const float LandedViewDelay = 0.6f;   

        private readonly List<CursedObject.CursedObject> _options = new List<CursedObject.CursedObject>();
        private CursedRewardScreen _screen;
        private CursedObject.CursedObject _chosen;
        private bool _delivering;

        public CursedRewardState(GameContext context, IStateMachine stateMachine) : base(context, stateMachine)
        {
        }

        private List<CursedObject.CursedObject> Owned => _context.RunData.CursedObject;

        public override void Enter()
        {
            base.Enter();
            PickOptions();

            _context.CameraController.MoveTo(CameraShotId.Gameplay, () =>
            {
                if (_options.Count == 0)
                {
                    GoToShop();   
                    return;
                }

                _screen = _context.UIManager.ShowScreen<CursedRewardScreen>();
                _screen.SkipClicked += GoToShop;
                ShowOptions();
            });
        }

        public override void Exit()
        {
            base.Exit();
            if (_screen == null) return;
            _screen.SkipClicked -= GoToShop;
            _screen.ClearChoices();
        }

        private void PickOptions()
        {
            _options.Clear();
            CursedObject.CursedObject[] pool = _context.GameConfig.cursedPool;
            if (pool == null) return;

            List<CursedObject.CursedObject> candidates = pool.Where(o => o != null && !Owned.Contains(o)).Distinct().ToList();
            int count = Mathf.Min(_context.GameConfig.cursedChoices, candidates.Count);
            for (int i = 0; i < count; i++)
            {
                int index = Random.Range(0, candidates.Count);
                _options.Add(candidates[index]);
                candidates.RemoveAt(index);
            }
        }

        private void ShowOptions()
        {
            _screen.ShowTitle("Elige un objeto maldito");
            _screen.ShowChoices(_options, OnOptionPicked);
        }

        private void OnOptionPicked(int index)
        {
            if (_delivering) return;
            _chosen = _options[index];

            if (_context.RunData.HasCursedRoom)
            {
                Deliver();
                return;
            }

            _screen.ShowTitle("Estantería llena: elige qué objeto sustituir");
            _screen.ShowChoices(Owned, OnReplacePicked);
        }

        private void OnReplacePicked(int index)
        {
            if (_delivering) return;
            CursedObject.CursedObject replaced = Owned[index];
            Owned.RemoveAt(index);
            _context.CursedShelf.Remove(replaced);
            Deliver();
        }

        private void Deliver()
        {
            _delivering = true;
            _screen.ClearChoices();
            _screen.SetSkipAvailable(false);
            Owned.Add(_chosen);

            int pending = 2;
            void OnStepDone()
            {
                if (--pending == 0) DOVirtual.DelayedCall(LandedViewDelay, GoToShop);
            }

            Vector3 spawnPoint = _context.PlayArea.Center + Vector3.up;
            _context.CursedShelf.Deliver(_chosen, spawnPoint,
                () => _context.CameraController.MoveTo(CameraShotId.CursedShelf, OnStepDone),
                OnStepDone);
        }

        private void GoToShop() => _stateMachine.ChangeState(new ShopState(_context, _stateMachine));
    }
}