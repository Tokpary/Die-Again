using UnityEngine;

namespace Code.Scripts.Dice
{
    public interface IDiceDropTarget
    {
        bool ContainsScreenPoint(Vector2 screenPoint);
        void SetHover(bool hover);
    }
}