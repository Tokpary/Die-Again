using Code.Scripts.States;
using Code.Scripts.UI;

namespace Code.Scripts
{
    public class GameContext 
    {
        public IStateMachine StateMachine { get; private set; }
        public UIManager UIManager { get; private set; }
        public GameInputs GameInputs { get; private set; }
        public DiceThrower DiceThrower { get; private set; }
        public GameContext(UIManager uiManager, GameInputs gameInputs, DiceThrower diceThrower)
        {
            UIManager = uiManager;
            GameInputs = gameInputs;
            DiceThrower = diceThrower;
        }

    }
}