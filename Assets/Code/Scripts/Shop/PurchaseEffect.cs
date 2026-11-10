using System;
using System.Collections.Generic;
using Code.Scripts.Dice;
using UnityEngine;

namespace Code.Scripts.Shop
{
    public class PurchaseEffect : MonoBehaviour
    {
        [SerializeField] private DiceAnimator _animator;
        [SerializeField] private DiceDrawer _diceDrawer;
        [SerializeField] private ParticleSystem _mergeVfx;
        [SerializeField] private float _mergeHeight = 1.5f;
        
        public void Play(IReadOnlyList<Dice.Dice> spent, DiceInstance bought, Action onComplete)
        {
            Vector3 point = MergePoint(spent);
            _animator.ArcTo(spent, _ => point, null, () =>
            {
                if (_mergeVfx != null)
                {
                    _mergeVfx.transform.position = point;
                    _mergeVfx.Play();
                }

                _animator.Vanish(spent, d => Destroy(d.gameObject), () =>
                {
                    Dice.Dice dice = _diceDrawer.Spawn(bought, point);
                    _animator.Appear(new []{dice}, null, () => _animator.ArcTo(new []{dice}, _diceDrawer.GetReturnPoint, d => _diceDrawer.Add(d), onComplete));
                });
            });
        }
        
        private Vector3 MergePoint(IReadOnlyList<Dice.Dice> spent)
        {
            Vector3 center = Vector3.zero;
            foreach (Dice.Dice d in spent)
            {
                center += d.transform.position;
            }
            return center/spent.Count + Vector3.up * _mergeHeight;
        }
    }
}