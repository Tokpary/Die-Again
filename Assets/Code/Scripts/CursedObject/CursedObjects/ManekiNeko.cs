
using UnityEngine;

namespace Code.Scripts.CursedObject.CursedObjects
{
    [CreateAssetMenu(
        fileName = "ManekiNeko",
        menuName = "CursedObjects/ManekiNeko"
    )]
    public class ManekiNeko : CursedObject
    {
        [Header("Efecto")]
        [Min(0)]
        [Tooltip("Rerolls adicionales durante una ronda de jefe.")]
        public int extraBossRerolls = 1;

        public override int ModifyRerolls(int rerolls, bool isBoss)
        {
            if (!isBoss)
                return rerolls;

            return rerolls + extraBossRerolls;
        }
    }
}
