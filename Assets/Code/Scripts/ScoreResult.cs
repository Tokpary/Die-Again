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
        public bool IsSuccess => Mathf.Abs(TotalScore - TargetScore) <= Tolerance;
    }
}