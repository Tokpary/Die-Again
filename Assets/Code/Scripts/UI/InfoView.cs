using UnityEngine;

namespace Code.Scripts.UI
{
    public abstract class InfoView : MonoBehaviour
    {
        public abstract bool CanShow(object data);
        public abstract void ShowData(object data);
    }

    public abstract class InfoView<T> : InfoView where T : class
    {
        public override bool CanShow(object data) => data is T;
        public override void ShowData(object data) => Show((T)data);
        public abstract void Show(T data);
    }
}