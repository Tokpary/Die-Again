using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace Code.Scripts.Dice
{
    public interface IDiceTable
    {
        IReadOnlyList<Dice> ThrownDice { get; }

        bool IsInPlay(Dice dice);
        void Eliminate(Dice dice, Func<Transform, Tween> vanish = null);

        void Blast(Vector3 center, float force, float radius, float upwards = 0.5f);

        void Pull(Vector3 center, float acceleration, float radius, float duration);
        
        Dice Convert(Dice target, Dice model);
    }
}