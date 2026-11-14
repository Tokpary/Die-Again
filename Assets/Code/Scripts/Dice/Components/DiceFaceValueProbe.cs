using System.Collections.Generic;
using Code.Scripts.Dice;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Press Space to identify the upward face or D4 vertex and print its value.
/// </summary>
[RequireComponent(typeof(MeshFilter))]
public sealed class DiceFaceValueProbe : MonoBehaviour
{
    private const int PaletteSize = 20;

    private DiceSO diceData;
    [SerializeField] private MeshFilter meshFilter;

    public void SetDiceData(DiceSO diceData)
    {
        this.diceData = diceData;
    }
    
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

        if (!TryGetUpwardId(mesh, colors, vertices, out int upwardId, out float upAlignment))
        {
            Debug.LogError(
                IsD4 ? "Could not detect the upward D4 vertex. Check the D4VertexID UV channel and opaque face colors." :
                    "No valid FaceID face was found. Check that flat faces have opaque colors and bevels have alpha 0.",
                this);
            return;
        }

        if (upwardId >= diceData.faceValues.Length)
        {
            Debug.LogError($"ID {upwardId} was found, but DiceSO only has {diceData.faceValues.Length} values. Make the array match the die's face count.", this);
            return;
        }

        string idType = IsD4 ? "vertex" : "face";
        Debug.Log($"{name}: upward {idType} ID {upwardId}, value {diceData.faceValues[upwardId]} (up alignment {upAlignment:F2}).", this);
    }

    public int GetUpwardFaceValue()
    {
        if (meshFilter == null || meshFilter.sharedMesh == null)
        {
            Debug.LogError("No mesh was found on this die.", this);
            return -1;
        }

        Mesh mesh = meshFilter.sharedMesh;
        Color32[] colors = mesh.colors32;
        Vector3[] vertices = mesh.vertices;

        if (colors.Length != vertices.Length || colors.Length == 0)
        {
            Debug.LogError("The mesh has no readable vertex colors. Enable Read/Write in the FBX import settings and reimport it.", this);
            return -1;
        }

        if (TryGetUpwardId(mesh, colors, vertices, out int upwardId, out _))
            return upwardId;

        Debug.LogError(
            IsD4 ? "Could not detect the upward D4 vertex. Check the D4VertexID UV channel and opaque face colors." :
                "No valid FaceID face was found. Check that flat faces have opaque colors and bevels have alpha 0.",
            this);
        return -1;
    }

    private bool IsD4 => diceData != null &&
                         diceData.faceValues != null &&
                         diceData.faceValues.Length == 4;

    private bool TryGetUpwardId(
        Mesh mesh,
        Color32[] colors,
        Vector3[] vertices,
        out int upwardId,
        out float upAlignment)
    {
        if (IsD4)
            return TryGetUpwardD4Vertex(mesh, colors, vertices, out upwardId, out upAlignment);

        return TryGetUpwardFace(mesh, colors, vertices, out upwardId, out upAlignment);
    }

    private bool TryGetUpwardD4Vertex(
        Mesh mesh,
        Color32[] colors,
        Vector3[] vertices,
        out int upwardId,
        out float upAlignment)
    {
        List<Vector2> vertexIds = new List<Vector2>();
        mesh.GetUVs(2, vertexIds);
        if (vertexIds.Count != vertices.Length)
        {
            upwardId = -1;
            upAlignment = float.NegativeInfinity;
            return false;
        }

        float largestId = 0f;
        for (int i = 0; i < vertexIds.Count; i++)
            largestId = Mathf.Max(largestId, Mathf.Abs(vertexIds[i].x));

        bool idsAreRaw = largestId > 1.001f;
        Vector3[] positionSums = new Vector3[4];
        int[] counts = new int[4];

        for (int submesh = 0; submesh < mesh.subMeshCount; submesh++)
        {
            int[] triangles = mesh.GetTriangles(submesh);
            for (int i = 0; i < triangles.Length; i += 3)
            {
                int i0 = triangles[i];
                int i1 = triangles[i + 1];
                int i2 = triangles[i + 2];

                if (colors[i0].a < 128 || colors[i1].a < 128 || colors[i2].a < 128)
                    continue;

                if (!AccumulateD4Vertex(i0, vertexIds, idsAreRaw, vertices, positionSums, counts) ||
                    !AccumulateD4Vertex(i1, vertexIds, idsAreRaw, vertices, positionSums, counts) ||
                    !AccumulateD4Vertex(i2, vertexIds, idsAreRaw, vertices, positionSums, counts))
                {
                    upwardId = -1;
                    upAlignment = float.NegativeInfinity;
                    return false;
                }
            }
        }

        Vector3 center = Vector3.zero;
        for (int id = 0; id < counts.Length; id++)
        {
            if (counts[id] == 0)
            {
                upwardId = -1;
                upAlignment = float.NegativeInfinity;
                return false;
            }

            positionSums[id] = transform.TransformPoint(positionSums[id] / counts[id]);
            center += positionSums[id];
        }
        center /= counts.Length;

        upwardId = -1;
        upAlignment = float.NegativeInfinity;
        for (int id = 0; id < positionSums.Length; id++)
        {
            float alignment = Vector3.Dot((positionSums[id] - center).normalized, Vector3.up);
            if (alignment > upAlignment)
            {
                upwardId = id;
                upAlignment = alignment;
            }
        }

        return upwardId >= 0;
    }

    private bool TryGetUpwardFace(
        Mesh mesh,
        Color32[] colors,
        Vector3[] vertices,
        out int faceId,
        out float bestDot)
    {
        faceId = -1;
        bestDot = float.NegativeInfinity;

        for (int submesh = 0; submesh < mesh.subMeshCount; submesh++)
        {
            int[] triangles = mesh.GetTriangles(submesh);
            for (int i = 0; i < triangles.Length; i += 3)
            {
                int i0 = triangles[i];
                int i1 = triangles[i + 1];
                int i2 = triangles[i + 2];

                if (colors[i0].a < 128 || colors[i1].a < 128 || colors[i2].a < 128)
                    continue;

                int triangleFaceId = DecodeFaceId(colors[i0]);
                if (DecodeFaceId(colors[i1]) != triangleFaceId ||
                    DecodeFaceId(colors[i2]) != triangleFaceId)
                    continue;

                Vector3 a = transform.TransformPoint(vertices[i0]);
                Vector3 b = transform.TransformPoint(vertices[i1]);
                Vector3 c = transform.TransformPoint(vertices[i2]);
                Vector3 normal = Vector3.Cross(b - a, c - a).normalized;
                float dot = Vector3.Dot(normal, Vector3.up);

                if (dot > bestDot)
                {
                    bestDot = dot;
                    faceId = triangleFaceId;
                }
            }
        }

        return faceId >= 0;
    }

    private bool AccumulateD4Vertex(
        int index,
        List<Vector2> vertexIds,
        bool idsAreRaw,
        Vector3[] vertices,
        Vector3[] positionSums,
        int[] counts)
    {
        int id = idsAreRaw
            ? Mathf.RoundToInt(vertexIds[index].x)
            : Mathf.RoundToInt(vertexIds[index].x * 3f);

        if (id < 0 || id >= positionSums.Length)
            return false;

        positionSums[id] += vertices[index];
        counts[id]++;
        return true;
    }

    private static int DecodeFaceId(Color32 color)
    {
        Color.RGBToHSV(color, out float hue, out _, out _);
        return Mathf.RoundToInt(hue * PaletteSize) % PaletteSize;
    }
}
