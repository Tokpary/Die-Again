using Code.Scripts.Ambient;
using Code.Scripts.Core;
using Code.Scripts.UI;
using Code.Scripts.UI.Screens;

namespace Code.Scripts.States
{
    public static class RunSetup
    {
        public static void StartNewRun(GameContext context)
        {
            context.RunData = new RunData(context.GameConfig);
            context.UIManager.Get<GameplayScreen>().ShowBoss(null);
            WeekDayBanner.Refresh(context.RunData.Progress);
            context.DiceDrawer.Clear();
            context.SetTableMood(TableMood.Normal);
            context.DiceDrawer.Populate(context.RunData.DiceCase);
            context.BankChest.Clear();
            context.BankChest.Populate(context.RunData.Bank);
            context.CursedShelf.Populate(context.RunData.CursedObject);
        }
    }
}