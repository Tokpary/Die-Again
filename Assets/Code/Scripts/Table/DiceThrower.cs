using System;
using System.Collections.Generic;
using Code.Scripts.Dice;
using Code.Scripts.Table;
using Code.Scripts.Utils;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using Random = UnityEngine.Random;

public class DiceThrower : MonoBehaviour
{
    private Rigidbody diceRb;
    [SerializeField] private Camera cam;
    [SerializeField] private LayerMask aimMask = ~0;    
    [SerializeField, Min(0.1f)] private float shotSpeed = 20f;
    [SerializeField] private Transform _holdPoint;
    private int _loadedDiceLayer;

    [Header("Giro al lanzar")]
    [SerializeField, Min(0f)] private float _spinMin = 3f;
    [SerializeField, Min(0f)] private float _spinMax = 10f;
    [SerializeField, Range(0f, 1f)] private float _spinRandomness = 0.3f;

    [Header("Trayectoria")]
    [SerializeField] private LineRenderer _trajectoryLine;
    [SerializeField] private Transform _impactMarker;
    [SerializeField] private LayerMask _trajectoryMask;
    [SerializeField, Min(0.1f)] private float _trajectoryMaxTime = 3f;

    private readonly List<Vector3> _trajectoryPoints = new List<Vector3>();

    private DiceDragController diceDragController;

    private GameInputs gameInput;
    private Vector3 aimPoint;
    private bool hasAimPoint;
    
    public event Action<Dice> OnDiceShot;
    private Dice _loadedDice;
    
    public bool HasLoadedDice => _loadedDice != null;
    public bool ShootEnabled { get; set; } = true;
    private void Awake()
    {
        if (cam == null) cam = Camera.main;
        if (_trajectoryMask == 0) _trajectoryMask = CollisionMaskOf(LayerMask.NameToLayer("DiceInFlight"));
        HideTrajectory();
    }
    

    private void Update()
    {
        if (gameInput == null || !gameInput.Gameplay.enabled)
        {
            HideTrajectory();
            return;
        }
        Vector2 screenPos = gameInput.Gameplay.Apuntar.ReadValue<Vector2>();
        Ray ray = cam.ScreenPointToRay(screenPos);

        if (Physics.Raycast(ray, out RaycastHit hit, 500f, aimMask, QueryTriggerInteraction.Ignore)) 
        {
            aimPoint = hit.point;
            hasAimPoint = true;
        }
        else
        {
            Plane plane = new Plane(Vector3.up, transform.position);
            hasAimPoint = plane.Raycast(ray, out float dist);
            if (hasAimPoint) aimPoint = ray.GetPoint(dist);
        }

        if (hasAimPoint)
        {
            Vector3 flat = aimPoint - transform.position;
            flat.y = 0f;
            if (flat.sqrMagnitude > 0.01f)
                transform.rotation = Quaternion.LookRotation(flat);
        }

        UpdateTrajectory();
        
        bool overUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        bool clickIsForDice = diceDragController.IsDragging || diceDragController.PressConsumed;
        if(ShootEnabled && gameInput.Gameplay.Disparo.WasPressedThisFrame() && !overUI && !clickIsForDice)
        {
            OnShoot();
        }
    }

    private void OnShoot()
    {
        if (!hasAimPoint || diceRb == null) return;

        Rigidbody rb = diceRb;
        Vector3 velocity = LaunchVelocity(_loadedDice.transform.position, RadiusOf(_loadedDice)); 
        Dice shot = ReleaseLoadedDice();
        Utils.SetLayer(shot.gameObject, LayerMask.NameToLayer("DiceInFlight"));
        diceRb = null;
        HideTrajectory();

        rb.isKinematic = false;
        rb.AddForce(velocity, ForceMode.VelocityChange);

        // Giro: sin él, monedas y D4 caen planos sin voltear.
        rb.maxAngularVelocity = Mathf.Max(rb.maxAngularVelocity, _spinMax);
        rb.AddTorque(Spin(velocity), ForceMode.VelocityChange);

        OnDiceShot?.Invoke(shot);
    }

    private Vector3 LaunchVelocity(Vector3 from, float radius)
    {
        Vector3 target = aimPoint + Vector3.up * radius;   
        Vector3 toTarget = target - from;
        Vector3 straight = toTarget.normalized * shotSpeed;

        Vector3 flat = new Vector3(toTarget.x, 0f, toTarget.z);
        float x = flat.magnitude;
        float y = toTarget.y;
        float g = -Physics.gravity.y;
        if (x < 0.01f || g <= 0f) return straight;

        float v2 = shotSpeed * shotSpeed;
        float discriminant = v2 * v2 - g * (g * x * x + 2f * y * v2);
        if (discriminant < 0f) return straight;

        float angle = Mathf.Atan((v2 - Mathf.Sqrt(discriminant)) / (g * x));
        return (flat / x * Mathf.Cos(angle) + Vector3.up * Mathf.Sin(angle)) * shotSpeed;
    }

