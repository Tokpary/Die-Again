using System;
using UnityEngine;
using UnityEngine.UI;

namespace Code.Scripts.UI.Screens
{
    public class MainMenuScreen : UIScreen
    {
        [SerializeField] private Button _startButton;
        [SerializeField] private MainMenuOption[] _menuOptions;
        [SerializeField] private MainMenuSettingsPanel _settingsPanel;
        [SerializeField] private AudioClip _menuMusic;

        public event Action<MainMenuOptionType> MenuOptionClicked;
        public event Action PlayButtonClicked;

        public override void Show()
        {
            base.Show();

            if (_menuMusic == null)
            {
                Debug.LogError("[MainMenu] Asigna la música del menú en MainMenuScreen.", this);
                return;
            }

            if (AudioManager.Instance == null)
            {
                Debug.LogError("[MainMenu] No hay un AudioManager activo en la escena.", this);
                return;
            }

            AudioManager.Instance.PlayMusic(_menuMusic);
        }

        public void ShowOptionsPanel(MainMenuOptionType optionType)
        {
            if (_settingsPanel == null)
            {
                Debug.LogError("[MainMenu] Asigna el panel en MainMenuScreen.", this);
                return;
            }

            _settingsPanel.ShowFor(optionType);
        }

        protected override void Awake()
        {
            base.Awake();

            if (_startButton != null)
                _startButton.onClick.AddListener(() => PlayButtonClicked?.Invoke());

            if (_menuOptions != null)
            {
                foreach (var option in _menuOptions)
                {
                    if (option == null)
                        continue;

                    option.OptionClicked += HandleOptionClicked;
                }
            }
        }

        private void OnDestroy()
        {
            if (_menuOptions != null)
            {
                foreach (var option in _menuOptions)
                {
                    if (option == null)
                        continue;

                    option.OptionClicked -= HandleOptionClicked;
                }
            }
        }

        private void HandleOptionClicked(MainMenuOptionType optionType)
        {
            MenuOptionClicked?.Invoke(optionType);

            // El botón 3D Jugar reutiliza el mismo evento que el botón UI 2D.
            if (optionType == MainMenuOptionType.Play)
                PlayButtonClicked?.Invoke();
        }
    }
}
