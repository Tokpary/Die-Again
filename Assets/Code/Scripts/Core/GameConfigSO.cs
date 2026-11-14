using Code.Scripts.Bosses;
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

        [Header("Shop")] 
        public DiceSO[] shopPool;

        public int shopOfferSize = 10;
        public int shopMaxNumber = 20;
        
        [Header("Dice coins ")]
        public int bankCapacity = 5;              
        public DiceSO[] coinPool;               
        public int coinChoices = 3;  
        public DiceSO[] startingBankDice;
        
        [Header("Bosses")]
        [Tooltip("Jefes de la partida: se barajan al empezar y cada semana toca uno, sin repetir.")]
        public BossSO[] bossPool;
        
        [Header("Cursed objects")]
        public int cursedCapacity = 5;
        public CursedObject.CursedObject[] startingCursedObjects = new CursedObject.CursedObject[0];
        public CursedObject.CursedObject[] cursedPool;
        public int cursedChoices = 3;
        
        public float KillPlaneHeight = -1f;
    }
}