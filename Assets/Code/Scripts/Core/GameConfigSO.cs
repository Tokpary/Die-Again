using Code.Scripts.Dice;
using UnityEngine;

namespace Code.Scripts.Core
{
    [CreateAssetMenu(fileName = "GameConfig", menuName = "ScriptableObjects/GameConfigSO", order = 1)]
    public class GameConfigSO : ScriptableObject
    {
        [Header("Initial Progress")]
        public int InitialWeek = 1;
        public int InitialDay = 1;
        
        [Header("Dice Case")]
        public int diceCaseCapacity = 12;
        public DiceSO[] startingDices;
        
        [Header("Initial Budget")]
        public int shotsPerDay = 5;
        public int rerollsPerDay = 2;
    }
}