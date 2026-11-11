using Code.Scripts.Camera;
using Code.Scripts.States;
using Code.Scripts.UI;
using Code.Scripts.Core;
using Code.Scripts.Dice;
using Code.Scripts.Roulette;
using Code.Scripts.Shop;
using Code.Scripts.Table;

namespace Code.Scripts
{
    public class GameContext 
    {
        public UIManager UIManager { get; private set; }
        public GameInputs GameInputs { get; private set; }
        public DiceThrower DiceThrower { get; private set; }
        public RouletteManager RouletteManager { get; private set; }
        public GameConfigSO GameConfig { get; private set; }
        public RunData RunData { get; set; }
        public DiceDrawer DiceDrawer { get; private set; }
        public DiceDragController DiceDragController { get; set; }
        public DiceAnimator DiceAnimator { get; private set; }
        public CameraController CameraController { get; set; }
        public DicePlayArea PlayArea { get; set; }
        public PurchaseEffect PurchaseEffect { get; set; }
        public CameraViewInput CameraViewInput { get; set; }
        public DiceDrawer BankChest { get; set; }
        public GameContext(UIManager uiManager, GameInputs gameInputs, DiceThrower diceThrower, RouletteManager rouletteManager, 
            RunData runData, GameConfigSO gameConfig, DiceDrawer diceDrawer, DiceDragController diceDragController = null,
            DiceAnimator diceAnimator = null, CameraController cameraController = null, DicePlayArea dicePlayArea = null, PurchaseEffect purchaseEffect = null)
        {
            UIManager = uiManager;
            GameInputs = gameInputs;
            DiceThrower = diceThrower;
            RouletteManager = rouletteManager;
            RunData = runData;
            DiceDrawer = diceDrawer;
            GameConfig = gameConfig;
            DiceDragController = diceDragController;
            DiceAnimator = diceAnimator;
            PlayArea = dicePlayArea;
            CameraController = cameraController;
            PurchaseEffect = purchaseEffect;
        }

    }
}
