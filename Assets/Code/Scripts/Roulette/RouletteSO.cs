
using System;
using UnityEngine;

namespace Code.Scripts.Roulette
{
    [CreateAssetMenu(fileName = "RouletteSO", menuName = "ScriptableObjects/RouletteSO", order = 2)]
    public class RouletteSO : ScriptableObject
    {
        [Serializable]
        public class NumberRange
        {
            [Min(1)] public int minimum = 1;
            [Min(1)] public int maximum = 6;
        }

        [Serializable]
        public class WeekRanges
        {
            public NumberRange day1 = new NumberRange();
            public NumberRange day2 = new NumberRange();
            public NumberRange day3 = new NumberRange();
            public NumberRange day4 = new NumberRange();
        }

        [Header("Week 1 - Lucky Number Ranges")]
        public WeekRanges week1 = new WeekRanges();

        [Header("Week 2 - Lucky Number Ranges")]
        public WeekRanges week2 = new WeekRanges();

        [Header("Week 3 - Lucky Number Ranges")]
        public WeekRanges week3 = new WeekRanges();

        [Header("Numbered Wedges")]
        [Range(1, 20)]
        public int maximumSlices = 20;

        [Header("Boss Probability (%)")]
        [Range(0, 100)] public int day1BossProbability = 1;
        [Range(0, 100)] public int day2BossProbability = 25;
        [Range(0, 100)] public int day3BossProbability = 50;

        [Tooltip("Must remain 100.")]
        public int day4BossProbability = 100;

        public NumberRange GetNumberRange(int week, int day)
        {
            WeekRanges ranges;

            switch (week)
            {
                case 1: ranges = week1; break;
                case 2: ranges = week2; break;
                case 3: ranges = week3; break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(week));
            }

            if (ranges == null)
                throw new ArgumentException($"Missing ranges for week {week}.");

            NumberRange result;

            switch (day)
            {
                case 1: result = ranges.day1; break;
                case 2: result = ranges.day2; break;
                case 3: result = ranges.day3; break;
                case 4: result = ranges.day4; break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(day));
            }

            if (result == null)
                throw new ArgumentException(
                    $"Missing number range for week {week}, day {day}."
                );

            RouletteRules.ValidateRange(result.minimum, result.maximum);

            return result;
        }

        public int GetBossProbability(int day)
        {
            int probability;

            switch (day)
            {
                case 1: probability = day1BossProbability; break;
                case 2: probability = day2BossProbability; break;
                case 3: probability = day3BossProbability; break;
                case 4: probability = day4BossProbability; break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(day));
            }

            RouletteRules.SelectBoss(day, probability, 0);
            return probability;
        }

        public bool TryValidate(out string error)
        {
            try
            {
                for (int week = 1; week <= 3; week++)
                {
                    for (int day = 1; day <= 4; day++)
                        GetNumberRange(week, day);
                }

                if (maximumSlices < 1 || maximumSlices > 20)
                    throw new ArgumentOutOfRangeException(nameof(maximumSlices));

                for (int day = 1; day <= 4; day++)
                    GetBossProbability(day);

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
