
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(MeshRenderer))]
public class ToxicSlimeResponse : MonoBehaviour
{
    [Header("Collision Sensitivity")]
    [SerializeField, Min(0f)]
    private float _minimumImpactSpeed = 0.5f;

    [SerializeField, Min(0.01f)]
    private float _fullImpactSpeed = 6f;

    [Header("Jelly Damping")]
    [SerializeField, Range(0.1f, 30f)]
    private float _impactDecay = 5f;

    private static readonly int SlimeImpactID =
        Shader.PropertyToID("_SlimeImpact");

    private static readonly int SlimeImpactAgeID =
        Shader.PropertyToID("_SlimeImpactAge");

    private static readonly int SlimeImpactAxisID =
        Shader.PropertyToID("_SlimeImpactAxisOS");

    private MeshRenderer _renderer;
    private MaterialPropertyBlock _block;

    private float _impact;
    private float _impactAge;

    private Vector3 _impactAxisOS = Vector3.up;

    private void Awake()
    {
        _renderer = GetComponent<MeshRenderer>();
        _block = new MaterialPropertyBlock();
    }

    private void OnEnable()
    {
        _impact = 0f;
        _impactAge = 0f;
        _impactAxisOS = Vector3.up;
    }

    private void OnCollisionEnter(Collision collision)
    {
        float speed =
            collision.relativeVelocity.magnitude;

        if (speed < _minimumImpactSpeed)
        {
            return;
        }

        float strength = Mathf.InverseLerp(
            _minimumImpactSpeed,
            Mathf.Max(
                _minimumImpactSpeed + 0.01f,
                _fullImpactSpeed
            ),
            speed
        );

        // Keep the strongest impact if multiple
        // contacts happen within a short interval.
        _impact = Mathf.Max(
            _impact,
            strength
        );

        // Restart the squash/stretch oscillation.
        _impactAge = 0f;

        // Squash along the collision normal.
        if (collision.contactCount > 0)
        {
            Vector3 normalWS =
                collision.GetContact(0).normal;

            _impactAxisOS =
                transform.InverseTransformDirection(
                    normalWS
                ).normalized;
        }
        else
        {
            _impactAxisOS = Vector3.up;
        }
    }

    private void LateUpdate()
    {
        if (_renderer == null)
        {
            return;
        }

        Material material =
            _renderer.sharedMaterial;

        if (material == null ||
            !material.HasProperty(SlimeImpactID))
        {
            return;
        }

        float dt = Time.deltaTime;

        _impactAge += dt;

        // Exponential damping creates a
        // spring-like visual after each impact.
        _impact *= Mathf.Exp(
            -_impactDecay * dt
        );

        if (_impact < 0.0001f)
        {
            _impact = 0f;
        }

        // Preserve the number atlas / D4 data /
        // pattern scale already stored by DiceMaster.
        _renderer.GetPropertyBlock(_block);

        _block.SetFloat(
            SlimeImpactID,
            _impact
        );

        _block.SetFloat(
            SlimeImpactAgeID,
            _impactAge
        );

        _block.SetVector(
            SlimeImpactAxisID,
            new Vector4(
                _impactAxisOS.x,
                _impactAxisOS.y,
                _impactAxisOS.z,
                0f
            )
        );

        _renderer.SetPropertyBlock(_block);
    }
}
