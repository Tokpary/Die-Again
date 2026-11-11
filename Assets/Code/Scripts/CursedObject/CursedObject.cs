
using UnityEngine;

namespace Code.Scripts.CursedObject
{
    public abstract class CursedObject : ScriptableObject
    {
        [TextArea]
        public string description;

        public virtual void ModifyScore(ScoreResult result)
        {
        }

        public virtual void ModifyResult(ScoreResult result)
        {
        }

        public virtual int ModifyBossProbability(
            int day,
            int probability)
        {
            return probability;
        }

        // Daruma, Inverted Horseshoe y Maneki-neko
        public virtual int ModifyShots(int shots, bool isBoss)
        {
            return shots;
        }

        public virtual int ModifyRerolls(int rerolls, bool isBoss)
        {
            return rerolls;
        }

        // Golden Coin
        // 1 = 100% de probabilidad de rotura.
        // 0.5 = 50%.
        // 0 = 0%.
        public virtual float ModifyCrystalBreakChance(float chance)
        {
            return chance;
        }

        [Header("Info")]
        public string displayName;

        [TextArea]
        public string descriptionInGame;

        public Sprite preview;

        [Header("Escena")]
        public GameObject prefab;

        public string DisplayName =>
            string.IsNullOrEmpty(displayName)
                ? name
                : displayName;
    }
}
