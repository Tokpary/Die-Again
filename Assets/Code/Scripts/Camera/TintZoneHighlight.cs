using UnityEngine;

namespace Code.Scripts.Camera
{
    public class TintZoneHighlight : ZoneHighlight
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        [SerializeField] private Color _highlightColor = new Color(1f, 0.85f, 0.4f);
        [SerializeField, Range(0f, 1f)] private float _strength = 0.5f;

        private Renderer[] _renderers;
        private MaterialPropertyBlock _block;

        private void Awake()
        {
            _renderers = GetComponentsInChildren<Renderer>();
            _block = new MaterialPropertyBlock();
        }

        public override void SetHighlighted(bool highlighted)
        {
            foreach (Renderer r in _renderers)
            {
                if (!highlighted)
                {
                    r.SetPropertyBlock(null);
                    continue;
                }

                Color baseColor = r.sharedMaterial != null && r.sharedMaterial.HasProperty(BaseColorId)
                    ? r.sharedMaterial.GetColor(BaseColorId)
                    : Color.white;
                _block.SetColor(BaseColorId, Color.Lerp(baseColor, _highlightColor, _strength));
                r.SetPropertyBlock(_block);
            }
        }
    }
}