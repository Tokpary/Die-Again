using System;
using Code.Scripts.States;
using Code.Scripts.States.State;
using Code.Scripts.UI;
using Code.Scripts.Roulette;
using UnityEngine;

namespace Code.Scripts.Core
{
    public class GameManager : MonoBehaviour
    {
        [SerializeField] UIManager _uiManager;
        [SerializeField] DiceThrower _diceThrower;
        [SerializeField] private RouletteSO _rouletteSettings;
        [SerializeField] private RouletteView _rouletteView;
        [Header("Initial progression for testing (set before Play)")]
        [SerializeField] private bool _startInRouletteForTesting;
        [SerializeField, Range(1, 3)] private int _initialWeek = 1;
        [SerializeField, Range(1, 4)] private int _initialDay = 1;
        
        private GameInputs _gameInputs;
        private GameContext _gameContext;
        private IStateMachine _stateMachine;

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
            _gameInputs = new GameInputs();
            if (_diceThrower != null) _diceThrower.Init(_gameInputs);
            _gameInputs.Gameplay.Disable();
            var progress = new GameProgress(_initialWeek, _initialDay);
            if (_rouletteSettings != null && _rouletteSettings.TryValidate(out _))
                progress.PrepareNextRound(_rouletteSettings.luckyNumberMinimum,
                    _rouletteSettings.luckyNumberMaximum);
            _gameContext = new GameContext(_uiManager, _gameInputs, _diceThrower,
                progress, _rouletteSettings, _rouletteView);
            if (_rouletteView != null) _rouletteView.Hide();
            _stateMachine = new GameStateMachine();
        }

        private void Start()
        {
            _stateMachine.ChangeState(_startInRouletteForTesting
                ? new RouletteState(_gameContext, _stateMachine)
                : new MainMenuState(_gameContext, _stateMachine));
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
