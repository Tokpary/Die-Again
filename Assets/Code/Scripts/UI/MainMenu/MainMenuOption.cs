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
        [SerializeField] private MainMenuOptionType optionType;

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
                Mouse.current.position.ReadValue());

            if (!Physics.Raycast(ray, out RaycastHit hit))
                return;

            MainMenuOption opcionPulsada =
                hit.collider.GetComponentInParent<MainMenuOption>();

            if (opcionPulsada != this)
                return;

            Debug.Log($"[MainMenu] Se ha pulsado: {optionType}");
            OptionClicked?.Invoke(optionType);
        }
    }
}
