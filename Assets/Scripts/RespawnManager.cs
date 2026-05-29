using UnityEngine;
using UnityEngine.Events;

public class RespawnManager : MonoBehaviour
{
    [SerializeField] private Transform player;
    [SerializeField] private Transform respawnPoint;
    [SerializeField] private float fallY = -10f;
    [SerializeField] private string boxTag = "Box";

    [Header("ゲームオーバー処理")]
    [SerializeField] private UnityEvent onGameOver;

    private Rigidbody playerRigidbody;
    private GameObject[] boxes;

    private void Awake()
    {
        if (player != null)
        {
            playerRigidbody = player.GetComponent<Rigidbody>();
        }
    }

    private void Start()
    {
        // シーン内の箱をすべて探して登録する
        boxes = GameObject.FindGameObjectsWithTag(boxTag);
    }

    private void Update()
    {
        // プレイヤーの落下チェック
        if (player != null && respawnPoint != null)
        {
            if (player.position.y < fallY)
            {
                Respawn();
            }
        }

        // 箱の落下チェック
        if (boxes != null)
        {
            foreach (var box in boxes)
            {
                if (box != null && box.transform.position.y < fallY)
                {
                    GameOver();
                    break; // 重複して呼ばれないようにする
                }
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
        Debug.Log("箱が場外に落ちました。ゲームオーバー！");

        // UnityEvent に演出が登録されていれば呼び出す
        onGameOver?.Invoke();

        // とりあえずプレイヤーを初期位置に戻し、簡易的なリトライとする
        // （シーンの再読み込みをしたい場合は SceneManager を利用してください）
        Respawn();
    }
}
