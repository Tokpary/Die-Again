
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Code.Scripts.UI
{
    public class MainMenuSettingsPanel : MonoBehaviour
    {
        [Header("Panel Movement")]
        [SerializeField] private Transform menuOptions;
        [SerializeField] private Transform hidePoint;
        [SerializeField] private Transform spawnPoint;
        [SerializeField] private GameObject settingsContent;
        [SerializeField] private GameObject creditsContent;
        [SerializeField] private float movementDuration = 0.8f;

        [Header("Settings Gears Animation")]
        [SerializeField] private Animator gearsAnimator;
        [SerializeField]
        private string gearsAnimationState =
            "Base Layer.Engranajes";

        private Vector3 hiddenPosition;
        private Vector3 shownPosition;
        private bool isVisible;
        private Coroutine movement;

        public MainMenuOptionType? CurrentOption { get; private set; }

        private void Awake()
        {
            if (menuOptions == null)
                menuOptions = transform;

            if (hidePoint == null || spawnPoint == null)
            {
                Debug.LogError(
                    "[MainMenuSettingsPanel] Asigna Hide Point y Spawn Point.",
                    this
                );
                enabled = false;
                return;
            }

            hiddenPosition = hidePoint.position;
            shownPosition = new Vector3(
                hiddenPosition.x,
                spawnPoint.position.y,
                hiddenPosition.z
            );

            menuOptions.position = hiddenPosition;

            SetContentActive(settingsContent, false);
            SetContentActive(creditsContent, false);
        }

        private void Update()
        {
            if (isVisible &&
                Keyboard.current != null &&
                Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                Hide();
            }
        }

        public void ShowFor(MainMenuOptionType option)
        {
            if (!enabled ||
                (option != MainMenuOptionType.Settings &&
                 option != MainMenuOptionType.Credits))
                return;

            // Reproducir los engranajes al abrir Settings
            // o al cambiar desde Credits a Settings.
            if (option == MainMenuOptionType.Settings &&
                (!isVisible || CurrentOption != option))
            {
                PlayGearsAnimation();
            }

            CurrentOption = option;

            SetContentActive(
                settingsContent,
                option == MainMenuOptionType.Settings
            );

            SetContentActive(
                creditsContent,
                option == MainMenuOptionType.Credits
            );

            if (!isVisible)
                SetVisible(true);
        }

        public void Hide()
        {
            if (!enabled || !isVisible)
                return;

            // Reproducir la animacion al cerrar Settings.
            if (CurrentOption == MainMenuOptionType.Settings)
                PlayGearsAnimation();

            SetVisible(false);
        }

        private void PlayGearsAnimation()
        {
            if (gearsAnimator == null)
            {
                Debug.LogWarning(
                    "[MainMenuSettingsPanel] Falta asignar Gears Animator.",
                    this
                );
                return;
            }

            if (string.IsNullOrEmpty(gearsAnimationState))
            {
                Debug.LogWarning(
                    "[MainMenuSettingsPanel] Falta el nombre del estado de animacion.",
                    this
                );
                return;
            }

            // Iniciar siempre la animacion desde el primer frame.
            gearsAnimator.Play(gearsAnimationState, 0, 0f);

            Debug.Log(
                "[MainMenuSettingsPanel] Reproduciendo animacion: " +
                gearsAnimationState
            );
        }

        private void SetVisible(bool visible)
        {
            isVisible = visible;

            if (movement != null)
                StopCoroutine(movement);

            Vector3 destination = visible
                ? shownPosition
                : hiddenPosition;

            movement = StartCoroutine(MoveTo(destination));
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

                menuOptions.position = Vector3.Lerp(
                    start,
                    destination,
                    t
                );

                yield return null;
            }

            menuOptions.position = destination;
            movement = null;

            if (!isVisible)
            {
                CurrentOption = null;

                SetContentActive(settingsContent, false);
                SetContentActive(creditsContent, false);
            }
        }

        private static void SetContentActive(
            GameObject content,
            bool active)
        {
            if (content != null)
                content.SetActive(active);
        }
    }
}
