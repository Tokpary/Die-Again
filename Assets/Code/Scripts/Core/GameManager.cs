using System;
using Code.Scripts.Ambient;
using Code.Scripts.Camera;
using Code.Scripts.CursedObject;
using Code.Scripts.Dice;
using Code.Scripts.States;
using Code.Scripts.States.State;
using Code.Scripts.UI;
using Code.Scripts.Roulette;
using Code.Scripts.Shop;
using Code.Scripts.Table;
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
        [SerializeField] private CameraController _cameraController;
        [SerializeField] private DicePlayArea _dicePlayArea;
        [SerializeField] private PurchaseEffect _purchaseEffect;
        [SerializeField] private CameraViewInput _cameraViewInput;
        [SerializeField] private DiceDrawer _bankChest;
        [SerializeField] private CursedShelf _cursedShelf;
        [SerializeField] private HoverController _hoverController;[SerializeField] private TableLight _tableLight;

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
            if(_cameraController != null) _cameraController.Init(_gameInputs);
            if (_cameraViewInput != null) _cameraViewInput.Init(_gameInputs, _diceDragController, _diceThrower, _cameraController);
            if (_hoverController != null) _hoverController.Init(_gameInputs, _cameraController, _diceDragController);
            _gameInputs.Gameplay.Disable();
            _runData = new RunData(_gameConfig);
            _gameContext = new GameContext(_uiManager, _gameInputs, _diceThrower, _rouletteManager, _runData, _gameConfig, _diceDrawer, _diceDragController, _diceAnimator, _cameraController, _dicePlayArea, _purchaseEffect);
            _gameContext.HoverController = _hoverController;
            _gameContext.CameraViewInput = _cameraViewInput;
            _gameContext.TableLight = _tableLight;
            _gameContext.BankChest = _bankChest;
            _gameContext.CursedShelf = _cursedShelf;
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
