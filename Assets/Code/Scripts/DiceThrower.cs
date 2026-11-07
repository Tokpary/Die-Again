using Code.Scripts.Dice;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class DiceThrower : MonoBehaviour
{
    private Rigidbody diceRb;
    [SerializeField] private Camera cam;
    [SerializeField] private LayerMask aimMask = ~0;    
    [SerializeField] private float shotSpeed = 15f;

    private GameInputs gameInput;
    private Vector3 aimPoint;
    private bool hasAimPoint;

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
        if(gameInput.Gameplay.Disparo.WasPressedThisFrame() && !overUI)
        {
            OnShoot(new InputAction.CallbackContext());
        }
    }

    private void OnShoot(InputAction.CallbackContext ctx)
    {
        if (!hasAimPoint) return;

        diceRb.isKinematic = false;
        Vector3 velocity = (aimPoint - diceRb.position).normalized * shotSpeed;

        diceRb.AddForce(velocity, ForceMode.VelocityChange);
        diceRb.AddTorque(Random.insideUnitSphere * 10f, ForceMode.VelocityChange); // que ruede como un dado
    }


    public void LoadDice(Dice dice)
    {
        if (diceRb != null) Destroy(diceRb.gameObject);
        diceRb = Instantiate(dice, transform.position + transform.forward * 1.5f, Quaternion.identity).GetComponent<Rigidbody>();
        diceRb.isKinematic = true;
        diceRb.position = transform.position + transform.forward * 1.5f + Vector3.up * -0.5f;
        diceRb.rotation = Quaternion.identity;
    }

    private void OnDrawGizmos()
    {
        if (!hasAimPoint) return;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(aimPoint, 0.3f);
    }

    public void Init(GameInputs gameInputs)
    {
        gameInput = gameInputs;
    }
}