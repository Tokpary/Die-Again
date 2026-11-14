using Code.Scripts.Camera;

namespace Code.Scripts.UI
{
    public interface IHoverable
    {
        bool CanHover(CameraShotId view);

        object TooltipData { get; }

        void OnHoverEnter();
        void OnHoverExit();
    }
}