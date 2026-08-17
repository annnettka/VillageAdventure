using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public sealed class SpriteFrameAnimator : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerController player;
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Header("Frames")]
    [SerializeField] private Sprite[] idleFrames;
    [SerializeField] private Sprite[] runFrames;
    [SerializeField] private Sprite[] airFrames;
    [SerializeField] private Sprite[] deathFrames;

    [Header("Timing")]
    [SerializeField] private float idleFPS = 12f;
    [SerializeField] private float runFPS = 12f;
    [SerializeField] private float airFPS = 12f;
    [SerializeField] private float deathFPS = 12f;

    [Header("State")]
    [SerializeField] private float runSpeedThreshold = 0.05f;

    private AnimationState currentState = AnimationState.Idle;
    private Sprite fallbackSprite;
    private int frameIndex;
    private float frameTimer;
    private bool deathFinished;

    private enum AnimationState
    {
        Idle,
        Run,
        Air,
        Death
    }

    private void Reset()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        player = GetComponentInParent<PlayerController>();
    }

    private void Awake()
    {
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        if (player == null)
        {
            player = GetComponentInParent<PlayerController>();
        }

        fallbackSprite = FindFallbackSprite();
        ApplySprite(fallbackSprite);
    }

    private void OnEnable()
    {
        frameIndex = 0;
        frameTimer = 0f;
        deathFinished = false;
        fallbackSprite = FindFallbackSprite();
        ApplySprite(GetFrame(GetFramesForState(currentState), 0));
    }

    private void LateUpdate()
    {
        if (spriteRenderer == null)
        {
            return;
        }

        AnimationState nextState = ChooseState();
        if (nextState != currentState)
        {
            currentState = nextState;
            frameIndex = 0;
            frameTimer = 0f;
            deathFinished = false;
        }

        UpdateFacing();
        AdvanceFrames(Time.deltaTime);
    }

    public void SetFrames(Sprite[] idle, Sprite[] run, Sprite[] air, Sprite[] death)
    {
        idleFrames = idle;
        runFrames = run;
        airFrames = air;
        deathFrames = death;
        fallbackSprite = FindFallbackSprite();
        frameIndex = 0;
        frameTimer = 0f;
        ApplySprite(fallbackSprite);
    }

    private AnimationState ChooseState()
    {
        if (player != null && player.IsDead)
        {
            return AnimationState.Death;
        }

        if (player != null && !player.IsGrounded)
        {
            return AnimationState.Air;
        }

        if (player != null && player.HorizontalSpeed > runSpeedThreshold)
        {
            return AnimationState.Run;
        }

        return AnimationState.Idle;
    }

    private void AdvanceFrames(float deltaTime)
    {
        Sprite[] frames = GetFramesForState(currentState);
        if (frames == null || frames.Length == 0)
        {
            ApplySprite(fallbackSprite);
            return;
        }

        if (currentState == AnimationState.Death && deathFinished)
        {
            ApplySprite(frames[frames.Length - 1]);
            return;
        }

        float fps = Mathf.Max(1f, GetFPSForState(currentState));
        frameTimer += deltaTime;
        float frameDuration = 1f / fps;

        while (frameTimer >= frameDuration)
        {
            frameTimer -= frameDuration;
            frameIndex++;

            if (frameIndex >= frames.Length)
            {
                if (currentState == AnimationState.Death)
                {
                    frameIndex = frames.Length - 1;
                    deathFinished = true;
                    break;
                }

                frameIndex = 0;
            }
        }

        ApplySprite(GetFrame(frames, frameIndex));
    }

    private void UpdateFacing()
    {
        if (player == null)
        {
            return;
        }

        float xVelocity = player.Velocity.x;
        if (Mathf.Abs(xVelocity) > runSpeedThreshold)
        {
            spriteRenderer.flipX = xVelocity < 0f;
        }
    }

    private Sprite[] GetFramesForState(AnimationState state)
    {
        switch (state)
        {
            case AnimationState.Run:
                return runFrames;
            case AnimationState.Air:
                return airFrames;
            case AnimationState.Death:
                return deathFrames;
            default:
                return idleFrames;
        }
    }

    private float GetFPSForState(AnimationState state)
    {
        switch (state)
        {
            case AnimationState.Run:
                return runFPS;
            case AnimationState.Air:
                return airFPS;
            case AnimationState.Death:
                return deathFPS;
            default:
                return idleFPS;
        }
    }

    private Sprite FindFallbackSprite()
    {
        Sprite sprite = GetFrame(idleFrames, 0);
        if (sprite != null)
        {
            return sprite;
        }

        sprite = GetFrame(runFrames, 0);
        if (sprite != null)
        {
            return sprite;
        }

        sprite = GetFrame(airFrames, 0);
        if (sprite != null)
        {
            return sprite;
        }

        sprite = GetFrame(deathFrames, 0);
        if (sprite != null)
        {
            return sprite;
        }

        return spriteRenderer != null ? spriteRenderer.sprite : null;
    }

    private Sprite GetFrame(Sprite[] frames, int index)
    {
        if (frames == null || frames.Length == 0)
        {
            return null;
        }

        return frames[Mathf.Clamp(index, 0, frames.Length - 1)];
    }

    private void ApplySprite(Sprite sprite)
    {
        if (spriteRenderer == null)
        {
            return;
        }

        if (sprite != null)
        {
            fallbackSprite = sprite;
            spriteRenderer.sprite = sprite;
        }
        else if (fallbackSprite != null)
        {
            spriteRenderer.sprite = fallbackSprite;
        }
    }
}
