using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class CharacterShopUI : MonoBehaviour
{
    [SerializeField] private CharacterDatabase characterDatabase;
    [SerializeField] private GameObject panel;
    [SerializeField] private Transform gridRoot;
    [SerializeField] private CharacterShopCard cardTemplate;
    [SerializeField] private TMP_Text currencyText;
    [SerializeField] private TMP_Text feedbackText;

    private Coroutine feedbackRoutine;

    private void Awake()
    {
        if (panel != null)
        {
            panel.SetActive(false);
        }

        if (cardTemplate != null)
        {
            cardTemplate.gameObject.SetActive(false);
        }
    }

    private void OnEnable()
    {
        RefreshCurrency();
    }

    public void OpenCharacters()
    {
        if (panel != null)
        {
            panel.SetActive(true);
        }

        Refresh();
    }

    public void CloseCharacters()
    {
        if (panel != null)
        {
            panel.SetActive(false);
        }
    }

    public void Refresh()
    {
        CharacterProgress.EnsureDefaults(characterDatabase);
        RefreshCurrency();
        ClearCards();

        if (characterDatabase == null || gridRoot == null || cardTemplate == null)
        {
            ShowFeedback("Characters unavailable");
            return;
        }

        int visibleCharacterCount = 0;
        foreach (CharacterDefinition character in characterDatabase.Characters)
        {
            if (!IsUsableCharacter(character))
            {
                continue;
            }

            CharacterShopCard card = Instantiate(cardTemplate, gridRoot);
            card.gameObject.SetActive(true);
            card.Setup(this, character, characterDatabase);
            visibleCharacterCount++;
        }

        if (visibleCharacterCount == 0)
        {
            ShowFeedback("Characters unavailable");
        }
        else
        {
            ClearFeedback();
        }
    }

    public void HandleCardAction(CharacterDefinition character)
    {
        if (character == null)
        {
            return;
        }

        if (CharacterProgress.IsUnlocked(character))
        {
            if (CharacterProgress.Select(character))
            {
                ShowFeedback($"{character.DisplayName} selected");
            }
            Refresh();
            return;
        }

        if (!CharacterProgress.TrySpendFlowers(character.Price))
        {
            ShowFeedback("Not enough flowers");
            RefreshCurrency();
            return;
        }

        CharacterProgress.Unlock(character);
        CharacterProgress.Select(character);
        ShowFeedback($"{character.DisplayName} unlocked");
        Refresh();
    }

    private void RefreshCurrency()
    {
        if (currencyText != null)
        {
            currencyText.text = $"x {CharacterProgress.TotalFlowers}";
        }
    }

    private void ClearCards()
    {
        if (gridRoot == null)
        {
            return;
        }

        for (int i = gridRoot.childCount - 1; i >= 0; i--)
        {
            Transform child = gridRoot.GetChild(i);
            if (cardTemplate != null && child == cardTemplate.transform)
            {
                continue;
            }

            Destroy(child.gameObject);
        }
    }

    private void ShowFeedback(string message)
    {
        if (feedbackText == null)
        {
            return;
        }

        if (feedbackRoutine != null)
        {
            StopCoroutine(feedbackRoutine);
        }

        feedbackRoutine = StartCoroutine(FeedbackRoutine(message));
    }

    private void ClearFeedback()
    {
        if (feedbackRoutine != null)
        {
            StopCoroutine(feedbackRoutine);
            feedbackRoutine = null;
        }

        if (feedbackText != null)
        {
            feedbackText.text = string.Empty;
        }
    }

    private static bool IsUsableCharacter(CharacterDefinition character)
    {
        return character != null
            && !string.IsNullOrEmpty(character.Id)
            && character.CharacterPrefab != null;
    }

    private IEnumerator FeedbackRoutine(string message)
    {
        feedbackText.text = message;
        yield return new WaitForSecondsRealtime(1.4f);
        feedbackText.text = string.Empty;
        feedbackRoutine = null;
    }
}
