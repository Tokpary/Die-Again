using System.Collections.Generic;
using Code.Scripts.Dice;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class D4FaceShaderData : MonoBehaviour
{
    [SerializeField] private DiceSO diceData;

    private static readonly int D4ValuesId =
        Shader.PropertyToID("_D4Values");

    private Mesh runtimeMesh;

    private void Awake()
    {
        MeshFilter meshFilter = GetComponent<MeshFilter>();
        MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
        Mesh sourceMesh = meshFilter.sharedMesh;

        if (sourceMesh == null || diceData == null ||
            diceData.faceValues == null || diceData.faceValues.Length != 4)
        {
            Debug.LogError(
                "Asigna la malla y el DiceSO del D4, que debe tener 4 valores.",
                this);
            enabled = false;
            return;
        }

        runtimeMesh = Instantiate(sourceMesh);
        runtimeMesh.name = sourceMesh.name + " (D4 runtime)";
        meshFilter.mesh = runtimeMesh;

        // UV2 contiene D4VertexID. UV3 contiene D4FaceUV.
        List<Vector2> vertexIds = new List<Vector2>();
        List<Vector2> faceUvs = new List<Vector2>();
        runtimeMesh.GetUVs(2, vertexIds);
        runtimeMesh.GetUVs(3, faceUvs);

        Color32[] originalColors = runtimeMesh.colors32;

        if (vertexIds.Count != runtimeMesh.vertexCount ||
            faceUvs.Count != runtimeMesh.vertexCount ||
            originalColors.Length != runtimeMesh.vertexCount)
        {
            Debug.LogError(
                "Faltan D4VertexID, D4FaceUV o colores FaceID. Revisa Read/Write y la importación del FBX.",
                this);
            enabled = false;
            return;
        }

        float largestId = 0f;
        foreach (Vector2 uv in vertexIds)
            largestId = Mathf.Max(largestId, Mathf.Abs(uv.x));

        // Admite IDs guardados como 0-3 o normalizados como 0-1.
        bool idsAreRaw = largestId > 1.001f;

        List<Vector4> packedIds = new List<Vector4>();
        bool[] assigned = new bool[runtimeMesh.vertexCount];

        for (int i = 0; i < runtimeMesh.vertexCount; i++)
            packedIds.Add(Vector4.zero);

        for (int submesh = 0; submesh < runtimeMesh.subMeshCount; submesh++)
        {
            int[] triangles = runtimeMesh.GetTriangles(submesh);

            for (int i = 0; i < triangles.Length; i += 3)
            {
                int a = triangles[i];
                int b = triangles[i + 1];
                int c = triangles[i + 2];

                // Alpha 0 marca los biseles, que no son caras jugables.
                if (originalColors[a].a < 128 ||
                    originalColors[b].a < 128 ||
                    originalColors[c].a < 128)
                    continue;

                // El orden del triángulo no garantiza qué esquina es cuál.
                // Usamos D4FaceUV para ordenar siempre arriba, abajo-izquierda,
                // abajo-derecha, igual que en el Shader Graph.
                int top = FindClosestVertex(
                    new Vector2(0.50f, 1.01f), a, b, c, faceUvs);
                int lowerLeft = FindClosestVertex(
                    new Vector2(0.06f, 0.25f), a, b, c, faceUvs);
                int lowerRight = FindClosestVertex(
                    new Vector2(0.94f, 0.25f), a, b, c, faceUvs);

                if (top == lowerLeft || top == lowerRight ||
                    lowerLeft == lowerRight)
                {
                    Debug.LogError(
                        "No se pudieron distinguir las tres esquinas de D4FaceUV en una cara.",
                        this);
                    enabled = false;
                    return;
                }

                int topId = DecodeId(vertexIds[top].x, idsAreRaw);
                int leftId = DecodeId(vertexIds[lowerLeft].x, idsAreRaw);
                int rightId = DecodeId(vertexIds[lowerRight].x, idsAreRaw);

                // R=arriba, G=abajo-izquierda, B=abajo-derecha.
                // A=1 identifica una cara jugable; los biseles quedan en 0.
                Vector4 data = new Vector4(
                    topId / 3f, leftId / 3f, rightId / 3f, 1f);

                if (!AssignVertex(a, data, packedIds, assigned) ||
                    !AssignVertex(b, data, packedIds, assigned) ||
                    !AssignVertex(c, data, packedIds, assigned))
                {
                    Debug.LogError(
                        "La malla comparte vértices entre caras. El D4 necesita vértices separados por cara.",
                        this);
                    enabled = false;
                    return;
                }
            }
        }

        // UV4 de Shader Graph corresponde al canal 4 de la malla.
        runtimeMesh.SetUVs(4, packedIds);

        Vector4 values = new Vector4(
            diceData.faceValues[0],
            diceData.faceValues[1],
            diceData.faceValues[2],
            diceData.faceValues[3]);

        MaterialPropertyBlock block = new MaterialPropertyBlock();
        meshRenderer.GetPropertyBlock(block);
        block.SetVector(D4ValuesId, values);
        meshRenderer.SetPropertyBlock(block);
    }

    private static int DecodeId(float value, bool idsAreRaw)
    {
        int id = idsAreRaw
            ? Mathf.RoundToInt(value)
            : Mathf.RoundToInt(value * 3f);

        return Mathf.Clamp(id, 0, 3);
    }

    private static int FindClosestVertex(
        Vector2 target, int a, int b, int c, List<Vector2> faceUvs)
    {
        float distanceA = (faceUvs[a] - target).sqrMagnitude;
        float distanceB = (faceUvs[b] - target).sqrMagnitude;
        float distanceC = (faceUvs[c] - target).sqrMagnitude;

        if (distanceA <= distanceB && distanceA <= distanceC)
            return a;
        if (distanceB <= distanceC)
            return b;
        return c;
    }

    private static bool AssignVertex(
        int index,
        Vector4 data,
        List<Vector4> packedIds,
        bool[] assigned)
    {
        if (assigned[index] &&
            Vector4.Distance(packedIds[index], data) > 0.001f)
            return false;

        packedIds[index] = data;
        assigned[index] = true;
        return true;
    }

    private void OnDestroy()
    {
        if (runtimeMesh != null)
            Destroy(runtimeMesh);
    }
}
