using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D))]
public sealed class EnemyPatrol : MonoBehaviour
{
    private const float TurnCooldown = 0.08f;
    private const float FootProbeHeight = 0.08f;

    [SerializeField] private float moveSpeed = 1.5f;
    [SerializeField] private float patrolDistance = 3f;
    [SerializeField] private LayerMask groundLayers = ~0;
    [SerializeField] private Transform visualRoot;
    [SerializeField] private Vector2 edgeCheckOffset = new Vector2(0.08f, 0.08f);
    [SerializeField] private float edgeCheckDistance = 0.55f;

    private Rigidbody2D body;
    private Collider2D bodyCollider;
    private Vector2 startPosition;
    private int direction = 1;
    private float nextAllowedTurnTime;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        bodyCollider = GetComponent<Collider2D>();
        body.bodyType = RigidbodyType2D.Kinematic;
        body.gravityScale = 0f;
        body.freezeRotation = true;
        startPosition = body.position;
        ApplyFacing();
    }

    private void FixedUpdate()
    {
        if (moveSpeed <= 0f)
        {
            return;
        }

        if (Time.time >= nextAllowedTurnTime && ShouldTurnAround())
        {
            TurnAround();
        }

        Vector2 nextPosition = body.position + Vector2.right * (direction * moveSpeed * Time.fixedDeltaTime);
        body.MovePosition(nextPosition);
    }

    private bool ShouldTurnAround()
    {
        if (ReachedPatrolLimit())
        {
            return true;
        }

        return HasGroundBelowSelf() && !HasGroundAhead();
    }

    private bool ReachedPatrolLimit()
    {
        float maxDistance = Mathf.Max(0.05f, patrolDistance);
        float distanceFromStart = body.position.x - startPosition.x;
        return direction > 0
            ? distanceFromStart >= maxDistance
            : distanceFromStart <= -maxDistance;
    }

    private bool HasGroundBelowSelf()
    {
        return HasGroundBelow(GetGroundProbeOrigin(0));
    }

    private bool HasGroundAhead()
    {
        return HasGroundBelow(GetGroundProbeOrigin(direction));
    }

    private bool HasGroundBelow(Vector2 origin)
    {
        RaycastHit2D groundHit = Physics2D.Raycast(origin, Vector2.down, GetGroundProbeDistance(), GetGroundMask());
        return groundHit.collider != null && groundHit.collider != bodyCollider && !groundHit.collider.isTrigger;
    }

    private Vector2 GetGroundProbeOrigin(int probeDirection)
    {
        Vector2 position = body.position;
        float horizontalOffset = Mathf.Abs(edgeCheckOffset.x);
        float verticalOffset = Mathf.Clamp(edgeCheckOffset.y, -0.02f, 0.25f);

        if (bodyCollider != null)
        {
            Bounds bounds = bodyCollider.bounds;
            horizontalOffset += probeDirection != 0 ? bounds.extents.x : 0f;
            position.y = bounds.min.y + FootProbeHeight + verticalOffset;
        }
        else
        {
            position.y += verticalOffset;
        }

        position.x += probeDirection * horizontalOffset;
        return position;
    }

    private float GetGroundProbeDistance()
    {
        return Mathf.Max(0.1f, edgeCheckDistance);
    }

    private int GetGroundMask()
    {
        return groundLayers.value != 0 ? groundLayers.value : Physics2D.DefaultRaycastLayers;
    }

    private void TurnAround()
    {
        direction *= -1;
        nextAllowedTurnTime = Time.time + TurnCooldown;
        ApplyFacing();
    }

    private void ApplyFacing()
    {
        Transform target = visualRoot != null ? visualRoot : transform;
        foreach (SpriteRenderer renderer in target.GetComponentsInChildren<SpriteRenderer>(true))
        {
            renderer.flipX = direction < 0;
        }
    }
}
