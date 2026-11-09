using Code.Scripts.Dice;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Temporary debug helper: press Space to identify the upward face from
/// the FaceID vertex colors and print the corresponding DiceSO value.
/// </summary>
[RequireComponent(typeof(MeshFilter))]
public sealed class DiceFaceValueProbe : MonoBehaviour
{
    private const int PaletteSize = 20;

    [SerializeField] private DiceSO diceData;
    [SerializeField] private MeshFilter meshFilter;

    private void Awake()
    {
        if (meshFilter == null)
            meshFilter = GetComponent<MeshFilter>();
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
            LogUpwardFaceValue();
    }

    [ContextMenu("Log upward face value")]
    public void LogUpwardFaceValue()
    {
        if (diceData == null)
        {
            Debug.LogError("Assign the DiceSO asset to Dice Face Value Probe.", this);
            return;
        }

        if (meshFilter == null || meshFilter.sharedMesh == null)
        {
            Debug.LogError("No mesh was found on this die.", this);
            return;
        }

        Mesh mesh = meshFilter.sharedMesh;
        Color32[] colors = mesh.colors32;
        Vector3[] vertices = mesh.vertices;

        if (colors.Length != vertices.Length || colors.Length == 0)
        {
            Debug.LogError("The mesh has no readable vertex colors. Enable Read/Write in the FBX import settings and reimport it.", this);
            return;
        }

        float bestDot = float.NegativeInfinity;
        int bestFaceId = -1;

        for (int submesh = 0; submesh < mesh.subMeshCount; submesh++)
        {
            int[] triangles = mesh.GetTriangles(submesh);
            for (int i = 0; i < triangles.Length; i += 3)
            {
                int i0 = triangles[i];
                int i1 = triangles[i + 1];
                int i2 = triangles[i + 2];

                // Alpha 0 marks bevels, so they are not treated as game faces.
                if (colors[i0].a < 128 || colors[i1].a < 128 || colors[i2].a < 128)
                    continue;

                int faceId = DecodeFaceId(colors[i0]);
                if (DecodeFaceId(colors[i1]) != faceId || DecodeFaceId(colors[i2]) != faceId)
                    continue;

                Vector3 a = transform.TransformPoint(vertices[i0]);
                Vector3 b = transform.TransformPoint(vertices[i1]);
                Vector3 c = transform.TransformPoint(vertices[i2]);
                Vector3 normal = Vector3.Cross(b - a, c - a).normalized;
                float dot = Vector3.Dot(normal, Vector3.up);

                if (dot > bestDot)
                {
                    bestDot = dot;
                    bestFaceId = faceId;
                }
            }
        }

        if (bestFaceId < 0)
        {
            Debug.LogError("No valid FaceID face was found. Check that flat faces have opaque colors and bevels have alpha 0.", this);
            return;
        }

        if (bestFaceId >= diceData.faceValues.Length)
        {
            Debug.LogError($"Face ID {bestFaceId} was found, but DiceSO only has {diceData.faceValues.Length} values. Make the array match the die's face count.", this);
            return;
        }

        Debug.Log($"{name}: upward face ID {bestFaceId}, value {diceData.faceValues[bestFaceId]} (up alignment {bestDot:F2}).", this);
    }

    private static int DecodeFaceId(Color32 color)
    {
        Color.RGBToHSV(color, out float hue, out _, out _);
        return Mathf.RoundToInt(hue * PaletteSize) % PaletteSize;
    }
}
