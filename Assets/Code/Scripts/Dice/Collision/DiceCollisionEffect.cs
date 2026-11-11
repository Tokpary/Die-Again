using UnityEngine;

namespace Code.Scripts.Dice
{
    public abstract class DiceCollisionEffect : ScriptableObject
    {
        public abstract void Apply(DiceCollision hit);

    }
}