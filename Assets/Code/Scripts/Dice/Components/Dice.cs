using UnityEngine;

namespace Code.Scripts.Dice
{
    [RequireComponent(typeof(DiceFaceValueProbe), typeof(DiceSettleDetector))]
    public class Dice : MonoBehaviour
    {
        public DiceInstance Data { get; private set; }
        public DiceSettleDetector Motion { get; private set; }
        private DiceFaceValueProbe FaceValueProbe { get; set; }
        public Vector3 BaseScale { get; private set; }
        
        
        
        private void Awake()
        {
            FaceValueProbe = GetComponent<DiceFaceValueProbe>();
            Motion = GetComponent<DiceSettleDetector>();
            BaseScale = transform.localScale;
            if (GetComponent<DiceHover>() == null) gameObject.AddComponent<DiceHover>();
        }
        
        public void Init(DiceInstance data)
        {
            Data = data;
            FaceValueProbe.SetDiceData(data.DiceData);
            data.DiceMaterial?.ApplyVisual(this);
        }
        
        public int GetUpwardFaceValue()
        {
            int index = FaceValueProbe.GetUpwardFaceValue();
            return index >= 0 && index < Data.FaceValues.Length ? Data.FaceValues[index] : 0; 
        }
    }
}
