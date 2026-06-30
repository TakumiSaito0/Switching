using UnityEngine;

public class FireballHazard : MonoBehaviour
{
    [SerializeField] private float speed = 7f;
    [SerializeField] private float lifetime = 3f;

    private float destroyTime;

    public void Initialize(float launchSpeed, float lifeSeconds)
    {
        speed = launchSpeed;
        lifetime = lifeSeconds;
        destroyTime = Time.time + lifetime;
    }

    private void OnEnable()
    {
        destroyTime = Time.time + lifetime;
    }

    private void Update()
    {
        transform.position += Vector3.up * speed * Time.deltaTime;

        if (Time.time >= destroyTime)
        {
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
        {
            return;
        }

        RespawnManager respawnManager = FindAnyObjectByType<RespawnManager>();
        if (respawnManager != null)
        {
            respawnManager.RespawnPlayer();
        }

        Destroy(gameObject);
    }
}
