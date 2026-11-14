
using UnityEngine;

namespace Code.Scripts.Dice.DiceMaterials
{
    [CreateAssetMenu(
        fileName = "LavaCartoon",
        menuName = "ScriptableObjects/DiceMaterials/LavaCartoon"
    )]
    public class LavaCartoonMaterial : DiceMaterial
    {
        [Header("Cartoon Lava Visual Material")]
        [SerializeField]
        private Material _visualMaterial;

        public override void ApplyVisual(Dice dice)
        {
            if (dice == null || dice.Data == null)
            {
                Debug.LogWarning(
                    "[LavaCartoon] Invalid dice or dice data."
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
                    "[LavaCartoon] Missing visual material or dice mesh.",
                    dice
                );
                return;
            }

            // --------------------------------------
            // APPLY CARTOON LAVA MATERIAL
            // --------------------------------------

            Material[] materials = renderer.sharedMaterials;

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
            // PRESERVE EXISTING SHADER DATA
            // --------------------------------------

            MaterialPropertyBlock block =
                new MaterialPropertyBlock();

            renderer.GetPropertyBlock(block);

            int faceCount = dice.Data.FaceValues.Length;

            // --------------------------------------
            // AUTOMATIC NUMBER MODE
            // --------------------------------------

            // D4: corner numbers.
            // D2/D6: procedural pips.
            // Other dice: number atlas.

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
            // NORMALIZE LAVA PATTERN
            // --------------------------------------

            Bounds bounds = filter.sharedMesh.bounds;

            float largestDimension = Mathf.Max(
                bounds.size.x,
                Mathf.Max(bounds.size.y, bounds.size.z)
            );

            float inverseSize = 1f / Mathf.Max(
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
            // APPLY SHADER PROPERTIES
            // --------------------------------------

            renderer.SetPropertyBlock(block);

            Debug.Log(
                $"[LavaCartoon] {dice.name} | " +
                $"Faces: {faceCount} | " +
                $"Number Mode: {numberMode} | " +
                $"Material: {_visualMaterial.name}",
                dice
            );
        }
    }
}
