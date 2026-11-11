using System;
using Code.Scripts.Dice.DiceMaterials;
using UnityEngine;

namespace Code.Scripts.Dice
{

    public abstract class DiceCollisionEffect : ScriptableObject
    {

        [SerializeField] private int _priority;

        [SerializeField] private bool _ignoreSameMaterial = true;
        [SerializeField] private DiceMaterial[] _immuneMaterials;
        [SerializeField, Min(0f)] private float _minImpact;

        public int Priority => _priority;

        public void Apply(DiceCollision hit)
        {
            if (Affects(hit)) OnHit(hit);
        }

        protected abstract void OnHit(DiceCollision hit);

        private bool Affects(DiceCollision hit)
        {
            if (hit.Impact < _minImpact) return false;

            DiceMaterial otherMaterial = hit.Other.Data.DiceMaterial;
            if (otherMaterial == null) return true;
            if (_ignoreSameMaterial && otherMaterial == hit.Self.Data.DiceMaterial) return false;
            return _immuneMaterials == null || Array.IndexOf(_immuneMaterials, otherMaterial) < 0;
        }
    }
}