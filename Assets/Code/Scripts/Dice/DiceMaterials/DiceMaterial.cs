using UnityEngine;

namespace Code.Scripts.Dice.DiceMaterials
{
    public abstract class DiceMaterial : ScriptableObject
    {
        public string displayName;
        public virtual void ApplyVisual(Dice dice) {}
        public virtual int ModifyValue(int value)
        {
            return value;
        }
        public virtual void OnCollision(Dice self, Dice other)
        {
        }
    }
}
