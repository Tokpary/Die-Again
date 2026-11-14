using System.Collections.Generic;
using Code.Scripts.Dice;
using UnityEngine;

namespace Code.Scripts
{
    public class ScoreResult
    {
        public List<DiceResult> Dice { get; private set; } = new List<DiceResult>();
        public int TotalScore { get;  set; }
        public int TargetScore { get;  set; }
        public int Tolerance { get;  set; }
        public int LowerToleranceBonus { get; set; }
        public int UpperToleranceBonus { get; set; }
        public int Day { get; set; }
        public int MinimumScore => TargetScore - Tolerance - LowerToleranceBonus;
        public int MaximumScore => TargetScore + Tolerance + UpperToleranceBonus;
        public bool IsSuccess => TotalScore >= MinimumScore && TotalScore <= MaximumScore;
    }
}