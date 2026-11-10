using System;
using Code.Scripts.Dice;
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
    [SerializeField] private float shotSpeed = 15f;
    [SerializeField] private Transform _holdPoint;
    private int _loadedDiceLayer;
    
    private DiceDragController diceDragController;

    private GameInputs gameInput;
    private Vector3 aimPoint;
    private bool hasAimPoint;
    
    public event Action<Dice> OnDiceShot;
    private Dice _loadedDice;
    
    private void Awake()
    {
        if (cam == null) cam = Camera.main;
    }
    

    private void Update()
    {
        if(gameInput == null || !gameInput.Gameplay.enabled) return;
        Vector2 screenPos = gameInput.Gameplay.Apuntar.ReadValue<Vector2>();
        Ray ray = cam.ScreenPointToRay(screenPos);

        if (Physics.Raycast(ray, out RaycastHit hit, 500f, aimMask))
        {
            aimPoint = hit.point;
            hasAimPoint = true;
        }
        else
        {
            // Si no hay nada debajo del cursor, usamos un plano a la altura del cañón
            Plane plane = new Plane(Vector3.up, transform.position);
            hasAimPoint = plane.Raycast(ray, out float dist);
            if (hasAimPoint) aimPoint = ray.GetPoint(dist);
        }

        // Que el cañón mire hacia el objetivo (solo en horizontal)
        if (hasAimPoint)
        {
            Vector3 flat = aimPoint - transform.position;
            flat.y = 0f;
            if (flat.sqrMagnitude > 0.01f)
                transform.rotation = Quaternion.LookRotation(flat);
        }
        
        bool overUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        bool clickIsForDice = diceDragController.IsDragging || diceDragController.PressConsumed;
        if(gameInput.Gameplay.Disparo.WasPressedThisFrame() && !overUI && !clickIsForDice)
        {
            OnShoot();
        }
    }

    private void OnShoot()
    {
        if (!hasAimPoint || diceRb == null) return;

        Rigidbody rb = diceRb;
        Dice shot = ReleaseLoadedDice();
        diceRb = null;
        
        rb.isKinematic = false;
        Vector3 velocity = (aimPoint - rb.position).normalized * shotSpeed;

        rb.AddForce(velocity, ForceMode.VelocityChange);
        rb.AddTorque(Random.insideUnitSphere * 10f, ForceMode.VelocityChange);

        OnDiceShot?.Invoke(shot);
    }

    public Dice UnloadDice()
    {
        Dice dice = ReleaseLoadedDice();
        diceRb = null;
        return dice;
    }

    private Dice ReleaseLoadedDice()
    {
        Dice dice = _loadedDice;
        if(dice == null) return null;

        dice.transform.DOKill();
        dice.transform.SetParent(null, true);
        SetLayer(dice.gameObject, _loadedDiceLayer);
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
        SetLayer(dice.gameObject, LayerMask.NameToLayer("Ignore Raycast"));
        
        dice.transform.SetParent(_holdPoint, true);
        dice.transform.DOLocalMove(Vector3.zero, 0.2f).SetEase(Ease.OutBack);
        dice.transform.DOLocalRotateQuaternion(Quaternion.identity, 0.2f).SetEase(Ease.OutBack);
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
    
    private static void SetLayer(GameObject obj, int layer)
    {
        obj.layer = layer;
        foreach (Transform child in obj.GetComponentsInChildren<Transform>(true))
        {
            child.gameObject.layer = layer;
        }
    }
}