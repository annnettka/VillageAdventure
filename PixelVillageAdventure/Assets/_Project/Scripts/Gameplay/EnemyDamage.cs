using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider2D))]
public sealed class EnemyDamage : MonoBehaviour
{
    [SerializeField] private int damage = 1;
    [SerializeField] private float hitCooldown = 0.25f;

    private float nextAllowedHitTime;

    private void Reset()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    private void Awake()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryDamage(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        TryDamage(other);
    }

    private void TryDamage(Collider2D other)
    {
        if (Time.time < nextAllowedHitTime)
        {
            return;
        }

        PlayerController player = other.GetComponentInParent<PlayerController>();
        if (player == null)
        {
            return;
        }

        GameManager manager = GameManager.Instance != null ? GameManager.Instance : FindFirstObjectByType<GameManager>();
        if (manager != null && manager.TryDamagePlayer(player, damage, transform.position))
        {
            nextAllowedHitTime = Time.time + hitCooldown;
        }
    }
}
