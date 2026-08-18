#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using UnityEngine.UI;
using TMPro;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

public static class PixelVillageSetup
{
    private const string GameScenePath = "Assets/_Project/Scenes/Game.unity";
    private const string MainMenuScenePath = "Assets/_Project/Scenes/MainMenu.unity";
    private const string ScenesFolder = "Assets/_Project/Scenes";
    private const string MenuAssetsFolder = "Assets/_Project/MenuAssets";
    private const string GameBackgroundPath = "Assets/_Project/Scenes/backgroundGAME.png";
    private const string FlowerCollectiblePrefabPath = "Assets/_Project/Prefabs/Gameplay/FlowerCollectible.prefab";
    private const string CharacterSourceFolder = "Assets/_Project/Characters";
    private const string EnemySourceFolder = "Assets/_Project/Characters/Enemies";
    private const string CharacterDataFolder = "Assets/_Project/Data/Characters";
    private const string CharacterDefinitionFolder = "Assets/_Project/Data/Characters/Definitions";
    private const string CharacterDatabasePath = "Assets/_Project/Data/Characters/CharacterDatabase.asset";
    private const string EnemyPrefabFolder = "Assets/_Project/Prefabs/Enemies";
    private const string PuppeteerFolder = "Assets/GDD - Quinnipiac/Pixel Art Character Package/Characters/Puppeteer/Puppeteer Grey";

    private static readonly Color ButtonColor = new Color(1f, 1f, 1f, 0.34f);
    private static readonly Color PanelColor = new Color(0f, 0f, 0f, 0.72f);
    private static readonly Color TextColor = new Color(1f, 0.95f, 0.82f, 1f);

    [MenuItem("Tools/Pixel Village/Setup Complete Game")]
    public static void SetupCompleteGame()
    {
        EnsureProjectFolders();
        CreateOrUpdateFlowerCollectiblePrefab();
        CharacterDatabase characterDatabase = CreateOrUpdateCharacterDatabase();
        CreateOrUpdateEnemyPrefabs();
        PlayerAnimationFrames playerAnimationFrames = LoadPlayerAnimationFrames();

        SetupGameScene(playerAnimationFrames, characterDatabase);
        SetupMainMenuScene(characterDatabase);
        ConfigureBuildSettings();
        ConfigureAndroidSettings();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("Pixel Village setup complete. Game and MainMenu scenes were configured and saved.");
    }

    [MenuItem("Tools/Pixel Village/Add 100 Flowers")]
    public static void Add100Flowers()
    {
        CharacterProgress.AddFlowers(100);
        Debug.Log($"Added 100 flowers. TotalFlowers is now {CharacterProgress.TotalFlowers}.");
    }

    [MenuItem("Tools/Pixel Village/Reset Character Shop")]
    public static void ResetCharacterShop()
    {
        CharacterDatabase characterDatabase = AssetDatabase.LoadAssetAtPath<CharacterDatabase>(CharacterDatabasePath);
        CharacterProgress.ResetCharacterShop(characterDatabase);
        Debug.Log("Character shop PlayerPrefs were reset. Default character remains unlocked.");
    }

