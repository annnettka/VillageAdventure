using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class GameManager : MonoBehaviour
{
    private const string BestTimeKey = "PixelVillageAdventure.BestTime";

    public static GameManager Instance { get; private set; }

    [Header("Scene")]
    [SerializeField] private string gameSceneName = "Game";
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    [Header("References")]
    [SerializeField] private PlayerController player;
    [SerializeField] private Transform playerSpawn;

    [Header("HUD")]
    [SerializeField] private Text timerText;
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private GameObject winPanel;
    [SerializeField] private Text winTimeText;
    [SerializeField] private Text winBestTimeText;

    [Header("Timing")]
    [SerializeField] private float respawnDelay = 0.8f;

    private GameState currentState = GameState.Playing;
    private float elapsedTime;
    private Coroutine deathRoutine;

    public GameState State => currentState;
    public bool IsPlaying => currentState == GameState.Playing;
    public float ElapsedTime => elapsedTime;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("Multiple GameManager instances found. The first instance remains active.");
            return;
        }

        Instance = this;
        Time.timeScale = 1f;

        if (player == null)
        {
            player = FindFirstObjectByType<PlayerController>();
        }

        SetPanelActive(pausePanel, false);
        SetPanelActive(winPanel, false);
        SetState(GameState.Playing);
        UpdateTimerText();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void Update()
    {
        if (!IsPlaying)
        {
            return;
        }

        elapsedTime += Time.deltaTime;
        UpdateTimerText();
    }

    public void BeginPlayerDeath(PlayerController deadPlayer)
    {
        if (!IsPlaying)
        {
            return;
        }

        if (player == null)
        {
            player = deadPlayer;
        }

        if (deathRoutine != null)
        {
            StopCoroutine(deathRoutine);
        }

        deathRoutine = StartCoroutine(RespawnRoutine());
    }

    public void CompleteLevel()
    {
        if (!IsPlaying)
        {
            return;
        }

        SetState(GameState.Won);
        Time.timeScale = 1f;

        if (player != null)
        {
            player.SetInputLocked(true, true);
        }

        float bestTime = SaveBestTime(elapsedTime);
        if (winTimeText != null)
        {
            winTimeText.text = $"Time: {FormatSeconds(elapsedTime)}";
        }

        if (winBestTimeText != null)
        {
            winBestTimeText.text = $"Best: {FormatSeconds(bestTime)}";
        }

        SetPanelActive(winPanel, true);
    }

    public void PauseGame()
    {
        if (!IsPlaying)
        {
            return;
        }

        SetState(GameState.Paused);
        Time.timeScale = 0f;

        if (player != null)
        {
            player.SetInputLocked(true);
        }

        SetPanelActive(pausePanel, true);
    }

    public void ResumeGame()
    {
        if (currentState != GameState.Paused)
        {
            return;
        }

        SetPanelActive(pausePanel, false);
        Time.timeScale = 1f;
        SetState(GameState.Playing);

        if (player != null)
        {
            player.SetInputLocked(false);
        }
    }

    public void RestartLevel()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(gameSceneName);
    }

    public void GoToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuSceneName);
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        Debug.Log("Quit requested. Application.Quit is ignored in the Unity Editor.");
#else
        Application.Quit();
#endif
    }

    private IEnumerator RespawnRoutine()
    {
        SetState(GameState.Dead);

        if (player != null)
        {
            player.Die();
        }

        yield return new WaitForSeconds(respawnDelay);

        Vector3 spawnPosition = playerSpawn != null ? playerSpawn.position : Vector3.zero;
        if (player != null)
        {
            player.Respawn(spawnPosition);
        }

        SetState(GameState.Playing);
        deathRoutine = null;
    }

    private void SetState(GameState state)
    {
        currentState = state;
    }

    private void UpdateTimerText()
    {
        if (timerText != null)
        {
            int minutes = Mathf.FloorToInt(elapsedTime / 60f);
            int seconds = Mathf.FloorToInt(elapsedTime % 60f);
            timerText.text = $"Time: {minutes:00}:{seconds:00}";
        }
    }

    private static float SaveBestTime(float completedTime)
    {
        if (!PlayerPrefs.HasKey(BestTimeKey))
        {
            PlayerPrefs.SetFloat(BestTimeKey, completedTime);
            PlayerPrefs.Save();
            return completedTime;
        }

        float bestTime = PlayerPrefs.GetFloat(BestTimeKey);
        if (completedTime < bestTime)
        {
            PlayerPrefs.SetFloat(BestTimeKey, completedTime);
            PlayerPrefs.Save();
            return completedTime;
        }

        return bestTime;
    }

    private static string FormatSeconds(float seconds)
    {
        return seconds.ToString("00.00");
    }

    private static void SetPanelActive(GameObject panel, bool active)
    {
        if (panel != null)
        {
            panel.SetActive(active);
        }
    }
}
