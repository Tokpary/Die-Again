using UnityEngine;

namespace Code.Scripts.Dice
{
    public class Dice : MonoBehaviour
    {
        public DiceInstance Data { get; private set; }
        public void Init(DiceInstance data)
        {
            Data = data;
        }
    }
}