    [MenuItem("Tools/Pixel Village/Setup Level Progression")]
    public static void SetupLevelProgression()
    {
        List<GameplaySceneInfo> gameplayScenes = FindGameplayScenes();
        ValidateGameplaySceneSequence(gameplayScenes);
        ConfigureBuildSettings(gameplayScenes);
        ConfigureMainMenuForLevelProgression();

        foreach (GameplaySceneInfo gameplayScene in gameplayScenes)
        {
            ConfigureGameplaySceneForLevelProgression(gameplayScene, gameplayScenes.Count);
        }

        ConfigureAndroidSettings();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"Level progression setup complete. Found {gameplayScenes.Count} gameplay levels.");
    }

    private static PlayerAnimationFrames LoadPlayerAnimationFrames()
    {
        Sprite[] idleSprites = LoadOrderedSprites(FindPuppeteerSpriteSheet("the_puppet_idle"));
        return new PlayerAnimationFrames
        {
            Idle = idleSprites,
            Run = LoadOrderedSprites(FindPuppeteerSpriteSheet("the_puppet_run")),
            Air = LoadOrderedSprites(FindPuppeteerSpriteSheet("the_puppet_air")),
            Death = LoadOrderedSprites(FindPuppeteerSpriteSheet("the_puppet_death")),
            FirstIdleSprite = idleSprites.Length > 0 ? idleSprites[0] : null
        };
    }

    private static string FindPuppeteerSpriteSheet(string sheetName)
    {
        string expectedPath = $"{PuppeteerFolder}/{sheetName}-Sheet.png";
        if (AssetDatabase.LoadAssetAtPath<Texture2D>(expectedPath) != null)
        {
            return expectedPath;
        }

        string[] guids = AssetDatabase.FindAssets($"{sheetName} t:Texture2D", new[] { PuppeteerFolder });
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            string fileName = System.IO.Path.GetFileNameWithoutExtension(path);
            if (fileName.ToLowerInvariant().Contains(sheetName.ToLowerInvariant()))
            {
                return path;
            }
        }

        Debug.LogError($"Could not locate Puppeteer Grey sprite sheet containing '{sheetName}' under {PuppeteerFolder}.");
        return expectedPath;
    }

    private static Sprite[] LoadOrderedSprites(string sheetPath)
    {
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(sheetPath);
        List<Sprite> sprites = new List<Sprite>();
        foreach (Object asset in assets)
        {
            if (asset is Sprite sprite)
            {
                sprites.Add(sprite);
            }
        }

        sprites.Sort(CompareSpritesByFrameOrder);

        if (sprites.Count == 0)
        {
            Debug.LogError($"No Sprite sub-assets were found in Puppeteer sheet: {sheetPath}");
        }

        return sprites.ToArray();
    }

    private static int CompareSpritesByFrameOrder(Sprite left, Sprite right)
    {
        int leftIndex = ExtractTrailingFrameIndex(left.name);
        int rightIndex = ExtractTrailingFrameIndex(right.name);
        if (leftIndex >= 0 && rightIndex >= 0 && leftIndex != rightIndex)
        {
            return leftIndex.CompareTo(rightIndex);
        }

        int xComparison = left.rect.x.CompareTo(right.rect.x);
        if (xComparison != 0)
        {
            return xComparison;
        }

        return string.CompareOrdinal(left.name, right.name);
    }

    private static int ExtractTrailingFrameIndex(string spriteName)
    {
        int separatorIndex = spriteName.LastIndexOf('_');
        if (separatorIndex < 0 || separatorIndex == spriteName.Length - 1)
        {
            return -1;
        }

        return int.TryParse(spriteName.Substring(separatorIndex + 1), out int frameIndex) ? frameIndex : -1;
    }

    private static void SetupGameScene(PlayerAnimationFrames playerAnimationFrames, CharacterDatabase characterDatabase)
    {
        Scene scene = EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Single);

        int groundLayer = EnsureLayer("Ground");
        int playerLayer = EnsureLayer("Player");
        int deathZoneLayer = EnsureLayer("DeathZone");

        GameObject gameManagerObject = FindOrCreateRoot(scene, "GameManager");
        GameManager gameManager = GetOrAdd<GameManager>(gameManagerObject);

        GameObject playerObject = FindInScene(scene, "Player");
        if (playerObject == null)
        {
            Debug.LogError("Setup could not find the existing Player object in the Game scene.");
            return;
        }

        GameObject visualObject = FindChild(playerObject.transform, "Visual");
        if (visualObject == null)
        {
            visualObject = new GameObject("Visual", typeof(SpriteRenderer), typeof(Animator));
            visualObject.transform.SetParent(playerObject.transform, false);
            MarkCreated(visualObject);
        }

        ConfigurePlayer(playerObject, visualObject, playerAnimationFrames, characterDatabase, playerLayer, groundLayer);

        PlayerController player = playerObject.GetComponent<PlayerController>();
        GameObject gameplayObject = FindOrCreateRoot(scene, "Gameplay");
        Transform playerSpawn = EnsurePlayerSpawn(gameplayObject.transform, playerObject.transform.position);
        GameObject groundObject = FindInScene(scene, "Ground");
        ConfigureGround(groundObject, groundLayer);
        GameObject deathZoneObject = ConfigureDeathZone(gameplayObject.transform, groundObject, gameManager, deathZoneLayer);
        GameObject finishChest = ConfigureFinishChest(scene, gameManager);

        GameHudRefs hudRefs = SetupGameHud(scene, gameManager, player);
        ConfigureCamera(scene, playerObject.transform);
        ConfigureGameBackground(scene);
        EnsureSingleEventSystem(scene);

        SetObjectReference(gameManager, "player", player);
        SetObjectReference(gameManager, "playerRespawn", playerObject.GetComponent<PlayerRespawn>());
        SetObjectReference(gameManager, "playerSpawn", playerSpawn);
        SetObjectReference(gameManager, "levelProgression", GetOrAdd<LevelProgression>(gameManagerObject));
        SetObjectReference(gameManager, "gameHUD", hudRefs.GameHUD);
        SetObjectReference(gameManager, "timerText", hudRefs.TimerText);
        SetObjectReference(gameManager, "levelText", hudRefs.LevelText);
        SetObjectReference(gameManager, "pausePanel", hudRefs.PausePanel);
        SetObjectReference(gameManager, "winPanel", hudRefs.WinPanel);
        SetObjectReference(gameManager, "winTitleText", hudRefs.WinTitleText);
        SetObjectReference(gameManager, "winMessageText", hudRefs.WinMessageText);
        SetObjectReference(gameManager, "winTimeText", hudRefs.WinTimeText);
        SetObjectReference(gameManager, "winBestTimeText", hudRefs.WinBestTimeText);
        SetObjectReference(gameManager, "winPrimaryButton", hudRefs.WinPrimaryButton);
        SetObjectReference(gameManager, "winPrimaryButtonText", hudRefs.WinPrimaryButtonText);
        SetObjectReference(gameManager, "gameOverPanel", hudRefs.GameOverPanel);
        SetString(gameManager, "gameSceneName", "Game");
        SetString(gameManager, "mainMenuSceneName", "MainMenu");
        SetFloat(gameManager, "respawnDelay", 0.8f);
        SetInt(gameManager, "maxLives", 3);
        SetFloat(gameManager, "damageInvulnerabilityDuration", 1f);
        SetFloat(gameManager, "damageFlashInterval", 0.1f);

        SetObjectReference(deathZoneObject.GetComponent<DeathZone>(), "gameManager", gameManager);

        if (finishChest != null)
        {
            SetObjectReference(finishChest.GetComponent<FinishTrigger>(), "gameManager", gameManager);
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    private static void ConfigurePlayer(GameObject playerObject, GameObject visualObject, PlayerAnimationFrames playerAnimationFrames, CharacterDatabase characterDatabase, int playerLayer, int groundLayer)
    {
        playerObject.layer = playerLayer >= 0 ? playerLayer : playerObject.layer;

        Rigidbody2D body = GetOrAdd<Rigidbody2D>(playerObject);
        body.bodyType = RigidbodyType2D.Dynamic;
        body.gravityScale = 3.5f;
        body.constraints = RigidbodyConstraints2D.FreezeRotation;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        CapsuleCollider2D capsule = GetOrAdd<CapsuleCollider2D>(playerObject);
        capsule.isTrigger = false;
        capsule.direction = CapsuleDirection2D.Vertical;

        SpriteRenderer spriteRenderer = GetOrAdd<SpriteRenderer>(visualObject);
        spriteRenderer.sortingOrder = 20;
        if (playerAnimationFrames.FirstIdleSprite != null)
        {
            spriteRenderer.sprite = playerAnimationFrames.FirstIdleSprite;
        }

        Bounds visualBounds = spriteRenderer.sprite != null
            ? spriteRenderer.bounds
            : new Bounds(playerObject.transform.position + Vector3.up, new Vector3(0.8f, 1.8f, 0.1f));
        Vector3 localCenter = playerObject.transform.InverseTransformPoint(visualBounds.center);
        Vector3 localBottom = playerObject.transform.InverseTransformPoint(new Vector3(visualBounds.center.x, visualBounds.min.y, visualBounds.center.z));
        float scaleX = Mathf.Max(0.01f, Mathf.Abs(playerObject.transform.lossyScale.x));
        float scaleY = Mathf.Max(0.01f, Mathf.Abs(playerObject.transform.lossyScale.y));
        Vector2 colliderSize = new Vector2(
            Mathf.Max(0.45f, visualBounds.size.x / scaleX * 0.55f),
            Mathf.Max(1.1f, visualBounds.size.y / scaleY * 0.86f));
        capsule.size = colliderSize;
        capsule.offset = new Vector2(localCenter.x, localBottom.y + colliderSize.y * 0.5f + 0.03f);

        Animator animator = visualObject.GetComponent<Animator>();
        if (animator != null)
        {
            animator.runtimeAnimatorController = null;
            animator.applyRootMotion = false;
            animator.enabled = false;
            EditorUtility.SetDirty(animator);
        }

        PlayerController controller = GetOrAdd<PlayerController>(playerObject);
        PlayerAnimator playerAnimator = playerObject.GetComponent<PlayerAnimator>();
        if (playerAnimator != null)
        {
            playerAnimator.enabled = false;
            SetObjectReference(playerAnimator, "animator", null);
            EditorUtility.SetDirty(playerAnimator);
        }

        SpriteFrameAnimator spriteFrameAnimator = GetOrAdd<SpriteFrameAnimator>(visualObject);

        SetObjectReference(controller, "body", body);
        SetObjectReference(controller, "bodyCollider", capsule);
        SetObjectReference(controller, "visualRenderer", spriteRenderer);
        SetObjectReference(controller, "playerAnimator", null);
        SetInt(controller, "groundLayers", groundLayer >= 0 ? 1 << groundLayer : Physics2D.DefaultRaycastLayers);
        SetFloat(controller, "moveSpeed", 5.5f);
        SetFloat(controller, "acceleration", 55f);
        SetFloat(controller, "deceleration", 70f);
        SetFloat(controller, "jumpForce", 9.5f);
        SetFloat(controller, "gravityScale", 3.5f);
        SetFloat(controller, "fallMultiplier", 1.65f);
        SetFloat(controller, "groundCheckRadius", 0.12f);
        SetFloat(controller, "groundCheckDistance", 0.08f);
        SetObjectReference(spriteFrameAnimator, "player", controller);
        SetObjectReference(spriteFrameAnimator, "spriteRenderer", spriteRenderer);
        SetSpriteArray(spriteFrameAnimator, "idleFrames", playerAnimationFrames.Idle);
        SetSpriteArray(spriteFrameAnimator, "runFrames", playerAnimationFrames.Run);
        SetSpriteArray(spriteFrameAnimator, "airFrames", playerAnimationFrames.Air);
        SetSpriteArray(spriteFrameAnimator, "deathFrames", playerAnimationFrames.Death);
        SetFloat(spriteFrameAnimator, "idleFPS", 12f);
        SetFloat(spriteFrameAnimator, "runFPS", 12f);
        SetFloat(spriteFrameAnimator, "airFPS", 12f);
        SetFloat(spriteFrameAnimator, "deathFPS", 12f);
        SetFloat(spriteFrameAnimator, "runSpeedThreshold", 0.05f);

        PlayerRespawn playerRespawn = GetOrAdd<PlayerRespawn>(playerObject);
        SetObjectReference(playerRespawn, "player", controller);
        SetObjectReference(playerRespawn, "body", body);
        SetObjectReference(playerRespawn, "bodyCollider", capsule);
        SetInt(playerRespawn, "groundLayers", groundLayer >= 0 ? 1 << groundLayer : Physics2D.DefaultRaycastLayers);
        SetFloat(playerRespawn, "safeGroundedSeconds", 0.16f);
        SetFloat(playerRespawn, "groundProbeDistance", 0.22f);
        SetFloat(playerRespawn, "respawnYOffset", 0.65f);

        GameObject characterVisualRoot = FindChild(playerObject.transform, "CharacterVisualRoot");
        if (characterVisualRoot == null)
        {
            characterVisualRoot = new GameObject("CharacterVisualRoot");
            characterVisualRoot.transform.SetParent(playerObject.transform, false);
            MarkCreated(characterVisualRoot);
        }

        RemoveChildren(characterVisualRoot.transform);

        PlayerCharacterLoader characterLoader = GetOrAdd<PlayerCharacterLoader>(playerObject);
        SetObjectReference(characterLoader, "characterDatabase", characterDatabase);
        SetObjectReference(characterLoader, "player", controller);
        SetObjectReference(characterLoader, "characterVisualRoot", characterVisualRoot.transform);
        SetObjectReference(characterLoader, "fallbackVisual", visualObject);
        SetInt(characterLoader, "sortingOrder", 20);
    }

    private static void ConfigureGround(GameObject groundObject, int groundLayer)
    {
        if (groundObject == null)
        {
            Debug.LogWarning("Ground Tilemap was not found. Player ground checks may need a manual ground layer assignment.");
            return;
        }

        groundObject.layer = groundLayer >= 0 ? groundLayer : groundObject.layer;

        TilemapCollider2D tilemapCollider = GetOrAdd<TilemapCollider2D>(groundObject);
        tilemapCollider.isTrigger = false;

        Rigidbody2D body = groundObject.GetComponent<Rigidbody2D>();
        if (body != null)
        {
            body.bodyType = RigidbodyType2D.Static;
        }

        Tilemap tilemap = groundObject.GetComponent<Tilemap>();
        if (tilemap != null)
        {
            tilemap.CompressBounds();
        }
    }

    private static Transform EnsurePlayerSpawn(Transform gameplayRoot, Vector3 playerPosition)
    {
        GameObject spawn = FindChild(gameplayRoot, "PlayerSpawn");
        if (spawn == null)
        {
            spawn = new GameObject("PlayerSpawn");
            spawn.transform.SetParent(gameplayRoot, false);
            spawn.transform.position = playerPosition;
            MarkCreated(spawn);
        }

        return spawn.transform;
    }

    private static GameObject ConfigureDeathZone(Transform gameplayRoot, GameObject groundObject, GameManager gameManager, int deathZoneLayer)
    {
        GameObject deathZone = FindChild(gameplayRoot, "DeathZone");
        if (deathZone == null)
        {
            deathZone = new GameObject("DeathZone");
            deathZone.transform.SetParent(gameplayRoot, false);
            MarkCreated(deathZone);
        }

        deathZone.layer = deathZoneLayer >= 0 ? deathZoneLayer : deathZone.layer;
        Bounds levelBounds = GetWorldBounds(groundObject);
        float width = Mathf.Max(36f, levelBounds.size.x + 20f);
        deathZone.transform.position = new Vector3(levelBounds.center.x, levelBounds.min.y - 4f, 0f);

        BoxCollider2D trigger = GetOrAdd<BoxCollider2D>(deathZone);
        trigger.isTrigger = true;
        trigger.offset = Vector2.zero;
        trigger.size = new Vector2(width, 4f);

        DeathZone deathZoneComponent = GetOrAdd<DeathZone>(deathZone);
        SetObjectReference(deathZoneComponent, "gameManager", gameManager);
        return deathZone;
    }

    private static GameObject ConfigureFinishChest(Scene scene, GameManager gameManager)
    {
        GameObject finishChest = FindInScene(scene, "FinishChest");
        if (finishChest == null)
        {
            Debug.LogWarning("FinishChest was not found. Add FinishTrigger manually to the chest if it was renamed.");
            return null;
        }

        Collider2D trigger = finishChest.GetComponent<Collider2D>();
        if (trigger == null)
        {
            trigger = finishChest.AddComponent<BoxCollider2D>();
        }

        trigger.isTrigger = true;
        if (trigger is BoxCollider2D box)
        {
            Bounds bounds = GetWorldBounds(finishChest);
            Vector3 localCenter = finishChest.transform.InverseTransformPoint(bounds.center);
            Vector3 localMin = finishChest.transform.InverseTransformPoint(bounds.min);
            Vector3 localMax = finishChest.transform.InverseTransformPoint(bounds.max);
            box.offset = localCenter;
            box.size = new Vector2(Mathf.Max(1f, Mathf.Abs(localMax.x - localMin.x)), Mathf.Max(1f, Mathf.Abs(localMax.y - localMin.y)));
        }

        FinishTrigger finishTrigger = GetOrAdd<FinishTrigger>(finishChest);
        SetObjectReference(finishTrigger, "gameManager", gameManager);
        SetObjectReference(finishTrigger, "chestAnimator", finishChest.GetComponentInChildren<Animator>());
        SetBool(finishTrigger, "openChest", true);
        return finishChest;
    }

    private static GameHudRefs SetupGameHud(Scene scene, GameManager gameManager, PlayerController player)
    {
        GameObject heartPrefab = FindHeartPrefab();
        Sprite flowerSprite = GetFlowerSourceSprite(FindFlowerSourcePrefab());

        Transform uiMarker = FindInScene(scene, "--- UI ---")?.transform;
        GameObject hud = FindChild(uiMarker, "GameHUD");
        if (hud == null)
        {
            hud = new GameObject("GameHUD", typeof(RectTransform));
            if (uiMarker != null)
            {
                hud.transform.SetParent(uiMarker, false);
            }
            else
            {
                SceneManager.MoveGameObjectToScene(hud, scene);
            }
            MarkCreated(hud);
        }

        RemoveChildren(hud.transform);
        int uiLayer = LayerMask.NameToLayer("UI");
        if (uiLayer >= 0)
        {
            hud.layer = uiLayer;
        }

        Canvas canvas = GetOrAdd<Canvas>(hud);
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;

        CanvasScaler scaler = GetOrAdd<CanvasScaler>(hud);
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        GetOrAdd<GraphicRaycaster>(hud);
        GameHUD gameHud = GetOrAdd<GameHUD>(hud);
        SetObjectReference(gameHud, "gameManager", gameManager);

        RectTransform safeArea = CreateRect("SafeArea", hud.transform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        GetOrAdd<SafeArea>(safeArea.gameObject);

        RectTransform header = CreateRect("HUD", safeArea, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

        Button pauseButton = CreateButton("PauseButton", header, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(110f, 78f), new Vector2(36f, -36f), "II", 34);
        SetButtonListener(pauseButton, gameManager.PauseGame);

        GameObject[] hearts = CreateHeartIcons(header, heartPrefab);
        SetObjectArray(gameHud, "hearts", hearts);

        TMP_Text flowerCountText = CreateFlowerCounter(header, flowerSprite);
        SetObjectReference(gameHud, "flowerCountText", flowerCountText);

        Text timerText = CreateLabel("TimerText", header, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(250f, 74f), new Vector2(-38f, -38f), "Time: 00:00", 34);
        timerText.alignment = TextAnchor.MiddleRight;
        Text levelText = CreateLabel("LevelText", header, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(300f, 46f), new Vector2(0f, -42f), "LEVEL 1 / 71", 24);
        levelText.color = new Color(1f, 0.95f, 0.82f, 0.9f);

        RectTransform moveGroup = CreateRect("MoveControls", safeArea, Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(430f, 170f), new Vector2(52f, 52f));
        Button leftButton = CreateButton("LeftButton", moveGroup, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(180f, 150f), new Vector2(0f, 0f), "LEFT", 30);
        Button rightButton = CreateButton("RightButton", moveGroup, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(180f, 150f), new Vector2(210f, 0f), "RIGHT", 30);

        ConfigureMoveButton(leftButton, player, -1);
        ConfigureMoveButton(rightButton, player, 1);

        Button jumpButton = CreateButton("JumpButton", safeArea, Vector2.one, Vector2.one, Vector2.one, new Vector2(210f, 170f), new Vector2(-58f, 56f), "JUMP", 32);
        ConfigureJumpButton(jumpButton, player);

        GameObject pausePanel = CreateOverlayPanel("PausePanel", safeArea, "PAUSED");
        Button resumeButton = CreateButton("ResumeButton", pausePanel.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(360f, 88f), new Vector2(0f, 30f), "RESUME", 30);
        Button restartButton = CreateButton("RestartButton", pausePanel.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(360f, 88f), new Vector2(0f, -80f), "RESTART", 30);
        Button mainMenuButton = CreateButton("MainMenuButton", pausePanel.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(360f, 88f), new Vector2(0f, -190f), "MAIN MENU", 28);
        SetButtonListener(resumeButton, gameManager.ResumeGame);
        SetButtonListener(restartButton, gameManager.RestartLevel);
        SetButtonListener(mainMenuButton, gameManager.GoToMainMenu);
        pausePanel.SetActive(false);

        GameObject winPanel = CreateOverlayPanel("WinPanel", safeArea, "LEVEL COMPLETE");
        Text winTimeText = CreateLabel("WinTimeText", winPanel.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(520f, 58f), new Vector2(0f, 58f), "Time: 00.00", 28);
        Text winBestText = CreateLabel("WinBestTimeText", winPanel.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(520f, 58f), new Vector2(0f, 12f), "Best: 00.00", 28);
        Button playAgainButton = CreateButton("PlayAgainButton", winPanel.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(360f, 88f), new Vector2(0f, -86f), "NEXT LEVEL", 28);
        Button winMenuButton = CreateButton("WinMainMenuButton", winPanel.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(360f, 88f), new Vector2(0f, -196f), "MAIN MENU", 28);
        SetButtonListener(playAgainButton, gameManager.ContinueAfterWin);
        SetButtonListener(winMenuButton, gameManager.GoToMainMenu);
        winPanel.SetActive(false);

        GameObject gameOverPanel = CreateOverlayPanel("GameOverPanel", safeArea, "YOU LOST");
        Button tryAgainButton = CreateButton("TryAgainButton", gameOverPanel.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(360f, 88f), new Vector2(0f, -38f), "TRY AGAIN", 28);
        Button lostMenuButton = CreateButton("LostMainMenuButton", gameOverPanel.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(360f, 88f), new Vector2(0f, -148f), "MAIN MENU", 28);
        SetButtonListener(tryAgainButton, gameManager.RestartLevel);
        SetButtonListener(lostMenuButton, gameManager.GoToMainMenu);
        gameOverPanel.SetActive(false);

        return new GameHudRefs
        {
            GameHUD = gameHud,
            TimerText = timerText,
            LevelText = levelText,
            PausePanel = pausePanel,
            WinPanel = winPanel,
            WinTitleText = FindChild(winPanel.transform, "Title")?.GetComponent<Text>(),
            WinMessageText = EnsureWinMessageText(winPanel.transform),
            WinTimeText = winTimeText,
            WinBestTimeText = winBestText,
            WinPrimaryButton = playAgainButton,
            WinPrimaryButtonText = playAgainButton.GetComponentInChildren<Text>(true),
            GameOverPanel = gameOverPanel
        };
    }

    private static GameObject[] CreateHeartIcons(Transform parent, GameObject heartPrefab)
    {
        RectTransform container = CreateRect("LivesContainer", parent, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(240f, 78f), new Vector2(168f, -36f));
        GameObject[] hearts = new GameObject[3];
        for (int i = 0; i < hearts.Length; i++)
        {
            GameObject heartObject = null;
            if (heartPrefab != null)
            {
                heartObject = PrefabUtility.InstantiatePrefab(heartPrefab) as GameObject;
            }

            if (heartObject == null)
            {
                RectTransform fallbackRect = CreateRect($"Heart_{i + 1}", container, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(56f, 56f), new Vector2(i * 62f, 0f));
                Image fallbackImage = GetOrAdd<Image>(fallbackRect.gameObject);
                fallbackImage.color = new Color(1f, 0.08f, 0.12f, 1f);
                heartObject = fallbackRect.gameObject;
            }
            else
            {
                heartObject.name = $"Heart_{i + 1}";
                heartObject.transform.SetParent(container, false);
            }

            RectTransform rect = GetOrAdd<RectTransform>(heartObject);
            rect.anchorMin = new Vector2(0f, 0.5f);
            rect.anchorMax = new Vector2(0f, 0.5f);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.sizeDelta = new Vector2(56f, 56f);
            rect.anchoredPosition = new Vector2(i * 62f, 0f);
            rect.localScale = Vector3.one;
            rect.localRotation = Quaternion.identity;

            foreach (Graphic graphic in heartObject.GetComponentsInChildren<Graphic>(true))
            {
                graphic.raycastTarget = false;
            }

            hearts[i] = heartObject;
        }

        return hearts;
    }

    private static TMP_Text CreateFlowerCounter(Transform parent, Sprite flowerSprite)
    {
        RectTransform counter = CreateRect("FlowerCounter", parent, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(210f, 74f), new Vector2(-312f, -38f));

        RectTransform iconRect = CreateRect("FlowerIcon", counter, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(48f, 48f), new Vector2(0f, 0f));
        Image icon = GetOrAdd<Image>(iconRect.gameObject);
        icon.sprite = flowerSprite;
        icon.preserveAspect = true;
        icon.raycastTarget = false;
        icon.color = flowerSprite != null ? Color.white : new Color(1f, 0.84f, 0.18f, 1f);

        RectTransform countRect = CreateRect("CountText", counter, new Vector2(0f, 0f), Vector2.one, new Vector2(0f, 0.5f), new Vector2(-64f, 0f), new Vector2(64f, 0f));
        TextMeshProUGUI countText = GetOrAdd<TextMeshProUGUI>(countRect.gameObject);
        countText.text = "x 0";
        countText.fontSize = 34f;
        countText.enableAutoSizing = true;
        countText.fontSizeMin = 20f;
        countText.fontSizeMax = 34f;
        countText.alignment = TextAlignmentOptions.Left;
        countText.color = TextColor;
        countText.raycastTarget = false;
        return countText;
    }

    private static void CreateOrUpdateFlowerCollectiblePrefab()
    {
        GameObject flowerSource = FindFlowerSourcePrefab();
        if (flowerSource == null)
        {
            Debug.LogWarning("Could not find Cainos PF Village Props - Flower 01 prefab. FlowerCollectible prefab was not created.");
            return;
        }

        GameObject root = new GameObject("FlowerCollectible");
        try
        {
            GameObject visual = PrefabUtility.InstantiatePrefab(flowerSource) as GameObject;
            if (visual != null)
            {
                visual.name = "Visual";
                visual.transform.SetParent(root.transform, false);
                visual.transform.localPosition = Vector3.zero;
                visual.transform.localRotation = Quaternion.identity;
                visual.transform.localScale = Vector3.one;
            }

            BoxCollider2D trigger = GetOrAdd<BoxCollider2D>(root);
            trigger.isTrigger = true;
            CollectibleFlower collectible = GetOrAdd<CollectibleFlower>(root);

            SpriteRenderer renderer = root.GetComponentInChildren<SpriteRenderer>(true);
            if (renderer != null && renderer.sprite != null)
            {
                Vector2 spriteSize = renderer.sprite.bounds.size;
                trigger.size = new Vector2(Mathf.Max(0.35f, spriteSize.x * 0.9f), Mathf.Max(0.38f, spriteSize.y * 1.25f));
                trigger.offset = new Vector2(0f, trigger.size.y * 0.38f);
                renderer.sortingOrder = Mathf.Max(renderer.sortingOrder, 5);
            }
            else
            {
                trigger.size = new Vector2(0.45f, 0.55f);
                trigger.offset = new Vector2(0f, 0.2f);
            }

            foreach (Rigidbody2D body in root.GetComponentsInChildren<Rigidbody2D>(true))
            {
                Object.DestroyImmediate(body);
            }

            PrefabUtility.SaveAsPrefabAsset(root, FlowerCollectiblePrefabPath);
            EditorUtility.SetDirty(collectible);
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    private static CharacterDatabase CreateOrUpdateCharacterDatabase()
    {
        EnsureFolder(CharacterDataFolder);
        EnsureFolder(CharacterDefinitionFolder);

        List<CharacterDefinition> definitions = new List<CharacterDefinition>();
        HashSet<string> usedIds = new HashSet<string>();
        int createdDefinitionCount = 0;
        string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { CharacterSourceFolder });
        List<string> prefabPaths = new List<string>();
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (!path.StartsWith(CharacterSourceFolder + "/", System.StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (path.StartsWith(EnemySourceFolder + "/", System.StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            prefabPaths.Add(path);
        }

        prefabPaths.Sort(System.StringComparer.OrdinalIgnoreCase);

        foreach (string prefabPath in prefabPaths)
        {
            GameObject characterPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (!IsCompatibleCharacterPrefab(characterPrefab))
            {
                continue;
            }

            string displayName = System.IO.Path.GetFileNameWithoutExtension(prefabPath);
            string assetPath = $"{CharacterDefinitionFolder}/{SanitizeAssetName(displayName)}.asset";
            CharacterDefinition definition = AssetDatabase.LoadAssetAtPath<CharacterDefinition>(assetPath);
            bool created = definition == null;
            if (created)
            {
                definition = ScriptableObject.CreateInstance<CharacterDefinition>();
                AssetDatabase.CreateAsset(definition, assetPath);
                createdDefinitionCount++;
            }

            string id = GetSerializedString(definition, "id");
            if (string.IsNullOrWhiteSpace(id))
            {
                id = MakeUniqueId(MakeStableId(displayName), usedIds);
                SetString(definition, "id", id);
            }
            else if (!usedIds.Add(id))
            {
                id = MakeUniqueId(MakeStableId(displayName), usedIds);
                SetString(definition, "id", id);
            }

            if (string.IsNullOrWhiteSpace(GetSerializedString(definition, "displayName")))
            {
                SetString(definition, "displayName", displayName);
            }

            bool isDefaultCharacter = displayName.Equals("Puppeteer Grey", System.StringComparison.OrdinalIgnoreCase);
            SetObjectReference(definition, "characterPrefab", characterPrefab);
            SetObjectReference(definition, "previewSprite", GetPreviewSprite(characterPrefab));
            if (isDefaultCharacter)
            {
                SetInt(definition, "price", 0);
                SetBool(definition, "unlockedByDefault", true);
            }
            else if (created)
            {
                SetInt(definition, "price", 25);
                SetBool(definition, "unlockedByDefault", false);
            }

            EditorUtility.SetDirty(definition);
            definitions.Add(definition);
        }

        definitions.Sort((left, right) => string.Compare(left.DisplayName, right.DisplayName, System.StringComparison.OrdinalIgnoreCase));
        if (definitions.Count == 0)
        {
            Debug.LogWarning($"Found 0 playable character prefabs. Searched '{CharacterSourceFolder}' recursively and excluded '{EnemySourceFolder}'.");
        }
        else
        {
            Debug.Log($"Found {definitions.Count} playable character prefabs under {CharacterSourceFolder}. Created {createdDefinitionCount} character definitions.");
        }

        CharacterDatabase database = AssetDatabase.LoadAssetAtPath<CharacterDatabase>(CharacterDatabasePath);
        if (database == null)
        {
            database = ScriptableObject.CreateInstance<CharacterDatabase>();
            AssetDatabase.CreateAsset(database, CharacterDatabasePath);
        }

        SetObjectArray(database, "characters", definitions.ToArray());
        EditorUtility.SetDirty(database);
        CharacterProgress.EnsureDefaults(database);
        Debug.Log($"CharacterDatabase contains {definitions.Count} entries: {CharacterDatabasePath}");
        return database;
    }

    private static void CreateOrUpdateEnemyPrefabs()
    {
        EnsureFolder(EnemyPrefabFolder);

        int groundLayer = EnsureLayer("Ground");
        int enemyLayer = EnsureLayer("Enemy");
        int groundMask = groundLayer >= 0 ? 1 << groundLayer : Physics2D.DefaultRaycastLayers;
        string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { EnemySourceFolder });
        List<string> enemySourcePaths = new List<string>();
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (!path.StartsWith(EnemySourceFolder + "/", System.StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (IsEnemyUtilityPrefab(path))
            {
                continue;
            }

            enemySourcePaths.Add(path);
        }

        enemySourcePaths.Sort(System.StringComparer.OrdinalIgnoreCase);
        foreach (string sourcePath in enemySourcePaths)
        {
            GameObject sourcePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(sourcePath);
            if (sourcePrefab == null || sourcePrefab.GetComponentInChildren<SpriteRenderer>(true) == null)
            {
                continue;
            }

            CreateOrUpdateEnemyPrefab(sourcePrefab, sourcePath, enemyLayer, groundMask);
        }
    }

    private static void CreateOrUpdateEnemyPrefab(GameObject sourcePrefab, string sourcePath, int enemyLayer, int groundMask)
    {
        string sourceName = System.IO.Path.GetFileNameWithoutExtension(sourcePath);
        string prefabName = "Enemy_" + SanitizeIdentifier(sourceName);
        string prefabPath = $"{EnemyPrefabFolder}/{prefabName}.prefab";

        GameObject root = new GameObject(prefabName);
        try
        {
            if (enemyLayer >= 0)
            {
                SetLayerRecursively(root, enemyLayer);
            }

            GameObject visual = PrefabUtility.InstantiatePrefab(sourcePrefab) as GameObject;
            if (visual != null)
            {
                visual.name = "Visual";
                visual.transform.SetParent(root.transform, false);
                visual.transform.localPosition = Vector3.zero;
                visual.transform.localRotation = Quaternion.identity;
                visual.transform.localScale = Vector3.one;
                StripGameplayComponents(visual);
                ConfigureSpriteRenderers(visual, 12);
                if (enemyLayer >= 0)
                {
                    SetLayerRecursively(visual, enemyLayer);
                }
            }

            Rigidbody2D body = GetOrAdd<Rigidbody2D>(root);
            body.bodyType = RigidbodyType2D.Kinematic;
            body.gravityScale = 0f;
            body.freezeRotation = true;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            BoxCollider2D trigger = GetOrAdd<BoxCollider2D>(root);
            trigger.isTrigger = true;
            FitBoxColliderToRenderers(root, trigger, new Vector2(0.62f, 0.86f), new Vector2(0f, 0.03f));

            EnemyPatrol patrol = GetOrAdd<EnemyPatrol>(root);
            SetFloat(patrol, "moveSpeed", 1.5f);
            SetFloat(patrol, "patrolDistance", 3f);
            SetInt(patrol, "groundLayers", groundMask);
            SetObjectReference(patrol, "visualRoot", visual != null ? visual.transform : root.transform);
            SetFloat(patrol, "edgeCheckDistance", 0.65f);

            EnemyDamage damage = GetOrAdd<EnemyDamage>(root);
            SetInt(damage, "damage", 1);
            SetFloat(damage, "hitCooldown", 0.25f);

            GameObject attackOrigin = new GameObject("AttackOrigin");
            attackOrigin.transform.SetParent(root.transform, false);
            attackOrigin.transform.localPosition = new Vector3(0.45f, 0.2f, 0f);
            if (enemyLayer >= 0)
            {
                attackOrigin.layer = enemyLayer;
            }

            GameObject goopAttackSource = FindEnemyUtilityPrefab(sourceName, "Attack");
            if (goopAttackSource != null)
            {
                GameObject projectilePrefab = CreateOrUpdateEnemyProjectilePrefab(sourceName, goopAttackSource, enemyLayer);
                EnemyRangedAttack rangedAttack = GetOrAdd<EnemyRangedAttack>(root);
                SetBool(rangedAttack, "attackEnabled", true);
                SetFloat(rangedAttack, "attackRange", 5f);
                SetFloat(rangedAttack, "attackCooldown", 2f);
                SetFloat(rangedAttack, "projectileSpeed", 4f);
                SetInt(rangedAttack, "damage", 1);
                SetObjectReference(rangedAttack, "attackOrigin", attackOrigin.transform);
                SetObjectReference(rangedAttack, "projectilePrefab", projectilePrefab);
            }

            GameObject weaponSource = FindEnemyUtilityPrefab(sourceName, "Weapon");
            if (weaponSource != null)
            {
                GameObject weaponVisual = PrefabUtility.InstantiatePrefab(weaponSource) as GameObject;
                if (weaponVisual != null)
                {
                    weaponVisual.name = "WeaponVisual";
                    weaponVisual.transform.SetParent(root.transform, false);
                    weaponVisual.transform.localPosition = new Vector3(0.42f, 0.15f, 0f);
                    weaponVisual.transform.localRotation = Quaternion.identity;
                    weaponVisual.transform.localScale = Vector3.one;
                    StripGameplayComponents(weaponVisual);
                    ConfigureSpriteRenderers(weaponVisual, 14);
                    weaponVisual.SetActive(false);
                    if (enemyLayer >= 0)
                    {
                        SetLayerRecursively(weaponVisual, enemyLayer);
                    }

                    EnemyMeleeAttack meleeAttack = GetOrAdd<EnemyMeleeAttack>(root);
                    SetBool(meleeAttack, "attackEnabled", true);
                    SetFloat(meleeAttack, "attackRange", 1.35f);
                    SetFloat(meleeAttack, "attackCooldown", 1.8f);
                    SetFloat(meleeAttack, "activeTime", 0.18f);
                    SetInt(meleeAttack, "damage", 1);
                    SetObjectReference(meleeAttack, "weaponVisual", weaponVisual);
                }
            }

            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    private static GameObject CreateOrUpdateEnemyProjectilePrefab(string sourceEnemyName, GameObject attackSourcePrefab, int enemyLayer)
    {
        string prefabName = "Projectile_" + SanitizeIdentifier(sourceEnemyName) + "_Attack";
        string prefabPath = $"{EnemyPrefabFolder}/{prefabName}.prefab";
        GameObject root = new GameObject(prefabName);
        try
        {
            if (enemyLayer >= 0)
            {
                SetLayerRecursively(root, enemyLayer);
            }

            GameObject visual = PrefabUtility.InstantiatePrefab(attackSourcePrefab) as GameObject;
            if (visual != null)
            {
                visual.name = "Visual";
                visual.transform.SetParent(root.transform, false);
                visual.transform.localPosition = Vector3.zero;
                visual.transform.localRotation = Quaternion.identity;
                visual.transform.localScale = Vector3.one;
                StripGameplayComponents(visual);
                ConfigureSpriteRenderers(visual, 15);
                if (enemyLayer >= 0)
                {
                    SetLayerRecursively(visual, enemyLayer);
                }
            }

            CircleCollider2D trigger = GetOrAdd<CircleCollider2D>(root);
            trigger.isTrigger = true;
            FitCircleColliderToRenderers(root, trigger);

            EnemyProjectile projectile = GetOrAdd<EnemyProjectile>(root);
            SetFloat(projectile, "speed", 4f);
            SetInt(projectile, "damage", 1);
            SetFloat(projectile, "lifetime", 4f);

            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        }
        finally
        {
            Object.DestroyImmediate(root);
        }

        return AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
    }

    private static GameObject FindHeartPrefab()
    {
        const string expectedPath = "Assets/JazzCreate/JazzCreateMultiUI/Prefabs/Pre_Made_Prefabs/Heart.prefab";
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(expectedPath);
        if (prefab != null)
        {
            return prefab;
        }

        string[] guids = AssetDatabase.FindAssets("Heart t:Prefab", new[] { "Assets/JazzCreate" });
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (System.IO.Path.GetFileNameWithoutExtension(path) == "Heart")
            {
                return AssetDatabase.LoadAssetAtPath<GameObject>(path);
            }
        }

        Debug.LogWarning("Could not locate the JazzCreate Heart prefab. HUD will use a simple fallback heart marker.");
        return null;
    }

    private static GameObject FindFlowerSourcePrefab()
    {
        const string expectedPath = "Assets/Cainos/Pixel Art Platformer - Village Props/Prefab/PF Village Props - Flower 01.prefab";
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(expectedPath);
        if (prefab != null)
        {
            return prefab;
        }

        string[] guids = AssetDatabase.FindAssets("Flower 01 t:Prefab", new[] { "Assets/Cainos" });
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (System.IO.Path.GetFileNameWithoutExtension(path).Contains("Flower 01"))
            {
                return AssetDatabase.LoadAssetAtPath<GameObject>(path);
            }
        }

        return null;
    }

    private static Sprite GetFlowerSourceSprite(GameObject flowerSource)
    {
        if (flowerSource == null)
        {
            return null;
        }

        SpriteRenderer renderer = flowerSource.GetComponentInChildren<SpriteRenderer>(true);
        return renderer != null ? renderer.sprite : null;
    }

    private static bool IsCompatibleCharacterPrefab(GameObject prefab)
    {
        if (prefab == null)
        {
            return false;
        }

        return prefab.GetComponentInChildren<SpriteRenderer>(true) != null
            || prefab.GetComponentInChildren<Animator>(true) != null;
    }

    private static Sprite GetPreviewSprite(GameObject prefab)
    {
        if (prefab == null)
        {
            return null;
        }

        foreach (SpriteRenderer renderer in prefab.GetComponentsInChildren<SpriteRenderer>(true))
        {
            if (renderer.sprite != null)
            {
                return renderer.sprite;
            }
        }

        return null;
    }

    private static bool IsEnemyUtilityPrefab(string path)
    {
        string fileName = System.IO.Path.GetFileNameWithoutExtension(path);
        return fileName.EndsWith(" Attack", System.StringComparison.OrdinalIgnoreCase)
            || fileName.EndsWith(" Weapon", System.StringComparison.OrdinalIgnoreCase);
    }

    private static GameObject FindEnemyUtilityPrefab(string sourceEnemyName, string suffix)
    {
        string expectedPath = $"{EnemySourceFolder}/{sourceEnemyName} {suffix}.prefab";
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(expectedPath);
        if (prefab != null)
        {
            return prefab;
        }

        string expectedName = $"{sourceEnemyName} {suffix}";
        string[] guids = AssetDatabase.FindAssets($"{expectedName} t:Prefab", new[] { EnemySourceFolder });
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            string fileName = System.IO.Path.GetFileNameWithoutExtension(path);
            if (fileName.Equals(expectedName, System.StringComparison.OrdinalIgnoreCase))
            {
                return AssetDatabase.LoadAssetAtPath<GameObject>(path);
            }
        }

        return null;
    }

    private static void StripGameplayComponents(GameObject visualRoot)
    {
        foreach (Rigidbody2D body in visualRoot.GetComponentsInChildren<Rigidbody2D>(true))
        {
            Object.DestroyImmediate(body);
        }

        foreach (Collider2D collider in visualRoot.GetComponentsInChildren<Collider2D>(true))
        {
            Object.DestroyImmediate(collider);
        }

        foreach (MonoBehaviour behaviour in visualRoot.GetComponentsInChildren<MonoBehaviour>(true))
        {
            Object.DestroyImmediate(behaviour);
        }
    }

    private static void ConfigureSpriteRenderers(GameObject visualRoot, int sortingOrder)
    {
        foreach (SpriteRenderer renderer in visualRoot.GetComponentsInChildren<SpriteRenderer>(true))
        {
            renderer.sortingOrder = sortingOrder;
        }
    }

    private static void FitBoxColliderToRenderers(GameObject root, BoxCollider2D collider, Vector2 sizeScale, Vector2 offsetPadding)
    {
        Bounds bounds = GetWorldBounds(root);
        if (bounds.size.sqrMagnitude <= 0.0001f)
        {
            collider.size = new Vector2(0.75f, 0.9f);
            collider.offset = new Vector2(0f, 0.45f);
            return;
        }

        Vector3 localCenter = root.transform.InverseTransformPoint(bounds.center);
        collider.size = new Vector2(
            Mathf.Max(0.35f, bounds.size.x * sizeScale.x),
            Mathf.Max(0.45f, bounds.size.y * sizeScale.y));
        collider.offset = new Vector2(localCenter.x + offsetPadding.x, localCenter.y + offsetPadding.y);
    }

    private static void FitCircleColliderToRenderers(GameObject root, CircleCollider2D collider)
    {
        Bounds bounds = GetWorldBounds(root);
        if (bounds.size.sqrMagnitude <= 0.0001f)
        {
            collider.radius = 0.2f;
            collider.offset = Vector2.zero;
            return;
        }

        Vector3 localCenter = root.transform.InverseTransformPoint(bounds.center);
        collider.offset = new Vector2(localCenter.x, localCenter.y);
        collider.radius = Mathf.Max(0.12f, Mathf.Max(bounds.size.x, bounds.size.y) * 0.42f);
    }

    private static void SetLayerRecursively(GameObject gameObject, int layer)
    {
        gameObject.layer = layer;
        foreach (Transform child in gameObject.transform)
        {
            SetLayerRecursively(child.gameObject, layer);
        }
    }

    private static string SanitizeAssetName(string rawName)
    {
        if (string.IsNullOrWhiteSpace(rawName))
        {
            return "Asset";
        }

        char[] invalidChars = System.IO.Path.GetInvalidFileNameChars();
        List<char> characters = new List<char>(rawName.Length);
        foreach (char character in rawName)
        {
            bool invalid = false;
            for (int i = 0; i < invalidChars.Length; i++)
            {
                if (character == invalidChars[i])
                {
                    invalid = true;
                    break;
                }
            }

            characters.Add(invalid ? '_' : character);
        }

        string sanitized = new string(characters.ToArray()).Trim();
        return string.IsNullOrEmpty(sanitized) ? "Asset" : sanitized;
    }

    private static string SanitizeIdentifier(string rawName)
    {
        if (string.IsNullOrWhiteSpace(rawName))
        {
            return "Generated";
        }

        List<char> characters = new List<char>(rawName.Length);
        bool lastWasUnderscore = false;
        foreach (char character in rawName.Trim())
        {
            if (char.IsLetterOrDigit(character))
            {
                characters.Add(character);
                lastWasUnderscore = false;
            }
            else if (!lastWasUnderscore)
            {
                characters.Add('_');
                lastWasUnderscore = true;
            }
        }

        while (characters.Count > 0 && characters[characters.Count - 1] == '_')
        {
            characters.RemoveAt(characters.Count - 1);
        }

        return characters.Count > 0 ? new string(characters.ToArray()) : "Generated";
    }

    private static string MakeStableId(string rawName)
    {
        if (string.IsNullOrWhiteSpace(rawName))
        {
            return "character";
        }

        List<char> characters = new List<char>(rawName.Length);
        bool lastWasUnderscore = false;
        foreach (char character in rawName.Trim())
        {
            if (char.IsLetterOrDigit(character))
            {
                characters.Add(char.ToLowerInvariant(character));
                lastWasUnderscore = false;
            }
            else if (!lastWasUnderscore)
            {
                characters.Add('_');
                lastWasUnderscore = true;
            }
        }

        while (characters.Count > 0 && characters[characters.Count - 1] == '_')
        {
            characters.RemoveAt(characters.Count - 1);
        }

        return characters.Count > 0 ? new string(characters.ToArray()) : "character";
    }

    private static string MakeUniqueId(string baseId, HashSet<string> usedIds)
    {
        string uniqueId = string.IsNullOrWhiteSpace(baseId) ? "character" : baseId;
        int suffix = 2;
        while (!usedIds.Add(uniqueId))
        {
            uniqueId = $"{baseId}_{suffix}";
            suffix++;
        }

        return uniqueId;
    }

    private static void ConfigureMoveButton(Button button, PlayerController player, int direction)
    {
        button.transition = Selectable.Transition.None;
        button.onClick = new Button.ButtonClickedEvent();

        MobileMoveButton moveButton = GetOrAdd<MobileMoveButton>(button.gameObject);
        SetObjectReference(moveButton, "player", player);
        SetObjectReference(moveButton, "targetImage", button.GetComponent<Image>());
        SetInt(moveButton, "direction", direction);
    }

    private static void ConfigureJumpButton(Button button, PlayerController player)
    {
        button.transition = Selectable.Transition.None;
        button.onClick = new Button.ButtonClickedEvent();

        MobileJumpButton jumpButton = GetOrAdd<MobileJumpButton>(button.gameObject);
        SetObjectReference(jumpButton, "player", player);
        SetObjectReference(jumpButton, "targetImage", button.GetComponent<Image>());
    }

    private static void ConfigureCamera(Scene scene, Transform player)
    {
        GameObject cameraObject = FindInScene(scene, "Main Camera");
        if (cameraObject == null)
        {
            cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            SceneManager.MoveGameObjectToScene(cameraObject, scene);
            MarkCreated(cameraObject);
        }

        Camera camera = GetOrAdd<Camera>(cameraObject);
        camera.orthographic = true;
        camera.orthographicSize = Mathf.Max(camera.orthographicSize, 4.8f);

        CameraFollow2D follow = GetOrAdd<CameraFollow2D>(cameraObject);
        SetObjectReference(follow, "target", player);
        SetVector3(follow, "offset", new Vector3(1.8f, 1.2f, -10f));
        SetFloat(follow, "smoothTime", 0.18f);
        SetBool(follow, "followVertical", true);
        cameraObject.transform.position = player.position + new Vector3(1.8f, 1.2f, -10f);
    }

    private static void ConfigureGameBackground(Scene scene)
    {
        Sprite backgroundSprite = LoadFirstSpriteAtPath(GameBackgroundPath);
        if (backgroundSprite == null)
        {
            Debug.LogWarning($"Could not locate the game background sprite at {GameBackgroundPath}.");
            return;
        }

        GameObject environment = FindInScene(scene, "Environment");
        if (environment == null)
        {
            environment = FindOrCreateRoot(scene, "Environment");
        }

        GameObject backgroundObject = FindChild(environment.transform, "GameBackground");
        if (backgroundObject == null)
        {
            backgroundObject = new GameObject("GameBackground", typeof(SpriteRenderer));
            backgroundObject.transform.SetParent(environment.transform, false);
            MarkCreated(backgroundObject);
        }

        SpriteRenderer spriteRenderer = GetOrAdd<SpriteRenderer>(backgroundObject);
        spriteRenderer.sprite = backgroundSprite;
        spriteRenderer.sortingOrder = -100;
        spriteRenderer.color = Color.white;

        foreach (Collider2D collider in backgroundObject.GetComponents<Collider2D>())
        {
            Object.DestroyImmediate(collider);
        }

        Camera camera = FindInScene(scene, "Main Camera")?.GetComponent<Camera>();
        if (camera != null)
        {
            backgroundObject.transform.position = new Vector3(camera.transform.position.x, camera.transform.position.y, camera.transform.position.z + 10f);
        }

        backgroundObject.transform.localRotation = Quaternion.identity;

        GameBackgroundFitter fitter = GetOrAdd<GameBackgroundFitter>(backgroundObject);
        SetObjectReference(fitter, "targetCamera", camera);
        SetObjectReference(fitter, "spriteRenderer", spriteRenderer);
        SetFloat(fitter, "parallaxStrength", 0f);
        SetFloat(fitter, "coverPadding", 1.08f);
        SetFloat(fitter, "cameraZOffset", 10f);
        fitter.FitNow();
    }

    private static void SetupMainMenuScene(CharacterDatabase characterDatabase)
    {
        Scene scene = EditorSceneManager.OpenScene(MainMenuScenePath, OpenSceneMode.Single);
        RepairCharacterShopDataReferences(scene, characterDatabase);
        EnsureSingleEventSystem(scene);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    private static void RepairCharacterShopDataReferences(Scene scene, CharacterDatabase characterDatabase)
    {
        int validCharacterCount = CountValidCharacters(characterDatabase);
        if (characterDatabase == null)
        {
            Debug.LogWarning($"CharacterDatabase was not available at {CharacterDatabasePath}; Characters panel will remain unavailable.");
        }

        List<CharacterShopUI> shopUis = FindComponentsInScene<CharacterShopUI>(scene);
        if (shopUis.Count == 0)
        {
            Debug.LogWarning("No CharacterShopUI component was found in MainMenu. Setup did not rebuild the menu layout.");
            return;
        }

        foreach (CharacterShopUI shopUi in shopUis)
        {
            SetObjectReference(shopUi, "characterDatabase", characterDatabase);

            GameObject panel = FindChild(shopUi.transform, "CharactersPanel") ?? FindInScene(scene, "CharactersPanel");
            if (panel != null)
            {
                SetObjectReference(shopUi, "panel", panel);

                GameObject content = FindChild(panel.transform, "Content");
                if (content != null)
                {
                    SetObjectReference(shopUi, "gridRoot", content.transform);
                }

                CharacterShopCard cardTemplate = FindComponentByName<CharacterShopCard>(panel.transform, "CharacterCardTemplate");
                if (cardTemplate != null)
                {
                    cardTemplate.gameObject.SetActive(false);
                    SetObjectReference(shopUi, "cardTemplate", cardTemplate);
                }

                TMP_Text currencyText = FindComponentByName<TMP_Text>(panel.transform, "CurrencyText");
                if (currencyText != null)
                {
                    SetObjectReference(shopUi, "currencyText", currencyText);
                }

                TMP_Text feedbackText = FindComponentByName<TMP_Text>(panel.transform, "FeedbackText");
                if (feedbackText != null)
                {
                    feedbackText.text = string.Empty;
                    SetObjectReference(shopUi, "feedbackText", feedbackText);
                }
            }

            EditorUtility.SetDirty(shopUi);
            Debug.Log($"Assigned CharacterDatabase with {validCharacterCount} valid entries to CharacterShopUI on '{shopUi.gameObject.name}'.");
        }
    }

    private static int CountValidCharacters(CharacterDatabase characterDatabase)
    {
        if (characterDatabase == null)
        {
            return 0;
        }

        int count = 0;
        foreach (CharacterDefinition character in characterDatabase.Characters)
        {
            if (character != null && !string.IsNullOrEmpty(character.Id) && character.CharacterPrefab != null)
            {
                count++;
            }
        }

        return count;
    }

    private static T FindComponentByName<T>(Transform root, string objectName) where T : Component
    {
        GameObject child = FindChild(root, objectName);
        return child != null ? child.GetComponent<T>() : null;
    }

    private static MainMenuVisualAssets LoadMainMenuVisualAssets()
    {
        return new MainMenuVisualAssets
        {
            Background = LoadMenuSprite("BackgroundMenu"),
            Pixel = LoadMenuSpriteStrip("PIXEL"),
            Village = LoadMenuSpriteStrip("VILLAGE"),
            Adventure = LoadMenuSpriteStrip("ADVENTURE"),
            PlayButton = LoadMenuSprite("PlayButton"),
            SettingsButton = LoadMenuSprite("SettingsButton"),
            QuitButton = LoadMenuSprite("QuitButton")
        };
    }

    private static Sprite LoadMenuSprite(string assetName)
    {
        return LoadFirstSpriteAtPath($"{MenuAssetsFolder}/{assetName}.png");
    }

    private static Sprite[] LoadMenuSpriteStrip(string assetName)
    {
        return LoadSpritesAtPath($"{MenuAssetsFolder}/{assetName}.png");
    }

    private static Sprite LoadFirstSpriteAtPath(string assetPath)
    {
        Sprite[] sprites = LoadSpritesAtPath(assetPath);
        return sprites.Length > 0 ? sprites[0] : null;
    }

    private static Sprite[] LoadSpritesAtPath(string assetPath)
    {
        List<Sprite> sprites = new List<Sprite>();
        Sprite directSprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
        if (directSprite != null)
        {
            sprites.Add(directSprite);
        }

        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
        foreach (Object asset in assets)
        {
            if (asset is Sprite sprite && !sprites.Contains(sprite))
            {
                sprites.Add(sprite);
            }
        }

        sprites.Sort(CompareSpritesByTexturePosition);

        if (sprites.Count == 0)
        {
            Debug.LogWarning($"No Sprite assets were found at {assetPath}.");
        }

        return sprites.ToArray();
    }

    private static int CompareSpritesByTexturePosition(Sprite left, Sprite right)
    {
        int yComparison = right.rect.y.CompareTo(left.rect.y);
        if (yComparison != 0)
        {
            return yComparison;
        }

        int xComparison = left.rect.x.CompareTo(right.rect.x);
        if (xComparison != 0)
        {
            return xComparison;
        }

        return string.CompareOrdinal(left.name, right.name);
    }

    private static GameObject CreateSettingsPanel(Transform parent, MainMenuSettingsUI settingsUi)
    {
        RectTransform overlay = CreateRect("SettingsPanel", parent, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        Image overlayImage = GetOrAdd<Image>(overlay.gameObject);
        overlayImage.color = new Color(0f, 0f, 0f, 0.62f);
        overlayImage.raycastTarget = true;

        RectTransform panel = CreateRect("SettingsCard", overlay, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(640f, 500f), Vector2.zero);
        Image panelImage = GetOrAdd<Image>(panel.gameObject);
        panelImage.color = new Color(0.14f, 0.11f, 0.08f, 0.94f);
        Outline panelOutline = GetOrAdd<Outline>(panel.gameObject);
        panelOutline.effectColor = new Color(1f, 0.82f, 0.45f, 0.65f);
        panelOutline.effectDistance = new Vector2(4f, -4f);

        CreateLabel("Title", panel, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(520f, 72f), new Vector2(0f, -42f), "SETTINGS", 44);

        Button musicButton = CreateButton("MusicToggleButton", panel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(420f, 88f), new Vector2(0f, 74f), "MUSIC ON", 30);
        Button soundButton = CreateButton("SoundToggleButton", panel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(420f, 88f), new Vector2(0f, -34f), "SOUND ON", 30);
        Button closeButton = CreateButton("CloseButton", panel, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(320f, 78f), new Vector2(0f, 46f), "CLOSE", 28);

        SetButtonListener(musicButton, settingsUi.ToggleMusic);
        SetButtonListener(soundButton, settingsUi.ToggleSound);
        SetButtonListener(closeButton, settingsUi.CloseSettings);

        SetObjectReference(settingsUi, "musicButtonText", musicButton.GetComponentInChildren<Text>(true));
        SetObjectReference(settingsUi, "soundButtonText", soundButton.GetComponentInChildren<Text>(true));

        return overlay.gameObject;
    }

    private static CharacterShopPanelRefs CreateCharactersPanel(Transform parent, CharacterShopUI shopUi, Sprite flowerSprite)
    {
        RectTransform overlay = CreateRect("CharactersPanel", parent, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        Image overlayImage = GetOrAdd<Image>(overlay.gameObject);
        overlayImage.color = new Color(0f, 0f, 0f, 0.68f);
        overlayImage.raycastTarget = true;

        CreateTMPLabel("Title", overlay, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(620f, 76f), new Vector2(0f, -42f), "CHARACTERS", 46f, TextAlignmentOptions.Center);

        RectTransform currency = CreateRect("Currency", overlay, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(230f, 68f), new Vector2(-54f, -42f));
        RectTransform currencyIconRect = CreateRect("FlowerIcon", currency, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(48f, 48f), new Vector2(0f, 0f));
        Image currencyIcon = GetOrAdd<Image>(currencyIconRect.gameObject);
        currencyIcon.sprite = flowerSprite;
        currencyIcon.preserveAspect = true;
        currencyIcon.color = flowerSprite != null ? Color.white : new Color(1f, 0.84f, 0.18f, 1f);
        currencyIcon.raycastTarget = false;
        TMP_Text currencyText = CreateTMPLabel("CurrencyText", currency, new Vector2(0f, 0f), Vector2.one, new Vector2(0f, 0.5f), new Vector2(-62f, 0f), new Vector2(62f, 0f), "x 0", 34f, TextAlignmentOptions.Left);

        RectTransform scrollRoot = CreateRect("CharacterScroll", overlay, new Vector2(0.06f, 0.16f), new Vector2(0.94f, 0.80f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        Image scrollImage = GetOrAdd<Image>(scrollRoot.gameObject);
        scrollImage.color = new Color(0.08f, 0.06f, 0.04f, 0.56f);
        ScrollRect scrollRect = GetOrAdd<ScrollRect>(scrollRoot.gameObject);
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;

        RectTransform viewport = CreateRect("Viewport", scrollRoot, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        Image viewportImage = GetOrAdd<Image>(viewport.gameObject);
        viewportImage.color = new Color(0f, 0f, 0f, 0.01f);
        Mask mask = GetOrAdd<Mask>(viewport.gameObject);
        mask.showMaskGraphic = false;

        RectTransform content = CreateRect("Content", viewport, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), Vector2.zero, Vector2.zero);
        GridLayoutGroup grid = GetOrAdd<GridLayoutGroup>(content.gameObject);
        grid.padding = new RectOffset(26, 26, 26, 26);
        grid.cellSize = new Vector2(220f, 282f);
        grid.spacing = new Vector2(24f, 24f);
        grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
        grid.startAxis = GridLayoutGroup.Axis.Horizontal;
        grid.childAlignment = TextAnchor.UpperCenter;
        grid.constraint = GridLayoutGroup.Constraint.Flexible;
        ContentSizeFitter fitter = GetOrAdd<ContentSizeFitter>(content.gameObject);
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        scrollRect.viewport = viewport;
        scrollRect.content = content;

        CharacterShopCard cardTemplate = CreateCharacterCardTemplate(content, flowerSprite);
        cardTemplate.gameObject.SetActive(false);

        Button backButton = CreateButton("BackButton", overlay, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(300f, 72f), new Vector2(0f, 42f), "BACK", 28);
        SetButtonListener(backButton, shopUi.CloseCharacters);
        TMP_Text feedbackText = CreateTMPLabel("FeedbackText", overlay, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(620f, 48f), new Vector2(0f, 120f), string.Empty, 24f, TextAlignmentOptions.Center);

        return new CharacterShopPanelRefs
        {
            Panel = overlay.gameObject,
            GridRoot = content,
            CardTemplate = cardTemplate,
            CurrencyText = currencyText,
            FeedbackText = feedbackText
        };
    }

    private static CharacterShopCard CreateCharacterCardTemplate(Transform parent, Sprite flowerSprite)
    {
        RectTransform card = CreateRect("CharacterCardTemplate", parent, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(220f, 282f), Vector2.zero);
        Image background = GetOrAdd<Image>(card.gameObject);
        background.color = new Color(0.09f, 0.08f, 0.08f, 0.88f);
        Outline outline = GetOrAdd<Outline>(card.gameObject);
        outline.effectColor = new Color(1f, 0.82f, 0.45f, 0.42f);
        outline.effectDistance = new Vector2(2f, -2f);

        RectTransform previewRect = CreateRect("Preview", card, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(140f, 116f), new Vector2(0f, -20f));
        Image previewImage = GetOrAdd<Image>(previewRect.gameObject);
        previewImage.preserveAspect = true;
        previewImage.raycastTarget = false;

        TMP_Text nameText = CreateTMPLabel("NameText", card, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(-24f, 46f), new Vector2(0f, -142f), "Character", 23f, TextAlignmentOptions.Center);

        RectTransform priceRow = CreateRect("PriceRow", card, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(130f, 34f), new Vector2(0f, -190f));
        RectTransform priceIconRect = CreateRect("FlowerIcon", priceRow, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(28f, 28f), Vector2.zero);
        Image priceIcon = GetOrAdd<Image>(priceIconRect.gameObject);
        priceIcon.sprite = flowerSprite;
        priceIcon.preserveAspect = true;
        priceIcon.color = flowerSprite != null ? Color.white : new Color(1f, 0.84f, 0.18f, 1f);
        priceIcon.raycastTarget = false;
        TMP_Text priceText = CreateTMPLabel("PriceText", priceRow, new Vector2(0f, 0f), Vector2.one, new Vector2(0f, 0.5f), new Vector2(-36f, 0f), new Vector2(36f, 0f), "x 25", 22f, TextAlignmentOptions.Left);

        RectTransform actionRect = CreateRect("ActionButton", card, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(160f, 48f), new Vector2(0f, 24f));
        Image actionImage = GetOrAdd<Image>(actionRect.gameObject);
        actionImage.color = ButtonColor;
        Button actionButton = GetOrAdd<Button>(actionRect.gameObject);
        actionButton.targetGraphic = actionImage;
        actionButton.transition = Selectable.Transition.ColorTint;
        ConfigureSpriteButtonColors(actionButton);
        TMP_Text actionText = CreateTMPLabel("ActionText", actionRect, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, "BUY", 22f, TextAlignmentOptions.Center);
        actionText.raycastTarget = false;

        CharacterShopCard shopCard = GetOrAdd<CharacterShopCard>(card.gameObject);
        SetObjectReference(shopCard, "background", background);
        SetObjectReference(shopCard, "previewImage", previewImage);
        SetObjectReference(shopCard, "nameText", nameText);
        SetObjectReference(shopCard, "priceText", priceText);
        SetObjectReference(shopCard, "actionText", actionText);
        SetObjectReference(shopCard, "actionButton", actionButton);
        return shopCard;
    }

    private static Button CreateSpriteButton(string name, Transform parent, Sprite sprite, Vector2 maxSize, Vector2 position)
    {
        Vector2 fittedSize = GetFittedSpriteSize(sprite, maxSize);
        RectTransform rect = CreateRect(name, parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), fittedSize, position);

        Image image = GetOrAdd<Image>(rect.gameObject);
        image.sprite = sprite;
        image.type = Image.Type.Simple;
        image.preserveAspect = true;
        image.color = sprite != null ? Color.white : ButtonColor;

        Button button = GetOrAdd<Button>(rect.gameObject);
        button.targetGraphic = image;
        button.transition = Selectable.Transition.ColorTint;
        ConfigureSpriteButtonColors(button);
        GetOrAdd<UIButtonPressScale>(rect.gameObject);

        if (sprite == null)
        {
            string fallbackLabel = name.Replace("Button", string.Empty).ToUpperInvariant();
            Text label = CreateLabel("Label", rect, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, fallbackLabel, 30);
            label.raycastTarget = false;
        }

        return button;
    }

    private static RectTransform CreateSpriteStrip(string name, Transform parent, Sprite[] sprites, Vector2 position, float maxWidth, float maxHeight)
    {
        RectTransform strip = CreateRect(name, parent, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(maxWidth, maxHeight), position);
        if (sprites == null || sprites.Length == 0)
        {
            CreateLabel("FallbackLabel", strip, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, name, 46);
            return strip;
        }

        float minX = sprites[0].rect.x;
        float minY = sprites[0].rect.y;
        float maxX = sprites[0].rect.xMax;
        float maxY = sprites[0].rect.yMax;
        for (int i = 1; i < sprites.Length; i++)
        {
            Rect rect = sprites[i].rect;
            minX = Mathf.Min(minX, rect.x);
            minY = Mathf.Min(minY, rect.y);
            maxX = Mathf.Max(maxX, rect.xMax);
            maxY = Mathf.Max(maxY, rect.yMax);
        }

        float sourceWidth = Mathf.Max(1f, maxX - minX);
        float sourceHeight = Mathf.Max(1f, maxY - minY);
        float scale = Mathf.Min(maxWidth / sourceWidth, maxHeight / sourceHeight);
        Vector2 stripSize = new Vector2(sourceWidth * scale, sourceHeight * scale);
        strip.sizeDelta = stripSize;

        Vector2 sourceCenter = new Vector2(minX + sourceWidth * 0.5f, minY + sourceHeight * 0.5f);
        for (int i = 0; i < sprites.Length; i++)
        {
            Sprite sprite = sprites[i];
            Vector2 spriteSize = new Vector2(sprite.rect.width * scale, sprite.rect.height * scale);
            Vector2 spriteCenter = new Vector2(sprite.rect.x + sprite.rect.width * 0.5f, sprite.rect.y + sprite.rect.height * 0.5f);
            Vector2 anchoredPosition = (spriteCenter - sourceCenter) * scale;
            RectTransform letter = CreateRect(sprite.name, strip, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), spriteSize, anchoredPosition);
            Image image = GetOrAdd<Image>(letter.gameObject);
            image.sprite = sprite;
            image.type = Image.Type.Simple;
            image.preserveAspect = true;
            image.color = Color.white;
            image.raycastTarget = false;
        }

        return strip;
    }

    private static Vector2 GetFittedSpriteSize(Sprite sprite, Vector2 maxSize)
    {
        if (sprite == null || sprite.rect.width <= 0f || sprite.rect.height <= 0f)
        {
            return maxSize;
        }

        float scale = Mathf.Min(maxSize.x / sprite.rect.width, maxSize.y / sprite.rect.height);
        return new Vector2(sprite.rect.width * scale, sprite.rect.height * scale);
    }

    private static float GetSpriteAspect(Sprite sprite, float fallback)
    {
        if (sprite == null || sprite.rect.height <= 0f)
        {
            return fallback;
        }

        return sprite.rect.width / sprite.rect.height;
    }

    private static void ConfigureSpriteButtonColors(Button button)
    {
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1f, 0.96f, 0.84f, 1f);
        colors.pressedColor = new Color(0.82f, 0.75f, 0.62f, 1f);
        colors.selectedColor = new Color(1f, 0.96f, 0.84f, 1f);
        colors.disabledColor = new Color(1f, 1f, 1f, 0.45f);
        colors.fadeDuration = 0.08f;
        button.colors = colors;
    }

    private static void EnsureSingleEventSystem(Scene scene)
    {
        List<EventSystem> systems = FindComponentsInScene<EventSystem>(scene);
        EventSystem eventSystem;
        if (systems.Count == 0)
        {
            GameObject eventSystemObject = new GameObject("EventSystem");
            SceneManager.MoveGameObjectToScene(eventSystemObject, scene);
            eventSystem = eventSystemObject.AddComponent<EventSystem>();
            MarkCreated(eventSystemObject);
        }
        else
        {
            eventSystem = systems[0];
            for (int i = 1; i < systems.Count; i++)
            {
                Object.DestroyImmediate(systems[i].gameObject);
            }
        }

#if ENABLE_INPUT_SYSTEM
        GetOrAdd<InputSystemUIInputModule>(eventSystem.gameObject);
        StandaloneInputModule legacyModule = eventSystem.GetComponent<StandaloneInputModule>();
        if (legacyModule != null)
        {
            Object.DestroyImmediate(legacyModule);
        }
#else
        GetOrAdd<StandaloneInputModule>(eventSystem.gameObject);
#endif
    }

    private static GameObject CreateOverlayPanel(string name, Transform parent, string title)
    {
        RectTransform panel = CreateRect(name, parent, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        Image image = GetOrAdd<Image>(panel.gameObject);
        image.color = PanelColor;
        CreateLabel("Title", panel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(700f, 90f), new Vector2(0f, 158f), title, 50);
        return panel.gameObject;
    }

    private static Text EnsureWinMessageText(Transform winPanel)
    {
        GameObject existing = FindChild(winPanel, "WinMessageText");
        Text messageText = existing != null ? existing.GetComponent<Text>() : null;
        if (messageText == null)
        {
            messageText = CreateLabel("WinMessageText", winPanel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(720f, 52f), new Vector2(0f, 106f), "Level complete", 26);
        }

        messageText.alignment = TextAnchor.MiddleCenter;
        messageText.color = TextColor;
        return messageText;
    }

    private static Text EnsureLevelText(Transform hudRoot, int levelIndex, int totalLevels)
    {
        GameObject existing = FindChild(hudRoot, "LevelText");
        Text levelText = existing != null ? existing.GetComponent<Text>() : null;
        if (levelText == null)
        {
            Transform parent = FindChild(hudRoot, "HUD")?.transform
                ?? FindChild(hudRoot, "SafeArea")?.transform
                ?? hudRoot;
            levelText = CreateLabel("LevelText", parent, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(300f, 46f), new Vector2(0f, -42f), string.Empty, 24);
        }

        levelText.text = $"LEVEL {levelIndex + 1} / {totalLevels}";
        levelText.alignment = TextAnchor.MiddleCenter;
        levelText.color = new Color(1f, 0.95f, 0.82f, 0.9f);
        return levelText;
    }

    private static Button CreateButton(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 size, Vector2 position, string label, int fontSize)
    {
        RectTransform rect = CreateRect(name, parent, anchorMin, anchorMax, pivot, size, position);
        Image image = GetOrAdd<Image>(rect.gameObject);
        image.color = ButtonColor;

        Button button = GetOrAdd<Button>(rect.gameObject);
        button.targetGraphic = image;
        button.transition = Selectable.Transition.ColorTint;

        Text text = CreateLabel("Label", rect, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, label, fontSize);
        text.raycastTarget = false;
        return button;
    }

    private static Text CreateLabel(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 size, Vector2 position, string textValue, int fontSize)
    {
        RectTransform rect = CreateRect(name, parent, anchorMin, anchorMax, pivot, size, position);
        Text text = GetOrAdd<Text>(rect.gameObject);
        text.text = textValue;
        text.font = GetDefaultFont();
        text.fontSize = fontSize;
        text.resizeTextForBestFit = true;
        text.resizeTextMinSize = Mathf.Max(12, fontSize - 10);
        text.resizeTextMaxSize = fontSize;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = TextColor;
        return text;
    }

    private static TextMeshProUGUI CreateTMPLabel(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 size, Vector2 position, string textValue, float fontSize, TextAlignmentOptions alignment)
    {
        RectTransform rect = CreateRect(name, parent, anchorMin, anchorMax, pivot, size, position);
        TextMeshProUGUI text = GetOrAdd<TextMeshProUGUI>(rect.gameObject);
        text.text = textValue;
        text.fontSize = fontSize;
        text.enableAutoSizing = true;
        text.fontSizeMin = Mathf.Max(10f, fontSize - 10f);
        text.fontSizeMax = fontSize;
        text.alignment = alignment;
        text.color = TextColor;
        text.characterSpacing = 0f;
        text.raycastTarget = false;
        return text;
    }

    private static RectTransform CreateRect(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 size, Vector2 position)
    {
        GameObject gameObject = new GameObject(name, typeof(RectTransform));
        RectTransform rect = gameObject.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.identity;
        return rect;
    }

    private static void SetButtonListener(Button button, UnityAction action)
    {
        button.onClick = new Button.ButtonClickedEvent();
        UnityEventTools.AddPersistentListener(button.onClick, action);
        EditorUtility.SetDirty(button);
    }

    private static Font GetDefaultFont()
    {
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        return font != null ? font : Resources.GetBuiltinResource<Font>("Arial.ttf");
    }

    private static void ConfigureBuildSettings()
    {
        List<GameplaySceneInfo> gameplayScenes = FindGameplayScenes();
        ValidateGameplaySceneSequence(gameplayScenes);
        ConfigureBuildSettings(gameplayScenes);
    }

    private static void ConfigureBuildSettings(List<GameplaySceneInfo> gameplayScenes)
    {
        List<EditorBuildSettingsScene> buildScenes = new List<EditorBuildSettingsScene>
        {
            new EditorBuildSettingsScene(MainMenuScenePath, true)
        };

        foreach (GameplaySceneInfo gameplayScene in gameplayScenes)
        {
            buildScenes.Add(new EditorBuildSettingsScene(gameplayScene.Path, true));
        }

        EditorBuildSettings.scenes = buildScenes.ToArray();
        Debug.Log($"Configured build scene order: MainMenu plus {gameplayScenes.Count} gameplay scenes.");
    }

    private static List<GameplaySceneInfo> FindGameplayScenes()
    {
        List<GameplaySceneInfo> scenes = new List<GameplaySceneInfo>();
        string[] guids = AssetDatabase.FindAssets("t:Scene", new[] { ScenesFolder });
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (TryParseGameplayScene(path, out int levelIndex, out string sceneName))
            {
                scenes.Add(new GameplaySceneInfo(path, sceneName, levelIndex));
            }
        }

        scenes.Sort((left, right) => left.LevelIndex.CompareTo(right.LevelIndex));
        return scenes;
    }

    private static bool TryParseGameplayScene(string path, out int levelIndex, out string sceneName)
    {
        levelIndex = -1;
        sceneName = System.IO.Path.GetFileNameWithoutExtension(path);
        if (sceneName == LevelProgression.FirstLevelSceneName)
        {
            levelIndex = 0;
            return true;
        }

        string prefix = LevelProgression.FirstLevelSceneName + " ";
        if (!sceneName.StartsWith(prefix, System.StringComparison.Ordinal))
        {
            return false;
        }

        string suffix = sceneName.Substring(prefix.Length);
        return int.TryParse(suffix, out levelIndex) && levelIndex >= 1 && levelIndex < LevelProgression.TotalLevels;
    }

    private static void ValidateGameplaySceneSequence(List<GameplaySceneInfo> gameplayScenes)
    {
        bool[] found = new bool[LevelProgression.TotalLevels];
        foreach (GameplaySceneInfo gameplayScene in gameplayScenes)
        {
            if (gameplayScene.LevelIndex >= 0 && gameplayScene.LevelIndex < found.Length)
            {
                found[gameplayScene.LevelIndex] = true;
            }
        }

        bool missingAny = false;
        for (int i = 0; i < found.Length; i++)
        {
            if (!found[i])
            {
                missingAny = true;
                Debug.LogWarning($"Missing expected gameplay scene: {LevelProgression.GetSceneNameForLevelIndex(i)}");
            }
        }

        if (!missingAny && gameplayScenes.Count == LevelProgression.TotalLevels)
        {
            Debug.Log($"Found {LevelProgression.TotalLevels} gameplay levels.");
        }
        else
        {
            Debug.LogWarning($"Found {gameplayScenes.Count} gameplay levels. Expected {LevelProgression.TotalLevels}.");
        }
    }

    private static void ConfigureMainMenuForLevelProgression()
    {
        Scene scene = EditorSceneManager.OpenScene(MainMenuScenePath, OpenSceneMode.Single);
        List<MainMenuUI> menuUis = FindComponentsInScene<MainMenuUI>(scene);
        if (menuUis.Count == 0)
        {
            Debug.LogWarning("MainMenuUI was not found in MainMenu. PLAY button scene name was not updated.");
        }

        foreach (MainMenuUI menuUi in menuUis)
        {
            SetString(menuUi, "gameSceneName", LevelProgression.FirstLevelSceneName);
            EditorUtility.SetDirty(menuUi);
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    private static void ConfigureGameplaySceneForLevelProgression(GameplaySceneInfo gameplayScene, int totalLevels)
    {
        Scene scene = EditorSceneManager.OpenScene(gameplayScene.Path, OpenSceneMode.Single);

        GameObject gameManagerObject = FindInScene(scene, "GameManager");
        if (gameManagerObject == null)
        {
            gameManagerObject = FindOrCreateRoot(scene, "GameManager");
        }

        GameManager gameManager = GetOrAdd<GameManager>(gameManagerObject);
        LevelProgression levelProgression = GetOrAdd<LevelProgression>(gameManagerObject);
        SetObjectReference(gameManager, "levelProgression", levelProgression);
        SetString(gameManager, "gameSceneName", LevelProgression.FirstLevelSceneName);
        SetString(gameManager, "mainMenuSceneName", LevelProgression.MainMenuSceneName);

        ConfigureGameplayHudForLevelProgression(scene, gameManager, gameplayScene.LevelIndex, totalLevels);
        ConfigureFinishTriggerForLevelProgression(scene, gameManager);
        EnsureSingleEventSystem(scene);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    private static void ConfigureGameplayHudForLevelProgression(Scene scene, GameManager gameManager, int levelIndex, int totalLevels)
    {
        GameObject hud = FindInScene(scene, "GameHUD");
        if (hud == null)
        {
            Debug.LogWarning($"{scene.name}: GameHUD was not found. Level progression UI references were not fully configured.");
            return;
        }

        GameHUD gameHud = hud.GetComponent<GameHUD>();
        if (gameHud != null)
        {
            SetObjectReference(gameHud, "gameManager", gameManager);
        }

        SetObjectReference(gameManager, "gameHUD", gameHud);
        SetObjectReference(gameManager, "timerText", FindText(hud.transform, "TimerText"));
        SetObjectReference(gameManager, "levelText", EnsureLevelText(hud.transform, levelIndex, totalLevels));
        SetObjectReference(gameManager, "pausePanel", FindChild(hud.transform, "PausePanel"));
        SetObjectReference(gameManager, "gameOverPanel", FindChild(hud.transform, "GameOverPanel"));

        GameObject winPanel = FindChild(hud.transform, "WinPanel");
        SetObjectReference(gameManager, "winPanel", winPanel);
        if (winPanel != null)
        {
            Text winTitleText = FindText(winPanel.transform, "Title");
            Text winMessageText = EnsureWinMessageText(winPanel.transform);
            Text winTimeText = FindText(winPanel.transform, "WinTimeText");
            Text winBestText = FindText(winPanel.transform, "WinBestTimeText");
            Button primaryButton = FindButton(winPanel.transform, "PlayAgainButton");
            Button mainMenuButton = FindButton(winPanel.transform, "WinMainMenuButton");

            if (winTitleText != null)
            {
                winTitleText.text = levelIndex == LevelProgression.TotalLevels - 1 ? "ADVENTURE COMPLETE!" : "LEVEL COMPLETE";
            }

            winMessageText.text = levelIndex == LevelProgression.TotalLevels - 1
                ? $"You completed all {LevelProgression.TotalLevels} levels!"
                : $"Level {levelIndex + 1} / {LevelProgression.TotalLevels} complete";

            if (primaryButton != null)
            {
                Text primaryLabel = primaryButton.GetComponentInChildren<Text>(true);
                if (primaryLabel != null)
                {
                    primaryLabel.text = levelIndex == LevelProgression.TotalLevels - 1 ? "PLAY AGAIN" : "NEXT LEVEL";
                }
                SetButtonListener(primaryButton, gameManager.ContinueAfterWin);
                SetObjectReference(gameManager, "winPrimaryButton", primaryButton);
                SetObjectReference(gameManager, "winPrimaryButtonText", primaryLabel);
            }

            if (mainMenuButton != null)
            {
                SetButtonListener(mainMenuButton, gameManager.GoToMainMenu);
            }

            SetObjectReference(gameManager, "winTitleText", winTitleText);
            SetObjectReference(gameManager, "winMessageText", winMessageText);
            SetObjectReference(gameManager, "winTimeText", winTimeText);
            SetObjectReference(gameManager, "winBestTimeText", winBestText);
        }

        Button tryAgainButton = FindButton(hud.transform, "TryAgainButton");
        if (tryAgainButton != null)
        {
            SetButtonListener(tryAgainButton, gameManager.RestartLevel);
        }
    }

    private static void ConfigureFinishTriggerForLevelProgression(Scene scene, GameManager gameManager)
    {
        GameObject finishChest = FindInScene(scene, "FinishChest");
        if (finishChest == null)
        {
            Debug.LogWarning($"{scene.name}: FinishChest was not found. Finish trigger was not configured.");
            return;
        }

        FinishTrigger finishTrigger = GetOrAdd<FinishTrigger>(finishChest);
        SetObjectReference(finishTrigger, "gameManager", gameManager);
        SetObjectReference(finishTrigger, "chestAnimator", finishChest.GetComponentInChildren<Animator>(true));
        SetBool(finishTrigger, "openChest", true);
    }

    private static Text FindText(Transform root, string objectName)
    {
        GameObject gameObject = FindChild(root, objectName);
        return gameObject != null ? gameObject.GetComponent<Text>() : null;
    }

    private static Button FindButton(Transform root, string objectName)
    {
        GameObject gameObject = FindChild(root, objectName);
        return gameObject != null ? gameObject.GetComponent<Button>() : null;
    }

    private static void ConfigureAndroidSettings()
    {
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
        PlayerSettings.allowedAutorotateToPortrait = false;
        PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
        PlayerSettings.allowedAutorotateToLandscapeLeft = true;
        PlayerSettings.allowedAutorotateToLandscapeRight = true;
        PlayerSettings.fullScreenMode = FullScreenMode.FullScreenWindow;
        PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
    }

    private static void EnsureProjectFolders()
    {
        EnsureFolder("Assets/_Project");
        EnsureFolder("Assets/_Project/Scripts");
        EnsureFolder("Assets/_Project/Scripts/Core");
        EnsureFolder("Assets/_Project/Scripts/Player");
        EnsureFolder("Assets/_Project/Scripts/Gameplay");
        EnsureFolder("Assets/_Project/Scripts/UI");
        EnsureFolder("Assets/_Project/Scripts/Camera");
        EnsureFolder("Assets/_Project/Scripts/Editor");
        EnsureFolder("Assets/_Project/Data");
        EnsureFolder(CharacterDataFolder);
        EnsureFolder(CharacterDefinitionFolder);
        EnsureFolder("Assets/_Project/Prefabs");
        EnsureFolder("Assets/_Project/Prefabs/Gameplay");
        EnsureFolder(EnemyPrefabFolder);
        EnsureFolder("Assets/_Project/Art");
        EnsureFolder("Assets/_Project/Art/Animations");
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
        {
            return;
        }

        string[] parts = path.Split('/');
        string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
            {
                AssetDatabase.CreateFolder(current, parts[i]);
            }
            current = next;
        }
    }

    private static int EnsureLayer(string layerName)
    {
        int existingLayer = LayerMask.NameToLayer(layerName);
        if (existingLayer >= 0)
        {
            return existingLayer;
        }

        SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty layers = tagManager.FindProperty("layers");
        for (int i = 8; i < layers.arraySize; i++)
        {
            SerializedProperty layer = layers.GetArrayElementAtIndex(i);
            if (string.IsNullOrEmpty(layer.stringValue))
            {
                layer.stringValue = layerName;
                tagManager.ApplyModifiedProperties();
                return i;
            }
        }

        Debug.LogWarning($"No free user layer slot was available for {layerName}.");
        return -1;
    }

    private static GameObject FindOrCreateRoot(Scene scene, string name)
    {
        GameObject existing = FindInScene(scene, name);
        if (existing != null)
        {
            return existing;
        }

        GameObject created = new GameObject(name);
        SceneManager.MoveGameObjectToScene(created, scene);
        MarkCreated(created);
        return created;
    }

    private static GameObject FindInScene(Scene scene, string name)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            GameObject found = FindChild(root.transform, name);
            if (found != null)
            {
                return found;
            }

            if (root.name == name)
            {
                return root;
            }
        }

        return null;
    }

    private static GameObject FindChild(Transform parent, string name)
    {
        if (parent == null)
        {
            return null;
        }

        if (parent.name == name)
        {
            return parent.gameObject;
        }

        for (int i = 0; i < parent.childCount; i++)
        {
            GameObject found = FindChild(parent.GetChild(i), name);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }

    private static List<T> FindComponentsInScene<T>(Scene scene) where T : Component
    {
        List<T> components = new List<T>();
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            components.AddRange(root.GetComponentsInChildren<T>(true));
        }
        return components;
    }

    private static Bounds GetWorldBounds(GameObject gameObject)
    {
        if (gameObject == null)
        {
            return new Bounds(Vector3.zero, new Vector3(18f, 6f, 0f));
        }

        bool hasBounds = false;
        Bounds bounds = new Bounds(gameObject.transform.position, Vector3.zero);

        foreach (Renderer renderer in gameObject.GetComponentsInChildren<Renderer>(true))
        {
            if (!hasBounds)
            {
                bounds = renderer.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }

        foreach (Collider2D collider in gameObject.GetComponentsInChildren<Collider2D>(true))
        {
            if (!hasBounds)
            {
                bounds = collider.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(collider.bounds);
            }
        }

        return hasBounds ? bounds : new Bounds(gameObject.transform.position, Vector3.one);
    }

    private static T GetOrAdd<T>(GameObject gameObject) where T : Component
    {
        T component = gameObject.GetComponent<T>();
        if (component == null)
        {
            component = gameObject.AddComponent<T>();
            EditorUtility.SetDirty(gameObject);
        }
        return component;
    }

    private static void RemoveChildren(Transform parent)
    {
        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            Object.DestroyImmediate(parent.GetChild(i).gameObject);
        }
    }

    private static void MarkCreated(GameObject gameObject)
    {
        EditorUtility.SetDirty(gameObject);
    }

    private static void SetObjectReference(Object target, string propertyName, Object value)
    {
        SerializedProperty property = GetProperty(target, propertyName);
        if (property == null)
        {
            return;
        }

        property.objectReferenceValue = value;
        property.serializedObject.ApplyModifiedProperties();
    }

    private static void SetFloat(Object target, string propertyName, float value)
    {
        SerializedProperty property = GetProperty(target, propertyName);
        if (property == null)
        {
            return;
        }

        property.floatValue = value;
        property.serializedObject.ApplyModifiedProperties();
    }

    private static void SetInt(Object target, string propertyName, int value)
    {
        SerializedProperty property = GetProperty(target, propertyName);
        if (property == null)
        {
            return;
        }

        property.intValue = value;
        property.serializedObject.ApplyModifiedProperties();
    }

    private static void SetBool(Object target, string propertyName, bool value)
    {
        SerializedProperty property = GetProperty(target, propertyName);
        if (property == null)
        {
            return;
        }

        property.boolValue = value;
        property.serializedObject.ApplyModifiedProperties();
    }

    private static void SetString(Object target, string propertyName, string value)
    {
        SerializedProperty property = GetProperty(target, propertyName);
        if (property == null)
        {
            return;
        }

        property.stringValue = value;
        property.serializedObject.ApplyModifiedProperties();
    }

    private static string GetSerializedString(Object target, string propertyName)
    {
        SerializedProperty property = GetProperty(target, propertyName);
        return property != null ? property.stringValue : string.Empty;
    }

    private static void SetVector3(Object target, string propertyName, Vector3 value)
    {
        SerializedProperty property = GetProperty(target, propertyName);
        if (property == null)
        {
            return;
        }

        property.vector3Value = value;
        property.serializedObject.ApplyModifiedProperties();
    }

    private static void SetSpriteArray(Object target, string propertyName, Sprite[] sprites)
    {
        SerializedProperty property = GetProperty(target, propertyName);
        if (property == null)
        {
            return;
        }

        property.arraySize = sprites != null ? sprites.Length : 0;
        for (int i = 0; i < property.arraySize; i++)
        {
            property.GetArrayElementAtIndex(i).objectReferenceValue = sprites[i];
        }

        property.serializedObject.ApplyModifiedProperties();
    }

    private static void SetObjectArray(Object target, string propertyName, Object[] objects)
    {
        SerializedProperty property = GetProperty(target, propertyName);
        if (property == null)
        {
            return;
        }

        property.arraySize = objects != null ? objects.Length : 0;
        for (int i = 0; i < property.arraySize; i++)
        {
            property.GetArrayElementAtIndex(i).objectReferenceValue = objects[i];
        }

        property.serializedObject.ApplyModifiedProperties();
    }

    private static SerializedProperty GetProperty(Object target, string propertyName)
    {
        if (target == null)
        {
            return null;
        }

        SerializedObject serializedObject = new SerializedObject(target);
        return serializedObject.FindProperty(propertyName);
    }

    private readonly struct GameplaySceneInfo
    {
        public readonly string Path;
        public readonly string SceneName;
        public readonly int LevelIndex;

        public GameplaySceneInfo(string path, string sceneName, int levelIndex)
        {
            Path = path;
            SceneName = sceneName;
            LevelIndex = levelIndex;
        }
    }

    private struct GameHudRefs
    {
        public GameHUD GameHUD;
        public Text TimerText;
        public Text LevelText;
        public GameObject PausePanel;
        public GameObject WinPanel;
        public Text WinTitleText;
        public Text WinMessageText;
        public Text WinTimeText;
        public Text WinBestTimeText;
        public Button WinPrimaryButton;
        public Text WinPrimaryButtonText;
        public GameObject GameOverPanel;
    }

    private struct CharacterShopPanelRefs
    {
        public GameObject Panel;
        public Transform GridRoot;
        public CharacterShopCard CardTemplate;
        public TMP_Text CurrencyText;
        public TMP_Text FeedbackText;
    }

    private struct PlayerAnimationFrames
    {
        public Sprite[] Idle;
        public Sprite[] Run;
        public Sprite[] Air;
        public Sprite[] Death;
        public Sprite FirstIdleSprite;
    }

    private struct MainMenuVisualAssets
    {
        public Sprite Background;
        public Sprite[] Pixel;
        public Sprite[] Village;
        public Sprite[] Adventure;
        public Sprite PlayButton;
        public Sprite SettingsButton;
        public Sprite QuitButton;
    }
}
#endif
