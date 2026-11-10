using Code.Scripts.Dice.DiceMaterials;

namespace Code.Scripts.Dice
{
    public class DiceInstance
    {
        public DiceSO DiceData {get; private set;}
        public int[] FaceValues {get; private set;}
        
        public DiceMaterial DiceMaterial {get; set;}
        
        public DiceInstance(DiceSO diceData)
        {
            DiceData = diceData;
            FaceValues = (int[])diceData.faceValues.Clone();
            DiceMaterial = diceData.diceMaterial;
        }
    }
}