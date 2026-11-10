using Code.Scripts.Core;
using Code.Scripts.UI;
using Code.Scripts.UI.Screens;
using UnityEngine;

namespace Code.Scripts.States.State
{
    public class MainMenuState : AState
    {
        private MainMenuScreen _screen;

        public MainMenuState(GameContext context, IStateMachine stateMachine) : base(context, stateMachine)
        {
        }

        public override void Enter()
        {
            base.Enter();
            Debug.Log("Entering Main Menu State");
            _screen = _context.UIManager.ShowScreen<MainMenuScreen>();
            _screen.PlayButtonClicked += OnPlayButtonClicked;
            _screen.MenuOptionClicked += OnMenuOptionClicked;
        }

        public override void Exit()
        {
            base.Exit();
            _screen.PlayButtonClicked -= OnPlayButtonClicked;
            _screen.MenuOptionClicked -= OnMenuOptionClicked;
        }

        private void OnPlayButtonClicked()
        {
            Debug.Log("[MainMenu] Botón Jugar pulsado");
            _screen.PlayButtonClicked -= OnPlayButtonClicked;
            _context.RunData = new RunData(_context.GameConfig); 
            _context.DiceDrawer.Clear();
            _context.DiceDrawer.Populate(_context.RunData.DiceCase);
            _stateMachine.ChangeState(new RouletteState(_context, _stateMachine));
        }

        private void OnMenuOptionClicked(MainMenuOptionType optionType)
        {
            Debug.Log($"[MainMenu] Opción clicada: {optionType}");

            switch (optionType)
            {
                case MainMenuOptionType.Play:
                    Debug.Log("[MainMenu] Ir a jugar");
                    break;
                case MainMenuOptionType.Quit:
                    Debug.Log("[MainMenu] Salir del juego");
#if UNITY_EDITOR
                    UnityEditor.EditorApplication.isPlaying = false;
#else
                    Application.Quit();
#endif
                    break;
                case MainMenuOptionType.Settings:
                    Debug.Log("[MainMenu] Ir a configuración");
                    _screen.ShowOptionsPanel(optionType);
                    break;
                case MainMenuOptionType.Credits:
                    Debug.Log("[MainMenu] Ir a créditos");
                    _screen.ShowOptionsPanel(optionType);
                    break;
            }
        }
    }
}
