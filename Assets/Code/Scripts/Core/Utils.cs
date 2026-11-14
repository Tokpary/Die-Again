using UnityEngine;

namespace Code.Scripts.Utils
{
    public static class Utils
    {
        public static void SetLayer(GameObject obj, int layer)
        {
            obj.layer = layer;
            foreach (Transform child in obj.GetComponentsInChildren<Transform>(true))
            {
                child.gameObject.layer = layer;
            }
        }
    }
}