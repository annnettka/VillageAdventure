using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider2D))]
public sealed class CollectibleFlower : MonoBehaviour
{
    private bool collected;

    private void Reset()
    {
        Collider2D trigger = GetComponent<Collider2D>();
        trigger.isTrigger = true;
    }

    private void Awake()
    {
        Collider2D trigger = GetComponent<Collider2D>();
        trigger.isTrigger = true;
    }

    private void OnEnable()
    {
        collected = false;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (collected || other.GetComponentInParent<PlayerController>() == null)
        {
            return;
        }

        GameManager manager = GameManager.Instance != null ? GameManager.Instance : FindFirstObjectByType<GameManager>();
        if (manager == null || !manager.TryCollectFlower(this))
        {
            return;
        }

        collected = true;
        gameObject.SetActive(false);
    }
}
