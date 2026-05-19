using UnityEngine;
using UnityEngine.Events;

public class ClearManager : MonoBehaviour
{
    [SerializeField]
    private string boxTag = "Box";

    [SerializeField]
    private UnityEvent onClear;

    private bool isCleared;

    private void OnTriggerEnter(Collider other)
    {
        if (isCleared)
        {
            return;
        }

        if (!other.CompareTag(boxTag))
        {
            return;
        }

        isCleared = true;
        onClear?.Invoke();
        Debug.Log("Clear");
    }
}
