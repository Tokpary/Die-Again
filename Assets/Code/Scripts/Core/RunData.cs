using System.Collections.Generic;
using Code.Scripts.Dice;

namespace Code.Scripts.Core
{
    public class RunData
    {
        public GameProgress Progress { get; }
        public DiceCase DiceCase { get; }

        public List<CursedObject.CursedObject> CursedObject { get; } = new List<CursedObject.CursedObject>();

        public RunData(GameConfigSO config)
        {
            Progress = new GameProgress(config.InitialWeek, config.InitialDay);
            DiceCase = new DiceCase(config.diceCaseCapacity);
            foreach (DiceSO dice in config.startingDices)
            {
                DiceCase.TryAdd(new DiceInstance(dice));
            }
        }
    }
}