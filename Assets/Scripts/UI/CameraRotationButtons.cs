using UnityEngine;
using UnityEngine.InputSystem;

public class CameraRotationButtons : MonoBehaviour
{
    [SerializeField] private RectTransform leftButton;
    [SerializeField] private RectTransform rightButton;

    private void Awake()
    {
        if (leftButton == null)
        {
            leftButton = FindChildRect("Left Button");
        }

        if (rightButton == null)
        {
            rightButton = FindChildRect("Right Button");
        }
    }

    private void Update()
    {
        CameraManager cameraManager = CameraManager.GetOrCreateMainCameraManager();
        if (cameraManager == null)
        {
            return;
        }

        Keyboard keyboard = Keyboard.current;
        if (keyboard != null)
        {
            if (keyboard.qKey.wasPressedThisFrame)
            {
                cameraManager.RotateLeft();
            }

            if (keyboard.eKey.wasPressedThisFrame)
            {
                cameraManager.RotateRight();
            }
        }
    }

    private RectTransform FindChildRect(string childName)
    {
        Transform child = transform.Find(childName);
        return child != null ? child.GetComponent<RectTransform>() : null;
    }

    private bool IsPointerInside(RectTransform rectTransform, Vector2 pointerPosition)
    {
        return rectTransform != null
            && RectTransformUtility.RectangleContainsScreenPoint(rectTransform, pointerPosition);
    }
}
