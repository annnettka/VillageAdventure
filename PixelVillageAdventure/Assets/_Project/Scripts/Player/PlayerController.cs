using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[RequireComponent(typeof(Rigidbody2D))]
public sealed class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5.5f;
    [SerializeField] private float acceleration = 55f;
    [SerializeField] private float deceleration = 70f;
    [SerializeField] private float jumpForce = 9.5f;

    [Header("Gravity")]
    [SerializeField] private float gravityScale = 3.5f;
    [SerializeField] private float fallMultiplier = 1.65f;

    [Header("Grounding")]
    [SerializeField] private LayerMask groundLayers = ~0;
    [SerializeField] private float groundCheckRadius = 0.12f;
    [SerializeField] private float groundCheckDistance = 0.08f;

    [Header("References")]
    [SerializeField] private Rigidbody2D body;
    [SerializeField] private Collider2D bodyCollider;
    [SerializeField] private SpriteRenderer visualRenderer;
    [SerializeField] private PlayerAnimator playerAnimator;

    private readonly RaycastHit2D[] groundHits = new RaycastHit2D[8];
    private float moveInput;
    private bool jumpQueued;
    private bool leftHeld;
    private bool rightHeld;
    private bool inputLocked;
    private bool dead;

    public bool IsGrounded { get; private set; }
    public bool IsDead => dead;
    public float HorizontalSpeed => body != null ? Mathf.Abs(body.linearVelocity.x) : 0f;
    public Vector2 Velocity => body != null ? body.linearVelocity : Vector2.zero;

    private void Reset()
    {
        body = GetComponent<Rigidbody2D>();
        bodyCollider = GetComponent<Collider2D>();
        visualRenderer = GetComponentInChildren<SpriteRenderer>();
        playerAnimator = GetComponent<PlayerAnimator>();
    }

    private void Awake()
    {
        if (body == null)
        {
            body = GetComponent<Rigidbody2D>();
        }

        if (bodyCollider == null)
        {
            bodyCollider = GetComponent<Collider2D>();
        }

        if (visualRenderer == null)
        {
            visualRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        if (playerAnimator == null)
        {
            playerAnimator = GetComponent<PlayerAnimator>();
        }

        body.gravityScale = gravityScale;
        body.freezeRotation = true;
    }

    private void Update()
    {
        if (!CanAcceptInput())
        {
            moveInput = 0f;
            return;
        }

        moveInput = Mathf.Clamp(GetKeyboardAxis() + GetMobileAxis(), -1f, 1f);

        if (GetKeyboardJumpDown())
        {
            QueueJump();
        }

        UpdateFacing(moveInput);
    }

    private void FixedUpdate()
    {
        if (body == null)
        {
            return;
        }

        IsGrounded = CheckGrounded();

        if (dead)
        {
            body.linearVelocity = Vector2.zero;
            return;
        }

        float targetX = CanAcceptInput() ? moveInput * moveSpeed : 0f;
        float rate = Mathf.Abs(targetX) > 0.01f ? acceleration : deceleration;
        Vector2 velocity = body.linearVelocity;
        velocity.x = Mathf.MoveTowards(velocity.x, targetX, rate * Time.fixedDeltaTime);

        if (jumpQueued && IsGrounded)
        {
            velocity.y = jumpForce;
            IsGrounded = false;
        }

        jumpQueued = false;
        body.linearVelocity = velocity;
        body.gravityScale = velocity.y < -0.05f ? gravityScale * fallMultiplier : gravityScale;
    }

    public void SetMobileMove(int direction, bool held)
    {
        if (direction < 0)
        {
            leftHeld = held;
        }
        else if (direction > 0)
        {
            rightHeld = held;
        }
    }

    public void RequestMobileJump()
    {
        if (CanAcceptInput())
        {
            QueueJump();
        }
    }

    public void SetInputLocked(bool locked, bool stopImmediately = false)
    {
        inputLocked = locked;

        if (locked)
        {
            leftHeld = false;
            rightHeld = false;
            moveInput = 0f;
            jumpQueued = false;
        }

        if (stopImmediately && body != null)
        {
            body.linearVelocity = Vector2.zero;
        }
    }

    public void Die()
    {
        if (dead)
        {
            return;
        }

        dead = true;
        SetInputLocked(true, true);

        if (playerAnimator != null)
        {
            playerAnimator.SetDead(true);
        }
    }

    public void Respawn(Vector3 spawnPosition)
    {
        transform.position = spawnPosition;

        if (body != null)
        {
            body.position = spawnPosition;
            body.linearVelocity = Vector2.zero;
            body.angularVelocity = 0f;
            body.gravityScale = gravityScale;
        }

        dead = false;
        IsGrounded = false;
        SetInputLocked(false);

        if (playerAnimator != null)
        {
            playerAnimator.ResetToIdle();
        }
    }

    private bool CanAcceptInput()
    {
        return !inputLocked && !dead && (GameManager.Instance == null || GameManager.Instance.IsPlaying);
    }

    private void QueueJump()
    {
        jumpQueued = true;
    }

    private float GetMobileAxis()
    {
        if (leftHeld == rightHeld)
        {
            return 0f;
        }

        return rightHeld ? 1f : -1f;
    }

    private float GetKeyboardAxis()
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return 0f;
        }

        float axis = 0f;
        if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed)
        {
            axis -= 1f;
        }

        if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed)
        {
            axis += 1f;
        }

        return axis;
#elif ENABLE_LEGACY_INPUT_MANAGER
        float axis = 0f;
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
        {
            axis -= 1f;
        }

        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
        {
            axis += 1f;
        }

        return axis;
#else
        return 0f;
#endif
    }

    private bool GetKeyboardJumpDown()
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard keyboard = Keyboard.current;
        return keyboard != null && keyboard.spaceKey.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetKeyDown(KeyCode.Space);
#else
        return false;
#endif
    }

    private bool CheckGrounded()
    {
        if (bodyCollider == null)
        {
            return false;
        }

        Bounds bounds = bodyCollider.bounds;
        Vector2 origin = new Vector2(bounds.center.x, bounds.min.y + groundCheckRadius);

        ContactFilter2D filter = new ContactFilter2D
        {
            useTriggers = false
        };
        filter.SetLayerMask(groundLayers);

        int count = Physics2D.CircleCast(origin, groundCheckRadius, Vector2.down, filter, groundHits, groundCheckDistance);
        for (int i = 0; i < count; i++)
        {
            Collider2D hitCollider = groundHits[i].collider;
            if (hitCollider != null && hitCollider != bodyCollider && !hitCollider.isTrigger)
            {
                return true;
            }
        }

        return false;
    }

    private void UpdateFacing(float axis)
    {
        if (visualRenderer == null || Mathf.Abs(axis) < 0.01f)
        {
            return;
        }

        visualRenderer.flipX = axis < 0f;
    }

    private void OnDrawGizmosSelected()
    {
        Collider2D targetCollider = bodyCollider != null ? bodyCollider : GetComponent<Collider2D>();
        if (targetCollider == null)
        {
            return;
        }

        Bounds bounds = targetCollider.bounds;
        Vector3 origin = new Vector3(bounds.center.x, bounds.min.y + groundCheckRadius, transform.position.z);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(origin + Vector3.down * groundCheckDistance, groundCheckRadius);
    }
}
