using Code.Scripts.Core;
using Code.Scripts.UI;

namespace Code.Scripts.States
{
    public static class RunSetup
    {
        public static void StartNewRun(GameContext context)
        {
            context.RunData = new RunData(context.GameConfig);
            WeekDayBanner.Refresh(context.RunData.Progress);
            context.DiceDrawer.Clear();
            context.DiceDrawer.Populate(context.RunData.DiceCase);
            context.BankChest.Clear();
            context.BankChest.Populate(context.RunData.Bank);
            context.CursedShelf.Populate(context.RunData.CursedObject);
        }
    }
}