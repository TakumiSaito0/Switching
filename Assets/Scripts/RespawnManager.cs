using UnityEngine;

public class RespawnManager : MonoBehaviour
{
    [SerializeField] private Transform player;
    [SerializeField] private Transform respawnPoint;
    [SerializeField] private float fallY = -10f;

    private Rigidbody playerRigidbody;

    private void Awake()
    {
        if (player != null)
        {
            playerRigidbody = player.GetComponent<Rigidbody>();
        }
    }

    private void Update()
    {
        if (player == null || respawnPoint == null)
        {
            return;
        }

        if (player.position.y < fallY)
        {
            Respawn();
        }
    }

    private void Respawn()
    {
        player.position = respawnPoint.position;

        if (playerRigidbody != null)
        {
            playerRigidbody.linearVelocity = Vector3.zero;
            playerRigidbody.angularVelocity = Vector3.zero;
        }
    }
}
