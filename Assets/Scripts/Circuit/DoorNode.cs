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
    private GameObject runtimeDoorBodyObject;
    private bool hasInitializedPowerState;

    private void Awake()
    {
        if (doorBody == null)
        {
            doorBody = CreateRuntimeDoorBody();
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

    protected override void OnDestroy()
    {
        base.OnDestroy();

        if (runtimeDoorBodyObject != null)
        {
            if (Application.isPlaying)
            {
                Destroy(runtimeDoorBodyObject);
            }
            else
            {
                DestroyImmediate(runtimeDoorBodyObject);
            }
        }
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

        if (hasInitializedPowerState)
        {
            GameSfx.PlayAt("sfx_door_open_lowpoly", transform.position, 0.8f);
        }

        hasInitializedPowerState = true;
    }

    private void ApplyAngle(float angle)
    {
        currentAngle = angle;

        Vector3 hingeWorldPosition = closedPosition + closedRotation * hingeLocalOffset;
        Quaternion rotationOffset = Quaternion.AngleAxis(currentAngle, Vector3.up);

        doorBody.rotation = rotationOffset * closedRotation;
        doorBody.position = hingeWorldPosition + rotationOffset * (closedPosition - hingeWorldPosition);
    }

    private Transform CreateRuntimeDoorBody()
    {
        runtimeDoorBodyObject = new GameObject("DoorBody");
        runtimeDoorBodyObject.layer = gameObject.layer;
        runtimeDoorBodyObject.transform.SetParent(transform.parent, false);
        runtimeDoorBodyObject.transform.SetPositionAndRotation(transform.position, transform.rotation);
        runtimeDoorBodyObject.transform.localScale = transform.localScale;

        MeshFilter rootMeshFilter = GetComponent<MeshFilter>();
        if (rootMeshFilter != null)
        {
            MeshFilter bodyMeshFilter = runtimeDoorBodyObject.AddComponent<MeshFilter>();
            bodyMeshFilter.sharedMesh = rootMeshFilter.sharedMesh;
        }

        Renderer rootRenderer = GetComponent<Renderer>();
        if (rootRenderer != null)
        {
            MeshRenderer bodyRenderer = runtimeDoorBodyObject.AddComponent<MeshRenderer>();
            bodyRenderer.sharedMaterials = rootRenderer.sharedMaterials;
            rootRenderer.enabled = false;
        }

        Collider rootCollider = GetComponent<Collider>();
        if (rootCollider != null)
        {
            blockingCollider = CopyCollider(rootCollider, runtimeDoorBodyObject);
            rootCollider.enabled = false;
        }

        return runtimeDoorBodyObject.transform;
    }

    private Collider CopyCollider(Collider source, GameObject target)
    {
        if (source is BoxCollider sourceBox)
        {
            BoxCollider targetBox = target.AddComponent<BoxCollider>();
            targetBox.center = sourceBox.center;
            targetBox.size = sourceBox.size;
            CopyColliderSettings(sourceBox, targetBox);
            return targetBox;
        }

        if (source is MeshCollider sourceMesh)
        {
            MeshCollider targetMesh = target.AddComponent<MeshCollider>();
            targetMesh.sharedMesh = sourceMesh.sharedMesh;
            targetMesh.convex = sourceMesh.convex;
            CopyColliderSettings(sourceMesh, targetMesh);
            return targetMesh;
        }

        if (source is CapsuleCollider sourceCapsule)
        {
            CapsuleCollider targetCapsule = target.AddComponent<CapsuleCollider>();
            targetCapsule.center = sourceCapsule.center;
            targetCapsule.radius = sourceCapsule.radius;
            targetCapsule.height = sourceCapsule.height;
            targetCapsule.direction = sourceCapsule.direction;
            CopyColliderSettings(sourceCapsule, targetCapsule);
            return targetCapsule;
        }

        return null;
    }

    private void CopyColliderSettings(Collider source, Collider target)
    {
        target.sharedMaterial = source.sharedMaterial;
        target.isTrigger = source.isTrigger;
    }
}
