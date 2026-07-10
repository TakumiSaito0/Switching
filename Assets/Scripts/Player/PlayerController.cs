using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour, PlayerAction.IPlayerActions
{
    private const float DefaultMaxPlaceGroundRise = 0.75f;

    [SerializeField]
    private float moveSpeed = 5f;

    [SerializeField]
    private float climbSpeed = 3f;

    [SerializeField]
    private float grabDistance = 2f;

    [SerializeField]
    private float releaseDistance = 1.15f;

    [SerializeField]
    private LayerMask grabbableLayers = ~0;

    [SerializeField]
    private Transform grabPoint;

    [Header("Action Settings")]
    public float interactDistance = 2.0f;
    public float placeDistance = 1.2f;
    public float placeHeightOffset = 0.5f;
    public float maxPlaceGroundRise = 0.75f;
    public float maxPlaceGroundDrop = 1.5f;
    public LayerMask placeGroundLayers = ~0;
    [SerializeField] private string placeableSurfaceTag = "Ground";
    public GameObject wirePrefab;
    [SerializeField] private string[] blockedPlacementSurfaceNames = { "Stairs", "Step" };
    [SerializeField] private int startingCircuitCount = 0;
    [SerializeField] private int currentCircuitCount = 0;

    [Header("Circuit Placement Preview")]
    [SerializeField] private Color placePreviewColor = new Color(0.2f, 1f, 0.45f, 0.7f);
    [SerializeField] private Color removePreviewColor = new Color(1f, 0.55f, 0.15f, 0.7f);
    [SerializeField] private Color invalidPreviewColor = new Color(1f, 0.15f, 0.15f, 0.55f);
    [SerializeField] private float previewBlinkSpeed = 6f;

    [HideInInspector]
    public bool isClimbing = false;
    [HideInInspector]
    public bool isGrounded = false;

    private PlayerAction playerAction;
    private Vector2 moveInput;
    private Rigidbody heldRigidbody;
    private RigidbodyConstraints heldOriginalConstraints;
    private Rigidbody rb;
    private GameObject circuitPlacementPreview;
    private Material[] circuitPlacementPreviewMaterials;

    public bool IsHoldingObject => heldRigidbody != null;
    public int CurrentCircuitCount => currentCircuitCount;

    public void AddCircuits(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        currentCircuitCount += amount;
    }

    private Vector3? lastTargetGridPos = null;
    private bool isCircuitPlacementMode = false;
    private bool isCircuitPlacementStrokeActive = false;
    private bool isPlacingMode = false;
    private bool isDeletingMode = false;

    private struct CircuitPlacementTarget
    {
        public Vector3 GridPosition;
        public Vector3 PlacePosition;
        public WireNode ExistingWire;
        public bool CanPlace;
        public bool CanRemove;
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        currentCircuitCount = Mathf.Max(0, startingCircuitCount);

        EnsurePlayerAction();

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
        EnsurePlayerAction();
        playerAction.Enable();
    }

    private void OnDisable()
    {
        isCircuitPlacementMode = false;
        EndCircuitPlacementStroke();
        HideCircuitPlacementPreview();
        playerAction?.Disable();
    }

    private void OnDestroy()
    {
        DestroyCircuitPlacementPreview();
    }

    private void EnsurePlayerAction()
    {
        if (playerAction != null)
        {
            return;
        }

        playerAction = new PlayerAction();
        playerAction.Player.SetCallbacks(this);
    }

    private void Update()
    {
        PlaceCircuit();

        // Calculate movement relative to the active camera.
        Vector3 move = GetCameraRelativeMove(moveInput);

        // Movement while attached to a ladder.
        if (isClimbing)
        {
            // Split camera-relative movement into forward climb intent and horizontal ladder movement.
            // Positive climb intent climbs up; negative intent climbs down.
            float climbIntent = Vector3.Dot(move, transform.forward);

            // Horizontal input slides the player sideways while staying on the ladder.
            float horizontalIntent = Vector3.Dot(move, transform.right);

            // Vertical climb movement.
            Vector3 verticalMove = new Vector3(0, climbIntent, 0) * climbSpeed * Time.deltaTime;

            // Sideways movement along the ladder.
            Vector3 horizontalMove = transform.right * horizontalIntent * climbSpeed * Time.deltaTime;

            if (isGrounded && climbIntent < -0.01f)
            {
                // Step down when the player is grounded and moves away from the ladder.
                Vector3 stepDownMove = new Vector3(0, -climbSpeed * 1.5f, 0) * Time.deltaTime;
                transform.position += stepDownMove + horizontalMove;
            }
            else
            {
                // Normal ladder climbing and top-out movement.
                Vector3 climbForwardMove = Vector3.zero;

                if (climbIntent > 0.01f) // The player is trying to climb up.
                {
                    Vector3 rayOrigin = transform.position + Vector3.up * 0.8f;
                    bool hasWallAhead = Physics.Raycast(rayOrigin, transform.forward, 1.0f);
                    bool hasWallOverlap = Physics.CheckSphere(rayOrigin + transform.forward * 0.5f, 0.2f);

                    // Move forward after reaching the top when there is no wall ahead.
                    if (!hasWallAhead && !hasWallOverlap)
                    {
                        climbForwardMove = transform.forward * climbSpeed * Time.deltaTime;
                    }
                }

                transform.position += verticalMove + horizontalMove + climbForwardMove;
            }
            return;
        }

        // Normal ground movement.
        if (move.sqrMagnitude > 0f)
        {
            // While dragging circuits, keep facing direction fixed and only strafe.
            if (Mouse.current == null || !Mouse.current.rightButton.isPressed)
            {
                transform.rotation = Quaternion.LookRotation(move, Vector3.up);
            }
        }

        transform.position += move * moveSpeed * Time.deltaTime;
    }

    private Vector3 GetCameraRelativeMove(Vector2 input)
    {
        Vector3 fallbackMove = new Vector3(input.x, 0f, input.y);
        if (fallbackMove.sqrMagnitude <= 0f)
        {
            return Vector3.zero;
        }

        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            return Vector3.ClampMagnitude(fallbackMove, 1f);
        }

        Vector3 cameraForward = mainCamera.transform.forward;
        cameraForward.y = 0f;

        Vector3 cameraRight = mainCamera.transform.right;
        cameraRight.y = 0f;

        if (cameraForward.sqrMagnitude <= 0.001f || cameraRight.sqrMagnitude <= 0.001f)
        {
            return Vector3.ClampMagnitude(fallbackMove, 1f);
        }

        cameraForward.Normalize();
        cameraRight.Normalize();

        Vector3 cameraRelativeMove = cameraRight * input.x + cameraForward * input.y;
        return Vector3.ClampMagnitude(cameraRelativeMove, 1f);
    }


    private bool TryInteractSwitchOrButton()
    {
        Vector3 boxCenter = transform.position + transform.forward * (interactDistance / 2f) + Vector3.up * 0.5f;
        Vector3 boxHalfExtents = new Vector3(0.5f, 1.0f, interactDistance / 2f);

        Collider[] colliders = Physics.OverlapBox(boxCenter, boxHalfExtents, transform.rotation);
        foreach (Collider hit in colliders)
        {
            ElevatorButton elevatorButton = hit.GetComponent<ElevatorButton>();
            if (elevatorButton != null)
            {
                elevatorButton.PressButton();
                Debug.Log("Elevator button pressed: " + hit.gameObject.name);
                return true;
            }

            SwitchNode switchNode = hit.GetComponent<SwitchNode>();
            if (switchNode != null)
            {
                switchNode.ToggleSwitch();
                Debug.Log("Switch toggled: " + hit.gameObject.name);
                return true;
            }
        }

        Debug.Log("No interactable found in front of the player.");
        return false;
    }

    // Right click: place or remove a circuit wire on the ground.
    void PlaceCircuit()
    {
        // Do not place or remove circuits while carrying an object.
        if (IsHoldingObject || wirePrefab == null || Mouse.current == null)
        {
            ExitCircuitPlacementMode();
            return;
        }

        if (!isCircuitPlacementMode)
        {
            if (Mouse.current.rightButton.wasPressedThisFrame)
            {
                EnterCircuitPlacementMode();
            }

            return;
        }

        if (ShouldCancelCircuitPlacement())
        {
            ExitCircuitPlacementMode();
            return;
        }

        UpdateCircuitPlacementPreview();

        if (Mouse.current.rightButton.wasPressedThisFrame)
        {
            BeginCircuitPlacementStroke();
        }

        if (!Mouse.current.rightButton.isPressed)
        {
            EndCircuitPlacementStroke();
            return;
        }

        ApplyCircuitPlacementStroke();
    }

    private void EnterCircuitPlacementMode()
    {
        isCircuitPlacementMode = true;
        EndCircuitPlacementStroke();
        UpdateCircuitPlacementPreview();
    }

    private void ExitCircuitPlacementMode()
    {
        isCircuitPlacementMode = false;
        EndCircuitPlacementStroke();
        HideCircuitPlacementPreview();
    }

    private void BeginCircuitPlacementStroke()
    {
        Vector3 gridPos = GetTargetGridPosition();
        WireNode targetWire = FindWireAtGridPosition(gridPos);
        isDeletingMode = targetWire != null;
        isPlacingMode = targetWire == null;
        isCircuitPlacementStrokeActive = true;
        lastTargetGridPos = null;
    }

    private void EndCircuitPlacementStroke()
    {
        lastTargetGridPos = null;
        isCircuitPlacementStrokeActive = false;
        isPlacingMode = false;
        isDeletingMode = false;
    }

    private bool ShouldCancelCircuitPlacement()
    {
        if (Mouse.current != null
            && (Mouse.current.leftButton.wasPressedThisFrame || Mouse.current.middleButton.wasPressedThisFrame))
        {
            return true;
        }

        Keyboard keyboard = Keyboard.current;
        if (keyboard == null || !keyboard.anyKey.wasPressedThisFrame)
        {
            return false;
        }

        foreach (var key in keyboard.allKeys)
        {
            if (key.wasPressedThisFrame && !IsMovementKey(key.keyCode))
            {
                return true;
            }
        }

        return false;
    }

    private bool IsMovementKey(Key key)
    {
        return key == Key.W
            || key == Key.A
            || key == Key.S
            || key == Key.D
            || key == Key.UpArrow
            || key == Key.DownArrow
            || key == Key.LeftArrow
            || key == Key.RightArrow;
    }

    private Vector3 GetTargetGridPosition()
    {
        Vector3 frontPos = transform.position + transform.forward * placeDistance;
        float gridX = Mathf.Round(frontPos.x);
        float gridZ = Mathf.Round(frontPos.z);
        return new Vector3(gridX, transform.position.y, gridZ);
    }

    private void ApplyCircuitPlacementStroke()
    {
        if (!isCircuitPlacementStrokeActive)
        {
            return;
        }

        CircuitPlacementTarget target = GetCircuitPlacementTarget();
        if (lastTargetGridPos.HasValue && lastTargetGridPos.Value == target.GridPosition)
        {
            return;
        }

        if (isDeletingMode)
        {
            if (target.CanRemove)
            {
                Destroy(target.ExistingWire.gameObject);
                AddCircuits(1);
                lastTargetGridPos = target.GridPosition;
                GameSfx.PlayAt("sfx_circuit_remove_lowpoly", target.GridPosition);
                Invoke(nameof(RefreshAllCircuits), 0.05f);
            }

            return;
        }

        if (!isPlacingMode || !target.CanPlace)
        {
            return;
        }

        GameObject placedCircuit = Instantiate(wirePrefab, target.PlacePosition, Quaternion.identity);
        SnapBottomToGround(placedCircuit, target.PlacePosition.y);
        currentCircuitCount--;
        lastTargetGridPos = target.GridPosition;
        GameSfx.PlayAt("sfx_circuit_place", target.PlacePosition);
        Invoke(nameof(RefreshAllCircuits), 0.05f);
        UpdateCircuitPlacementPreview();
    }

    private void UpdateCircuitPlacementPreview()
    {
        CircuitPlacementTarget target = GetCircuitPlacementTarget();

        EnsureCircuitPlacementPreview();
        circuitPlacementPreview.SetActive(true);
        circuitPlacementPreview.transform.position = target.PlacePosition + Vector3.up * 0.03f;

        Color previewColor = target.CanRemove ? removePreviewColor : target.CanPlace ? placePreviewColor : invalidPreviewColor;
        float blink = (Mathf.Sin(Time.time * previewBlinkSpeed) + 1f) * 0.5f;
        previewColor.a *= Mathf.Lerp(0.25f, 1f, blink);
        SetCircuitPlacementPreviewColor(previewColor);
    }

    private CircuitPlacementTarget GetCircuitPlacementTarget()
    {
        Vector3 gridPos = GetTargetGridPosition();
        WireNode existingWire = FindWireAtGridPosition(gridPos);
        bool hasValidGround = TryGetGroundedPlacePosition(gridPos.x, gridPos.z, out Vector3 placePos);
        bool hasBlockedSurface = IsBlockedPlacementArea(gridPos.x, gridPos.z, placePos.y);

        bool canPlace = hasValidGround
            && !hasBlockedSurface
            && currentCircuitCount > 0
            && existingWire == null
            && FindCircuitAtGridPosition(gridPos) == null
            && !IsCircuitPlaceBlocked(placePos);

        return new CircuitPlacementTarget
        {
            GridPosition = gridPos,
            PlacePosition = placePos,
            ExistingWire = existingWire,
            CanPlace = canPlace,
            CanRemove = existingWire != null
        };
    }

    private void EnsureCircuitPlacementPreview()
    {
        if (circuitPlacementPreview != null)
        {
            return;
        }

        circuitPlacementPreview = Instantiate(wirePrefab);
        circuitPlacementPreview.name = "CircuitPlacementPreview";
        circuitPlacementPreview.hideFlags = HideFlags.DontSave;

        foreach (WireNode previewWire in circuitPlacementPreview.GetComponentsInChildren<WireNode>())
        {
            previewWire.RebuildPreviewVisuals();
        }

        foreach (Transform child in circuitPlacementPreview.GetComponentsInChildren<Transform>())
        {
            child.gameObject.hideFlags = HideFlags.DontSave;
        }

        foreach (Collider previewCollider in circuitPlacementPreview.GetComponentsInChildren<Collider>())
        {
            previewCollider.enabled = false;
        }

        foreach (CircuitNode previewNode in circuitPlacementPreview.GetComponentsInChildren<CircuitNode>())
        {
            previewNode.enabled = false;
        }

        Renderer[] previewRenderers = circuitPlacementPreview.GetComponentsInChildren<Renderer>();
        circuitPlacementPreviewMaterials = new Material[previewRenderers.Length];
        for (int i = 0; i < previewRenderers.Length; i++)
        {
            circuitPlacementPreviewMaterials[i] = new Material(FindCircuitPreviewShader());
            ConfigureTransparentMaterial(circuitPlacementPreviewMaterials[i]);
            previewRenderers[i].material = circuitPlacementPreviewMaterials[i];
        }

        HideCircuitPlacementPreview();
    }

    private void HideCircuitPlacementPreview()
    {
        if (circuitPlacementPreview != null)
        {
            circuitPlacementPreview.SetActive(false);
        }
    }

    private void DestroyCircuitPlacementPreview()
    {
        if (circuitPlacementPreview != null)
        {
            Destroy(circuitPlacementPreview);
            circuitPlacementPreview = null;
        }

        if (circuitPlacementPreviewMaterials != null)
        {
            foreach (Material previewMaterial in circuitPlacementPreviewMaterials)
            {
                if (previewMaterial != null)
                {
                    Destroy(previewMaterial);
                }
            }

            circuitPlacementPreviewMaterials = null;
        }
    }

    private void SetCircuitPlacementPreviewColor(Color color)
    {
        if (circuitPlacementPreviewMaterials == null)
        {
            return;
        }

        foreach (Material previewMaterial in circuitPlacementPreviewMaterials)
        {
            if (previewMaterial != null)
            {
                previewMaterial.color = color;
            }
        }
    }

    private void ConfigureTransparentMaterial(Material material)
    {
        material.SetFloat("_Mode", 3f);
        material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        material.SetInt("_ZWrite", 0);
        material.DisableKeyword("_ALPHATEST_ON");
        material.EnableKeyword("_ALPHABLEND_ON");
        material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        material.renderQueue = 3000;
    }

    private Shader FindCircuitPreviewShader()
    {
        Shader shader = Shader.Find("Standard");
        if (shader != null)
        {
            return shader;
        }

        shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader != null)
        {
            return shader;
        }

        return Shader.Find("Sprites/Default");
    }

    private bool IsCircuitPlaceBlocked(Vector3 placePos)
    {
        Collider[] colliders = Physics.OverlapSphere(placePos, 0.4f);
        foreach (Collider col in colliders)
        {
            if (col.CompareTag("Player"))
            {
                return true;
            }
        }

        return false;
    }

    private WireNode FindWireAtGridPosition(Vector3 gridPos)
    {
        Vector3 boxCenter = new Vector3(gridPos.x, gridPos.y + 1f, gridPos.z);
        Collider[] colliders = Physics.OverlapBox(boxCenter, new Vector3(0.45f, 3f, 0.45f));

        foreach (Collider col in colliders)
        {
            WireNode wire = col.GetComponentInParent<WireNode>();
            if (wire != null)
            {
                return wire;
            }
        }

        return null;
    }

    private CircuitNode FindCircuitAtGridPosition(Vector3 gridPos)
    {
        if (CircuitManager.Instance == null)
        {
            return null;
        }

        foreach (CircuitNode node in CircuitManager.Instance.allNodes)
        {
            if (node == null)
            {
                continue;
            }

            if (Mathf.Round(node.transform.position.x) == Mathf.Round(gridPos.x)
                && Mathf.Round(node.transform.position.z) == Mathf.Round(gridPos.z))
            {
                return node;
            }
        }

        return null;
    }

    private bool TryGetGroundedPlacePosition(float gridX, float gridZ, out Vector3 placePos)
    {
        Vector3 rayOrigin = new Vector3(gridX, transform.position.y + 5f, gridZ);
        RaycastHit[] hits = Physics.RaycastAll(rayOrigin, Vector3.down, 20f, placeGroundLayers, QueryTriggerInteraction.Ignore);

        float currentGroundY = GetCurrentGroundY();
        float lowestAllowedGroundY = currentGroundY - maxPlaceGroundDrop;
        float effectiveMaxPlaceGroundRise = maxPlaceGroundRise > 0f ? maxPlaceGroundRise : DefaultMaxPlaceGroundRise;
        float highestAllowedGroundY = currentGroundY + effectiveMaxPlaceGroundRise;
        RaycastHit bestHit = default;
        bool foundCandidate = false;

        foreach (RaycastHit hit in hits)
        {
            if (hit.collider.GetComponentInParent<CircuitNode>() != null || hit.collider.CompareTag("Player"))
            {
                continue;
            }

            if (!IsPlaceableSurface(hit.collider))
            {
                continue;
            }

            if (hit.point.y < lowestAllowedGroundY || hit.point.y > highestAllowedGroundY)
            {
                continue;
            }

            if (IsBlockedPlacementSurface(hit.collider))
            {
                placePos = new Vector3(gridX, hit.point.y, gridZ);
                return false;
            }

            if (!foundCandidate || hit.point.y > bestHit.point.y)
            {
                bestHit = hit;
                foundCandidate = true;
            }
        }

        if (foundCandidate)
        {
            placePos = new Vector3(gridX, bestHit.point.y, gridZ);
            return !IsBlockedPlacementSurface(bestHit.collider);
        }

        placePos = new Vector3(gridX, placeHeightOffset, gridZ);
        return false;
    }

    private bool IsBlockedPlacementArea(float gridX, float gridZ, float groundY)
    {
        Vector3 boxCenter = new Vector3(gridX, groundY + 0.6f, gridZ);
        Vector3 halfExtents = new Vector3(0.48f, 1.2f, 0.48f);
        Collider[] colliders = Physics.OverlapBox(boxCenter, halfExtents, Quaternion.identity, placeGroundLayers, QueryTriggerInteraction.Ignore);

        foreach (Collider col in colliders)
        {
            if (IsBlockedPlacementSurface(col))
            {
                return true;
            }
        }

        return false;
    }

    private bool IsBlockedPlacementSurface(Collider surfaceCollider)
    {
        if (surfaceCollider == null || blockedPlacementSurfaceNames == null)
        {
            return false;
        }

        if (surfaceCollider.GetComponentInParent<PlacementBlockedSurface>() != null)
        {
            return true;
        }

        Transform current = surfaceCollider.transform;
        while (current != null)
        {
            foreach (string blockedName in blockedPlacementSurfaceNames)
            {
                if (!string.IsNullOrWhiteSpace(blockedName)
                    && current.name.IndexOf(blockedName, System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }

            current = current.parent;
        }

        return false;
    }

    private bool IsPlaceableSurface(Collider surfaceCollider)
    {
        if (surfaceCollider == null)
        {
            return false;
        }

        return string.IsNullOrWhiteSpace(placeableSurfaceTag) || surfaceCollider.CompareTag(placeableSurfaceTag);
    }

    private float GetCurrentGroundY()
    {
        Vector3 rayOrigin = transform.position + Vector3.up * 0.5f;
        RaycastHit[] hits = Physics.RaycastAll(rayOrigin, Vector3.down, 4f, placeGroundLayers, QueryTriggerInteraction.Ignore);

        float bestGroundY = float.NegativeInfinity;
        bool foundGround = false;

        foreach (RaycastHit hit in hits)
        {
            if (hit.collider.GetComponentInParent<CircuitNode>() != null || hit.collider.CompareTag("Player"))
            {
                continue;
            }

            if (!IsPlaceableSurface(hit.collider))
            {
                continue;
            }

            if (hit.point.y > bestGroundY)
            {
                bestGroundY = hit.point.y;
                foundGround = true;
            }
        }

        return foundGround ? bestGroundY : transform.position.y;
    }

    private void SnapBottomToGround(GameObject placedObject, float groundY)
    {
        Physics.SyncTransforms();

        Bounds? objectBounds = GetObjectBounds(placedObject);
        if (!objectBounds.HasValue)
        {
            return;
        }

        float yOffset = groundY - objectBounds.Value.min.y;
        placedObject.transform.position += Vector3.up * yOffset;
    }

    private Bounds? GetObjectBounds(GameObject target)
    {
        Bounds? combinedBounds = null;

        foreach (Collider col in target.GetComponentsInChildren<Collider>())
        {
            if (col == null || !col.enabled)
            {
                continue;
            }

            combinedBounds = EncapsulateBounds(combinedBounds, col.bounds);
        }

        if (combinedBounds.HasValue)
        {
            return combinedBounds;
        }

        foreach (Renderer renderer in target.GetComponentsInChildren<Renderer>())
        {
            if (renderer == null || !renderer.enabled)
            {
                continue;
            }

            combinedBounds = EncapsulateBounds(combinedBounds, renderer.bounds);
        }

        return combinedBounds;
    }

    private Bounds? EncapsulateBounds(Bounds? combinedBounds, Bounds bounds)
    {
        if (!combinedBounds.HasValue)
        {
            return bounds;
        }

        Bounds expandedBounds = combinedBounds.Value;
        expandedBounds.Encapsulate(bounds);
        return expandedBounds;
    }

    void RefreshAllCircuits()
    {
        if (CircuitManager.Instance == null) return;

        foreach (var node in CircuitManager.Instance.allNodes)
        {
            if (node != null)
            {
                node.ConnectToNeighbors();
            }
        }
        CircuitManager.Instance.RecalculatePower();
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }

    public void OnCatch(InputAction.CallbackContext context)
    {
        if (isCircuitPlacementMode)
        {
            if (context.started)
            {
                ExitCircuitPlacementMode();
            }

            return;
        }

        if (context.started)
        {
            if (heldRigidbody != null)
            {
                Release();
                return;
            }

            if (TryGrab())
            {
                return;
            }

            TryInteractSwitchOrButton();
        }
    }

    private bool TryGrab()
    {
        if (heldRigidbody != null)
        {
            return false;
        }

        var origin = transform.position;
        var direction = transform.forward;

        if (!Physics.Raycast(origin, direction, out var hit, grabDistance, grabbableLayers))
        {
            return false;
        }

        var target = hit.rigidbody;
        if (target == null || !target.CompareTag("Box"))
        {
            return false;
        }

        heldRigidbody = target;
        heldOriginalConstraints = heldRigidbody.constraints;
        heldRigidbody.constraints = RigidbodyConstraints.None;
        heldRigidbody.isKinematic = true;
        heldRigidbody.transform.SetParent(grabPoint, true);
        heldRigidbody.transform.localPosition = Vector3.zero;
        heldRigidbody.transform.localRotation = Quaternion.identity;
        GameSfx.PlayAt("sfx_box_pickup_lowpoly", heldRigidbody.position);

        if (isClimbing)
        {
            StopClimbing();
        }

        return true;
    }

    private void Release()
    {
        if (heldRigidbody == null)
        {
            return;
        }

        Rigidbody releasedRigidbody = heldRigidbody;
        releasedRigidbody.transform.SetParent(null, true);
        if (!MoveHeldObjectToSafeReleasePosition(releasedRigidbody))
        {
            releasedRigidbody.transform.SetParent(grabPoint, true);
            releasedRigidbody.transform.localPosition = Vector3.zero;
            releasedRigidbody.transform.localRotation = Quaternion.identity;
            return;
        }

        releasedRigidbody.isKinematic = false;
        releasedRigidbody.constraints = heldOriginalConstraints;
        releasedRigidbody.linearVelocity = Vector3.zero;
        releasedRigidbody.angularVelocity = Vector3.zero;
        GameSfx.PlayAt("sfx_box_drop_lowpoly", releasedRigidbody.position);
        heldRigidbody = null;
    }

    private bool MoveHeldObjectToSafeReleasePosition(Rigidbody targetRigidbody)
    {
        if (targetRigidbody == null)
        {
            return false;
        }

        Bounds? bounds = GetObjectBounds(targetRigidbody.gameObject);
        float objectHalfHeight = bounds.HasValue ? Mathf.Max(0.05f, bounds.Value.extents.y) : 0.5f;
        float objectRadius = bounds.HasValue
            ? Mathf.Max(Mathf.Max(bounds.Value.extents.x, bounds.Value.extents.z), 0.35f)
            : 0.5f;

        Vector3[] candidateCenters =
        {
            transform.position + transform.forward * releaseDistance,
            transform.position + transform.forward * (releaseDistance + objectRadius),
            transform.position + transform.forward * 0.6f,
            transform.position + transform.right * objectRadius,
            transform.position - transform.right * objectRadius
        };

        foreach (Vector3 candidateCenter in candidateCenters)
        {
            if (TryGetSafeReleasePosition(candidateCenter, objectRadius, objectHalfHeight, targetRigidbody, out Vector3 safePosition))
            {
                targetRigidbody.transform.position = safePosition;
                Physics.SyncTransforms();
                return true;
            }
        }

        return false;
    }

    private bool TryGetSafeReleasePosition(
        Vector3 candidateCenter,
        float objectRadius,
        float objectHalfHeight,
        Rigidbody targetRigidbody,
        out Vector3 safePosition)
    {
        safePosition = candidateCenter;

        Vector3 rayOrigin = candidateCenter + Vector3.up * 2f;
        if (!Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit groundHit, 5f, placeGroundLayers, QueryTriggerInteraction.Ignore))
        {
            return false;
        }

        if (groundHit.collider.GetComponentInParent<CircuitNode>() != null
            || groundHit.collider.CompareTag("Player")
            || !IsBoxReleaseSurface(groundHit.collider)
            || IsBlockedPlacementSurface(groundHit.collider))
        {
            return false;
        }

        Vector3 safeCenter = new Vector3(candidateCenter.x, groundHit.point.y + objectHalfHeight + 0.03f, candidateCenter.z);
        Vector3 halfExtents = new Vector3(objectRadius * 0.9f, objectHalfHeight * 0.9f, objectRadius * 0.9f);
        Collider[] overlaps = Physics.OverlapBox(safeCenter, halfExtents, targetRigidbody.rotation, ~0, QueryTriggerInteraction.Ignore);

        foreach (Collider overlap in overlaps)
        {
            if (overlap.attachedRigidbody == targetRigidbody || overlap.CompareTag("Player"))
            {
                continue;
            }

            if (overlap.GetComponentInParent<CircuitNode>() != null)
            {
                continue;
            }

            return false;
        }

        safePosition = safeCenter;
        return true;
    }

    private bool IsBoxReleaseSurface(Collider surfaceCollider)
    {
        if (IsPlaceableSurface(surfaceCollider))
        {
            return true;
        }

        return surfaceCollider != null
            && surfaceCollider.GetComponentInParent<Elevator>() != null
            && surfaceCollider.name.Contains("Floor");
    }

    // Called by external gimmicks such as ladders to enter climbing state.
    public void StartClimbing()
    {
        if (IsHoldingObject)
        {
            return;
        }

        isClimbing = true;
        if (rb != null)
        {
            rb.useGravity = false;
            rb.linearVelocity = Vector3.zero;
        }
    }

    // Called by external gimmicks such as ladders to leave climbing state.
    public void StopClimbing()
    {
        isClimbing = false;
        if (rb != null)
        {
            rb.useGravity = true;
        }
    }

    // Track whether the player is touching the ground.
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            isGrounded = true;
        }
    }

    private void OnCollisionStay(Collision collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            isGrounded = true;
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            isGrounded = false;
        }
    }
}
