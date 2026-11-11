using DG.Tweening;
using UnityEngine;

namespace Code.Scripts.Dice.Collision.Effects
{
    [CreateAssetMenu(fileName = "ConvertOnCollision", menuName = "ScriptableObjects/DiceCollisionEffects/Convert")]
    public class ConvertOnCollision : DiceCollisionEffect
    {
        [SerializeField] private GameObject _vfx;
        [SerializeField, Min(0f)] private float _punch = 0.2f;

        protected override void OnHit(DiceCollision hit)
        {
            Dice converted = hit.Table.Convert(hit.Other, hit.Self);
            if (converted == null) return;

            if (_vfx != null) Instantiate(_vfx, converted.transform.position, Quaternion.identity);
            if (_punch > 0f)
                converted.transform.DOPunchScale(converted.BaseScale * _punch, 0.3f, 6, 0.5f).SetLink(converted.gameObject);
        }
    }
}