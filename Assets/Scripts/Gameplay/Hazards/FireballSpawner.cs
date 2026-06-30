using UnityEngine;

public class FireballSpawner : MonoBehaviour
{
    [SerializeField] private float interval = 1.4f;
    [SerializeField] private float initialDelay = 0f;
    [SerializeField] private float fireballSpeed = 7f;
    [SerializeField] private float fireballLifetime = 3f;
    [SerializeField] private float fireballScale = 0.55f;
    [SerializeField] private Color fireballColor = new Color(1f, 0.28f, 0.04f, 1f);

    private float nextSpawnTime;

    private void OnEnable()
    {
        nextSpawnTime = Time.time + initialDelay;
    }

    private void Update()
    {
        if (Time.time < nextSpawnTime)
        {
            return;
        }

        SpawnFireball();
        nextSpawnTime = Time.time + interval;
    }

    private void SpawnFireball()
    {
        GameObject fireball = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        fireball.name = "Fireball";
        fireball.transform.position = transform.position;
        fireball.transform.localScale = Vector3.one * fireballScale;

        Collider fireballCollider = fireball.GetComponent<Collider>();
        fireballCollider.isTrigger = true;

        Rigidbody fireballRigidbody = fireball.AddComponent<Rigidbody>();
        fireballRigidbody.useGravity = false;
        fireballRigidbody.isKinematic = true;

        Renderer renderer = fireball.GetComponent<Renderer>();
        renderer.material.color = fireballColor;

        FireballHazard hazard = fireball.AddComponent<FireballHazard>();
        hazard.Initialize(fireballSpeed, fireballLifetime);
    }
}
