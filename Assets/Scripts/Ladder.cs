using UnityEngine;

public class Ladder : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        // 触れたオブジェクト（親や子階層も含めて）から新しいプレイヤースクリプトを探す
        PlayerController player = other.GetComponentInParent<PlayerController>();

        // プレイヤーであれば、登攀フラグをオンにする
        if (player != null && !player.IsHoldingObject)
        {
            player.StartClimbing();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        PlayerController player = other.GetComponentInParent<PlayerController>();

        // 梯子から離れたらフラグをオフにする
        if (player != null)
        {
            player.StopClimbing();
        }
    }
}
