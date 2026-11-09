using UnityEngine;

namespace Code.Scripts.CursedObject.CursedObjects
{
    [CreateAssetMenu(fileName = "NazarEye", menuName = "CursedObjects/NazarEye", order = 0)]
    public class NazarEye : CursedObject
    {
        [Min(0), Tooltip("Bonificación del margen inferior del Lucky Number.")] public int toleranceIncrease = 1;
        public override void ModifyResult(ScoreResult result)
        {
            base.ModifyResult(result);
            result.LowerToleranceBonus += toleranceIncrease;
        }
    }
}