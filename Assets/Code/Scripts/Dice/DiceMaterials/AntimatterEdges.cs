using System.Collections.Generic;
using UnityEngine;

namespace Code.Scripts.Dice.DiceMaterials
{
    // Extra render-only UV channels. Existing face IDs, number UVs and colliders stay intact.
    public sealed class AntimatterEdges : MonoBehaviour
    {
        private Mesh _mesh;

        public void Prepare(MeshFilter filter)
        {
            if (_mesh != null) return;
            Mesh source = filter.sharedMesh;
            Vector3[] vertices = source.vertices;
            Color32[] colors = source.colors32;
            if (colors.Length != vertices.Length) return;
            var faces = new Dictionary<int, HashSet<int>>();
            for (int i = 0; i < vertices.Length; i++)
            {
                if (colors[i].a < 128) continue;
                Color.RGBToHSV(colors[i], out float hue, out _, out _);
                int id = Mathf.RoundToInt(hue * 20) % 20;
                if (!faces.ContainsKey(id)) faces[id] = new HashSet<int>();
                faces[id].Add(i);
            }

            var first = new List<Vector4>(new Vector4[vertices.Length]);
            var second = new List<Vector4>(new Vector4[vertices.Length]);
            foreach (var face in faces.Values)
            {
                var points = new List<Vector3>();
                foreach (int index in face)
                    if (!points.Contains(vertices[index])) points.Add(vertices[index]);
                if (points.Count < 3) continue;
                Vector3 center = Vector3.zero;
                foreach (var point in points) center += point;
                center /= points.Count;
                Vector3 normal = Vector3.zero;
                for (int i = 1; i < points.Count - 1 && normal.sqrMagnitude < 0.5f; i++)
                    normal = Vector3.Cross(points[i] - points[0], points[i + 1] - points[0]).normalized;
                if (normal.sqrMagnitude < 0.5f) continue;
                float extent = 0;
                foreach (var point in points) extent = Mathf.Max(extent, (point - center).magnitude);
                float epsilon = Mathf.Max(extent * 0.0001f, 0.0000001f);
                var planes = new List<Vector4>();
                // Supporting lines of the convex face: excludes triangulation diagonals.
                for (int a = 0; a < points.Count; a++)
                for (int b = a + 1; b < points.Count; b++)
                {
                    Vector3 inward = Vector3.Cross(normal, points[b] - points[a]).normalized;
                    if (Vector3.Dot(inward, center - points[a]) < 0) inward = -inward;
                    float offset = -Vector3.Dot(inward, points[a]);
                    if (Vector3.Dot(inward, center) + offset <= epsilon) continue;
                    bool boundary = true;
                    foreach (var point in points)
                        if (Vector3.Dot(inward, point) + offset < -epsilon) { boundary = false; break; }
                    if (!boundary) continue;
                    bool duplicate = false;
                    foreach (var plane in planes)
                        if (Vector3.Dot(inward, new Vector3(plane.x, plane.y, plane.z)) > 0.9999f &&
                            Mathf.Abs(offset - plane.w) < epsilon) { duplicate = true; break; }
                    if (!duplicate) planes.Add(new Vector4(inward.x, inward.y, inward.z, offset));
                }
                if (planes.Count < 3 || planes.Count > 8)
                {
                    Debug.LogWarning("Antimatter: unsupported face boundary; expected 3 to 8 edges.", this);
                    continue;
                }
                float radius = float.MaxValue;
                foreach (var plane in planes)
                    radius = Mathf.Min(radius, Vector3.Dot(new Vector3(plane.x, plane.y, plane.z), center) + plane.w);
                foreach (int index in face)
                {
                    Vector4 lo = Vector4.one, hi = Vector4.one;
                    for (int e = 0; e < planes.Count; e++)
                    {
                        Vector4 plane = planes[e];
                        float distance = (Vector3.Dot(new Vector3(plane.x, plane.y, plane.z), vertices[index]) + plane.w) / radius;
                        if (e < 4) lo[e] = distance; else hi[e - 4] = distance;
                    }
                    first[index] = lo;
                    second[index] = hi;
                }
            }
            _mesh = Instantiate(source);
            _mesh.name = source.name + " (Antimatter edges)";
            _mesh.SetUVs(5, first);
            _mesh.SetUVs(6, second);
            filter.sharedMesh = _mesh;
        }

        private void OnDestroy()
        {
            if (_mesh != null) Destroy(_mesh);
        }
    }
}
