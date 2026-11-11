using UnityEngine;

namespace Code.Scripts.CursedObject
{
    public abstract class CursedObject : ScriptableObject
    {
        [TextArea] public string description;
        public virtual void ModifyScore(ScoreResult result) {}

        public virtual void ModifyResult(ScoreResult result) {}
        public virtual int ModifyBossProbability(int day, int probability) => probability;
        
        [Header("Info")]
        public string displayName;
        [TextArea] public string descriptionInGame;
        public Sprite preview;
        
        [Header("Escena")]
        public GameObject prefab;
        
        public string DisplayName => string.IsNullOrEmpty(displayName) ? name : displayName;
    }
}