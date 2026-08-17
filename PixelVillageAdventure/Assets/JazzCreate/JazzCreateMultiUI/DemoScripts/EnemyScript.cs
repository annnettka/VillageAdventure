namespace JazzCreate.MultiPurposeUIPixel
{
    using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class EnemyScript : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float enemySpeed = 2f;

    private Rigidbody2D rb2D;

    private void Awake()
    {
        rb2D = GetComponent<Rigidbody2D>();

        // Set these in prefab ideally; keep here as safe defaults
        rb2D.gravityScale = 0f;
        rb2D.freezeRotation = true;
    }

    private void OnEnable()
    {
        // Reset velocity when pulled from pool
        rb2D.linearVelocity = Vector2.zero;
        rb2D.angularVelocity = 0f;
        
    }

    private void FixedUpdate()
    {
        // Always move down screen (world -Y)
        rb2D.linearVelocity = Vector2.down * enemySpeed;
    }

    private void OnCollisionEnter2D(Collision2D other)
    {
        if (other.collider.CompareTag("Player"))
        {
            gameObject.SetActive(false);
        }
    }

    // Optional: call this from bullet collision if you add it later
    public void Kill()
    {
        gameObject.SetActive(false);
    }
}
}

