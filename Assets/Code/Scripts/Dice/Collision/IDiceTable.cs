using System.Collections.Generic;

namespace Code.Scripts.Dice
{
    public interface IDiceTable
    {
        IReadOnlyList<Dice> ThrownDices { get; }
        bool IsInPlay(Dice dice);
        void Eliminate(Dice dice);
    }
}