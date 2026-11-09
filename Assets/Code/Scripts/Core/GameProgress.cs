
using System;
using System.Collections.Generic;
using Code.Scripts.Roulette;

namespace Code.Scripts.Core
{
    // Runtime state persists across roulette rerolls and UI transitions.
    public class GameProgress
    {
        private readonly List<int> _selectedLuckyNumbers = new List<int>();
        public IReadOnlyList<int> SelectedLuckyNumbers => _selectedLuckyNumbers.AsReadOnly();
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

        // Idempotent within a round: the same numbers, in the same positions, survive rerolls.
        public void PrepareNextRound(int minimum, int maximum, int maximumSlices = 20, Random random = null)
        {
            if (IsComplete) throw new InvalidOperationException("The run is complete.");
            RouletteRules.ValidateRange(minimum, maximum);
            if (maximumSlices < 1 || maximumSlices > 20)
                throw new ArgumentOutOfRangeException(nameof(maximumSlices), "The roulette supports 1 to 20 slices.");
            if (IsRoundPrepared)
            {
                if (minimum != NextLuckyNumberMinimum || maximum != NextLuckyNumberMaximum ||
                    _selectedLuckyNumbers.Count != Math.Min(maximumSlices, maximum - minimum + 1))
                    throw new InvalidOperationException("The announced Lucky Number selection is locked.");
                return;
            }

            var selection = RouletteRules.SelectNumbers(minimum, maximum, maximumSlices, random ?? new Random());
            NextLuckyNumberMinimum = minimum;
            NextLuckyNumberMaximum = maximum;
            _selectedLuckyNumbers.AddRange(selection);
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
                result.BossProbability < 0 || result.BossProbability > 100 ||
                (Day == 4 && (!result.IsBoss || result.BossProbability != 100)) ||
                (result.BossProbability == 0 && result.IsBoss) ||
                (result.BossProbability == 100 && !result.IsBoss) ||
                result.StopAngle < 0 || result.StopAngle >= 360 ||
                result.SliceIndex < 0 || result.SliceIndex >= _selectedLuckyNumbers.Count ||
                _selectedLuckyNumbers[result.SliceIndex] != result.LuckyNumber ||
                RouletteRules.GetSliceIndex(result.StopAngle, _selectedLuckyNumbers.Count) != result.SliceIndex ||
                RouletteRules.IsBossAtAngle(result.StopAngle, result.BossProbability) != result.IsBoss)
                throw new InvalidOperationException("The roulette result does not match the prepared wheel.");
            CurrentResult = result;
            HasResult = true;
        }

        public void StartRound()
        {
            if (!IsRoundPrepared || !HasResult || IsComplete)
                throw new InvalidOperationException("A resolved roulette is required before gameplay.");
            IsRoundStarted = true;
        }

        public void CompleteRound()
        {
            if (!IsRoundStarted || !HasResult || IsComplete)
                throw new InvalidOperationException("There is no active round to complete.");
            IsRoundStarted = false;
            IsRoundPrepared = false;
            _selectedLuckyNumbers.Clear();
            if (CurrentResult.IsBoss && Week == 3)
            {
                IsComplete = true;
                return;
            }
            if (CurrentResult.IsBoss) { Week++; Day = 1; }
            else Day++;
            RoundId++;
            HasResult = false;
            CurrentResult = default;
        }
    }
}
