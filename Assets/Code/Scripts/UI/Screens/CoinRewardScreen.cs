using Code.Scripts.Dice;
using Code.Scripts.UI;
using Code.Scripts.UI.Screens;
using UnityEngine.UI;

public class CoinRewardScreen : RewardScreen<DiceInstance>
{
    protected override void FillCard(Button card, DiceInstance dice) =>
        card.GetComponentInChildren<DiceInfoView>(true).Show(dice);
}