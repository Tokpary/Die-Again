using System;
using DG.Tweening;
using UnityEngine;

namespace Code.Scripts.Shop
{
    public class ShopDisplayRotation : MonoBehaviour
    {
        [SerializeField] private float _visibleRotationY = 90f;
        [SerializeField] private float _duration = 1f;

        private Quaternion _hiddenRotation;
        private Quaternion _visibleRotation;
        private bool _isVisible;

        private void Awake()
        {
            _visibleRotation = transform.localRotation;
            _hiddenRotation = _visibleRotation * Quaternion.Euler(0f, -_visibleRotationY, 0f);
            transform.localRotation = _hiddenRotation;
        }

        public void Show(Action onComplete = null)
        {
            if (_isVisible)
            {
                onComplete?.Invoke();
                return;
            }

            _isVisible = true;
            transform.DOKill();
            transform.DOLocalRotateQuaternion(_visibleRotation, _duration)
                .SetEase(Ease.InOutSine)
                .OnComplete(() => onComplete?.Invoke());
        }

        public void Hide(Action onComplete = null)
        {
            if (!_isVisible)
            {
                onComplete?.Invoke();
                return;
            }

            _isVisible = false;
            transform.DOKill();
            transform.DOLocalRotateQuaternion(_hiddenRotation, _duration)
                .SetEase(Ease.InOutSine)
                .OnComplete(() => onComplete?.Invoke());
        }

        private void OnDestroy()
        {
            transform.DOKill();
        }
    }
}
