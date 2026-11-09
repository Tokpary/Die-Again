using System;
using Code.Scripts.Core;

namespace Code.Scripts.Roulette
{
    // No Unity or presentation dependency: the same rules run in fast simulations.
    public static class RouletteRules
    {
        public static void ValidateRange(int minimum, int maximum)
        {
            if (minimum < 1 || maximum < minimum || maximum == int.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(maximum),
                    "Lucky Number requires 1 <= minimum <= maximum < int.MaxValue.");
        }

        public static bool SelectBoss(int day, int probability, int roll)
        {
            if (day < 1 || day > 4) throw new ArgumentOutOfRangeException(nameof(day));
            if (probability < 0 || probability > 100 || (day == 4 && probability != 100))
                throw new ArgumentOutOfRangeException(nameof(probability), "Day 4 must guarantee a boss.");
            if (roll < 0 || roll >= 100) throw new ArgumentOutOfRangeException(nameof(roll));
            return roll < probability;
        }

        public static RouletteResult Roll(GameProgress progress, int probability, Random random)
        {
            if (progress == null) throw new ArgumentNullException(nameof(progress));
            if (random == null) throw new ArgumentNullException(nameof(random));
            if (!progress.IsRoundPrepared || progress.IsRoundStarted || progress.IsComplete)
                throw new InvalidOperationException("Prepare an unstarted round before spinning.");

            bool isBoss = SelectBoss(progress.Day, probability, random.Next(100));
            int luckyNumber = random.Next(progress.NextLuckyNumberMinimum,
                progress.NextLuckyNumberMaximum + 1);
            return new RouletteResult(progress.RoundId, progress.Week, progress.Day,
                progress.NextLuckyNumberMinimum, progress.NextLuckyNumberMaximum,
                luckyNumber, isBoss, probability);
        }

        // Degrees clockwise from the top of the unrotated disk. The pointer stays at the top.
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

        public static bool IsBossAtAngle(double angle, int probability)
        {
            if (probability < 0 || probability > 100 || double.IsNaN(angle) || double.IsInfinity(angle))
                throw new ArgumentOutOfRangeException(nameof(angle));
            angle = (angle % 360 + 360) % 360;
            return angle < probability * 3.6;
        }
    }
}
