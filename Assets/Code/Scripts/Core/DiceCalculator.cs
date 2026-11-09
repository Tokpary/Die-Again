using System.Collections.Generic;
using Code.Scripts.Dice;
using Code.Scripts.Dice.DiceMaterials;

namespace Code.Scripts.Core
{
    public static class DiceCalculator
    {
        public static ScoreResult Calculate(IReadOnlyList<Dice.Dice> thrownDice,
            IReadOnlyList<CursedObject.CursedObject> cursedObjects, int target, int tolerance)
        {
            var result = new ScoreResult{TargetScore = target, Tolerance = tolerance};

            foreach (Dice.Dice dice in thrownDice)
            {
                var diceResult = new DiceResult(dice, dice.GetUpwardFaceValue());
                DiceMaterial material = dice.Data.DiceMaterial;
                if(material != null)
                {
                    diceResult.Value = material.ModifyValue(diceResult.Value);
                }
                result.Dice.Add(diceResult);
            }
            
            foreach(CursedObject.CursedObject obj in cursedObjects) 
            {
                obj.ModifyScore(result);
            }
            
            result.TotalScore = 0;

            foreach (DiceResult diceResult in result.Dice)
            {
                result.TotalScore += diceResult.Value;
            }

            foreach (CursedObject.CursedObject obj in cursedObjects)
            {
                obj.ModifyResult(result);
            }
            return result;
        }
    }
}