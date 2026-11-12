
using UnityEngine;

namespace Code.Scripts.Dice.DiceMaterials
{
    [CreateAssetMenu(
        fileName = "Crystal",
        menuName = "ScriptableObjects/DiceMaterials/Crystal"
    )]
    public class CrystalMaterial : DiceMaterial
    {
        [Header("Crystal Visual Material")]
        [SerializeField]
        private Material _visualMaterial;

        [Header("Debug")]
        [SerializeField]
        private bool _logOnApply = false;

        public override void ApplyVisual(Dice dice)
        {
            // --------------------------------------
            // VALIDATION
            // --------------------------------------

            if (dice == null || dice.Data == null)
            {
                Debug.LogWarning(
                    "[Crystal] Invalid dice or dice data."
                );
                return;
            }

            MeshRenderer renderer =
                dice.GetComponent<MeshRenderer>();

            MeshFilter filter =
                dice.GetComponent<MeshFilter>();

            if (_visualMaterial == null ||
                renderer == null ||
                filter == null ||
                filter.sharedMesh == null)
            {
                Debug.LogWarning(
                    "[Crystal] Missing visual material or mesh.",
                    dice
                );
                return;
            }

            if (!_visualMaterial.HasProperty("_NumberMode"))
            {
                Debug.LogWarning(
                    "[Crystal] Material must use " +
                    "DiceMaster/Crystal Dice.",
                    dice
                );
                return;
            }

            // --------------------------------------
            // APPLY CRYSTAL MATERIAL
            // --------------------------------------

            Material[] materials =
                renderer.sharedMaterials;

            if (materials.Length == 0)
            {
                renderer.sharedMaterial = _visualMaterial;
            }
            else
            {
                for (int i = 0; i < materials.Length; i++)
                {
                    materials[i] = _visualMaterial;
                }

                renderer.sharedMaterials = materials;
            }

            // --------------------------------------
            // PRESERVE PER-DIE SHADER DATA
            // --------------------------------------

            MaterialPropertyBlock block =
                new MaterialPropertyBlock();

            renderer.GetPropertyBlock(block);

            int faceCount =
                dice.Data.FaceValues.Length;

            // --------------------------------------
            // AUTOMATIC NUMBER MODE
            // --------------------------------------

            // D4: corner numbers.
            // D2/D6: procedural pips.
            // D8/D10/D12/D20: number atlas.

            int numberMode;

            if (dice.GetComponent<D4FaceShaderData>() != null)
            {
                numberMode = 2;
            }
            else if (faceCount == 2 || faceCount == 6)
            {
                numberMode = 1;
            }
            else
            {
                numberMode = 0;
            }

            block.SetFloat(
                "_NumberMode",
                numberMode
            );

            // --------------------------------------
            // NORMALIZE CRYSTAL PATTERN
            // --------------------------------------

            Bounds bounds =
                filter.sharedMesh.bounds;

            float largestDimension = Mathf.Max(
                bounds.size.x,
                Mathf.Max(
                    bounds.size.y,
                    bounds.size.z
                )
            );

            float inverseSize =
                1f / Mathf.Max(
                    0.0001f,
                    largestDimension
                );

            block.SetVector(
                "_PatternCenter",
                bounds.center
            );

            block.SetFloat(
                "_PatternInvSize",
                inverseSize
            );

            // --------------------------------------
            // APPLY MATERIAL PROPERTY BLOCK
            // --------------------------------------

            renderer.SetPropertyBlock(block);

            if (_logOnApply)
            {
                Debug.Log(
                    $"[Crystal] {dice.name} | " +
                    $"Faces: {faceCount} | " +
                    $"Number Mode: {numberMode} | " +
                    $"Material: {_visualMaterial.name}",
                    dice
                );
            }
        }
    }
}
