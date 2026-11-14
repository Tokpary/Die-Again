using DG.Tweening;
using UnityEngine;

namespace Code.Scripts.Dice.Collision.Effects
{
    [CreateAssetMenu(fileName = "EliminateOnCollision", menuName = "ScriptableObjects/DiceCollisionEffects/Eliminate")]
    public class EliminateOnCollision : DiceCollisionEffect
    {
        private enum Target { Other, Self, Both }

        [SerializeField] private Target _target = Target.Other;
        [SerializeField, Min(0f)] private float _vanishDuration;
        [SerializeField] private GameObject _vfx;
        [SerializeField] private AudioClip _sfx;

        [SerializeField, Min(0f)] private float _blastForce;
        [SerializeField, Min(0f)] private float _blastRadius = 2.5f;
        [SerializeField, Min(0f)] private float _blastUpwards = 0.5f;

        protected override void OnHit(DiceCollision hit)
        {
            bool eliminated = false;
            if (_target != Target.Self) eliminated |= TryEliminate(hit.Table, hit.Other);
            if (_target != Target.Other) eliminated |= TryEliminate(hit.Table, hit.Self);
            if (!eliminated) return;

            if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX(_sfx);
            if (_vfx != null) Instantiate(_vfx, hit.Point, Quaternion.identity);
            if (_blastForce > 0f) hit.Table.Blast(hit.Point, _blastForce, _blastRadius, _blastUpwards);
        }

        private bool TryEliminate(IDiceTable table, Dice dice)
        {
            if (!table.IsInPlay(dice)) return false;

            if (_vanishDuration > 0f)
                table.Eliminate(dice, t => t.DOScale(Vector3.zero, _vanishDuration).SetEase(Ease.InBack));
            else
                table.Eliminate(dice);
            return true;
        }
    }
}