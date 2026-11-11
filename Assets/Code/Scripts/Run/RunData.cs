using System.Collections.Generic;
using Code.Scripts.Dice;
using Code.Scripts.Shop;

namespace Code.Scripts.Core
{
    public class RunData
    {
        public GameProgress Progress { get; }
        public DiceCase DiceCase { get; }
        public ShopOffer ShopOffer { get; }
        public DiceCase Bank { get; }

        public List<CursedObject.CursedObject> CursedObject { get; } = new List<CursedObject.CursedObject>();

        public RunData(GameConfigSO config)
        {
            Progress = new GameProgress(config.InitialWeek, config.InitialDay);
            if (config.startingCursedObjects != null)
                foreach (var passive in config.startingCursedObjects)
                    if (passive != null && !CursedObject.Contains(passive))
                        CursedObject.Add(passive);
            DiceCase = new DiceCase(config.diceCaseCapacity);
            Bank = new DiceCase(config.bankCapacity);
            foreach (DiceSO dice in config.startingDices)
            {
                DiceCase.TryAdd(new DiceInstance(dice));
            }
            ShopOffer = new ShopOffer(config.shopPool, config.shopOfferMin, config.shopOfferMax);
        }
        
        public bool RemoveDice(DiceInstance dice) => DiceCase.Remove(dice) || Bank.Remove(dice);
    }
}