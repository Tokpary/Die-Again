using System.Collections;
using Code.Scripts.Core;
using Code.Scripts.States.State;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Code.Scripts.UI
{
    public class InGameSettingsCard : MonoBehaviour
    {
        private static Transform _activeMenuOptions;

        public static bool BlocksGameplayInput { get; private set; }

        [Header("Button")]
        [SerializeField] private MainMenuOption settingsButton;

        [Header("Menu Movement")]
        [SerializeField] private Transform menuOptions;
        [SerializeField] private Transform hidePoint;
        [SerializeField] private Transform showPoint;
        [SerializeField] private float movementDuration = 0.8f;

        private GameManager _gameManager;
        private Vector3 _hiddenPosition;
        private Vector3 _shownPosition;
        private bool _isVisible;
        private Coroutine _movement;

        private void Awake()
        {
            if (settingsButton == null)
                settingsButton = GetComponent<MainMenuOption>();

            if (settingsButton == null ||
                menuOptions == null ||
                hidePoint == null ||
                showPoint == null)
            {
                Debug.LogError(
                    "[InGameSettingsCard] Asigna Button, Menu Options, Hide Point y Show Point en el Inspector.",
                    this
                );
                enabled = false;
                return;
            }

            _hiddenPosition = hidePoint.position;
            _shownPosition = new Vector3(
                _hiddenPosition.x,
                showPoint.position.y,
                _hiddenPosition.z
            );

            menuOptions.position = _hiddenPosition;
            menuOptions.gameObject.SetActive(false);
        }

        private void OnEnable()
        {
            if (settingsButton != null)
                settingsButton.OptionClicked += OnOptionClicked;
        }

        private void OnDisable()
        {
            if (settingsButton != null)
                settingsButton.OptionClicked -= OnOptionClicked;

            if (_movement != null)
            {
                StopCoroutine(_movement);
                _movement = null;
            }

            if (_activeMenuOptions == menuOptions)
            {
                BlocksGameplayInput = false;
                _activeMenuOptions = null;
            }

            _isVisible = false;
            if (menuOptions != null)
                menuOptions.gameObject.SetActive(false);
        }

        public static bool AllowsWorldInteraction(Transform target)
        {
            return !BlocksGameplayInput ||
                   (_activeMenuOptions != null &&
                    target != null &&
                    (target == _activeMenuOptions || target.IsChildOf(_activeMenuOptions)));
        }

        private void Update()
        {
            if (!_isVisible)
                return;

            if (!IsGameplayOrShop() ||
                (Keyboard.current != null &&
                 Keyboard.current.escapeKey.wasPressedThisFrame))
            {
                Hide();
            }
        }

        public void Hide()
        {
            if (!enabled || !_isVisible)
                return;

            SetVisible(false);
        }

        private void OnOptionClicked(MainMenuOptionType option)
        {
            if (option != MainMenuOptionType.Settings || _isVisible)
                return;

            if (!IsGameplayOrShop())
            {
                string stateName = _gameManager == null
                    ? "sin GameManager"
                    : _gameManager.CurrentState?.GetType().Name ?? "sin estado";
                Debug.Log(
                    $"[InGameSettingsCard] No se abre en el estado {stateName}.",
                    this
                );
                return;
            }

            menuOptions.gameObject.SetActive(true);
            SetVisible(true);
            Debug.Log("[InGameSettingsCard] Abriendo Menu Opciones_juego.", this);
        }

        private bool IsGameplayOrShop()
        {
            if (_gameManager == null)
                _gameManager = FindFirstObjectByType<GameManager>();

            if (_gameManager == null)
            {
                Debug.LogError(
                    "[InGameSettingsCard] No se encontro GameManager.",
                    this
                );
                return false;
            }

            return _gameManager.CurrentState is GameplayState ||
                   _gameManager.CurrentState is ShopState;
        }

        private void SetVisible(bool visible)
        {
            _isVisible = visible;

            if (visible)
            {
                _activeMenuOptions = menuOptions;
                BlocksGameplayInput = true;
            }

            if (_movement != null)
                StopCoroutine(_movement);

            Vector3 destination = visible
                ? _shownPosition
                : _hiddenPosition;

            _movement = StartCoroutine(MoveTo(destination));
        }

        private IEnumerator MoveTo(Vector3 destination)
        {
            Vector3 start = menuOptions.position;
            float elapsed = 0f;

            while (elapsed < movementDuration)
            {
                elapsed += Time.deltaTime;

                float t = movementDuration <= 0f
                    ? 1f
                    : Mathf.Clamp01(elapsed / movementDuration);

                t = Mathf.SmoothStep(0f, 1f, t);
                menuOptions.position = Vector3.Lerp(start, destination, t);

                yield return null;
            }

            menuOptions.position = destination;
            _movement = null;

            if (!_isVisible)
            {
                menuOptions.gameObject.SetActive(false);
                if (_activeMenuOptions == menuOptions)
                {
                    BlocksGameplayInput = false;
                    _activeMenuOptions = null;
                }
            }
        }
    }
}
