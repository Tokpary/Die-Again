using UnityEngine;

namespace Code.Scripts.Core
{
    public class CameraShot : MonoBehaviour
    {
        [SerializeField] private Transform _orbitPivot;
        [SerializeField] private float _maxOrbitAngle = 180f;
        
        [SerializeField] private CameraShotId _id;
        [SerializeField] private float _fieldOfView = 60f;
        
        public CameraShotId Id => _id;
        public float FieldOfView => _fieldOfView;
        
        public Transform OrbitPivot => _orbitPivot;
        public float MaxOrbitAngle => _maxOrbitAngle;
    }
}