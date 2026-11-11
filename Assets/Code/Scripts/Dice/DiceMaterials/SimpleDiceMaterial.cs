using UnityEngine;

namespace Code.Scripts.Dice.DiceMaterials
{
    [CreateAssetMenu(fileName = "SimpleDiceMaterial", menuName = "ScriptableObjects/DiceMaterials/Simple")]
    public class SimpleDiceMaterial : DiceMaterial
    {
        [Tooltip("Material visual que se pone en todos los huecos del renderer del dado.")]
        [SerializeField] private Material _visualMaterial;

        public override void ApplyVisual(Dice dice)
        {
            MeshRenderer renderer = dice.GetComponent<MeshRenderer>();
            if (_visualMaterial == null || renderer == null)
            {
                Debug.LogWarning($"[{name}] Falta el material visual o el MeshRenderer del dado.", dice);
                return;
            }

            Material[] materials = renderer.sharedMaterials;
            if (materials.Length == 0) materials = new Material[1];
            for (int i = 0; i < materials.Length; i++)
                materials[i] = _visualMaterial;
            renderer.sharedMaterials = materials;
        }
    }
}