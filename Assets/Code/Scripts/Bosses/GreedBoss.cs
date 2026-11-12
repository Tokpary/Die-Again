using Code.Scripts.Rolling;
using UnityEngine;

namespace Code.Scripts.Bosses
{
    [CreateAssetMenu(fileName = "ThrowAllBoss", menuName = "ScriptableObjects/Bosses/Greed")]
    public class GreedBoss : BossSO
    {
        public override int ModifyShots(int shots, int diceCount) => Mathf.Max(shots, diceCount);

        public override bool CanCheck(GameContext context, RollSession session)
        {
            bool allThrown = session.ThrownDice.Count + session.EliminatedCount >= context.RunData.DiceCase.Dice.Count;
            return allThrown || session.Budget.Shots <= 0;
        }
    }
}