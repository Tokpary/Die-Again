using System;
using System.Collections.Generic;

namespace Code.Scripts.Dice
{
    public class DiceCase 
    {
        private readonly List<DiceInstance> _diceInstances = new List<DiceInstance>();
        
        public DiceCase(int capacity) => Capacity = capacity;
        
        public int Capacity { get; }
        public IReadOnlyList<DiceInstance> Dice => _diceInstances;
        public bool IsFull => _diceInstances.Count >= Capacity;
        
        public event Action Changed;
        
        public bool TryAdd(DiceInstance dice)
        {
            if (IsFull || dice == null) return false;
            _diceInstances.Add(dice);
            Changed?.Invoke();
            return true;
        }
        
        public bool Remove(DiceInstance dice)
        {
            if (!_diceInstances.Remove(dice)) return false;
            Changed?.Invoke();
            return true;
        }
    }
}