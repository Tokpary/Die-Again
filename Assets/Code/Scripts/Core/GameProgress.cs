using System;
using Code.Scripts.Roulette;

namespace Code.Scripts.Core
{
    // Runtime data owned by GameManager. Only confirmed victories advance the run.
    public class GameProgress
    {
        public int Week { get; private set; }
        public int Day { get; private set; }
        public int RoundId { get; private set; } = 1;
        public int NextLuckyNumberMinimum { get; private set; }
        public int NextLuckyNumberMaximum { get; private set; }
        public bool IsRoundPrepared { get; private set; }
        public bool HasResult { get; private set; }
        public bool IsRoundStarted { get; private set; }
        public bool IsComplete { get; private set; }
        public RouletteResult CurrentResult { get; private set; }

        public GameProgress(int week = 1, int day = 1)
        {
            if (week < 1 || week > 3) throw new ArgumentOutOfRangeException(nameof(week));
            if (day < 1 || day > 4) throw new ArgumentOutOfRangeException(nameof(day));
            Week = week;
            Day = day;
        }

        // Call before announcing a range in the shop, or before the first roulette.
        // Once announced, the interval cannot change until that round is completed.
        public void PrepareNextRound(int minimum, int maximum)
        {
            if (IsComplete) throw new InvalidOperationException("The run is complete.");
            RouletteRules.ValidateRange(minimum, maximum);
            if (IsRoundPrepared)
            {
                if (minimum != NextLuckyNumberMinimum || maximum != NextLuckyNumberMaximum)
                    throw new InvalidOperationException("The announced Lucky Number range is locked.");
                return;
            }

            NextLuckyNumberMinimum = minimum;
            NextLuckyNumberMaximum = maximum;
            IsRoundPrepared = true;
        }

        public void ClearTestResult()
        {
            if (!IsRoundPrepared || IsRoundStarted || IsComplete)
                throw new InvalidOperationException("This round cannot be spun again.");
            HasResult = false;
            CurrentResult = default;
        }

        public void RecordResult(RouletteResult result)
        {
            if (!IsRoundPrepared || IsRoundStarted || IsComplete || result.RoundId != RoundId ||
                result.Week != Week || result.Day != Day ||
                result.Minimum != NextLuckyNumberMinimum || result.Maximum != NextLuckyNumberMaximum ||
                result.LuckyNumber < result.Minimum || result.LuckyNumber > result.Maximum ||
                result.BossProbability < 0 || result.BossProbability > 100 ||
                (Day == 4 && (!result.IsBoss || result.BossProbability != 100)) ||
                (result.BossProbability == 0 && result.IsBoss) ||
                (result.BossProbability == 100 && !result.IsBoss))
                throw new InvalidOperationException("The roulette result does not belong to the prepared round.");

            CurrentResult = result;
            HasResult = true;
        }

        public void StartRound()
        {
            if (!IsRoundPrepared || !HasResult || IsComplete)
                throw new InvalidOperationException("A resolved roulette is required before gameplay.");
            IsRoundStarted = true;
        }

        // Future scoring code calls this once after confirming victory. UI Next is not victory.
        public void CompleteRound()
        {
            if (!IsRoundStarted || !HasResult || IsComplete)
                throw new InvalidOperationException("There is no active round to complete.");

            IsRoundStarted = false;
            IsRoundPrepared = false;
            if (CurrentResult.IsBoss && Week == 3)
            {
                IsComplete = true;
                return;
            }

            if (CurrentResult.IsBoss)
            {
                Week++;
                Day = 1;
            }
            else
            {
                Day++;
            }

            RoundId++;
            HasResult = false;
            CurrentResult = default;
        }
    }
}
