using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider2D))]
public sealed class EnemyProjectile : MonoBehaviour
{
    [SerializeField] private float speed = 4f;
    [SerializeField] private int damage = 1;
    [SerializeField] private float lifetime = 4f;

    private Vector2 direction = Vector2.right;
    private float expireTime;

    private void Awake()
    {
        Collider2D trigger = GetComponent<Collider2D>();
        trigger.isTrigger = true;
        expireTime = Time.time + lifetime;
    }

    private void Update()
    {
        transform.position += (Vector3)(direction * speed * Time.deltaTime);
        if (Time.time >= expireTime)
        {
            Destroy(gameObject);
        }
    }

    public void Launch(Vector2 launchDirection, float launchSpeed, int hitDamage)
    {
        direction = launchDirection.sqrMagnitude > 0.001f ? launchDirection.normalized : Vector2.right;
        speed = launchSpeed;
        damage = Mathf.Max(1, hitDamage);
        expireTime = Time.time + lifetime;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        PlayerController player = other.GetComponentInParent<PlayerController>();
        if (player == null)
        {
            return;
        }

        GameManager manager = GameManager.Instance != null ? GameManager.Instance : FindFirstObjectByType<GameManager>();
        if (manager != null && manager.TryDamagePlayer(player, damage, transform.position))
        {
            Destroy(gameObject);
        }
    }
}
