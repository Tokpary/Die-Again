using UnityEngine;

namespace Code.Scripts.Dice.DiceMaterials
{
    public abstract class DiceMaterial : ScriptableObject
    {
        [Header("Choques")]
        public DiceCollisionEffect[] collisionEffects;
        
        public string displayName;
        [TextArea] public string description;
        public virtual void ApplyVisual(Dice dice) {}
        public virtual int ModifyValue(int value)
        {
            return value;
        }
    }
}
