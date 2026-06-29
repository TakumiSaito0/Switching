using UnityEngine;
using UnityEngine.Events;

public class RespawnManager : MonoBehaviour
{
    [SerializeField] private Transform player;
    [SerializeField] private Transform respawnPoint;
    [SerializeField] private float fallY = -10f;
    [SerializeField] private string boxTag = "Box";
    [SerializeField] private string stageSelectSceneName = "StageSelectScene";

    [Header("Game Over")]
    [SerializeField] private UnityEvent onGameOver;

    private Rigidbody playerRigidbody;
    private GameObject[] boxes;
    private bool isGameOver = false;

    private void Awake()
    {
        if (player != null)
        {
            playerRigidbody = player.GetComponent<Rigidbody>();
        }
    }

    private void Start()
    {
        boxes = GameObject.FindGameObjectsWithTag(boxTag);
    }

    private void Update()
    {
        if (isGameOver)
        {
            return;
        }

        if (player != null && respawnPoint != null && player.position.y < fallY)
        {
            Respawn();
        }

        if (boxes == null)
        {
            return;
        }

        foreach (GameObject box in boxes)
        {
            if (box != null && box.transform.position.y < fallY)
            {
                GameOver();
                break;
            }
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

    private void GameOver()
    {
        isGameOver = true;
        Debug.Log("A box fell out of the stage. Game Over.");

        onGameOver?.Invoke();
        GameSfx.Play("sfx_game_over_lowpoly");
        GameOverMenu.Show(stageSelectSceneName);
    }
}
