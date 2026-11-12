using DG.Tweening;
using UnityEngine;

namespace Code.Scripts.Dice.Collision.Effects
{
    [CreateAssetMenu(fileName = "AnnihilateOnCollision", menuName = "ScriptableObjects/DiceCollisionEffects/Annihilate")]
    public class AnnihilateOnCollision : DiceCollisionEffect
    {
        [SerializeField, Min(0f)] private float _repelDistance = 0.4f;
        [SerializeField, Min(0f)] private float _repelDuration = 0.25f;

        [SerializeField] private float _fusionHeight = 0.3f;
        [SerializeField, Min(0.05f)] private float _collapseDuration = 0.6f;
        [SerializeField] private float _spinTurns = 1.5f;

        [SerializeField, Min(0f)] private float _pullStrength = 15f;
        [SerializeField, Min(0f)] private float _pullRadius = 3f;
        [SerializeField, Min(0f)] private float _pullLinger = 0.3f;

        [SerializeField] private GameObject _startVfx;
        [SerializeField] private GameObject _endVfx;
        [SerializeField] private AudioClip _sfx;

        protected override void OnHit(DiceCollision hit)
        {
            if (!hit.Table.IsInPlay(hit.Other)) return; 
            Vector3 contact = hit.Point;
            Vector3 fusionPoint = contact + Vector3.up * _fusionHeight;
            IDiceTable table = hit.Table;

            table.Eliminate(hit.Self, t => Fuse(t, contact, fusionPoint));
            table.Eliminate(hit.Other, t => Fuse(t, contact, fusionPoint));

            if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX(_sfx);

            GameObject startVfx = _startVfx;
            GameObject endVfx = _endVfx;
            DOVirtual.DelayedCall(_repelDuration, () =>
            {
                if (startVfx != null) Instantiate(startVfx, fusionPoint, Quaternion.identity);
                if (_pullStrength > 0f) table.Pull(fusionPoint, _pullStrength, _pullRadius, _collapseDuration + _pullLinger);
            });
            if (endVfx != null)
                DOVirtual.DelayedCall(_repelDuration + _collapseDuration, () => Instantiate(endVfx, fusionPoint, Quaternion.identity));
        }

        private Tween Fuse(Transform t, Vector3 contact, Vector3 fusionPoint)
        {
            Vector3 away = t.position - contact;
            away.y = 0f;
            if (away.sqrMagnitude < 0.0001f)
            {
                Vector2 random = Random.insideUnitCircle;
                away = new Vector3(random.x, 0f, random.y);
            }
            Vector3 repelPoint = t.position + away.normalized * _repelDistance;

            return DOTween.Sequence()
                .Append(t.DOMove(repelPoint, _repelDuration).SetEase(Ease.OutQuad))
                .Append(t.DOMove(fusionPoint, _collapseDuration).From(repelPoint, false).SetEase(Ease.InQuad))
                .Join(t.DOScale(Vector3.zero, _collapseDuration).SetEase(Ease.InBack))
                .Join(t.DORotate(new Vector3(0f, 360f * _spinTurns, 0f), _collapseDuration, RotateMode.WorldAxisAdd)
                    .SetEase(Ease.InQuad));
        }
    }
}