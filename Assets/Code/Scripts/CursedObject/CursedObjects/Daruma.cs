
using UnityEngine;

namespace Code.Scripts.CursedObject.CursedObjects
{
    [CreateAssetMenu(
        fileName = "Daruma",
        menuName = "CursedObjects/Daruma"
    )]
    public class Daruma : CursedObject
    {
        [Header("Efecto")]
        [Min(0)]
        [Tooltip("Rerolls adicionales por ronda.")]
        public int extraRerolls = 1;

        public override int ModifyRerolls(int rerolls, bool isBoss)
        {
            return rerolls + extraRerolls;
        }
    }
}
