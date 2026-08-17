using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class EnemyMeleeAttack : MonoBehaviour
{
    [SerializeField] private bool attackEnabled = true;
    [SerializeField] private float attackRange = 1.35f;
    [SerializeField] private float attackCooldown = 1.8f;
    [SerializeField] private float activeTime = 0.18f;
    [SerializeField] private int damage = 1;
    [SerializeField] private GameObject weaponVisual;

    private PlayerController player;
    private float nextAttackTime;
    private Coroutine attackRoutine;

    private void Awake()
    {
        player = FindFirstObjectByType<PlayerController>();
        SetWeaponVisible(false);
    }

    private void Update()
    {
        if (!attackEnabled || Time.time < nextAttackTime || attackRoutine != null)
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

        if ((player.transform.position - transform.position).sqrMagnitude <= attackRange * attackRange)
        {
            attackRoutine = StartCoroutine(AttackRoutine());
        }
    }

    private IEnumerator AttackRoutine()
    {
        SetWeaponVisible(true);
        yield return new WaitForSeconds(activeTime);

        if (player != null && (player.transform.position - transform.position).sqrMagnitude <= attackRange * attackRange)
        {
            GameManager manager = GameManager.Instance != null ? GameManager.Instance : FindFirstObjectByType<GameManager>();
            if (manager != null)
            {
                manager.TryDamagePlayer(player, damage, transform.position);
            }
        }

        SetWeaponVisible(false);
        nextAttackTime = Time.time + attackCooldown;
        attackRoutine = null;
    }

    private void SetWeaponVisible(bool visible)
    {
        if (weaponVisual != null)
        {
            weaponVisual.SetActive(visible);
        }
    }
}
