using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public sealed class FinishTrigger : MonoBehaviour
{
    [SerializeField] private GameManager gameManager;
    [SerializeField] private bool openChest = true;
    [SerializeField] private Animator chestAnimator;

    private bool completed;

    private void Awake()
    {
        Collider2D trigger = GetComponent<Collider2D>();
        trigger.isTrigger = true;

        if (gameManager == null)
        {
            gameManager = GameManager.Instance != null ? GameManager.Instance : FindFirstObjectByType<GameManager>();
        }

        if (chestAnimator == null)
        {
            chestAnimator = GetComponentInChildren<Animator>();
        }
    }

    private void OnEnable()
    {
        completed = false;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (completed || other.GetComponentInParent<PlayerController>() == null)
        {
            return;
        }

        GameManager targetManager = gameManager != null ? gameManager : GameManager.Instance;
        if (targetManager == null || !targetManager.IsPlaying)
        {
            return;
        }

        completed = true;

        if (openChest)
        {
            OpenChest();
        }

        targetManager.CompleteLevel();
    }

    private void OpenChest()
    {
        SendMessage("Open", SendMessageOptions.DontRequireReceiver);

        if (chestAnimator != null)
        {
            chestAnimator.SetBool("IsOpened", true);
        }
    }
}
