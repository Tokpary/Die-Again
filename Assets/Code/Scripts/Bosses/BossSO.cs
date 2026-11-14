using Code.Scripts.Rolling;
using UnityEngine;

namespace Code.Scripts.Bosses
{
    public abstract class BossSO : ScriptableObject
    {
        public string displayName;
        [TextArea] public string description;
        public Sprite icon;

        public string DisplayName => string.IsNullOrEmpty(displayName) ? name : displayName;

        public virtual int ModifyShots(int shots, int diceCount) => shots;
        public virtual int ModifyRerolls(int rerolls) => rerolls;

        public virtual void OnFightStart(GameContext context) { }
        public virtual void OnFightEnd(GameContext context) { }

        public virtual bool CanCheck(GameContext context, RollSession session) => true;
    }
}