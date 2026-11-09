using System;
using UnityEngine;

namespace Code.Scripts.Dice
{
    [DefaultExecutionOrder(-10)]
    public class DiceDragController : MonoBehaviour
    {
        [SerializeField] private Camera _camera;
        [SerializeField] private LayerMask _diceLayerMask;
        [SerializeField] private DiceDrawer _diceDrawer;
        [SerializeField] private Collider _cannonLoadZone;
        [SerializeField] private float _dragHeight = 1.5f;
        
        private GameInputs _gameInputs;
        private Dice _draggedDice;
        
        public bool IsDragging => _draggedDice != null;
        public bool SelectionMode { get; set; }
        public bool PressConsumed { get; private set; }

        public event Action<Dice> DroppedOnCannon;
        public event Action<Dice> DiceSelected;
        
        public void Init(GameInputs input) 
        {
            _gameInputs = input;
        }

        private void Update()
        {
            PressConsumed = false;
            if(_gameInputs == null || !_gameInputs.Gameplay.enabled) return;
            Ray ray = _camera.ScreenPointToRay(_gameInputs.Gameplay.Apuntar.ReadValue<Vector2>());
            if(SelectionMode)
            {
                if(_gameInputs == null || !_gameInputs.Gameplay.enabled) return;
                if(_gameInputs.Gameplay.Disparo.WasPressedThisFrame())
                {
                    PressConsumed = true;
                    if(Physics.Raycast(ray, out RaycastHit hit, 500f, _diceLayerMask) && hit.rigidbody != null && hit.rigidbody.TryGetComponent(out Dice dice))
                    {
                        DiceSelected?.Invoke(dice);
                    }
                }
                return;
            }
            
            if(_draggedDice == null && _gameInputs.Gameplay.Disparo.WasPressedThisFrame())
            {
                TryPick(ray);
            }

            if (_draggedDice == null) return;
            
            var plane = new Plane(Vector3.up, new Vector3(0f, _dragHeight, 0f));
            if(plane.Raycast(ray, out float distance))
                _draggedDice.transform.position = ray.GetPoint(distance);
            
            if(_gameInputs.Gameplay.Disparo.WasReleasedThisFrame())
                Drop();
        }

        private void TryPick(Ray ray)
        {
            if(!Physics.Raycast(ray, out RaycastHit hit, 500f, _diceLayerMask)) return;
            if (hit.rigidbody == null || !hit.rigidbody.TryGetComponent(out Dice dice)) return;
            if (!_diceDrawer.Contains(dice)) return;
            
            _draggedDice = dice;
            hit.rigidbody.isKinematic = true;
            PressConsumed = true;
        }

        private void Drop()
        {
            Dice dice = _draggedDice;
            _draggedDice = null;
            
            if(_cannonLoadZone.bounds.Contains(dice.transform.position))
            {
                DroppedOnCannon?.Invoke(dice);
            }
            else
            {
                dice.GetComponent<Rigidbody>().isKinematic = false;
            }
        }
    }
}