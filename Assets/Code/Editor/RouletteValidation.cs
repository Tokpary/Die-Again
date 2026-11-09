
using System;
using System.Collections.Generic;
using Code.Scripts.Core;
using Code.Scripts.Roulette;

#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
#endif

namespace Code.Scripts.Editor
{
    public static class RouletteValidation
    {
#if UNITY_EDITOR
        [MenuItem("DiceMaster/Roulette/Validate numbered wheel (400000 outcomes)")]
        public static void RunInEditor() =>
            RunChecks(message => Debug.Log(message));
#endif

        public static void RunChecks(Action<string> log)
        {
            const int samples = 100000;
            int[] probabilities = { 1, 25, 50, 100 };
            var random = new System.Random(3523);

            for (int day = 1; day <= 4; day++)
            {
                int probability = probabilities[day - 1];
                var progress = new GameProgress(2, day);

                progress.PrepareNextRound(17, 43, 20, random);

                Require(progress.SelectedLuckyNumbers.Count == 20, "Wrong wedge count.");
                Require(new HashSet<int>(progress.SelectedLuckyNumbers).Count == 20, "Duplicate numbers.");

                int[] original = Copy(progress.SelectedLuckyNumbers);

                progress.PrepareNextRound(17, 43, 20, random);
                Require(Equal(original, progress.SelectedLuckyNumbers),
                    "Repeated preparation changed numbers.");

                int bosses = 0;
                var seen = new HashSet<int>();

                for (int i = 0; i < samples; i++)
                {
                    RouletteResult result = RouletteRules.Roll(progress, probability, random);

                    Require(result.StopAngle >= 0 && result.StopAngle < 360,
                        "Invalid stop angle.");
                    Require(result.LuckyNumber == original[result.SliceIndex],
                        "Wrong wedge number.");
                    Require(RouletteRules.GetSliceIndex(result.StopAngle, 20) == result.SliceIndex,
                        "Angle and wedge disagree.");
                    Require(RouletteRules.IsBossAtAngle(result.StopAngle, probability) == result.IsBoss,
                        "Angle and color disagree.");

                    progress.ClearTestResult();
                    progress.RecordResult(result);

                    if (result.IsBoss) bosses++;
                    seen.Add(result.LuckyNumber);

                    Require(progress.RoundId == 1, "Reroll advanced the round.");
                    Require(Equal(original, progress.SelectedLuckyNumbers),
                        "Reroll changed numbers.");
                }

                Require(seen.Count == 20,
                    "Not all wedges appeared in simulation.");

                double p = probability / 100.0;
                double tolerance = 6 * Math.Sqrt(samples * p * (1 - p));

                Require(Math.Abs(bosses - samples * p) <= tolerance,
                    "Boss rate exceeded six sigma.");

                log($"Day {day}: {bosses}/{samples} boss outcomes; 20 unique fixed wedges PASS.");
            }

            var small = new GameProgress();
            small.PrepareNextRound(9, 11, 20, random);

            Require(small.SelectedLuckyNumbers.Count == 3,
                "Short range must use three wedges.");
            Require(new HashSet<int>(small.SelectedLuckyNumbers).Count == 3,
                "Short range has duplicates.");

            Reject(() => small.PrepareNextRound(9, 12, 20, random),
                "Range changed silently.");
            Reject(() => RouletteRules.ValidateRange(5, 4),
                "Inverted range accepted.");
            Reject(() => RouletteRules.ValidateRange(0, 2),
                "Nonpositive range accepted.");
            Reject(() => RouletteRules.ValidateRange(1, int.MaxValue),
                "Overflowing endpoint accepted.");
            Reject(() => RouletteRules.SelectBoss(4, 99, 99),
                "Day 4 was not guaranteed.");

            for (int bossDay = 1; bossDay <= 4; bossDay++)
            {
                var run = new GameProgress();

                for (int week = 1; week <= 3; week++)
                {
                    for (int day = 1; day <= bossDay; day++)
                    {
                        Require(run.Week == week && run.Day == day,
                            "Wrong progression.");

                        run.PrepareNextRound(
                            week * 10 + day,
                            week * 10 + day + 2,
                            20,
                            random
                        );

                        int probability = day == bossDay ? 100 : 0;
                        RouletteResult result = RouletteRules.Roll(
                            run, probability, random
                        );

                        Require(result.VictoryTolerance == 3 - week,
                            "Wrong tolerance.");

                        Reject(() => run.StartRound(),
                            "Gameplay started without a result.");

                        run.RecordResult(result);
                        run.StartRound();

                        Reject(() => run.ClearTestResult(),
                            "Started round was rerolled.");
                        Reject(() => RouletteRules.Roll(run, probability, random),
                            "Started round accepted a spin.");

                        run.CompleteRound();

                        Reject(() => run.CompleteRound(),
                            "Victory counted twice.");
                    }
                }

                Require(run.IsComplete,
                    "Third boss did not finish the run.");

                Reject(() => run.PrepareNextRound(1, 6),
                    "Finished run prepared another round.");
            }

            log("PASS: 400000 physical spins, exact mapping, fixed rerolls, unique numbers, progression.");
        }

        private static int[] Copy(IReadOnlyList<int> source)
        {
            var copy = new int[source.Count];

            for (int i = 0; i < copy.Length; i++)
                copy[i] = source[i];

            return copy;
        }

        private static bool Equal(int[] a, IReadOnlyList<int> b)
        {
            if (a.Length != b.Count)
                return false;

            for (int i = 0; i < a.Length; i++)
            {
                if (a[i] != b[i])
                    return false;
            }

            return true;
        }

        private static void Require(bool condition, string message)
        {
            if (!condition)
                throw new InvalidOperationException(message);
        }

        private static void Reject(Action action, string message)
        {
            try
            {
                action();
            }
            catch (ArgumentException)
            {
                return;
            }
            catch (InvalidOperationException)
            {
                return;
            }

            throw new Exception(message);
        }
    }
}
