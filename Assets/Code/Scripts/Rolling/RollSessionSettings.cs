using Code.Scripts.Camera;
using Code.Scripts.Core;

namespace Code.Scripts.Rolls
{
    public class RollSessionSettings
    {
        public int Shots;
        public int Rerolls;
        public CameraShotId ChestShot = CameraShotId.Chest;
        public CameraShotId RestShot = CameraShotId.Gameplay;
        public CameraShotId AimShot = CameraShotId.Throwing;
        public CameraShotId DrawerShot = CameraShotId.Drawer;
        public CameraShotId[] Views = { CameraShotId.Drawer, CameraShotId.Throwing, CameraShotId.CursedShelf };
        public float ResultViewDelay = 1f;
        public float EliminateDuration = 0.3f; 
        public CameraShotId[] HoverViews = { CameraShotId.Drawer, CameraShotId.Chest, CameraShotId.Throwing };
    }
}