    private Vector3 Spin(Vector3 velocity)
    {
        Vector3 forward = new Vector3(velocity.x, 0f, velocity.z);
        forward = forward.sqrMagnitude > 0.0001f ? forward.normalized : transform.forward;
        Vector3 tumbleAxis = Vector3.Cross(forward, Vector3.up);  
        if (tumbleAxis.sqrMagnitude < 0.0001f) tumbleAxis = transform.right;

        Vector3 axis = Vector3.Slerp(tumbleAxis.normalized, Random.onUnitSphere, _spinRandomness).normalized;
        return axis * Random.Range(_spinMin, Mathf.Max(_spinMin, _spinMax));
    }

    private void UpdateTrajectory()
    {
        if (_loadedDice == null || !hasAimPoint || (_trajectoryLine == null && _impactMarker == null))
        {
            HideTrajectory();
            return;
        }
        float radius = RadiusOf(_loadedDice);   
        Vector3 position = _loadedDice.transform.position;
        Vector3 velocity = LaunchVelocity(position, radius);
        float step = Time.fixedDeltaTime;
        float damping = diceRb != null ? diceRb.linearDamping : 0f;
        int steps = Mathf.CeilToInt(_trajectoryMaxTime / step);

        _trajectoryPoints.Clear();
        _trajectoryPoints.Add(position);
        bool hasImpact = false;
        RaycastHit impact = default;

        for (int i = 0; i < steps; i++)
        {
            velocity += Physics.gravity * step;
            velocity *= Mathf.Clamp01(1f - damping * step);
            Vector3 move = velocity * step;
            float distance = move.magnitude;
            if (distance < 0.0001f) continue;

            if (Physics.SphereCast(position, radius, move / distance, out impact, distance,
                    _trajectoryMask, QueryTriggerInteraction.Ignore))
            {
                _trajectoryPoints.Add(position + move / distance * impact.distance);
                hasImpact = true;
                break;
            }

            position += move;
            _trajectoryPoints.Add(position);
        }

        if (_trajectoryLine != null)
        {
            _trajectoryLine.enabled = true;
            _trajectoryLine.positionCount = _trajectoryPoints.Count;
            for (int i = 0; i < _trajectoryPoints.Count; i++)
                _trajectoryLine.SetPosition(i, _trajectoryPoints[i]);
        }

        if (_impactMarker != null)
        {
            _impactMarker.gameObject.SetActive(hasImpact);
            if (hasImpact)
                _impactMarker.SetPositionAndRotation(impact.point + impact.normal * 0.01f,
                    Quaternion.FromToRotation(Vector3.up, impact.normal));
        }
    }

    private void HideTrajectory()
    {
        if (_trajectoryLine != null) _trajectoryLine.enabled = false;
        if (_impactMarker != null) _impactMarker.gameObject.SetActive(false);
    }

    private static int CollisionMaskOf(int layer)
    {
        if (layer < 0) return ~0;
        int mask = 0;
        for (int i = 0; i < 32; i++)
            if (!Physics.GetIgnoreLayerCollision(layer, i)) mask |= 1 << i;
        return mask;
    }


    public Dice UnloadDice()
    {
        Dice dice = ReleaseLoadedDice();
        diceRb = null;
        HideTrajectory();
        return dice;
    }

    private Dice ReleaseLoadedDice()
    {
        Dice dice = _loadedDice;
        if(dice == null) return null;

        dice.transform.DOKill();
        dice.transform.SetParent(null, true);
        Utils.SetLayer(dice.gameObject, _loadedDiceLayer);
        diceRb.interpolation = RigidbodyInterpolation.Interpolate;
        
        _loadedDice = null;
        return dice;
    }

    public void LoadDice(Dice dice)
    {
        _loadedDice = dice;
        diceRb = dice.GetComponent<Rigidbody>();
        diceRb.isKinematic = true;
        diceRb.interpolation = RigidbodyInterpolation.None;
        
        _loadedDiceLayer = dice.gameObject.layer;
        Utils.SetLayer(dice.gameObject, LayerMask.NameToLayer("Ignore Raycast"));
        
        dice.transform.SetParent(_holdPoint, true);
        dice.transform.DOLocalMove(Vector3.zero, 0.2f).SetEase(Ease.OutBack);
        dice.transform.DOLocalRotateQuaternion(Quaternion.identity, 0.2f).SetEase(Ease.OutBack);
    }

    private static float RadiusOf(Dice dice)
    {
        Collider body = dice.GetComponent<Collider>();
        if (body == null) return 0.1f;
        Vector3 extents = body.bounds.extents;
        return Mathf.Max(0.01f, Mathf.Min(extents.x, Mathf.Min(extents.y, extents.z)));
    }

    private void OnDrawGizmos()
    {
        if (!hasAimPoint) return;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(aimPoint, 0.3f);
    }

    public void Init(GameInputs gameInputs, DiceDragController diceDragController)
    {
        this.diceDragController = diceDragController;
        gameInput = gameInputs;
    }
}