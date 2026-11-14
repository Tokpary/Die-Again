
using UnityEngine;

namespace Code.Scripts.CursedObject.CursedObjects
{
    [CreateAssetMenu(
        fileName = "InvertedHorseshoe",
        menuName = "CursedObjects/InvertedHorseshoe"
    )]
    public class InvertedHorseshoe : CursedObject
    {
        [Header("Efecto")]
        [Min(0)]
        [Tooltip("Lanzamientos adicionales por ronda.")]
        public int extraShots = 1;

        public override int ModifyShots(int shots, bool isBoss)
        {
            return shots + extraShots;
        }
    }
}
