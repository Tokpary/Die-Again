using UnityEngine.UI;

namespace Code.Scripts.UI.Screens
{
    public class CursedRewardScreen : RewardScreen<CursedObject.CursedObject>
    {
        protected override void FillCard(Button card, CursedObject.CursedObject item) =>
            card.GetComponentInChildren<CursedObjectInfoView>(true).Show(item);
    }
}