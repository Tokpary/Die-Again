using UnityEngine;

namespace Code.Scripts.CursedObject.CursedObjects
{
    [CreateAssetMenu(fileName = "FourLeafClover", menuName = "CursedObjects/FourLeafClover")]
    public class FourLeafClover : CursedObject
    {
        [Min(0)] public int lowerToleranceIncrease = 1;
        [Min(0)] public int upperToleranceIncrease = 1;

        public override void ModifyResult(ScoreResult result)
        {
            if (result.Day != 1) return;
            result.LowerToleranceBonus += lowerToleranceIncrease;
            result.UpperToleranceBonus += upperToleranceIncrease;
        }
    }
}
