using System;
using System.Collections.Generic;
using DG.Tweening;
using Unity.VisualScripting;
using UnityEngine;

namespace Code.Scripts.Core
{
    public class CameraController : MonoBehaviour
    {
        [SerializeField] private Camera _mainCamera;
    
        [SerializeField] private Transform _shotsRoot;
    
        [SerializeField] private float _defaultDuration = 1f;
        [SerializeField] private Ease _defaultEase = Ease.InOutSine;
        [SerializeField] private float _orbitSpeed = 90f;
        
        [SerializeField] private float _unwindDuration = 0.6f;

        private GameInputs _input;
        private CameraShot _orbitShot;
        private float _orbitAngle;
        
        public void Init(GameInputs input)
        {
            _input = input;
        }

        private readonly Dictionary<CameraShotId, CameraShot> _shots = new Dictionary<CameraShotId, CameraShot>();
        
        public CameraShotId CurrentShotId { get; private set; }
        public bool IsMoving => DOTween.IsTweening(_mainCamera.transform);

        private void Awake()
        {
            foreach (CameraShot shot in _shotsRoot.GetComponentsInChildren<CameraShot>())
            {
                _shots[shot.Id] = shot;
            }
        }

        public void SnapTo(CameraShotId id)
        {
            StopTravel();
            _orbitShot = null;
            CameraShot shot = _shots[id];
            _mainCamera.transform.SetPositionAndRotation(shot.transform.position, shot.transform.rotation);
            _mainCamera.fieldOfView = shot.FieldOfView;
            CurrentShotId = id;
        }

        public void MoveTo(CameraShotId id, Action onArrived = null, float duration = -1f)
        {
            CameraShot shot = _shots[id];

            if (_orbitShot == shot)
            {
                onArrived?.Invoke();
                return;
            }
            
            StopTravel();
            CurrentShotId = id;
            
            if(_orbitShot != null) UnwindOrbit(() => Travel(shot, onArrived, duration));
            else Travel(shot, onArrived, duration);
            
        }

        private void UnwindOrbit(Action onDone)
        {
            Vector3 pivot = _orbitShot.OrbitPivot.position;
            float offset = Mathf.DeltaAngle(0f, _orbitAngle);
            _orbitShot = null;
            
            if(Mathf.Abs(offset) < 0.5f)
            {
                onDone();
                return;
            }

            float applied = offset;
            DOVirtual.Float(offset, 0f, Mathf.Abs(offset)/180f * _unwindDuration, value =>
            {
                _mainCamera.transform.RotateAround(pivot, Vector3.up, value - applied);
                applied = value;
            }).SetEase(Ease.InOutSine).SetTarget(_mainCamera.transform).OnComplete(() =>
            {
                onDone();
            });

        }

        private void Travel(CameraShot shot, Action onArrived, float duration)
        {
            
            float time = duration > 0f ? duration : _defaultDuration;
            
            _mainCamera.transform.DOMove(shot.transform.position, time).SetEase(_defaultEase).OnComplete((() =>
            {
                if (shot.OrbitPivot != null)
                {
                    _orbitShot = shot;
                    _orbitAngle = 0f;
                };
                onArrived?.Invoke();
            }));
            _mainCamera.transform.DORotateQuaternion(shot.transform.rotation, time).SetEase(_defaultEase);
            _mainCamera.DOFieldOfView(shot.FieldOfView, time).SetEase(_defaultEase);
        }
        

        private void LateUpdate()
        {
            if (_orbitShot == null || _input == null || !_input.Gameplay.enabled) return;

            float input = _input.Gameplay.Orbitar.ReadValue<float>();
            if (Mathf.Approximately(input, 0f)) return;

            float step = input * _orbitSpeed * Time.deltaTime;
            _mainCamera.transform.RotateAround(_orbitShot.OrbitPivot.position, Vector3.up, step);
            _orbitAngle = Mathf.Repeat(_orbitAngle + step, 360f);
        }
        
        

        private void StopTravel()
        {
            _mainCamera.transform.DOKill();
            _mainCamera.DOKill();
        }

        private void OnDestroy()
        {
            StopTravel();
        }
    }
}
