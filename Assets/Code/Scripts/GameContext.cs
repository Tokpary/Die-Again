using Code.Scripts.States;
using Code.Scripts.UI;
using Code.Scripts.Core;
using Code.Scripts.Roulette;

namespace Code.Scripts
{
    public class GameContext 
    {
        public IStateMachine StateMachine { get; private set; }
        public UIManager UIManager { get; private set; }
        public GameInputs GameInputs { get; private set; }
        public DiceThrower DiceThrower { get; private set; }
        public GameProgress Progress { get; private set; }
        public RouletteSO RouletteSettings { get; private set; }
        public RouletteView RouletteView { get; private set; }

        public GameContext(UIManager uiManager, GameInputs gameInputs, DiceThrower diceThrower,
            GameProgress progress, RouletteSO rouletteSettings, RouletteView rouletteView)
        {
            UIManager = uiManager;
            GameInputs = gameInputs;
            DiceThrower = diceThrower;
            Progress = progress;
            RouletteSettings = rouletteSettings;
            RouletteView = rouletteView;
        }

    }
}
