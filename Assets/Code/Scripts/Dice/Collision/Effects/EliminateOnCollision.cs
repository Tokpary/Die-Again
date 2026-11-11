using System;
using Code.Scripts.Dice.DiceMaterials;
using UnityEngine;

namespace Code.Scripts.Dice.Collision.Effects
{
    [CreateAssetMenu(fileName = "EliminateOnCollision", menuName = "ScriptableObjects/DiceCollisionEffects/Eliminate")]
    public class EliminateOnCollision : DiceCollisionEffect
    {
        [SerializeField] private bool _ignoreSameMaterial = true;
        [SerializeField] private DiceMaterial[] _immuneMaterials;
        [SerializeField, Min(0f)] private float _minImpact;
        [SerializeField] private GameObject _vfx;

        public override void Apply(DiceCollision hit)
        {
            if (hit.Impact < _minImpact || !hit.Table.IsInPlay(hit.Other)) return;

            DiceMaterial otherMaterial = hit.Other.Data.DiceMaterial;
            if (otherMaterial != null)
            {
                if (_ignoreSameMaterial && otherMaterial == hit.Self.Data.DiceMaterial) return;
                if (_immuneMaterials != null && Array.IndexOf(_immuneMaterials, otherMaterial) >= 0) return;
            }

            if (_vfx != null) Instantiate(_vfx, hit.Point, Quaternion.identity);
            hit.Table.Eliminate(hit.Other);
        }
    }
}