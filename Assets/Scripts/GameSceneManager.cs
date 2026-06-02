using UnityEngine;
using UnityEngine.SceneManagement; // シーン遷移に必要

public class GameSceneManager : MonoBehaviour
{
    // タイトル画面へ遷移
    public void LoadTitle()
    {
        SceneManager.LoadScene("TitleScene");
    }

    // ステージ選択画面へ遷移
    public void LoadStageSelect()
    {
        SceneManager.LoadScene("StageSelectScene");
    }

    // 指定したステージへ遷移
    public void LoadStage(string stageName)
    {
        SceneManager.LoadScene(stageName);
    }

    // Stage1専用のメソッドを追加
    public void LoadStage1()
    {
        SceneManager.LoadScene("Stage1");
    }
    
    // Stage2専用のメソッドを追加
    public void LoadStage2()
    {
        SceneManager.LoadScene("Stage2");
    }
}
