using UnityEngine;
using UnityEngine.UI;
using TMPro;

public sealed class GameHUD : MonoBehaviour
{
    [SerializeField] private GameManager gameManager;
    [SerializeField] private GameObject[] hearts;
    [SerializeField] private TMP_Text flowerCountText;

    private bool subscribed;

    private void OnEnable()
    {
        Bind(gameManager != null ? gameManager : GameManager.Instance);
    }

    private void OnDisable()
    {
        if (gameManager == null || !subscribed)
        {
            return;
        }

        gameManager.OnLivesChanged -= HandleLivesChanged;
        gameManager.OnFlowerCountChanged -= HandleFlowerCountChanged;
        subscribed = false;
    }

    public void Bind(GameManager manager)
    {
        if (gameManager != null && subscribed)
        {
            gameManager.OnLivesChanged -= HandleLivesChanged;
            gameManager.OnFlowerCountChanged -= HandleFlowerCountChanged;
            subscribed = false;
        }

        gameManager = manager;
        if (gameManager == null)
        {
            return;
        }

        gameManager.OnLivesChanged += HandleLivesChanged;
        gameManager.OnFlowerCountChanged += HandleFlowerCountChanged;
        subscribed = true;
        RefreshAll();
    }

    private void RefreshAll()
    {
        if (gameManager == null)
        {
            return;
        }

        HandleLivesChanged(gameManager.CurrentLives, gameManager.MaxLives);
        HandleFlowerCountChanged(gameManager.CollectedFlowers);
    }

    private void HandleLivesChanged(int currentLives, int maxLives)
    {
        if (hearts == null)
        {
            return;
        }

        for (int i = 0; i < hearts.Length; i++)
        {
            if (hearts[i] != null)
            {
                hearts[i].SetActive(i < currentLives);
            }
        }
    }

    private void HandleFlowerCountChanged(int count)
    {
        if (flowerCountText != null)
        {
            flowerCountText.text = $"x {count}";
        }
    }
}
