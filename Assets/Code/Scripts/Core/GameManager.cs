using System;
using Code.Scripts.States;
using Code.Scripts.States.State;
using Code.Scripts.UI;
using UnityEngine;

namespace Code.Scripts.Core
{
    public class GameManager : MonoBehaviour
    {
        [SerializeField] UIManager _uiManager;
        [SerializeField] DiceThrower _diceThrower;
        
        private GameInputs _gameInputs;
        private GameContext _gameContext;
        private IStateMachine _stateMachine;

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
            _gameInputs = new GameInputs();
            _diceThrower.Init(_gameInputs);
            _gameInputs.Gameplay.Disable();
            _gameContext = new GameContext(_uiManager, _gameInputs, _diceThrower);
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
            _gameInputs?.Dispose();
        }
    }
}