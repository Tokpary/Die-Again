using System.Collections.Generic;
using Code.Scripts.Bosses;
using Code.Scripts.Dice;
using Code.Scripts.Shop;
using UnityEngine;

namespace Code.Scripts.Core
{
    public class RunData
    {
        public GameProgress Progress { get; }
        public DiceCase DiceCase { get; }
        public ShopOffer ShopOffer { get; }
        public DiceCase Bank { get; }
        
        public int CursedCapacity { get; }
        public bool HasCursedRoom => CursedObject.Count < CursedCapacity;

        public List<CursedObject.CursedObject> CursedObject { get; } = new List<CursedObject.CursedObject>();
        
        private readonly List<BossSO> _bossOrder = new List<BossSO>();
        public BossSO BossOfWeek(int week) =>
            _bossOrder.Count == 0 ? null : _bossOrder[(Mathf.Max(1, week) - 1) % _bossOrder.Count];

        public RunData(GameConfigSO config)
        {
            Progress = new GameProgress(config.InitialWeek, config.InitialDay);
            CursedCapacity = config.cursedCapacity;
            if (config.startingCursedObjects != null)
                foreach (var passive in config.startingCursedObjects)
                    if (passive != null && !CursedObject.Contains(passive) && HasCursedRoom)
                        CursedObject.Add(passive);
            DiceCase = new DiceCase(config.diceCaseCapacity);
            Bank = new DiceCase(config.bankCapacity);
            if (config.startingBankDice != null)
                foreach (DiceSO coin in config.startingBankDice)
                    if (coin != null) Bank.TryAdd(new DiceInstance(coin));
            foreach (DiceSO dice in config.startingDices)
            {
                DiceCase.TryAdd(new DiceInstance(dice));
            }
            ShopOffer = new ShopOffer(config.shopPool, config.shopOfferSize, config.shopMaxNumber);
            ShuffleBosses(config.bossPool);
            
        }
        
        private void ShuffleBosses(BossSO[] pool)
        {
            if (pool != null)
                foreach (BossSO boss in pool)
                    if (boss != null && !_bossOrder.Contains(boss))
                        _bossOrder.Add(boss);

            for (int i = _bossOrder.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (_bossOrder[i], _bossOrder[j]) = (_bossOrder[j], _bossOrder[i]);
            }
        }
        
        public bool ReplaceDice(DiceInstance dice, DiceInstance replacement) =>
            DiceCase.Replace(dice, replacement) || Bank.Replace(dice, replacement);
        
        public bool RemoveDice(DiceInstance dice) => DiceCase.Remove(dice) || Bank.Remove(dice);
    }
}