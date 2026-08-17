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

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

public static class PixelVillageSetup
{
    private const string GameScenePath = "Assets/_Project/Scenes/Game.unity";
    private const string MainMenuScenePath = "Assets/_Project/Scenes/MainMenu.unity";
    private const string PuppeteerFolder = "Assets/GDD - Quinnipiac/Pixel Art Character Package/Characters/Puppeteer/Puppeteer Grey";

    private static readonly Color ButtonColor = new Color(1f, 1f, 1f, 0.34f);
    private static readonly Color PanelColor = new Color(0f, 0f, 0f, 0.72f);
    private static readonly Color TextColor = new Color(1f, 0.95f, 0.82f, 1f);

    [MenuItem("Tools/Pixel Village/Setup Complete Game")]
    public static void SetupCompleteGame()
    {
        EnsureProjectFolders();
        PlayerAnimationFrames playerAnimationFrames = LoadPlayerAnimationFrames();

        SetupGameScene(playerAnimationFrames);
        SetupMainMenuScene();
        ConfigureBuildSettings();
        ConfigureAndroidSettings();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("Pixel Village setup complete. Game and MainMenu scenes were configured and saved.");
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

    private static void SetupGameScene(PlayerAnimationFrames playerAnimationFrames)
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

        ConfigurePlayer(playerObject, visualObject, playerAnimationFrames, playerLayer, groundLayer);

        PlayerController player = playerObject.GetComponent<PlayerController>();
        GameObject gameplayObject = FindOrCreateRoot(scene, "Gameplay");
        Transform playerSpawn = EnsurePlayerSpawn(gameplayObject.transform, playerObject.transform.position);
        GameObject groundObject = FindInScene(scene, "Ground");
        ConfigureGround(groundObject, groundLayer);
        GameObject deathZoneObject = ConfigureDeathZone(gameplayObject.transform, groundObject, gameManager, deathZoneLayer);
        GameObject finishChest = ConfigureFinishChest(scene, gameManager);

        GameHudRefs hudRefs = SetupGameHud(scene, gameManager, player);
        ConfigureCamera(scene, playerObject.transform);
        EnsureSingleEventSystem(scene);

        SetObjectReference(gameManager, "player", player);
        SetObjectReference(gameManager, "playerSpawn", playerSpawn);
        SetObjectReference(gameManager, "timerText", hudRefs.TimerText);
        SetObjectReference(gameManager, "pausePanel", hudRefs.PausePanel);
        SetObjectReference(gameManager, "winPanel", hudRefs.WinPanel);
        SetObjectReference(gameManager, "winTimeText", hudRefs.WinTimeText);
        SetObjectReference(gameManager, "winBestTimeText", hudRefs.WinBestTimeText);
        SetString(gameManager, "gameSceneName", "Game");
        SetString(gameManager, "mainMenuSceneName", "MainMenu");
        SetFloat(gameManager, "respawnDelay", 0.8f);

        SetObjectReference(deathZoneObject.GetComponent<DeathZone>(), "gameManager", gameManager);

        if (finishChest != null)
        {
            SetObjectReference(finishChest.GetComponent<FinishTrigger>(), "gameManager", gameManager);
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    private static void ConfigurePlayer(GameObject playerObject, GameObject visualObject, PlayerAnimationFrames playerAnimationFrames, int playerLayer, int groundLayer)
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

        RectTransform safeArea = CreateRect("SafeArea", hud.transform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        GetOrAdd<SafeArea>(safeArea.gameObject);

        Button pauseButton = CreateButton("PauseButton", safeArea, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(110f, 78f), new Vector2(36f, -36f), "II", 34);
        SetButtonListener(pauseButton, gameManager.PauseGame);

        Text timerText = CreateLabel("TimerText", safeArea, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(340f, 74f), new Vector2(-38f, -38f), "Time: 00:00", 34);
        timerText.alignment = TextAnchor.MiddleRight;

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
        Button playAgainButton = CreateButton("PlayAgainButton", winPanel.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(360f, 88f), new Vector2(0f, -86f), "PLAY AGAIN", 28);
        Button winMenuButton = CreateButton("WinMainMenuButton", winPanel.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(360f, 88f), new Vector2(0f, -196f), "MAIN MENU", 28);
        SetButtonListener(playAgainButton, gameManager.RestartLevel);
        SetButtonListener(winMenuButton, gameManager.GoToMainMenu);
        winPanel.SetActive(false);

        return new GameHudRefs
        {
            TimerText = timerText,
            PausePanel = pausePanel,
            WinPanel = winPanel,
            WinTimeText = winTimeText,
            WinBestTimeText = winBestText
        };
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

    private static void SetupMainMenuScene()
    {
        Scene scene = EditorSceneManager.OpenScene(MainMenuScenePath, OpenSceneMode.Single);

        GameObject canvasObject = FindInScene(scene, "MainMenuCanvas");
        if (canvasObject == null)
        {
            canvasObject = new GameObject("MainMenuCanvas", typeof(RectTransform));
            SceneManager.MoveGameObjectToScene(canvasObject, scene);
            MarkCreated(canvasObject);
        }

        RemoveChildren(canvasObject.transform);

        Canvas canvas = GetOrAdd<Canvas>(canvasObject);
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = GetOrAdd<CanvasScaler>(canvasObject);
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        GetOrAdd<GraphicRaycaster>(canvasObject);
        MainMenuUI menuUi = GetOrAdd<MainMenuUI>(canvasObject);
        SetString(menuUi, "gameSceneName", "Game");

        RectTransform safeArea = CreateRect("SafeArea", canvasObject.transform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        GetOrAdd<SafeArea>(safeArea.gameObject);

        Image background = GetOrAdd<Image>(safeArea.gameObject);
        background.color = new Color(0.09f, 0.12f, 0.17f, 1f);
        background.raycastTarget = false;

        CreateLabel("Title", safeArea, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(1100f, 110f), new Vector2(0f, 210f), "PIXEL VILLAGE ADVENTURE", 58);
        CreateLabel("Subtitle", safeArea, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(700f, 70f), new Vector2(0f, 122f), "Reach the chest!", 30);
        Button playButton = CreateButton("PlayButton", safeArea, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(360f, 94f), new Vector2(0f, -10f), "PLAY", 34);
        Button quitButton = CreateButton("QuitButton", safeArea, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(360f, 94f), new Vector2(0f, -130f), "QUIT", 34);
        SetButtonListener(playButton, menuUi.Play);
        SetButtonListener(quitButton, menuUi.Quit);

        GameObject cameraObject = FindInScene(scene, "Main Camera");
        if (cameraObject != null)
        {
            Camera camera = cameraObject.GetComponent<Camera>();
            if (camera != null)
            {
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.09f, 0.12f, 0.17f, 1f);
            }
        }

        EnsureSingleEventSystem(scene);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
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
        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(MainMenuScenePath, true),
            new EditorBuildSettingsScene(GameScenePath, true)
        };
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

    private static SerializedProperty GetProperty(Object target, string propertyName)
    {
        if (target == null)
        {
            return null;
        }

        SerializedObject serializedObject = new SerializedObject(target);
        return serializedObject.FindProperty(propertyName);
    }

    private struct GameHudRefs
    {
        public Text TimerText;
        public GameObject PausePanel;
        public GameObject WinPanel;
        public Text WinTimeText;
        public Text WinBestTimeText;
    }

    private struct PlayerAnimationFrames
    {
        public Sprite[] Idle;
        public Sprite[] Run;
        public Sprite[] Air;
        public Sprite[] Death;
        public Sprite FirstIdleSprite;
    }
}
#endif
