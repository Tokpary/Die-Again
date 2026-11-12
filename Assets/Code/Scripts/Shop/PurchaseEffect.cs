using System;
using System.Collections.Generic;
using Code.Scripts.Dice;
using DG.Tweening;
using UnityEngine;

namespace Code.Scripts.Shop
{
    public class PurchaseEffect : MonoBehaviour
    {
        [SerializeField] private DiceAnimator _animator;
        [SerializeField] private DiceDrawer _diceDrawer;
        [SerializeField] private DicePlayArea _playArea;
        [SerializeField] private float _mergeHeight = 0.3f;

        [Header("VFX")]
        [SerializeField] private ParticleSystem _mergeVfx;
        [SerializeField] private GameObject _spawnVfx;

        [Header("Mezcla fallida")]
        [SerializeField] private GameObject _failVfx;
        [SerializeField, Min(0f)] private float _failHoldTime = 0.8f;

        public void Play(IReadOnlyList<Dice.Dice> spent, DiceInstance bought, Action onComplete)
        {
            Merge(spent, point =>
            {
                if (_spawnVfx != null) Instantiate(_spawnVfx, point, Quaternion.identity);
                Dice.Dice dice = _diceDrawer.Spawn(bought, point);
                _animator.Appear(new[] { dice }, null, () =>
                    _animator.ArcTo(new[] { dice }, _diceDrawer.GetReturnPoint, d => _diceDrawer.Add(d), onComplete));
            });
        }

        public void PlayFailed(IReadOnlyList<Dice.Dice> spent, Action onComplete)
        {
            Merge(spent, point =>
            {
                if (_failVfx != null) Instantiate(_failVfx, point, Quaternion.identity);
                if (_failHoldTime > 0f) DOVirtual.DelayedCall(_failHoldTime, () => onComplete?.Invoke());
                else onComplete?.Invoke();
            });
        }

        private void Merge(IReadOnlyList<Dice.Dice> spent, Action<Vector3> onMerged)
        {
            Vector3 point = MergePoint(spent);
            _animator.ArcTo(spent, _ => point, null, () =>
            {
                if (_mergeVfx != null)
                {
                    _mergeVfx.transform.position = point;
                    _mergeVfx.Play();
                }

                _animator.Vanish(spent, d => Destroy(d.gameObject), () => onMerged(point));
            });
        }

        private Vector3 MergePoint(IReadOnlyList<Dice.Dice> spent)
        {
            if (_playArea != null) return _playArea.SurfaceCenter + Vector3.up * _mergeHeight;

            Vector3 center = Vector3.zero;
            foreach (Dice.Dice d in spent) center += d.transform.position;
            return (spent.Count > 0 ? center / spent.Count : transform.position) + Vector3.up * _mergeHeight;
        }
    }
}