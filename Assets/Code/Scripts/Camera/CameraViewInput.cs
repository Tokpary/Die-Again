using System;
using System.Collections.Generic;
using Code.Scripts.Core;
using Code.Scripts.Dice;
using Code.Scripts.Table;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace Code.Scripts.Camera
{
    [DefaultExecutionOrder(-5)]
    public class CameraViewInput : MonoBehaviour
    {

        [SerializeField] private UnityEngine.Camera _camera;
        [SerializeField] private LayerMask _zoneLayerMask;
        [SerializeField] private CameraZone[] _zones;
        
        [Header("Acciones de mando")]
        [SerializeField] private string _navigateActionName = "NavegarVista";
        [SerializeField] private string _confirmAction = "ConfirmarVista";
        [SerializeField] private string _backAction = "Volver";
        
        private readonly HashSet<CameraShotId> _available = new HashSet<CameraShotId>();
        
        private GameInputs _input;
        private DiceDragController _drag;
        private DiceThrower _thrower;
        private CameraController _cameraController;
        private InputAction _navigate;
        private InputAction _confirm;
        private InputAction _back;

        private CameraZone _padSelection;   
        private CameraZone _highlighted;
        private Vector2 _lastPointer;

        public event Action<CameraShotId> ViewRequested;
        public event Action BackRequested;
        public void Init(GameInputs input, DiceDragController drag, DiceThrower thrower, CameraController cameraController)
        {
            _input = input;
            _drag = drag;
            _thrower = thrower;
            _cameraController = cameraController;
            _navigate = FindAction(_navigateActionName);
            _confirm = FindAction(_confirmAction);
            _back = FindAction(_backAction);
        }

        public void SetAvailableViews(IEnumerable<CameraShotId> views)
        {
            _available.Clear();
            if (views != null)
                foreach (CameraShotId view in views) _available.Add(view);
            _padSelection = null;
            SetHighlight(null);
        }

        private void Update()
        {
            if (_input == null || !_input.Gameplay.enabled)
            {
                SetHighlight(null);
                return;
            }

            Vector2 pointer = _input.Gameplay.Apuntar.ReadValue<Vector2>();
            if ((pointer - _lastPointer).sqrMagnitude > 1f) _padSelection = null;
            _lastPointer = pointer;

            if (ReadGamepad()) return;

            CameraZone hovered = _padSelection == null ? GetHoveredZone(pointer) : null;
            SetHighlight(_padSelection != null ? _padSelection : hovered);

            if (_back != null && _back.WasPressedThisFrame())
            {
                BackRequested?.Invoke();
                return;
            }

            if (_available.Count == 0)
            {
                SetHighlight(null);
                return;
            }

            if (hovered != null && _input.Gameplay.Disparo.WasPressedThisFrame() && !_drag.PressConsumed)
                Request(hovered);
        }


        private CameraZone GetHoveredZone(Vector2 pointer)
        {
            if (_drag.IsDragging || _drag.SelectionMode || _thrower.HasLoadedDice) return null;
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return null;

            Ray ray = _camera.ScreenPointToRay(pointer);
            if (!Physics.Raycast(ray, out RaycastHit hit, 500f, _zoneLayerMask, QueryTriggerInteraction.Collide)) return null;

            CameraZone zone = hit.collider.GetComponentInParent<CameraZone>();
            if (zone == null || !IsAvailable(zone) || zone.View == _cameraController.CurrentShotId) return null;
            return zone;
        }

        private bool ReadGamepad()
        {
            if (_confirm != null && _confirm.WasPressedThisFrame() && _padSelection != null)
            {
                Request(_padSelection);
                return true;
            }

            if (_navigate == null || !_navigate.WasPressedThisFrame()) return false;

            Vector2 direction = _navigate.ReadValue<Vector2>();
            CameraZone from = _padSelection != null ? _padSelection : ZoneFor(_cameraController.CurrentShotId);
            if (from == null)
            {
                _padSelection = FirstAvailableZone();
                return false;
            }

            CameraZone next = from.GetNeighbor(direction);
            for (int guard = 0; next != null && !IsAvailable(next) && guard < _zones.Length; guard++)
                next = next.GetNeighbor(direction);

            if (next != null) _padSelection = next;
            return false;
        }


        private void Request(CameraZone zone)
        {
            _padSelection = null;
            SetHighlight(null);
            ViewRequested?.Invoke(zone.View);
        }

        private void SetHighlight(CameraZone zone)
        {
            if (_highlighted == zone) return;
            if (_highlighted != null) _highlighted.SetHighlighted(false);
            _highlighted = zone;
            if (_highlighted != null) _highlighted.SetHighlighted(true);
        }

        private bool IsAvailable(CameraZone zone) => _available.Contains(zone.View);

        private CameraZone ZoneFor(CameraShotId view)
        {
            foreach (CameraZone zone in _zones)
                if (zone != null && zone.View == view) return zone;
            return null;
        }

        private CameraZone FirstAvailableZone()
        {
            foreach (CameraZone zone in _zones)
                if (zone != null && IsAvailable(zone)) return zone;
            return null;
        }

        private InputAction FindAction(string actionName)
        {
            InputAction action = _input.asset.FindAction("Gameplay/" + actionName);
            if (action == null) Debug.LogWarning($"[CameraViewInput] No existe la acción Gameplay/{actionName}.", this);
            return action;
        }
    }
}