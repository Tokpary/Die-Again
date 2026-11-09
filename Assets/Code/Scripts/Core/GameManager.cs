using System;
using Code.Scripts.Dice;
using Code.Scripts.States;
using Code.Scripts.States.State;
using Code.Scripts.UI;
using Code.Scripts.Roulette;
using UnityEngine;

namespace Code.Scripts.Core
{
    public class GameManager : MonoBehaviour
    {
        [SerializeField] private GameConfigSO _gameConfig;
        [SerializeField] UIManager _uiManager;
        [SerializeField] DiceThrower _diceThrower;
        [SerializeField] private RouletteManager _rouletteManager;
        [SerializeField] private DiceDrawer _diceDrawer;
        [SerializeField] private DiceDragController _diceDragController;
        [SerializeField] private DiceAnimator _diceAnimator;
        
        private RunData _runData;
        private GameInputs _gameInputs;
        private GameContext _gameContext;
        private IStateMachine _stateMachine;

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
            _gameInputs = new GameInputs();
            if (_diceThrower != null) _diceThrower.Init(_gameInputs, _diceDragController);
            if(_diceDragController != null) _diceDragController.Init(_gameInputs);
            _gameInputs.Gameplay.Disable();
            _runData = new RunData(_gameConfig);
            _gameContext = new GameContext(_uiManager, _gameInputs, _diceThrower, _rouletteManager, _runData, _gameConfig, _diceDrawer, _diceDragController, _diceAnimator);
            _stateMachine = new GameStateMachine();
        }

        private void Start()
        {
            _stateMachine.ChangeState(new MainMenuState(_gameContext, _stateMachine));
        }

        private void Update()
        {
            _stateMachine.CurrentState?.Update();
        }

        private void OnDestroy()
        {
            _stateMachine?.CurrentState?.Exit();
            _gameInputs?.Dispose();
        }
    }
}
