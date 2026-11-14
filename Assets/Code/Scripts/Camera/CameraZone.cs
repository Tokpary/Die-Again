using Code.Scripts.Camera;
using UnityEngine;

namespace Code.Scripts.Core
{
    public class CameraZone : MonoBehaviour
    {
        [SerializeField] private CameraShotId _view;
        [SerializeField] private ZoneHighlight _highlight;
        
        [Header("Vecinas")]
        [SerializeField] private CameraZone _leftZone;
        [SerializeField] private CameraZone _rightZone;
        [SerializeField] private CameraZone _upZone;
        [SerializeField] private CameraZone _downZone;
        
        public CameraShotId View => _view;
        
        public void SetHighlighted(bool value)
        {
            if(_highlight != null) _highlight.SetHighlighted(value);
        }

        public CameraZone GetNeighbor(Vector2 direction)
        {
            if(Mathf.Abs(direction.x) >= Mathf.Abs(direction.y))
            {
                return direction.x >= 0 ? _rightZone : _leftZone ;
            }
            else
            {
                return direction.y >= 0f ? _upZone : _downZone;
            }
        }
    }
}