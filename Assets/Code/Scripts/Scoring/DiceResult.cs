namespace Code.Scripts.Dice
{
    public class DiceResult
    {
        public Dice Dice { get;}
        public int FaceValue { get; }
        public int Value { get; set; }
        
        public DiceResult(Dice dice, int faceValue)
        {
            Dice = dice;
            FaceValue = faceValue;
            Value = faceValue;
        }
    }
}