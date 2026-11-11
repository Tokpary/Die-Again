using UnityEngine;

namespace Code.Scripts.Dice
{
    public readonly struct DiceCollision
    {
        public Dice Self { get; }
        public Dice Other { get; }
        public Vector3 Point { get; }
        public float Impact { get; }
        public IDiceTable Table { get; }
        
        public DiceCollision(Dice self, Dice other, Vector3 point, float impact, IDiceTable diceTable)
        {
            Self = self;
            Other = other;
            Point = point;
            Impact = impact;
            Table = diceTable;
        }
    }
}