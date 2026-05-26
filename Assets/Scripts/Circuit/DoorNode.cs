using UnityEngine;

public class DoorNode : CircuitNode
{
    [Header("Door")]
    [SerializeField] private Transform doorBody;
    [SerializeField] private Collider blockingCollider;
    [SerializeField] private Vector3 hingeLocalOffset = new Vector3(-0.5f, 0f, 0f);
    [SerializeField] private float openAngle = 90f;
    [SerializeField] private float openSpeed = 180f;
    [SerializeField] private bool disableColliderWhenOpen = false;

    [Header("Visual")]
    [SerializeField] private Color poweredColor = Color.cyan;
    [SerializeField] private Color unpoweredColor = Color.red;

    private Vector3 closedPosition;
    private Quaternion closedRotation;
    private float currentAngle;
    private float targetAngle;
    private Renderer doorRenderer;

    private void Awake()
    {
        if (doorBody == null)
        {
            doorBody = transform;
        }

        if (blockingCollider == null)
        {
            blockingCollider = doorBody.GetComponent<Collider>();
        }

        doorRenderer = doorBody.GetComponent<Renderer>();
        closedPosition = doorBody.position;
        closedRotation = doorBody.rotation;
    }

    protected override void Start()
    {
        nodeType = NodeType.Door;
        base.Start();
    }

    private void Update()
    {
        float nextAngle = Mathf.MoveTowards(currentAngle, targetAngle, openSpeed * Time.deltaTime);
        ApplyAngle(nextAngle);
    }

    public override void OnPowerChanged(bool powered)
    {
        base.OnPowerChanged(powered);

        targetAngle = powered ? openAngle : 0f;

        if (disableColliderWhenOpen && blockingCollider != null)
        {
            blockingCollider.enabled = !powered;
        }

        if (doorRenderer != null)
        {
            doorRenderer.material.color = powered ? poweredColor : unpoweredColor;
        }
    }

    private void ApplyAngle(float angle)
    {
        currentAngle = angle;

        Vector3 hingeWorldPosition = closedPosition + closedRotation * hingeLocalOffset;
        Quaternion rotationOffset = Quaternion.AngleAxis(currentAngle, Vector3.up);

        doorBody.rotation = rotationOffset * closedRotation;
        doorBody.position = hingeWorldPosition + rotationOffset * (closedPosition - hingeWorldPosition);
    }
}
