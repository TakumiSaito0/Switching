using UnityEngine;

public class CameraManager : MonoBehaviour
{
    public static CameraManager Instance { get; private set; }

    [SerializeField] private Transform focus;
    [SerializeField] private float height = 10f;
    [SerializeField] private float distance = 10f;
    [SerializeField] private float tiltAngle = 60f;
    [SerializeField] private float rotationSpeed = 720f;

    private float currentYaw;
    private float targetYaw;

    private void Awake()
    {
        Instance = this;

        if (focus == null)
        {
            PlayerController player = FindAnyObjectByType<PlayerController>();
            if (player != null)
            {
                focus = player.transform;
            }
        }

        currentYaw = transform.eulerAngles.y;
        targetYaw = currentYaw;
    }

    public static CameraManager GetOrCreateMainCameraManager()
    {
        if (Instance != null)
        {
            return Instance;
        }

        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            return null;
        }

        CameraManager manager = mainCamera.GetComponent<CameraManager>();
        if (manager == null)
        {
            manager = mainCamera.gameObject.AddComponent<CameraManager>();
        }

        Instance = manager;
        return manager;
    }

    private void LateUpdate()
    {
        currentYaw = Mathf.MoveTowardsAngle(currentYaw, targetYaw, rotationSpeed * Time.deltaTime);

        var rotation = Quaternion.Euler(tiltAngle, currentYaw, 0f);
        var offset = rotation * new Vector3(0f, height, -distance);
        var focusPosition = focus != null ? focus.position : Vector3.zero;

        transform.position = focusPosition + offset;
        transform.rotation = rotation;
    }

    public void RotateLeft()
    {
        targetYaw += 90f;
    }

    public void RotateRight()
    {
        targetYaw -= 90f;
    }
}
