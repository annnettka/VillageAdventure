using System;
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
    [SerializeField] private PlayerRespawn playerRespawn;
    [SerializeField] private Transform playerSpawn;
    [SerializeField] private LevelProgression levelProgression;

    [Header("HUD")]
    [SerializeField] private GameHUD gameHUD;
    [SerializeField] private Text timerText;
    [SerializeField] private Text levelText;
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private GameObject winPanel;
    [SerializeField] private Text winTitleText;
    [SerializeField] private Text winMessageText;
    [SerializeField] private Text winTimeText;
    [SerializeField] private Text winBestTimeText;
    [SerializeField] private Button winPrimaryButton;
    [SerializeField] private Text winPrimaryButtonText;
    [SerializeField] private GameObject gameOverPanel;

    [Header("Timing")]
    [SerializeField] private float respawnDelay = 0.8f;

    [Header("Lives")]
    [SerializeField] private int maxLives = 3;
    [SerializeField] private float damageInvulnerabilityDuration = 1f;
    [SerializeField] private float damageFlashInterval = 0.1f;

    private GameState currentState = GameState.Playing;
    private float elapsedTime;
    private Coroutine deathRoutine;
    private Coroutine damageInvulnerabilityRoutine;
    private int currentLives;
    private int collectedFlowers;
    private bool damageInvulnerable;
    private bool sceneTransitioning;

    public GameState State => currentState;
    public bool IsPlaying => currentState == GameState.Playing;
    public float ElapsedTime => elapsedTime;
    public int MaxLives => maxLives;
    public int CurrentLives => currentLives;
    public int CollectedFlowers => collectedFlowers;
    private string FirstLevelSceneToLoad => string.IsNullOrEmpty(gameSceneName) ? LevelProgression.FirstLevelSceneName : gameSceneName;
    private string MainMenuSceneToLoad => string.IsNullOrEmpty(mainMenuSceneName) ? LevelProgression.MainMenuSceneName : mainMenuSceneName;

    public event Action<int, int> OnLivesChanged;
    public event Action<int> OnFlowerCountChanged;

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

        if (playerRespawn == null && player != null)
        {
            playerRespawn = player.GetComponent<PlayerRespawn>();
        }

        if (gameHUD == null)
        {
            gameHUD = FindFirstObjectByType<GameHUD>();
        }

        if (levelProgression == null)
        {
            levelProgression = GetComponent<LevelProgression>();
        }

        maxLives = Mathf.Max(1, maxLives);
        currentLives = maxLives;
        collectedFlowers = 0;

        if (playerRespawn != null)
        {
            Vector3 initialSafePosition = playerSpawn != null ? playerSpawn.position : player.transform.position;
            playerRespawn.SetInitialSafePosition(initialSafePosition);
        }

        SetPanelActive(pausePanel, false);
        SetPanelActive(winPanel, false);
        SetPanelActive(gameOverPanel, false);
        SetState(GameState.Playing);
        UpdateTimerText();
        UpdateLevelText();
    }

    private void Start()
    {
        if (gameHUD != null)
        {
            gameHUD.Bind(this);
        }

        PublishHudState();
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
        if (!IsPlaying || deathRoutine != null)
        {
            return;
        }

        if (player == null)
        {
            player = deadPlayer;
        }

        currentLives = Mathf.Max(0, currentLives - 1);
        OnLivesChanged?.Invoke(currentLives, maxLives);

        if (currentLives > 0)
        {
            deathRoutine = StartCoroutine(RespawnRoutine());
        }
        else
        {
            deathRoutine = StartCoroutine(GameOverRoutine());
        }
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

        ConfigureWinPanelForCurrentLevel();
        SetPanelActive(winPanel, true);
    }

    public bool TryCollectFlower(CollectibleFlower flower)
    {
        if (!IsPlaying)
        {
            return false;
        }

        collectedFlowers++;
        CharacterProgress.AddFlowers(1);
        OnFlowerCountChanged?.Invoke(collectedFlowers);
        return true;
    }

    public bool TryDamagePlayer(PlayerController damagedPlayer, int damage, Vector2 sourcePosition)
    {
        if (!IsPlaying || deathRoutine != null || damageInvulnerable || damagedPlayer == null)
        {
            return false;
        }

        if (player == null)
        {
            player = damagedPlayer;
        }

        currentLives = Mathf.Max(0, currentLives - Mathf.Max(1, damage));
        OnLivesChanged?.Invoke(currentLives, maxLives);

        if (currentLives <= 0)
        {
            deathRoutine = StartCoroutine(GameOverRoutine());
        }
        else
        {
            if (damageInvulnerabilityRoutine != null)
            {
                StopCoroutine(damageInvulnerabilityRoutine);
            }

            damageInvulnerabilityRoutine = StartCoroutine(DamageInvulnerabilityRoutine());
        }

        return true;
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
        if (sceneTransitioning)
        {
            return;
        }

        sceneTransitioning = true;
        LevelProgression.RestartCurrentLevel();
    }

    public void GoToMainMenu()
    {
        if (sceneTransitioning)
        {
            return;
        }

        sceneTransitioning = true;
        Time.timeScale = 1f;
        SceneManager.LoadScene(MainMenuSceneToLoad);
    }

    public void ContinueAfterWin()
    {
        if (sceneTransitioning)
        {
            return;
        }

        sceneTransitioning = true;
        if (winPrimaryButton != null)
        {
            winPrimaryButton.interactable = false;
        }

        if (LevelProgression.IsFinalLevel)
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(FirstLevelSceneToLoad);
        }
        else
        {
            LevelProgression.LoadNextLevel();
        }
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
        SetState(GameState.Respawning);

        if (player != null)
        {
            player.Die();
        }

        yield return new WaitForSeconds(respawnDelay);

        Vector3 spawnPosition = ResolveRespawnPosition();
        if (player != null)
        {
            player.Respawn(spawnPosition);
        }

        SetState(GameState.Playing);
        deathRoutine = null;
    }

    private IEnumerator GameOverRoutine()
    {
        SetState(GameState.GameOver);
        damageInvulnerable = true;

        if (player != null)
        {
            player.Die();
        }

        yield return new WaitForSeconds(respawnDelay);

        SetPanelActive(gameOverPanel, true);
        deathRoutine = null;
    }

    private IEnumerator DamageInvulnerabilityRoutine()
    {
        damageInvulnerable = true;
        SpriteRenderer[] renderers = player != null ? player.GetComponentsInChildren<SpriteRenderer>(true) : new SpriteRenderer[0];
        float elapsed = 0f;
        bool visible = true;

        while (elapsed < damageInvulnerabilityDuration)
        {
            visible = !visible;
            SetRenderersVisible(renderers, visible);
            float wait = Mathf.Max(0.02f, damageFlashInterval);
            elapsed += wait;
            yield return new WaitForSeconds(wait);
        }

        SetRenderersVisible(renderers, true);
        damageInvulnerable = false;
        damageInvulnerabilityRoutine = null;
    }

    private void SetState(GameState state)
    {
        currentState = state;
    }

    private Vector3 ResolveRespawnPosition()
    {
        if (playerRespawn != null)
        {
            return playerRespawn.RespawnPosition;
        }

        if (playerSpawn != null)
        {
            return playerSpawn.position + Vector3.up * 0.65f;
        }

        return player != null ? player.transform.position : Vector3.zero;
    }

    private void PublishHudState()
    {
        OnLivesChanged?.Invoke(currentLives, maxLives);
        OnFlowerCountChanged?.Invoke(collectedFlowers);
    }

    private void ConfigureWinPanelForCurrentLevel()
    {
        bool finalLevel = LevelProgression.IsFinalLevel;
        if (winTitleText != null)
        {
            winTitleText.text = finalLevel ? "ADVENTURE COMPLETE!" : "LEVEL COMPLETE";
        }

        if (winMessageText != null)
        {
            winMessageText.text = finalLevel
                ? $"You completed all {LevelProgression.TotalLevels} levels!"
                : $"Level {LevelProgression.CurrentLevelNumber} / {LevelProgression.TotalLevels} complete";
        }

        if (winPrimaryButtonText != null)
        {
            winPrimaryButtonText.text = finalLevel ? "PLAY AGAIN" : "NEXT LEVEL";
        }

        if (winPrimaryButton != null)
        {
            winPrimaryButton.interactable = true;
        }
    }

    private static void SetRenderersVisible(SpriteRenderer[] renderers, bool visible)
    {
        foreach (SpriteRenderer renderer in renderers)
        {
            if (renderer != null)
            {
                renderer.enabled = visible;
            }
        }
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

    private void UpdateLevelText()
    {
        if (levelText == null || !LevelProgression.IsGameplayLevel)
        {
            return;
        }

        levelText.text = $"LEVEL {LevelProgression.CurrentLevelNumber} / {LevelProgression.TotalLevels}";
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
