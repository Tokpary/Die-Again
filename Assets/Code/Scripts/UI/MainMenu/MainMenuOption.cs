using System;
using UnityEngine;

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

        private void OnMouseDown()
        {
            if (!enabled)
                return;

            Debug.Log($"[MainMenu] Se ha pulsado: {optionType}");
            OptionClicked?.Invoke(optionType);
        }
    }
}
