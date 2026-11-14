using System.Collections.Generic;
using System.Linq;
using Code.Scripts.Camera;
using Code.Scripts.Dice;
using Code.Scripts.UI.Screens;
using UnityEngine;

namespace Code.Scripts.States.State
{
    public class DiceCoinRewardState : AState
    {
        private readonly List<DiceSO> _options = new List<DiceSO>();
        private CoinRewardScreen _screen;
        private DiceSO _chosen;
        private bool _delivering;

        public DiceCoinRewardState(GameContext context, IStateMachine stateMachine) : base(context, stateMachine)
        {
        }

        public override void Enter()
        {
            base.Enter();
            PickOptions();
            _context.BankChest.Open();
            _context.CameraController.MoveTo(CameraShotId.Gameplay, () =>
            {
                if (_options.Count == 0)
                {
                    GoToShop();  
                    return;
                }

                _screen = _context.UIManager.ShowScreen<CoinRewardScreen>();
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
            DiceSO[] pool = _context.GameConfig.coinPool;
            if (pool == null) return;

            List<DiceSO> candidates = pool.Where(d => d != null).Distinct().ToList();
            int count = Mathf.Min(_context.GameConfig.coinChoices, candidates.Count);
            for (int i = 0; i < count; i++)
            {
                int index = Random.Range(0, candidates.Count);
                _options.Add(candidates[index]);
                candidates.RemoveAt(index);
            }
        }

        private void ShowOptions()
        {
            _screen.ShowTitle("CHOOSE A COIN DIE");
            _screen.ShowChoices(_options.Select(o => new DiceInstance(o)).ToList(), OnOptionPicked);
        }

        private void OnOptionPicked(int index)
        {
            if (_delivering) return;
            _chosen = _options[index];
            if (AudioManager.Instance != null) AudioManager.Instance.PlayCoinRewardSelectionSFX();

            DiceCase bank = _context.RunData.Bank;
            if (!bank.IsFull)
            {
                Deliver();
                return;
            }

            _screen.ShowTitle("CHEST FULL: CHOOSE A DIE TO REPLACE");
            _screen.ShowChoices(bank.Dice, OnReplacePicked);
        }

        private void OnReplacePicked(int index)
        {
            if (_delivering) return;
            DiceInstance replaced = _context.RunData.Bank.Dice[index];
            _context.BankChest.Discard(replaced, _context.RunData.Bank);
            Deliver();
        }

        private void Deliver()
        {
            _delivering = true;
            _screen.ClearChoices();
            _screen.Hide(); 

            var data = new DiceInstance(_chosen);
            if (!_context.RunData.Bank.TryAdd(data))
            {
                Debug.LogWarning("[DiceCoinReward] No se ha podido añadir el dado al cofre.");
                GoToShop();
                return;
            }

            Vector3 spawnPoint = _context.PlayArea.Center + Vector3.up;
            Dice.Dice dice = _context.BankChest.Spawn(data, spawnPoint);
            _context.DiceAnimator.Appear(new[] { dice }, null, () =>
                _context.DiceAnimator.ArcTo(new[] { dice }, _context.BankChest.GetReturnPoint,
                    d => _context.BankChest.Add(d), GoToShop));
        }

        private void GoToShop() => _stateMachine.ChangeState(new ShopState(_context, _stateMachine));
    }
}