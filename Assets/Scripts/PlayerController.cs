using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour, PlayerAction.IPlayerActions
{
    [SerializeField]
    private float moveSpeed = 5f;

    [SerializeField]
    private float grabDistance = 2f;

    [SerializeField]
    private LayerMask grabbableLayers = ~0;

    [SerializeField]
    private Transform grabPoint;

    private PlayerAction playerAction;
    private Vector2 moveInput;
    private Rigidbody heldRigidbody;

    private void Awake()
    {
        playerAction = new PlayerAction();
        playerAction.Player.SetCallbacks(this);

        if (grabPoint == null)
        {
            var grabPointObject = new GameObject("GrabPoint");
            grabPointObject.transform.SetParent(transform, false);
            grabPointObject.transform.localPosition = new Vector3(0f, 0f, 1f);
            grabPoint = grabPointObject.transform;
        }
    }

    private void OnEnable()
    {
        playerAction.Enable();
    }

    private void OnDisable()
    {
        playerAction.Disable();
    }

    private void Update()
    {
        var move = new Vector3(moveInput.x, 0f, moveInput.y);
        if (move.sqrMagnitude > 0f)
        {
            transform.rotation = Quaternion.LookRotation(move, Vector3.up);
        }

        transform.position += move * moveSpeed * Time.deltaTime;
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }

    public void OnCatch(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            TryGrab();
        }
        else if (context.canceled)
        {
            Release();
        }
    }

    private void TryGrab()
    {
        if (heldRigidbody != null)
        {
            return;
        }

        var origin = transform.position;
        var direction = transform.forward;

        if (!Physics.Raycast(origin, direction, out var hit, grabDistance, grabbableLayers))
        {
            return;
        }

        var target = hit.rigidbody;
        if (target == null)
        {
            return;
        }

        heldRigidbody = target;
        heldRigidbody.isKinematic = true;
        heldRigidbody.transform.SetParent(grabPoint, true);
        heldRigidbody.transform.localPosition = Vector3.zero;
        heldRigidbody.transform.localRotation = Quaternion.identity;
    }

    private void Release()
    {
        if (heldRigidbody == null)
        {
            return;
        }

        heldRigidbody.transform.SetParent(null, true);
        heldRigidbody.isKinematic = false;
        heldRigidbody = null;
    }
}
