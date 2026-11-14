using System.Collections.Generic;
using Code.Scripts.Dice;
using Code.Scripts.Dice.DiceMaterials;
using UnityEngine;

namespace Code.Scripts.Bosses
{
    [CreateAssetMenu(fileName = "GlassBoss", menuName = "ScriptableObjects/Bosses/Lust")]
    public class LustBoss : BossSO
    {
        [SerializeField] private DiceMaterial _glassMaterial;

        private readonly Dictionary<DiceInstance, DiceMaterial> _originals = new Dictionary<DiceInstance, DiceMaterial>();

        public override void OnFightStart(GameContext context)
        {
            if (_glassMaterial == null)
            {
                Debug.LogWarning($"[{name}] Falta el material de cristal.", this);
                return;
            }

            _originals.Clear();
            foreach (DiceInstance dice in context.RunData.DiceCase.Dice)
            {
                _originals[dice] = dice.DiceMaterial;
                dice.DiceMaterial = _glassMaterial;
            }
            Respawn(context);
        }

        public override void OnFightEnd(GameContext context)
        {
            if (_originals.Count == 0) return;
            foreach (KeyValuePair<DiceInstance, DiceMaterial> original in _originals)
                original.Key.DiceMaterial = original.Value;
            _originals.Clear();
            Respawn(context);
        }

        private static void Respawn(GameContext context)
        {
            context.DiceDrawer.Clear();
            context.DiceDrawer.Populate(context.RunData.DiceCase);
        }
    }
}