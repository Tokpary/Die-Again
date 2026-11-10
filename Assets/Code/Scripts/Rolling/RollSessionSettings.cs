using Code.Scripts.Core;

namespace Code.Scripts.Rolls
{
    public class RollSessionSettings
    {
        public int Shots;
        public int Rerolls;
        public CameraShotId RestShot = CameraShotId.Gameplay;
        public CameraShotId AimShot = CameraShotId.Throwing;
        public float ResultViewDelay = 1f;
    }
}