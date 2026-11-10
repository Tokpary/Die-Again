using UnityEngine;

namespace Code.Scripts.Dice.DiceMaterials
{
    [CreateAssetMenu(fileName = "Antimatter", menuName = "ScriptableObjects/DiceMaterials/Antimatter")]
    public class AntimatterMaterial : DiceMaterial
    {
        [SerializeField] private Material _visualMaterial;

        public override void ApplyVisual(Dice dice)
        {
            var renderer = dice.GetComponent<MeshRenderer>();
            var filter = dice.GetComponent<MeshFilter>();
            if (_visualMaterial == null || renderer == null || filter == null || filter.sharedMesh == null)
            {
                Debug.LogWarning("Antimatter requires a visual material and a dice mesh.", dice);
                return;
            }

            var edges = dice.GetComponent<AntimatterEdges>();
            if (edges == null) edges = dice.gameObject.AddComponent<AntimatterEdges>();
            edges.Prepare(filter);

            // Replace only the spawned renderer's slots, never the prefab/material assets.
            var materials = renderer.sharedMaterials;
            for (int i = 0; i < materials.Length; i++) materials[i] = _visualMaterial;
            renderer.sharedMaterials = materials;
            var block = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(block); // Preserve D4 values supplied by its existing component.
            block.SetFloat("_NumberMode", dice.GetComponent<D4FaceShaderData>() != null ? 2 :
                dice.Data.FaceValues.Length == 6 ? 1 : 0);
            Bounds bounds = filter.sharedMesh.bounds;
            block.SetVector("_PatternCenter", bounds.center);
            block.SetFloat("_PatternInvSize", 1f / Mathf.Max(0.0001f,
                Mathf.Max(bounds.size.x, Mathf.Max(bounds.size.y, bounds.size.z))));
            renderer.SetPropertyBlock(block);
        }
    }
}
