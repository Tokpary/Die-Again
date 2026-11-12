
using UnityEngine;

namespace Code.Scripts.Dice.DiceMaterials
{
    [CreateAssetMenu(
        fileName = "ToxicUnified",
        menuName = "ScriptableObjects/DiceMaterials/ToxicUnified"
    )]
    public class ToxicUnifiedMaterial : DiceMaterial
    {
        [Header("Toxic Visual Material")]
        [SerializeField]
        private Material _visualMaterial;

        [Header("Debug")]
        [SerializeField]
        private bool _logOnApply = false;

        public override void ApplyVisual(Dice dice)
        {
            // ======================================
            // VALIDATION
            // ======================================

            if (dice == null || dice.Data == null)
            {
                Debug.LogWarning(
                    "[ToxicUnified] Invalid dice or data."
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
                    "[ToxicUnified] Missing visual material or mesh.",
                    dice
                );
                return;
            }

            if (!_visualMaterial.HasProperty("_NumberMode"))
            {
                Debug.LogWarning(
                    "[ToxicUnified] Incorrect shader. " +
                    "Expected DiceMaster/Toxic Dice Unified.",
                    dice
                );
                return;
            }

            // ======================================
            // APPLY TOXIC MATERIAL
            // ======================================

            Material[] materials =
                renderer.sharedMaterials;

            if (materials.Length == 0)
            {
                renderer.sharedMaterial =
                    _visualMaterial;
            }
            else
            {
                for (int i = 0; i < materials.Length; i++)
                {
                    materials[i] = _visualMaterial;
                }

                renderer.sharedMaterials = materials;
            }

            // ======================================
            // PRESERVE PROPERTY BLOCK
            // ======================================

            MaterialPropertyBlock block =
                new MaterialPropertyBlock();

            renderer.GetPropertyBlock(block);

            // ======================================
            // DETECT DICE TYPE
            // ======================================

            int faceCount =
                dice.Data.FaceValues.Length;

            int numberMode;

            if (dice.GetComponent<D4FaceShaderData>() != null)
            {
                // D4 corner numbers.
                numberMode = 2;
            }
            else if (faceCount == 2 || faceCount == 6)
            {
                // D2 and D6 procedural pips.
                numberMode = 1;
            }
            else
            {
                // Remaining numbered dice.
                numberMode = 0;
            }

            block.SetFloat(
                "_NumberMode",
                numberMode
            );

            // ======================================
            // NORMALIZE PROCEDURAL PATTERN
            // ======================================

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

            renderer.SetPropertyBlock(block);

            // ======================================
            // AUTOMATIC SLIME COMPONENT
            // ======================================

            ToxicSlimeResponse response =
                dice.GetComponent<ToxicSlimeResponse>();

            // Add only once, even if ApplyVisual()
            // is called multiple times.
            if (response == null)
            {
                response =
                    dice.gameObject.AddComponent<ToxicSlimeResponse>();
            }

            // Activate if this die was reused.
            response.enabled = true;

            // ======================================
            // DEBUG
            // ======================================

            if (_logOnApply)
            {
                Debug.Log(
                    $"[ToxicUnified] {dice.name} | " +
                    $"Faces: {faceCount} | " +
                    $"Number Mode: {numberMode} | " +
                    "Slime Response: Active",
                    dice
                );
            }
        }
    }
}
