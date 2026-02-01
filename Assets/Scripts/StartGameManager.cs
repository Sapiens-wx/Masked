using UnityEngine;
using UnityEngine.SceneManagement;

public class StartGameManager : Singleton<StartGameManager>
{
    public MyButton startButton, quitButton;
    public string sceneName;
    void Start()
    {
        startButton.onClick+=LoadScene;
        quitButton.onClick+=Application.Quit;
        MaskBox.inst.CreateMask();
    }
    public void LoadScene()
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogWarning("Scene name is empty.");
            return;
        }

        SceneManager.LoadScene(sceneName);
    }
}