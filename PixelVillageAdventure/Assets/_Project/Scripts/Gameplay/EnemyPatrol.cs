using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D))]
public sealed class EnemyPatrol : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 1.5f;
    [SerializeField] private float patrolDistance = 3f;
    [SerializeField] private LayerMask groundLayers = ~0;
    [SerializeField] private Transform visualRoot;
    [SerializeField] private Vector2 edgeCheckOffset = new Vector2(0.42f, -0.12f);
    [SerializeField] private float edgeCheckDistance = 0.55f;

    private Rigidbody2D body;
    private Vector2 startPosition;
    private int direction = 1;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
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

        if (ShouldTurnAround())
        {
            direction *= -1;
            ApplyFacing();
        }

        Vector2 nextPosition = body.position + Vector2.right * (direction * moveSpeed * Time.fixedDeltaTime);
        body.MovePosition(nextPosition);
    }

    private bool ShouldTurnAround()
    {
        if (Mathf.Abs(body.position.x - startPosition.x) >= Mathf.Max(0.05f, patrolDistance))
        {
            return true;
        }

        Vector2 edgeOrigin = body.position + new Vector2(edgeCheckOffset.x * direction, edgeCheckOffset.y);
        RaycastHit2D groundHit = Physics2D.Raycast(edgeOrigin, Vector2.down, edgeCheckDistance, groundLayers);
        return groundHit.collider == null;
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
