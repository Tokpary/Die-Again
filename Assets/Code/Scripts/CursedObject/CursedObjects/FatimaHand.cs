using UnityEngine;

namespace Code.Scripts.CursedObject.CursedObjects
{
    [CreateAssetMenu(fileName = "FatimaHand", menuName = "CursedObjects/FatimaHand")]
    public class FatimaHand : CursedObject
    {
        [Min(0), Tooltip("Bonificación del margen superior del Lucky Number.")]
        public int toleranceIncrease = 1;

        public override void ModifyResult(ScoreResult result) => result.UpperToleranceBonus += toleranceIncrease;
    }
}
