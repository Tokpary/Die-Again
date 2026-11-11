
using UnityEngine;

namespace Code.Scripts.Dice.DiceMaterials
{
    [CreateAssetMenu(
        fileName = "CursedRunic",
        menuName = "ScriptableObjects/DiceMaterials/CursedRunic"
    )]
    public class CursedRunicMaterial : DiceMaterial
    {
        [Header("Cursed Runic Visual")]
        [SerializeField]
        private Material _visualMaterial;

        [Header("Debug")]
        [SerializeField]
        private bool _logOnApply = false;

        public override int ModifyValue(int value)
        {
            return value > 0 ? -value : value;
        }

        public override void ApplyVisual(Dice dice)
        {
            if (dice == null || dice.Data == null)
            {
                Debug.LogWarning(
                    "[CursedRunic] Invalid dice or data."
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
                    "[CursedRunic] Missing material or mesh.",
                    dice
                );
                return;
            }

            if (!_visualMaterial.HasProperty("_NumberMode"))
            {
                Debug.LogWarning(
                    "[CursedRunic] Material must use " +
                    "DiceMaster/Cursed Runic Dice.",
                    dice
                );
                return;
            }

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

            MaterialPropertyBlock block =
                new MaterialPropertyBlock();

            renderer.GetPropertyBlock(block);

            int faceCount = dice.Data.FaceValues.Length;

            int numberMode;

            if (dice.GetComponent<D4FaceShaderData>() != null)
            {
                numberMode = 2;
            }
            else if (faceCount == 6)
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

            renderer.SetPropertyBlock(block);

            if (_logOnApply)
            {
                Debug.Log(
                    $"[CursedRunic] {dice.name} | " +
                    $"Faces: {faceCount} | " +
                    $"Number Mode: {numberMode} | " +
                    $"Material: {_visualMaterial.name}",
                    dice
                );
            }
        }
    }
}
