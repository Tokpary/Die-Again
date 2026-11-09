using System;
using UnityEngine;

namespace Code.Scripts.Dice
{
    public class DiceSettleDetector : MonoBehaviour
    {
        [SerializeField] private float _settleThreshold = 0.05f;
        [SerializeField] private float _settleTime = 0.3f;
        
        private Rigidbody _rigidbody;
        private float _calmTime;

        public bool IsSettled { get; private set; } = true;
        public event Action OnDiceSettled;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
        }

        public void MarkRolling()
        {
            IsSettled = false;
            _calmTime = 0f;
        }
        
        private void Update()
        {
            if(_rigidbody.isKinematic) return;
            
            bool calm = _rigidbody.linearVelocity.sqrMagnitude < _settleThreshold * _settleThreshold &&
                        _rigidbody.angularVelocity.sqrMagnitude < _settleThreshold * _settleThreshold;

            if (!calm)
            {
                _calmTime = 0f;
                IsSettled = false;
                return;
            }
            
            if(IsSettled) return;
            _calmTime += Time.fixedDeltaTime;
            if(_calmTime >= _settleTime)
            {
                IsSettled = true;
                OnDiceSettled?.Invoke();
            }
        }
    }
}