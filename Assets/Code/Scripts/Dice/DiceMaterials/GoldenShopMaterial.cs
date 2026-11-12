
using UnityEngine;

namespace Code.Scripts.Dice.DiceMaterials
{
    [CreateAssetMenu(
        fileName = "GoldenShop",
        menuName = "ScriptableObjects/DiceMaterials/GoldenShop"
    )]
    public class GoldenShopMaterial : DiceMaterial
    {
        [Header("Golden Shop Visual")]
        [SerializeField]
        private Material _visualMaterial;

        public override void ApplyVisual(Dice dice)
        {
            if (dice == null || dice.Data == null)
            {
                Debug.LogWarning(
                    "[GoldenShop] Invalid dice or data."
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
                    "[GoldenShop] Missing visual material or mesh.",
                    dice
                );
                return;
            }

            // Apply visual material to this die.
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

            // Preserve existing shader properties.
            MaterialPropertyBlock block =
                new MaterialPropertyBlock();

            renderer.GetPropertyBlock(block);

            int faceCount =
                dice.Data.FaceValues.Length;

            // Automatically select number mode.
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

            // Normalize the gold pattern to dice size.
            Bounds bounds = filter.sharedMesh.bounds;

            float largestDimension = Mathf.Max(
                bounds.size.x,
                Mathf.Max(
                    bounds.size.y,
                    bounds.size.z
                )
            );

            float inverseSize =
                1f / Mathf.Max(
                    largestDimension,
                    0.0001f
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

            Debug.Log(
                $"[GoldenShop] {dice.name} | " +
                $"Faces: {faceCount} | " +
                $"Number Mode: {numberMode}",
                dice
            );
        }
    }
}
