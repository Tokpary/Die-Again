
using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Code.Scripts.UI
{
    public enum MainMenuOptionType
    {
        Play,
        Quit,
        Settings,
        Credits
    }

    public class MainMenuOption : MonoBehaviour
    {
        private const string CreditsOpenAnimationState = "Base Layer.Open";

        [SerializeField] private MainMenuOptionType optionType;
        [SerializeField] private Animator creditsAnimator;

        public event Action<MainMenuOptionType> OptionClicked;

        public MainMenuOptionType OptionType => optionType;

        private void Update()
        {
            if (!enabled || Mouse.current == null ||
                !Mouse.current.leftButton.wasPressedThisFrame)
                return;

            UnityEngine.Camera cameraToUse = UnityEngine.Camera.main;

            if (cameraToUse == null)
                return;

            Ray ray = cameraToUse.ScreenPointToRay(
                Mouse.current.position.ReadValue()
            );

            if (!Physics.Raycast(ray, out RaycastHit hit))
                return;

            MainMenuOption opcionPulsada =
                hit.collider.GetComponentInParent<MainMenuOption>();

            if (opcionPulsada != this)
                return;

            Debug.Log($"[MainMenu] Se ha pulsado: {optionType}");

            if (optionType == MainMenuOptionType.Credits)
                PlayCreditsAnimation();

            OptionClicked?.Invoke(optionType);
        }

        private void PlayCreditsAnimation()
        {
            if (creditsAnimator == null)
            {
                Debug.LogWarning(
                    "[MainMenu] Asigna el Animator de Credits desde el Inspector.",
                    this
                );
                return;
            }

            if (creditsAnimator.runtimeAnimatorController == null)
            {
                Debug.LogWarning(
                    "[MainMenu] El Animator de Credits no tiene un controller asignado.",
                    creditsAnimator
                );
                return;
            }

            creditsAnimator.Play(CreditsOpenAnimationState, 0, 0f);
        }
    }
}
