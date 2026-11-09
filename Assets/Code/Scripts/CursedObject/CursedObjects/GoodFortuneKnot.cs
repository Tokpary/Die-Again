using UnityEngine;

namespace Code.Scripts.CursedObject.CursedObjects
{
    [CreateAssetMenu(fileName = "GoodFortuneKnot", menuName = "CursedObjects/GoodFortuneKnot")]
    public class GoodFortuneKnot : CursedObject
    {
        [Range(0, 100), Tooltip("Reducción en puntos porcentuales. Se conserva el mínimo del 1% y el jefe garantizado del día 4.")]
        public int probabilityReduction = 10;

        public override int ModifyBossProbability(int day, int probability)
        {
            return day == 4 ? probability : Mathf.Max(1, probability - probabilityReduction);
        }
    }
}
