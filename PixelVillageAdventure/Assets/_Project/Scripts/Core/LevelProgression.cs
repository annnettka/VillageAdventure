using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public sealed class LevelProgression : MonoBehaviour
{
    public const string MainMenuSceneName = "MainMenu";
    public const string FirstLevelSceneName = "Game";
    public const int TotalLevels = 71;

    public static int CurrentLevelIndex => GetLevelIndex(SceneManager.GetActiveScene().name);
    public static int CurrentLevelNumber => CurrentLevelIndex >= 0 ? CurrentLevelIndex + 1 : 0;
    public static bool IsGameplayLevel => CurrentLevelIndex >= 0;
    public static bool IsFinalLevel => CurrentLevelIndex == TotalLevels - 1;

    public int CurrentLevel => CurrentLevelNumber;
    public int LevelCount => TotalLevels;
    public bool FinalLevel => IsFinalLevel;

    public static void LoadFirstLevel()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(FirstLevelSceneName);
    }

    public static void LoadNextLevel()
    {
        Time.timeScale = 1f;
        int currentIndex = CurrentLevelIndex;
        if (currentIndex < 0 || currentIndex >= TotalLevels - 1)
        {
            LoadFirstLevel();
            return;
        }

        SceneManager.LoadScene(GetSceneNameForLevelIndex(currentIndex + 1));
    }

    public static void RestartCurrentLevel()
    {
        Time.timeScale = 1f;
        Scene activeScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(string.IsNullOrEmpty(activeScene.name) ? FirstLevelSceneName : activeScene.name);
    }

    public static void LoadMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(MainMenuSceneName);
    }

    public static string GetSceneNameForLevelIndex(int levelIndex)
    {
        return levelIndex <= 0 ? FirstLevelSceneName : $"Game {levelIndex}";
    }

    public static bool TryGetLevelIndex(string sceneName, out int levelIndex)
    {
        levelIndex = GetLevelIndex(sceneName);
        return levelIndex >= 0;
    }

    private static int GetLevelIndex(string sceneName)
    {
        if (sceneName == FirstLevelSceneName)
        {
            return 0;
        }

        const string numberedPrefix = FirstLevelSceneName + " ";
        if (!sceneName.StartsWith(numberedPrefix, System.StringComparison.Ordinal))
        {
            return -1;
        }

        string suffix = sceneName.Substring(numberedPrefix.Length);
        return int.TryParse(suffix, out int sceneNumber) && sceneNumber >= 1 && sceneNumber < TotalLevels
            ? sceneNumber
            : -1;
    }
}
