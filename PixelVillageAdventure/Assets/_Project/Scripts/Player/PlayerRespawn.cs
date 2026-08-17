using UnityEngine;

[DisallowMultipleComponent]
public sealed class PlayerRespawn : MonoBehaviour
{
    [SerializeField] private PlayerController player;
    [SerializeField] private Rigidbody2D body;
    [SerializeField] private Collider2D bodyCollider;
    [SerializeField] private LayerMask groundLayers = ~0;
    [SerializeField] private float safeGroundedSeconds = 0.16f;
    [SerializeField] private float groundProbeDistance = 0.22f;
    [SerializeField] private float respawnYOffset = 0.65f;

    private float groundedTimer;
    private Vector3 lastSafePosition;
    private bool hasSafePosition;

    public Vector3 LastSafePosition => hasSafePosition ? lastSafePosition : transform.position;
    public Vector3 RespawnPosition => LastSafePosition + Vector3.up * respawnYOffset;

    private void Reset()
    {
        player = GetComponent<PlayerController>();
        body = GetComponent<Rigidbody2D>();
        bodyCollider = GetComponent<Collider2D>();
    }

    private void Awake()
    {
        if (player == null)
        {
            player = GetComponent<PlayerController>();
        }

        if (body == null)
        {
            body = GetComponent<Rigidbody2D>();
        }

        if (bodyCollider == null)
        {
            bodyCollider = GetComponent<Collider2D>();
        }

        if (!hasSafePosition)
        {
            SetInitialSafePosition(transform.position);
        }
    }

    private void FixedUpdate()
    {
        if (player == null || player.IsDead || !player.IsGrounded)
        {
            groundedTimer = 0f;
            return;
        }

        if (body != null && body.linearVelocity.y < -0.05f)
        {
            groundedTimer = 0f;
            return;
        }

        if (!HasSafeGroundSupport())
        {
            groundedTimer = 0f;
            return;
        }

        groundedTimer += Time.fixedDeltaTime;
        if (groundedTimer >= safeGroundedSeconds)
        {
            lastSafePosition = transform.position;
            hasSafePosition = true;
        }
    }

    public void SetInitialSafePosition(Vector3 position)
    {
        lastSafePosition = position;
        hasSafePosition = true;
        groundedTimer = 0f;
    }

    private bool HasSafeGroundSupport()
    {
        if (bodyCollider == null)
        {
            return player == null || player.IsGrounded;
        }

        Bounds bounds = bodyCollider.bounds;
        float halfWidth = Mathf.Max(0.05f, Mathf.Min(bounds.extents.x * 0.55f, bounds.extents.x - 0.05f));
        Vector2 center = new Vector2(bounds.center.x, bounds.min.y + 0.04f);

        int supportedPoints = 0;
        supportedPoints += HasGroundBelow(center) ? 1 : 0;
        supportedPoints += HasGroundBelow(center + Vector2.left * halfWidth) ? 1 : 0;
        supportedPoints += HasGroundBelow(center + Vector2.right * halfWidth) ? 1 : 0;

        return supportedPoints >= 2;
    }

    private bool HasGroundBelow(Vector2 origin)
    {
        RaycastHit2D hit = Physics2D.Raycast(origin, Vector2.down, groundProbeDistance, groundLayers);
        return hit.collider != null && hit.collider != bodyCollider && !hit.collider.isTrigger;
    }
}
