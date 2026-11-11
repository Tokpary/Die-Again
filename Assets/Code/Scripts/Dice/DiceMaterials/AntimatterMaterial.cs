
using UnityEngine;

namespace Code.Scripts.Dice.DiceMaterials
{
    [CreateAssetMenu(
        fileName = "Antimatter",
        menuName = "ScriptableObjects/DiceMaterials/Antimatter"
    )]
    public class AntimatterMaterial : DiceMaterial
    {
        [SerializeField]
        private Material _visualMaterial;

        public override void ApplyVisual(Dice dice)
        {
            var renderer = dice.GetComponent<MeshRenderer>();
            var filter = dice.GetComponent<MeshFilter>();

            if (_visualMaterial == null ||
                renderer == null ||
                filter == null ||
                filter.sharedMesh == null)
            {
                Debug.LogWarning(
                    "Antimatter requires a visual material and a dice mesh.",
                    dice
                );
                return;
            }

            // Preserve existing render-only edge data.
            var edges = dice.GetComponent<AntimatterEdges>();

            if (edges == null)
            {
                edges = dice.gameObject.AddComponent<AntimatterEdges>();
            }

            edges.Prepare(filter);

            // Replace materials only on this die instance.
            var materials = renderer.sharedMaterials;

            for (int i = 0; i < materials.Length; i++)
            {
                materials[i] = _visualMaterial;
            }

            renderer.sharedMaterials = materials;

            // Preserve any existing property block,
            // including D4 values.
            var block = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(block);

            int faceCount = dice.Data.FaceValues.Length;

            // Automatically select number rendering.
            int numberMode =
                dice.GetComponent<D4FaceShaderData>() != null
                    ? 2
                    : faceCount == 6
                        ? 1
                        : 0;

            block.SetFloat(
                "_NumberMode",
                numberMode
            );

            // Enable special waves only on D2.
            bool isD2 = faceCount == 2;

            block.SetFloat(
                "_D2Pattern",
                isD2 ? 1f : 0f
            );

            // --------------------------------------
            // MESH DIMENSIONS
            // --------------------------------------

            Bounds bounds = filter.sharedMesh.bounds;

            float largestDimension = Mathf.Max(
                bounds.size.x,
                Mathf.Max(bounds.size.y, bounds.size.z)
            );

            float invSize = 1f / Mathf.Max(
                0.0001f,
                largestDimension
            );

            block.SetVector(
                "_PatternCenter",
                bounds.center
            );

            block.SetFloat(
                "_PatternInvSize",
                invSize
            );

            // --------------------------------------
            // D2 PERIMETER DATA
            // --------------------------------------

            // Normalized half dimensions of the mesh.
            Vector3 halfSize = bounds.extents * invSize;

            block.SetVector(
                "_D2HalfSize",
                new Vector4(
                    Mathf.Max(halfSize.x, 0.0001f),
                    Mathf.Max(halfSize.y, 0.0001f),
                    Mathf.Max(halfSize.z, 0.0001f),
                    0f
                )
            );

            // Find the thinnest local axis.
            // This should be perpendicular to the coin.
            Vector3 extents = bounds.extents;

            int flatAxis;

            if (extents.x <= extents.y &&
                extents.x <= extents.z)
            {
                flatAxis = 0;
            }
            else if (extents.y <= extents.z)
            {
                flatAxis = 1;
            }
            else
            {
                flatAxis = 2;
            }

            block.SetFloat(
                "_D2FlatAxis",
                flatAxis
            );

            renderer.SetPropertyBlock(block);

            Debug.Log(
                $"[Antimatter] {dice.name} | " +
                $"Faces: {faceCount} | " +
                $"D2: {isD2} | " +
                $"Flat Axis: {flatAxis} | " +
                $"Number Mode: {numberMode}",
                dice
            );
        }
    }
}
