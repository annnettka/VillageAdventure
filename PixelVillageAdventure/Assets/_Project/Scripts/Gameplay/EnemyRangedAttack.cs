using UnityEngine;

[DisallowMultipleComponent]
public sealed class EnemyRangedAttack : MonoBehaviour
{
    [SerializeField] private bool attackEnabled = true;
    [SerializeField] private float attackRange = 5f;
    [SerializeField] private float attackCooldown = 2f;
    [SerializeField] private float projectileSpeed = 4f;
    [SerializeField] private int damage = 1;
    [SerializeField] private Transform attackOrigin;
    [SerializeField] private GameObject projectilePrefab;

    private PlayerController player;
    private float nextAttackTime;

    private void Awake()
    {
        player = FindFirstObjectByType<PlayerController>();
        if (attackOrigin == null)
        {
            attackOrigin = transform;
        }
    }

    private void Update()
    {
        if (!attackEnabled || projectilePrefab == null || Time.time < nextAttackTime)
        {
            return;
        }

        if (player == null)
        {
            player = FindFirstObjectByType<PlayerController>();
            if (player == null)
            {
                return;
            }
        }

        Vector2 toPlayer = player.transform.position - attackOrigin.position;
        if (toPlayer.sqrMagnitude > attackRange * attackRange)
        {
            return;
        }

        Fire(toPlayer.normalized);
    }

    private void Fire(Vector2 direction)
    {
        GameObject projectile = Instantiate(projectilePrefab, attackOrigin.position, Quaternion.identity);
        EnemyProjectile enemyProjectile = projectile.GetComponent<EnemyProjectile>();
        if (enemyProjectile != null)
        {
            enemyProjectile.Launch(direction, projectileSpeed, damage);
        }

        nextAttackTime = Time.time + attackCooldown;
    }
}
