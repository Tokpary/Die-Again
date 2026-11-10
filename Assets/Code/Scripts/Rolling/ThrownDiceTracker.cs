using System;
using System.Collections.Generic;

namespace Code.Scripts.Dice
{
    public class ThrownDiceTracker
    {
        private readonly List<Dice> _dice = new List<Dice>();

        public IReadOnlyList<Dice> Dice => _dice;
        public event Action AllSettled;
        
        public void Add(Dice dice)
        {
            _dice.Add(dice);
            dice.Motion.MarkRolling();
            dice.Motion.OnDiceSettled += OnDiceSettled;
        }
        
        public void Remove(Dice dice)
        {
            if (_dice.Remove(dice))
            {
                dice.Motion.OnDiceSettled -= OnDiceSettled;
                CheckAllSettled();
            }
        }

        public void Clear()
        {
            foreach(Dice dice in _dice)
            {
                dice.Motion.OnDiceSettled -= OnDiceSettled;
            }
            _dice.Clear();
        }
        
        private void OnDiceSettled() => CheckAllSettled();
        
        private void CheckAllSettled()
        {
            if(_dice.Count == 0) return;
            foreach (Dice dice in _dice)
            {
                if (!dice.Motion.IsSettled)
                {
                    return;
                }
            }
            AllSettled?.Invoke();
        }
    }
}