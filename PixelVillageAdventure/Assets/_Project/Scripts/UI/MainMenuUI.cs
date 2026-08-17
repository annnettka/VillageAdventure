using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class MainMenuUI : MonoBehaviour
{
    [SerializeField] private string gameSceneName = "Game";

    public void Play()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(gameSceneName);
    }

    public void Quit()
    {
#if UNITY_EDITOR
        Debug.Log("Quit requested. Application.Quit is ignored in the Unity Editor.");
#else
        Application.Quit();
#endif
    }
}
