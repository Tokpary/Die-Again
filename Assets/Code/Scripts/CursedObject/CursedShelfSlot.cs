using Code.Scripts.Camera;
using Code.Scripts.UI;
using UnityEngine;

namespace Code.Scripts.CursedObject
{
    public class CursedShelfSlot : MonoBehaviour, IHoverable
    {
        private CursedShelf _shelf;
        private int _index;

        public void Init(CursedShelf shelf, int index)
        {
            _shelf = shelf;
            _index = index;
        }

        public object TooltipData => _shelf.GetItem(_index);

        public bool CanHover(CameraShotId view) => _shelf.IsHoverView(view) && _shelf.GetItem(_index) != null;

        public void OnHoverEnter() => _shelf.SetFocused(_index, true);
        public void OnHoverExit() => _shelf.SetFocused(_index, false);
    }
}