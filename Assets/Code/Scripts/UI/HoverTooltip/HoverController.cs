using Code.Scripts.Camera;
using Code.Scripts.Table;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace Code.Scripts.UI
{
    public class HoverController : MonoBehaviour
    {
        [SerializeField] private UnityEngine.Camera _camera;
        [SerializeField] private HoverTooltip _tooltip;
        [Tooltip("Capas que se pueden señalar (huecos de la estantería, dados...). Solo esas: cualquier otra cosa en ellas tapa el rayo.")]
        [SerializeField] private LayerMask _hoverMask;
        [SerializeField] private float _maxDistance = 500f;

        private GameInputs _input;
        private CameraController _cameraController;
        private DiceDragController _drag;
        private bool _gameplayActive;

        private IHoverable _hovered;
        private object _hoveredData;

        public void SetGameplayActive(bool active)
        {
            _gameplayActive = active;
            if (active) return;

            UIHoverTarget.ClearCurrent();
            SetHovered(null, null);
        }

        public void Init(GameInputs input, CameraController cameraController, DiceDragController drag)
        {
            _input = input;
            _cameraController = cameraController;
            _drag = drag;
        }

        private void Update()
        {
            if (!_gameplayActive)
            {
                UIHoverTarget.ClearCurrent();
                SetHovered(null, null);
                return;
            }

            UIHoverTarget ui = UIHoverTarget.Current;
            if (ui != null)
            {
                SetHovered(null, ui.TooltipData);
                if (_hoveredData != null && Pointer.current != null) _tooltip.MoveTo(Pointer.current.position.ReadValue());
                return;
            }

            IHoverable target = FindTarget(out Vector2 pointer);
            SetHovered(target, target?.TooltipData);
            if (_hoveredData != null) _tooltip.MoveTo(pointer);
        }

        private void OnDisable() => SetHovered(null, null);

        private IHoverable FindTarget(out Vector2 pointer)
        {
            pointer = default;
            if (_input == null || !_input.Gameplay.enabled || _cameraController.IsMoving) return null;
            if (_drag != null && _drag.IsDragging) return null;
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return null;

            pointer = _input.Gameplay.Apuntar.ReadValue<Vector2>();
            Ray ray = _camera.ScreenPointToRay(pointer);
            if (!Physics.Raycast(ray, out RaycastHit hit, _maxDistance, _hoverMask, QueryTriggerInteraction.Collide)) return null;

            IHoverable hoverable = hit.collider.GetComponentInParent<IHoverable>();
            return hoverable != null && hoverable.CanHover(_cameraController.CurrentShotId) ? hoverable : null;
        }

        private void SetHovered(IHoverable target, object data)
        {
            if (target == _hovered && ReferenceEquals(data, _hoveredData)) return;

            if (IsAlive(_hovered)) _hovered.OnHoverExit();   
            _hovered = target;
            _hoveredData = data;
            if (_hovered != null) _hovered.OnHoverEnter();

            if (data != null) _tooltip.Show(data);
            else _tooltip.Hide();
        }

        private static bool IsAlive(IHoverable hoverable) =>
            hoverable is UnityEngine.Object unityObject ? unityObject != null : hoverable != null;
    }
}