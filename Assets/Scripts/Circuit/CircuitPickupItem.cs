using UnityEngine;

public class CircuitPickupItem : MonoBehaviour
{
    [Header("Pickup Settings")]
    [SerializeField] private int amountToGive = 1; // 一度に拾える回路の数
    [SerializeField] private float rotationSpeed = 90f; // アイテムを回転させる速度（見た目用）

    private void Update()
    {
        // 拾えるアイテムっぽく、ゆっくり回転させる
        transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
    }

    private void OnTriggerEnter(Collider other)
    {
        // 触れたのがプレイヤーかどうか判定
        if (other.CompareTag("Player"))
        {
            PlayerController player = other.GetComponent<PlayerController>();
            if (player != null)
            {
                // プレイヤーの所持数を増やす
                player.AddCircuits(amountToGive);

                // アイテム自身を消滅させる
                Destroy(gameObject);
            }
        }
    }
}