using System;
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
        [MenuItem("DiceMaster/Roulette/Validate rules (400000 outcomes)")]
        public static void RunInEditor()
        {
            RunChecks(message => Debug.Log(message));
        }
#endif

        public static void RunChecks(Action<string> log)
        {
            const int samples = 100000;
            int[] probabilities = { 1, 25, 50, 100 };
            var random = new System.Random(3523);
            for (int day = 1; day <= 4; day++)
            {
                int expected = probabilities[day - 1];
                int exactBosses = 0;
                for (int roll = 0; roll < 100; roll++)
                    if (RouletteRules.SelectBoss(day, expected, roll)) exactBosses++;
                Require(exactBosses == expected, "Exact probability boundary failed.");

                var progress = new GameProgress(2, day);
                progress.PrepareNextRound(17, 43);
                int bosses = 0;
                bool sawMin = false;
                bool sawMax = false;
                for (int i = 0; i < samples; i++)
                {
                    RouletteResult result = RouletteRules.Roll(progress, expected, random);
                    progress.RecordResult(result);
                    if (result.IsBoss) bosses++;
                    Require(result.LuckyNumber >= 17 && result.LuckyNumber <= 43, "Number outside announced range.");
                    sawMin |= result.LuckyNumber == 17;
                    sawMax |= result.LuckyNumber == 43;
                    double angle = RouletteRules.GetSectorAngle(result.IsBoss, expected, (i % 997) / 996.0);
                    double boundary = expected * 360.0 / 100.0;
                    Require(result.IsBoss ? angle > 0 && angle < boundary : angle > boundary && angle < 360,
                        "Stop angle is not inside the selected sector.");
                    Require(RouletteRules.IsBossAtAngle(angle, expected) == result.IsBoss, "Angular mapping failed.");
                    Require(progress.Week == 2 && progress.Day == day && progress.RoundId == 1,
                        "Repeated test spins advanced progression.");
                }

                double p = expected / 100.0;
                double tolerance = 6 * Math.Sqrt(samples * p * (1 - p));
                Require(Math.Abs(bosses - samples * p) <= tolerance, "Seeded sample exceeds six sigma.");
                Require(sawMin && sawMax, "Range must include both endpoints.");
                log($"Day {day}: {bosses}/{samples} bosses ({100.0 * bosses / samples:F3}%). Range and angles PASS.");
            }

            var fixedRange = new GameProgress();
            fixedRange.PrepareNextRound(9, 9);
            Require(RouletteRules.Roll(fixedRange, 0, random).LuckyNumber == 9, "Single-value range failed.");
            fixedRange.PrepareNextRound(9, 9);
            Reject(() => fixedRange.PrepareNextRound(10, 20), "Announced range changed silently.");
            Reject(() => RouletteRules.ValidateRange(5, 4), "Inverted range accepted.");
            Reject(() => RouletteRules.ValidateRange(0, 2), "Nonpositive range accepted.");
            Reject(() => RouletteRules.ValidateRange(1, int.MaxValue), "Overflowing upper endpoint accepted.");
            Reject(() => RouletteRules.SelectBoss(4, 99, 99), "Day 4 was not guaranteed.");
            Reject(() => RouletteRules.SelectBoss(0, 25, 0), "Invalid day accepted.");
            Reject(() => RouletteRules.GetSectorAngle(true, 0, 0.5), "Empty boss sector accepted.");
            Reject(() => RouletteRules.GetSectorAngle(false, 100, 0.5), "Empty normal sector accepted.");
            Reject(() => new GameProgress(4, 1), "Invalid week accepted.");

            // Every possible boss day in every week; no duplicate completion and no fourth week.
            for (int bossDay = 1; bossDay <= 4; bossDay++)
            {
                var run = new GameProgress();
                for (int week = 1; week <= 3; week++)
                {
                    for (int day = 1; day <= bossDay; day++)
                    {
                        Require(run.Week == week && run.Day == day, "Incorrect progression.");
                        int minimum = week * 10 + day;
                        run.PrepareNextRound(minimum, minimum + 2);
                        bool boss = day == bossDay;
                        RouletteResult result = RouletteRules.Roll(run, boss ? 100 : 0, random);
                        Require(result.VictoryTolerance == 3 - week, "Incorrect weekly tolerance.");
                        Reject(() => run.StartRound(), "Gameplay started without a result.");
                        run.RecordResult(result);
                        Require(!run.IsComplete && run.Week == week, "Boss selection completed the week.");
                        run.StartRound();
                        Reject(() => run.ClearTestResult(), "Started round could be rerolled.");
                        Reject(() => RouletteRules.Roll(run, 100, random), "Started round accepted a new spin.");
                        run.CompleteRound();
                        Reject(() => run.CompleteRound(), "Victory counted twice.");
                        Reject(() => run.RecordResult(result), "Stale result accepted after victory.");
                    }
                }
                Require(run.IsComplete && run.Week == 3, "Third boss did not complete the run.");
                Reject(() => run.PrepareNextRound(1, 6), "Completed run prepared a fourth week.");
            }

            var unpublished = new GameProgress();
            unpublished.PrepareNextRound(4, 12);
            RouletteRules.Roll(unpublished, 25, random);
            Require(!unpublished.HasResult, "Selecting an outcome published it before animation finished.");
            log("PASS: exact boundaries, 400000 samples, inclusive ranges, frozen preview, stop angles, " +
                "repeat spins, delayed publication, stale results, all boss days and three-week completion.");
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }

        private static void Reject(Action action, string message)
        {
            try { action(); }
            catch (ArgumentException) { return; }
            catch (InvalidOperationException) { return; }
            throw new Exception(message);
        }
    }
}
