using UnityEngine;

public class CircuitPickupItem : MonoBehaviour
{
    [Header("Pickup Settings")]
    [SerializeField] private int amountToGive = 1;
    [SerializeField] private float rotationSpeed = 90f;
    [SerializeField] private float bobHeight = 0.12f;
    [SerializeField] private float bobSpeed = 2.5f;
    [SerializeField, Range(0.1f, 1f)] private float pickupScale = 0.5f;

    private Vector3 startPosition;

    private void Start()
    {
        transform.localScale *= pickupScale;

        SphereCollider pickupCollider = GetComponent<SphereCollider>();
        if (pickupCollider != null && pickupScale > 0f)
        {
            pickupCollider.radius /= pickupScale;
        }

        startPosition = transform.position;
    }

    private void Update()
    {
        transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
        transform.position = startPosition + Vector3.up * (Mathf.Sin(Time.time * bobSpeed) * bobHeight);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
        {
            return;
        }

        PlayerController player = other.GetComponent<PlayerController>();
        if (player == null)
        {
            return;
        }

        player.AddCircuits(amountToGive);
        Destroy(gameObject);
    }
}
