using System;
using Code.Scripts.Dice;
using UnityEngine;

namespace Code.Scripts.Table
{
    [DefaultExecutionOrder(-10)]
    public class DiceDragController : MonoBehaviour
    {
        [SerializeField] private UnityEngine.Camera _camera;
        [SerializeField] private LayerMask _diceLayerMask;
        [SerializeField] private DiceDrawer _diceDrawer;
        [SerializeField] private float _dragHeight = 1.5f;

        private GameInputs _gameInputs;
        private Dice.Dice _draggedDice;

        public bool IsDragging => _draggedDice != null;
        public bool SelectionMode { get; set; }
        public bool DragEnabled { get; set; }
        public bool PressConsumed { get; private set; }

        public IDiceDropTarget DropTarget { get; set; }

        public event Action<Dice.Dice> DiceSelected;
        public event Action<Dice.Dice> DroppedOnTarget;

        public void Init(GameInputs input)
        {
            _gameInputs = input;
        }

        private void Update()
        {
            PressConsumed = false;
            if (_gameInputs == null || !_gameInputs.Gameplay.enabled) return;

            Vector2 pointer = _gameInputs.Gameplay.Apuntar.ReadValue<Vector2>();
            Ray ray = _camera.ScreenPointToRay(pointer);

            if (SelectionMode)
            {
                if (_gameInputs.Gameplay.Disparo.WasPressedThisFrame())
                {
                    PressConsumed = true;
                    if (TryGetDice(ray, out Dice.Dice dice)) DiceSelected?.Invoke(dice);
                }
                return;
            }

            if (!DragEnabled)
            {
                if (_draggedDice != null) ReturnToDrawer(TakeDragged());   
                return;
            }

            if (_draggedDice == null && _gameInputs.Gameplay.Disparo.WasPressedThisFrame())
                TryPick(ray);

            if (_draggedDice == null) return;

            MoveDragged(ray);
            bool overTarget = DropTarget != null && DropTarget.ContainsScreenPoint(pointer);
            DropTarget?.SetHover(overTarget);

            if (_gameInputs.Gameplay.Disparo.WasReleasedThisFrame())
            {
                Dice.Dice dice = TakeDragged();
                if (overTarget) DroppedOnTarget?.Invoke(dice);
                else ReturnToDrawer(dice);
            }
        }

        private void TryPick(Ray ray)
        {
            if (!TryGetDice(ray, out Dice.Dice dice) || !_diceDrawer.Contains(dice)) return;

            _draggedDice = dice;
            dice.GetComponent<Rigidbody>().isKinematic = true;
            PressConsumed = true;
        }

        private void MoveDragged(Ray ray)
        {
            var plane = new Plane(Vector3.up, new Vector3(0f, _dragHeight, 0f));
            if (!plane.Raycast(ray, out float distance)) return;

            Vector3 target = _diceDrawer.Area.ClosestPoint(ray.GetPoint(distance));
            target.y = _dragHeight;
            _draggedDice.transform.position = target;
        }

        private Dice.Dice TakeDragged()
        {
            Dice.Dice dice = _draggedDice;
            _draggedDice = null;
            DropTarget?.SetHover(false);
            return dice;
        }

        private void ReturnToDrawer(Dice.Dice dice)
        {
            var rb = dice.GetComponent<Rigidbody>();
            rb.isKinematic = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        private bool TryGetDice(Ray ray, out Dice.Dice dice)
        {
            dice = null;
            return Physics.Raycast(ray, out RaycastHit hit, 500f, _diceLayerMask) &&
                   hit.rigidbody != null && hit.rigidbody.TryGetComponent(out dice);
        }
    }
}