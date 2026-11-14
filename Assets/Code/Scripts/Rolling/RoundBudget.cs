using System;

namespace Code.Scripts.Rounds
{
    public class RoundBudget
    {
        public int Shots { get; private set; }
        public int Rerolls { get; private set; }
        public event Action OnBudgetChanged;
        public RoundBudget(int shots, int rerolls)
        {
            Shots = shots;
            Rerolls = rerolls;
        }
        
        public bool TryUseShot()
        {
            if (Shots > 0)
            {
                Shots--;
                OnBudgetChanged?.Invoke();
                return true;
            }
            return false;
        }
        
        public bool TryUseReroll()
        {
            if (Rerolls > 0)
            {
                Rerolls--;
                OnBudgetChanged?.Invoke();
                return true;
            }
            return false;
        }
    }
}