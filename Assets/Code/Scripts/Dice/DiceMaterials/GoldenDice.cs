using UnityEngine;

namespace Code.Scripts.Dice.DiceMaterials
{
    [CreateAssetMenu(fileName = "GoldenDice", menuName = "ScriptableObjects/DiceMaterials/GoldenDice", order = 1)]
    public class GoldenMaterial : DiceMaterial
    {
        public override int ModifyValue(int value)
        {
            return value * 2;
        }
    }
}