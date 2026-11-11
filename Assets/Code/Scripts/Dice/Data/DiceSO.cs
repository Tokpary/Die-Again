using Code.Scripts.Dice.DiceMaterials;
using UnityEngine;

namespace Code.Scripts.Dice
{
    [CreateAssetMenu(fileName = "DiceSO", menuName = "ScriptableObjects/DiceSO", order = 1)]
    public class DiceSO : ScriptableObject
    {
        public string displayName;
        [TextArea] public string description;
        public Sprite preview;
        
        public int[] faceValues = { 1, 2, 3, 4, 5, 6 };
        public DiceMaterial diceMaterial;
        public Dice prefab;
        public string DisplayName => string.IsNullOrEmpty(displayName) ? name : displayName;
        
    }
}