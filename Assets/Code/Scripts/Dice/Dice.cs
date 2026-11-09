using UnityEngine;

namespace Code.Scripts.Dice
{
    [RequireComponent(typeof(DiceFaceValueProbe))]
    public class Dice : MonoBehaviour
    {
        public DiceInstance Data { get; private set; }
        private DiceFaceValueProbe FaceValueProbe { get; set; }
        
        private void Awake()
        {
            FaceValueProbe = GetComponent<DiceFaceValueProbe>();
        }
        
        public void Init(DiceInstance data)
        {
            Data = data;
            FaceValueProbe.SetDiceData(data.DiceData);
        }
        
        public int GetUpwardFaceValue()
        {
            int index = FaceValueProbe.GetUpwardFaceValue();
            return index >= 0 && index < Data.FaceValues.Length ? Data.FaceValues[index] : 0; 
        }
    }
}