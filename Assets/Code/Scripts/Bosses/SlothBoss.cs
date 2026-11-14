using UnityEngine;

namespace Code.Scripts.Bosses
{
    [CreateAssetMenu(fileName = "NoRerollBoss", menuName = "ScriptableObjects/Bosses/Sloth")]
    public class SlothBoss : BossSO
    {
        public override int ModifyRerolls(int rerolls) => 0;
    }
}