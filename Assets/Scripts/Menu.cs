using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem; // 新しいInput Systemを使用

public class Menu : MonoBehaviour
{
    [Header("メニューのUIパネル")]
    [SerializeField] private GameObject menuPanel;


    [Header("遷移先のステージ選択シーン名")]
    [SerializeField] private string stageSelectSceneName = "StageSelect";

    private void Start()
    {
        // 最初はメニューを非表示にし、時間の流れを通常通りにする
        menuPanel.SetActive(false);
        Time.timeScale = 1f;
    }

    private void Update()
    {
        // Escキーが押されたらメニューの表示/非表示を切り替える
        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            ToggleMenu();
        }
    }

    // メニューの開閉と時間の停止・再開
    public void ToggleMenu()
    {
        bool isOpen = !menuPanel.activeSelf;
        menuPanel.SetActive(isOpen);

        // メニューが開いているときは 0 (停止)、閉じているときは 1 (通常)
        Time.timeScale = isOpen ? 0f : 1f;
    }

    // 最初からやり直す (Restart)
    public void RestartStage()
    {
        Time.timeScale = 1f; // 時間を戻してから再読み込み
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    // ステージ選択画面に戻る
    public void ReturnToStageSelect()
    {
        Time.timeScale = 1f; // 時間を戻してから遷移
        SceneManager.LoadScene(stageSelectSceneName);
    }
}