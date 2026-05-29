using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour, PlayerAction.IPlayerActions
{
    [SerializeField]
    private float moveSpeed = 5f;

    [SerializeField]
    private float climbSpeed = 3f;

    [SerializeField]
    private float grabDistance = 2f;

    [SerializeField]
    private LayerMask grabbableLayers = ~0;

    [SerializeField]
    private Transform grabPoint;

    [Header("アクション設定")]
    public float interactDistance = 2.0f;
    public float placeDistance = 1.2f;
    public float placeHeightOffset = 0.5f;
    public LayerMask placeGroundLayers = ~0;
    public GameObject wirePrefab;

    [HideInInspector]
    public bool isClimbing = false;
    [HideInInspector]
    public bool isGrounded = false;

    private PlayerAction playerAction;
    private Vector2 moveInput;
    private Rigidbody heldRigidbody;
    private Rigidbody rb;

    public bool IsHoldingObject => heldRigidbody != null;

    private Vector3? lastTargetGridPos = null;
    private bool isPlacingMode = false;
    private bool isDeletingMode = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

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
        InteractSwitch();
        PlaceCircuit();

        // 共通でカメラ基準の移動ベクトルを算出しておく
        Vector3 move = GetCameraRelativeMove(moveInput);

        // 梯子に触れている状態での移動
        if (isClimbing)
        {
            // カメラ方向に基づいた移動方向を、プレイヤーの正面（梯子の方向）と左右にどの程度一致しているか射影して取得
            // climbIntent: >0 で前（梯子に向かう＝登る）、<0 で後ろ（梯子から離れる＝降りる）
            float climbIntent = Vector3.Dot(move, transform.forward);

            // horizontalIntent: 梯子に張り付いた状態での左右移動
            float horizontalIntent = Vector3.Dot(move, transform.right);

            // 上下移動
            Vector3 verticalMove = new Vector3(0, climbIntent, 0) * climbSpeed * Time.deltaTime;

            // 左右の移動（梯子に張り付いたまま左右にスライド）
            Vector3 horizontalMove = transform.right * horizontalIntent * climbSpeed * Time.deltaTime;

            if (isGrounded && climbIntent < -0.01f)
            {
                // 一番上に到達した状態でSキー等を押し「梯子から降りる」方向に入力した場合
                Vector3 stepDownMove = new Vector3(0, -climbSpeed * 1.5f, 0) * Time.deltaTime;
                transform.position += stepDownMove + horizontalMove;
            }
            else
            {
                // 通常通り下へ降りる処理や、上に登る処理
                Vector3 climbForwardMove = Vector3.zero;

                if (climbIntent > 0.01f) // 上に登ろうとしている
                {
                    Vector3 rayOrigin = transform.position + Vector3.up * 0.8f;
                    bool hasWallAhead = Physics.Raycast(rayOrigin, transform.forward, 1.0f);
                    bool hasWallOverlap = Physics.CheckSphere(rayOrigin + transform.forward * 0.5f, 0.2f);

                    // 目の前に壁がない（梯子を登り切った）場合、前方に進めるようにする
                    if (!hasWallAhead && !hasWallOverlap)
                    {
                        climbForwardMove = transform.forward * climbSpeed * Time.deltaTime;
                    }
                }

                transform.position += verticalMove + horizontalMove + climbForwardMove;
            }
            return;
        }

        // 通常の地上移動
        if (move.sqrMagnitude > 0f)
        {
            // 右クリックを押しっぱなしの時は向きを変えず平行移動のみにする
            if (!Mouse.current.rightButton.isPressed)
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


    // 左クリック: 目の前にあるスイッチを押す
    void InteractSwitch()
    {
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            Vector3 boxCenter = transform.position + transform.forward * (interactDistance / 2f) + Vector3.up * 0.5f;
            Vector3 boxHalfExtents = new Vector3(0.5f, 1.0f, interactDistance / 2f);

            bool switchFound = false;

            Collider[] colliders = Physics.OverlapBox(boxCenter, boxHalfExtents, transform.rotation);
            foreach (Collider hit in colliders)
            {
                ElevatorButton elevatorButton = hit.GetComponent<ElevatorButton>();
                if (elevatorButton != null)
                {
                    elevatorButton.PressButton();
                    Debug.Log("Elevator button pressed: " + hit.gameObject.name);
                    switchFound = true;
                    break;
                }

                SwitchNode switchNode = hit.GetComponent<SwitchNode>();
                if (switchNode != null)
                {
                    switchNode.ToggleSwitch();
                    Debug.Log("スイッチを切り替えました！ (" + hit.gameObject.name + ")");
                    switchFound = true;
                    break;
                }
            }

            if (!switchFound)
            {
                Debug.Log("正面の判定エリア内にスイッチが見つかりませんでした。");
            }
        }
    }

    // 右クリック: 地面に回路(ワイヤー)を設置・削除する
    void PlaceCircuit()
    {
        // 押されていない場合は記録とモードをリセットする
        if (!Mouse.current.rightButton.isPressed)
        {
            lastTargetGridPos = null;
            isPlacingMode = false;
            isDeletingMode = false;
            return;
        }

        if (wirePrefab != null)
        {
            Vector3 frontPos = transform.position + transform.forward * placeDistance;
            float gridX = Mathf.Round(frontPos.x);
            float gridZ = Mathf.Round(frontPos.z);
            Vector3 gridPos = new Vector3(gridX, transform.position.y, gridZ);

            // 右クリックを「押した瞬間」にモードを決定する
            if (Mouse.current.rightButton.wasPressedThisFrame)
            {
                WireNode targetWire = FindWireAtGridPosition(gridPos);
                if (targetWire != null)
                {
                    isDeletingMode = true; // 対象マスに回路がある場合は「削除モード」
                    isPlacingMode = false;
                }
                else
                {
                    isPlacingMode = true; // 対象マスが空の場合は「設置モード」
                    isDeletingMode = false;
                }
            }

            // すでにこの操作で処理済みのマスの場合はスキップ
            if (lastTargetGridPos.HasValue && lastTargetGridPos.Value == gridPos)
            {
                return;
            }

            lastTargetGridPos = gridPos;

            WireNode existingWire = FindWireAtGridPosition(gridPos);

            // === 削除モード時の処理 ===
            if (isDeletingMode)
            {
                if (existingWire != null)
                {
                    Destroy(existingWire.gameObject);
                    Invoke(nameof(RefreshAllCircuits), 0.05f);
                }
                return; // 削除モード中は設置の処理を行わない
            }

            // === 設置モード時の処理 ===
            if (isPlacingMode)
            {
                if (existingWire != null)
                {
                    // 設置モードでは既存の回路を消さないため、ここで処理を終了
                    return;
                }

                if (FindCircuitAtGridPosition(gridPos) != null)
                {
                    Debug.Log("そこには既に何かが置かれています。");
                    return;
                }

                if (!TryGetGroundedPlacePosition(gridX, gridZ, out Vector3 placePos))
                {
                    Debug.Log("配置先の地面が見つかりませんでした。");
                    return;
                }

                bool isBlocked = false;
                Collider[] colliders = Physics.OverlapSphere(placePos, 0.4f);
                foreach (var col in colliders)
                {
                    if (col.CompareTag("Player"))
                    {
                        isBlocked = true;
                        break;
                    }
                }

                if (!isBlocked)
                {
                    GameObject placedCircuit = Instantiate(wirePrefab, placePos, Quaternion.identity);
                    SnapBottomToGround(placedCircuit, placePos.y);
                    Invoke(nameof(RefreshAllCircuits), 0.05f);
                }
                else
                {
                    Debug.Log("そこには既に何かが置かれています。");
                }
            }
        }
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

        float highestGroundY = float.NegativeInfinity;
        bool foundGround = false;

        foreach (RaycastHit hit in hits)
        {
            if (hit.collider.GetComponentInParent<CircuitNode>() != null || hit.collider.CompareTag("Player"))
            {
                continue;
            }

            if (hit.point.y > highestGroundY)
            {
                highestGroundY = hit.point.y;
                foundGround = true;
            }
        }

        if (foundGround)
        {
            placePos = new Vector3(gridX, highestGroundY, gridZ);
            return true;
        }

        placePos = new Vector3(gridX, placeHeightOffset, gridZ);
        return false;
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

        if (isClimbing)
        {
            StopClimbing();
        }
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

    // 外部（梯子などのギミック）からプレイヤーを登り状態にするメソッド
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

    // 外部（梯子などのギミック）からプレイヤーの登り状態を解除するメソッド
    public void StopClimbing()
    {
        isClimbing = false;
        if (rb != null)
        {
            rb.useGravity = true;
        }
    }

    // 地面に触れているかを判定するための処理
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
