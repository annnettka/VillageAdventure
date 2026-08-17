using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public sealed class DeathZone : MonoBehaviour
{
    [SerializeField] private GameManager gameManager;

    private void Awake()
    {
        Collider2D trigger = GetComponent<Collider2D>();
        trigger.isTrigger = true;

        if (gameManager == null)
        {
            gameManager = GameManager.Instance != null ? GameManager.Instance : FindFirstObjectByType<GameManager>();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        PlayerController player = other.GetComponentInParent<PlayerController>();
        if (player == null)
        {
            return;
        }

        GameManager targetManager = gameManager != null ? gameManager : GameManager.Instance;
        if (targetManager != null)
        {
            targetManager.BeginPlayerDeath(player);
        }
    }
}
