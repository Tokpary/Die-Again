using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class DiceFaceShaderData : MonoBehaviour
{
    [SerializeField] private Code.Scripts.Dice.DiceSO diceData;

    private static readonly int FaceValuesId =
        Shader.PropertyToID("_FaceValues");

    private Mesh runtimeMesh;
    private MaterialPropertyBlock propertyBlock;

    private void Awake()
    {
        MeshFilter meshFilter = GetComponent<MeshFilter>();
        Mesh sourceMesh = meshFilter.sharedMesh;

        if (sourceMesh == null || diceData == null)
        {
            Debug.LogError("Falta la malla o el DiceSO.", this);
            enabled = false;
            return;
        }

        // Creamos una copia para no modificar el FBX compartido.
        runtimeMesh = Instantiate(sourceMesh);
        meshFilter.mesh = runtimeMesh;

        Color32[] colors = runtimeMesh.colors32;
        Vector2[] faceValues = new Vector2[colors.Length];

        if (colors.Length != runtimeMesh.vertexCount)
        {
            Debug.LogError("La malla no tiene colores FaceID legibles.", this);
            enabled = false;
            return;
        }

        for (int i = 0; i < colors.Length; i++)
        {
            // Alpha 0 identifica bordes/bisel, que no son caras de juego.
            if (colors[i].a < 128)
            {
                faceValues[i] = Vector2.zero;
                continue;
            }

            Color.RGBToHSV(colors[i], out float hue, out _, out _);
            int faceId = Mathf.RoundToInt(hue * 20) % 20;

            if (faceId >= diceData.faceValues.Length)
            {
                Debug.LogError(
                    $"FaceID {faceId} no tiene valor en DiceSO.", this);
                enabled = false;
                return;
            }

            // X = valor de esta cara. Se mantiene constante en sus vértices.
            faceValues[i] = new Vector2(diceData.faceValues[faceId], 0);
        }

        // Guardamos el valor por vértice en el canal UV3 (índice 2).
        runtimeMesh.SetUVs(2, new System.Collections.Generic.List<Vector2>(faceValues));

        propertyBlock = new MaterialPropertyBlock();
        GetComponent<MeshRenderer>().GetPropertyBlock(propertyBlock);
        GetComponent<MeshRenderer>().SetPropertyBlock(propertyBlock);
    }

    private void OnDestroy()
    {
        if (runtimeMesh != null)
            Destroy(runtimeMesh);
    }
}