
using System;
using UnityEngine;

namespace Code.Scripts.Roulette
{
    [CreateAssetMenu(fileName = "RouletteSO", menuName = "ScriptableObjects/RouletteSO", order = 2)]
    public class RouletteSO : ScriptableObject
    {
        [Header("Lucky Number range")]
        [Min(1)] public int luckyNumberMinimum = 1;
        [Min(1)] public int luckyNumberMaximum = 6;
        [Header("Numbered wedges")]
        [Range(1, 20)] public int maximumSlices = 20;
        [Header("Boss probability (%)")]
        [Range(0, 100)] public int day1BossProbability = 1;
        [Range(0, 100)] public int day2BossProbability = 25;
        [Range(0, 100)] public int day3BossProbability = 50;
        [Tooltip("Must remain 100.")]
        public int day4BossProbability = 100;

        public int GetBossProbability(int day)
        {
            int probability;
            switch (day)
            {
                case 1: probability = day1BossProbability; break;
                case 2: probability = day2BossProbability; break;
                case 3: probability = day3BossProbability; break;
                case 4: probability = day4BossProbability; break;
                default: throw new ArgumentOutOfRangeException(nameof(day));
            }
            RouletteRules.SelectBoss(day, probability, 0);
            return probability;
        }

        public bool TryValidate(out string error)
        {
            try
            {
                RouletteRules.ValidateRange(luckyNumberMinimum, luckyNumberMaximum);
                if (maximumSlices < 1 || maximumSlices > 20)
                    throw new ArgumentOutOfRangeException(nameof(maximumSlices));
                for (int day = 1; day <= 4; day++) GetBossProbability(day);
                error = null;
                return true;
            }
            catch (ArgumentException exception)
            {
                error = exception.Message;
                return false;
            }
        }
    }
}
