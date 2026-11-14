using System;
using UnityEngine;

namespace Code.Scripts.Table
{
    public class DiceDropZone : MonoBehaviour
    {
        [SerializeField] private Collider _trigger;
        [SerializeField] private GameObject _hoverHighlight;

        private void Reset()
        {
            _trigger = GetComponent<Collider>();
        }

        public bool Contains(Vector3 worldPosition)
        {
            Bounds b = _trigger.bounds;
            return worldPosition.x >= b.min.x && worldPosition.x <= b.max.x &&
                   worldPosition.z >= b.min.z && worldPosition.z <= b.max.z;
        }

        public void SetHover(bool hover)
        {
            if (_hoverHighlight != null) _hoverHighlight.SetActive(hover);
        }
    }
}