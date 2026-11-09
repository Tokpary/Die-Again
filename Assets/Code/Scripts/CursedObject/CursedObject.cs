using UnityEngine;

namespace Code.Scripts.CursedObject
{
    public abstract class CursedObject : ScriptableObject
    {
        public string displayName;
        [TextArea] public string description;
        public virtual void ModifyScore(ScoreResult result) {}

        public virtual void ModifyResult(ScoreResult result) {}
        public virtual int ModifyBossProbability(int day, int probability) => probability;
    }
}