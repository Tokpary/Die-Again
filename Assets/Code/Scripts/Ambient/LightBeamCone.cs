using UnityEngine;

namespace Code.Scripts.Ambient
{
    // Genera la malla del cono para el haz de luz falso (shader "DiceMaster/Light Beam").
    // El cono sale de este objeto hacia su +Z, como un Spot Light: ponlo de hijo de la luz con posición y rotación 0.
    // Con _matchSpotLight copia el ángulo y el alcance del Spot Light (en este objeto o en el padre).
    [ExecuteAlways]
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class LightBeamCone : MonoBehaviour
    {
        [Tooltip("Largo del haz (si no copia el Spot Light).")]
        [SerializeField, Min(0.01f)] private float _length = 3f;
        [Tooltip("Radio en el origen del haz. 0 = punta; algo más hace que salga del tamaño de la lámpara.")]
        [SerializeField, Min(0f)] private float _startRadius = 0.05f;
        [Tooltip("Radio al final del haz (si no copia el Spot Light).")]
        [SerializeField, Min(0.01f)] private float _endRadius = 1f;
        [SerializeField, Range(8, 64)] private int _segments = 32;

        [Header("Spot Light")]
        [Tooltip("Copia ángulo y alcance del Spot Light de este objeto o del padre.")]
        [SerializeField] private bool _matchSpotLight = true;
        [Tooltip("Parte del alcance de la luz que ocupa el haz.")]
        [SerializeField, Range(0.1f, 1f)] private float _lengthScale = 0.8f;

        private Mesh _mesh;
        private Light _light;
        private bool _dirty = true;
        private float _lastAngle;
        private float _lastRange;

        private void OnEnable() => _dirty = true;

        // Desde OnValidate no se puede tocar el MeshFilter: se marca y se reconstruye en el siguiente Update.
        private void OnValidate() => _dirty = true;

        private void Update()
        {
            Light spot = FindSpotLight();
            if (spot != null && (!Mathf.Approximately(spot.spotAngle, _lastAngle) || !Mathf.Approximately(spot.range, _lastRange)))
                _dirty = true;

            if (_dirty) Rebuild(spot);
        }

        private void OnDestroy()
        {
            if (_mesh == null) return;
            if (Application.isPlaying) Destroy(_mesh);
            else DestroyImmediate(_mesh);
        }

        private Light FindSpotLight()
        {
            if (!_matchSpotLight) return null;
            if (_light == null) _light = GetComponentInParent<Light>();
            return _light != null && _light.type == LightType.Spot ? _light : null;
        }

        private void Rebuild(Light spot)
        {
            _dirty = false;

            float length = _length;
            float startRadius = _startRadius;
            float endRadius = _endRadius;
            if (spot != null)
            {
                _lastAngle = spot.spotAngle;
                _lastRange = spot.range;
                length = spot.range * _lengthScale;
                endRadius = startRadius + Mathf.Tan(spot.spotAngle * 0.5f * Mathf.Deg2Rad) * length;
            }

            if (_mesh == null)
                _mesh = new Mesh { name = "LightBeamCone", hideFlags = HideFlags.DontSave };

            int segments = Mathf.Max(3, _segments);
            var vertices = new Vector3[(segments + 1) * 2];
            var normals = new Vector3[vertices.Length];
            var uvs = new Vector2[vertices.Length];
            var triangles = new int[segments * 6];

            // Dos anillos (origen y final). UV.y: 0 en el origen, 1 al final; UV.x da la vuelta.
            for (int i = 0; i <= segments; i++)
            {
                float angle = i / (float)segments * Mathf.PI * 2f;
                var around = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                Vector3 normal = new Vector3(around.x * length, around.y * length, -(endRadius - startRadius)).normalized;

                int top = i * 2;
                vertices[top] = new Vector3(around.x * startRadius, around.y * startRadius, 0f);
                vertices[top + 1] = new Vector3(around.x * endRadius, around.y * endRadius, length);
                normals[top] = normal;
                normals[top + 1] = normal;
                uvs[top] = new Vector2(i / (float)segments, 0f);
                uvs[top + 1] = new Vector2(i / (float)segments, 1f);
            }

            for (int i = 0; i < segments; i++)
            {
                int top = i * 2;
                int t = i * 6;
                triangles[t] = top;
                triangles[t + 1] = top + 1;
                triangles[t + 2] = top + 3;
                triangles[t + 3] = top;
                triangles[t + 4] = top + 3;
                triangles[t + 5] = top + 2;
            }

            _mesh.Clear();
            _mesh.vertices = vertices;
            _mesh.normals = normals;
            _mesh.uv = uvs;
            _mesh.triangles = triangles;
            _mesh.RecalculateBounds();

            GetComponent<MeshFilter>().sharedMesh = _mesh;
        }
    }
}