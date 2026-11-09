
using System;
using System.Collections.Generic;
using Code.Scripts.Core;

namespace Code.Scripts.Roulette
{
    // Pure rules: the physical angle decides BOTH the numbered wedge and boss/normal.
    public static class RouletteRules
    {
        public static void ValidateRange(int minimum, int maximum)
        {
            if (minimum < 1 || maximum < minimum || maximum == int.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(maximum),
                    "Lucky Number requires 1 <= minimum <= maximum < int.MaxValue.");
        }

        // Partial Fisher-Yates: uniform sample without replacement, O(maximumSlices) memory.
        public static int[] SelectNumbers(int minimum, int maximum, int maximumSlices, Random random)
        {
            ValidateRange(minimum, maximum);
            if (maximumSlices < 1 || maximumSlices > 20)
                throw new ArgumentOutOfRangeException(nameof(maximumSlices));
            if (random == null) throw new ArgumentNullException(nameof(random));
            int rangeSize = maximum - minimum + 1;
            int count = Math.Min(rangeSize, maximumSlices);
            var swaps = new Dictionary<int, int>();
            var result = new int[count];
            for (int i = 0; i < count; i++)
            {
                int j = random.Next(i, rangeSize);
                int chosen = swaps.TryGetValue(j, out int mappedJ) ? mappedJ : j;
                int previous = swaps.TryGetValue(i, out int mappedI) ? mappedI : i;
                swaps[j] = previous;
                result[i] = minimum + chosen;
            }
            return result;
        }

        // Kept for callers/tests that validate percentage boundaries.
        public static bool SelectBoss(int day, int probability, int roll)
        {
            if (day < 1 || day > 4) throw new ArgumentOutOfRangeException(nameof(day));
            if (probability < 0 || probability > 100 || (day == 4 && probability != 100))
                throw new ArgumentOutOfRangeException(nameof(probability), "Day 4 must guarantee a boss.");
            if (roll < 0 || roll >= 100) throw new ArgumentOutOfRangeException(nameof(roll));
            return roll < probability;
        }

        public static int GetSliceIndex(double angle, int sliceCount)
        {
            if (sliceCount < 1 || sliceCount > 20 || double.IsNaN(angle) || double.IsInfinity(angle))
                throw new ArgumentOutOfRangeException(nameof(sliceCount));
            double normalized = (angle % 360 + 360) % 360;
            return Math.Min(sliceCount - 1, (int)(normalized * sliceCount / 360.0));
        }

        public static RouletteResult Roll(GameProgress progress, int probability, Random random)
        {
            if (progress == null) throw new ArgumentNullException(nameof(progress));
            if (random == null) throw new ArgumentNullException(nameof(random));
            if (!progress.IsRoundPrepared || progress.IsRoundStarted || progress.IsComplete ||
                progress.SelectedLuckyNumbers.Count == 0)
                throw new InvalidOperationException("Prepare an unstarted round before spinning.");
            SelectBoss(progress.Day, probability, 0); // Validate probability/day.
            double angle = random.NextDouble() * 360.0;
            int index = GetSliceIndex(angle, progress.SelectedLuckyNumbers.Count);
            bool isBoss = IsBossAtAngle(angle, probability);
            return new RouletteResult(progress.RoundId, progress.Week, progress.Day,
                progress.NextLuckyNumberMinimum, progress.NextLuckyNumberMaximum,
                progress.SelectedLuckyNumbers[index], isBoss, probability, angle, index);
        }

        // Degrees clockwise from the top of the unrotated disc.
        public static bool IsBossAtAngle(double angle, int probability)
        {
            if (probability < 0 || probability > 100 || double.IsNaN(angle) || double.IsInfinity(angle))
                throw new ArgumentOutOfRangeException(nameof(angle));
            angle = (angle % 360 + 360) % 360;
            return angle < probability * 3.6;
        }

        // Legacy helper retained for other callers; not used for physical roulette spins.
        public static double GetSectorAngle(bool isBoss, int probability, double sample)
        {
            if (probability < 0 || probability > 100 || double.IsNaN(sample) || sample < 0 || sample > 1)
                throw new ArgumentOutOfRangeException(nameof(probability));
            double boundary = probability * 3.6;
            double start = isBoss ? 0 : boundary;
            double end = isBoss ? boundary : 360;
            double width = end - start;
            if (width <= 0) throw new InvalidOperationException("The selected sector has zero probability.");
            double inset = Math.Min(0.5, width * 0.1);
            return start + inset + sample * (width - 2 * inset);
        }
    }
}
