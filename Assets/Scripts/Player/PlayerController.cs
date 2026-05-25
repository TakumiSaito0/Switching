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
    public GameObject wirePrefab;

    [HideInInspector]
    public bool isClimbing = false;
    [HideInInspector]
    public bool isGrounded = false;

    private PlayerAction playerAction;
    private Vector2 moveInput;
    private Rigidbody heldRigidbody;
    private Rigidbody rb;

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

        float h = moveInput.x;
        float v = moveInput.y;

        // 梯子に触れている状態での移動
        if (isClimbing)
        {
            // 上下移動 (Wで登る、Sで降りる)
            Vector3 verticalMove = new Vector3(0, v, 0) * climbSpeed * Time.deltaTime;

            if (isGrounded && v < 0)
            {
                // 一番上に到達した状態でSキーを押した場合、「歩いて後ろに下がる（落下する）」のではなく、
                // 「梯子の側面に沿って一段下（Y軸マイナス方向）へ強制的に下ろす」ことで、梯子にへばりついたまま降りるようにする。
                Vector3 stepDownMove = new Vector3(0, -climbSpeed * 1.5f, 0) * Time.deltaTime;
                Vector3 horizontalScale = transform.right * h * moveSpeed * Time.deltaTime;
                transform.position += stepDownMove + horizontalScale;
            }
            else
            {
                // Sキーの入力（v < 0）でも登攀中で床についていない場合は通常通り下へ降りる処理を行う
                Vector3 climbForwardMove = Vector3.zero;

                if (v > 0)
                {
                    Vector3 rayOrigin = transform.position + Vector3.up * 0.8f;
                    bool hasWallAhead = Physics.Raycast(rayOrigin, transform.forward, 1.0f);
                    bool hasWallOverlap = Physics.CheckSphere(rayOrigin + transform.forward * 0.5f, 0.2f);

                    if (!hasWallAhead && !hasWallOverlap)
                    {
                        climbForwardMove = transform.forward * climbSpeed * Time.deltaTime;
                    }
                }

                Vector3 horizontalScale = transform.right * h * moveSpeed * Time.deltaTime;
                transform.position += verticalMove + horizontalScale + climbForwardMove;
            }
            return;
        }

        var move = new Vector3(moveInput.x, 0f, moveInput.y);
        if (move.sqrMagnitude > 0f)
        {
            transform.rotation = Quaternion.LookRotation(move, Vector3.up);
        }

        transform.position += move * moveSpeed * Time.deltaTime;
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

    // 右クリック: 地面に回路(ワイヤー)を設置する
    void PlaceCircuit()
    {
        if (Mouse.current.rightButton.wasPressedThisFrame && wirePrefab != null)
        {
            Vector3 frontPos = transform.position + transform.forward * placeDistance;
            float gridX = Mathf.Round(frontPos.x);
            float gridZ = Mathf.Round(frontPos.z);
            Vector3 placePos = new Vector3(gridX, placeHeightOffset, gridZ);

            bool isBlocked = false;
            Collider[] colliders = Physics.OverlapSphere(placePos, 0.4f);
            foreach(var col in colliders)
            {
                if (col.GetComponent<CircuitNode>() != null || col.CompareTag("Player"))
                {
                    isBlocked = true;
                    break;
                }
            }

            if (!isBlocked)
            {
                Instantiate(wirePrefab, placePos, Quaternion.identity);
                Invoke(nameof(RefreshAllCircuits), 0.05f);
            }
            else
            {
                Debug.Log("そこには既に何かが置かれています。");
            }
        }
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
        isGrounded = true;
    }

    private void OnCollisionStay(Collision collision)
    {
        isGrounded = true;
    }

    private void OnCollisionExit(Collision collision)
    {
        isGrounded = false;
    }
}
