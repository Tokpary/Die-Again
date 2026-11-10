using UnityEngine;

namespace Code.Scripts.Dice
{
    public class DicePlayArea : MonoBehaviour
    {
        [SerializeField] private Collider _area;
        [SerializeField] private float _fallMargin = 0.5f;

        public bool IsInside(Vector3 point, float margin)
        {
            Bounds bounds = _area.bounds;
            return point.x >= bounds.min.x - margin && point.x <= bounds.max.x + margin &&
                   point.z >= bounds.min.z - margin && point.z <= bounds.max.z + margin;
        }
        
        public bool HasFallen (Vector3 point) => point.y < _area.bounds.min.y - _fallMargin;
    }
}