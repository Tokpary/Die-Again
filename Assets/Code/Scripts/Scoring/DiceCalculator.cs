using System.Collections.Generic;
using UnityEngine;
using Code.Scripts.Dice;
using Code.Scripts.Dice.DiceMaterials;

namespace Code.Scripts.Core
{
    public static class DiceCalculator
    {
        public static ScoreResult Calculate(IReadOnlyList<Dice.Dice> thrownDice,
            IReadOnlyList<CursedObject.CursedObject> cursedObjects, int target, int tolerance, int day = 0)
        {
            var result = new ScoreResult{TargetScore = target, Tolerance = tolerance, Day = day};

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
                if (obj != null) obj.ModifyScore(result);
            }
            
            result.TotalScore = 0;

            foreach (DiceResult diceResult in result.Dice)
            {
                result.TotalScore += diceResult.Value;
            }

            ApplyResultModifiers(result, cursedObjects, false);
            return result;
        }

        public static ScoreResult PreviewTolerance(IReadOnlyList<CursedObject.CursedObject> cursedObjects,
            int target, int tolerance, int day)
        {
            var result = new ScoreResult { TargetScore = target, Tolerance = tolerance, Day = day };
            ApplyResultModifiers(result, cursedObjects, true);
            return result;
        }

        private static void ApplyResultModifiers(ScoreResult result,
            IReadOnlyList<CursedObject.CursedObject> cursedObjects, bool logEffects)
        {
            foreach (var obj in cursedObjects)
            {
                if (obj == null) continue;
                int lowerBefore = result.TargetScore - result.MinimumScore;
                int upperBefore = result.MaximumScore - result.TargetScore;
                obj.ModifyResult(result);
                if (logEffects)
                    Debug.Log($"[Objetos/Tolerancia] {obj.displayName} | Día {result.Day} | " +
                        $"Extra inferior: {result.TargetScore - result.MinimumScore - lowerBefore}; " +
                        $"extra superior: {result.MaximumScore - result.TargetScore - upperBefore} " +
                        "(0 = sin modificación de ese margen).", obj);
            }
        }
    }
}