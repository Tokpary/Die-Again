using System.Collections;
using Code.Scripts.Core;
using Code.Scripts.States.State;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Code.Scripts.UI
{
    public class InGameSettingsCard : MonoBehaviour
    {
        private static Transform _activeMenuOptions;

        public static bool BlocksGameplayInput { get; private set; }

        [Header("Button")]
        [SerializeField] private MainMenuOption settingsButton;
        [SerializeField] private MainMenuOption giveUpButton;
        private Button _giveUpUiButton;

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

            if (giveUpButton != null)
                _giveUpUiButton = giveUpButton.GetComponentInChildren<Button>(true);

            if (giveUpButton == null)
                Debug.LogError(
                    "[InGameSettingsCard] Asigna Give Up Button en el Inspector.",
                    this
                );
            else if (_giveUpUiButton == null)
                Debug.LogWarning(
                    "[InGameSettingsCard] Give Up Button no tiene un componente Button; se usara el clic 3D de MainMenuOption.",
                    giveUpButton
                );

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
            if (_giveUpUiButton != null)
                _giveUpUiButton.onClick.AddListener(OnGiveUpButtonClicked);
            else if (giveUpButton != null)
                giveUpButton.OptionClicked += OnGiveUpOptionClicked;
        }

        private void OnDisable()
        {
            if (settingsButton != null)
                settingsButton.OptionClicked -= OnOptionClicked;
            if (_giveUpUiButton != null)
                _giveUpUiButton.onClick.RemoveListener(OnGiveUpButtonClicked);
            else if (giveUpButton != null)
                giveUpButton.OptionClicked -= OnGiveUpOptionClicked;

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

        private void OnGiveUpOptionClicked(MainMenuOptionType option)
        {
            if (option != MainMenuOptionType.Quit)
                return;

            OnGiveUpButtonClicked();
        }

        private void OnGiveUpButtonClicked()
        {
            if (!IsGameplayOrShop())
                return;

            if (_gameManager == null)
                return;

            _gameManager.GiveUpAndReturnToMainMenu();
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
