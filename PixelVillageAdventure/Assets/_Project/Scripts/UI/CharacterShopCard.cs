using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class CharacterShopCard : MonoBehaviour
{
    [SerializeField] private Image background;
    [SerializeField] private Image previewImage;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text priceText;
    [SerializeField] private TMP_Text actionText;
    [SerializeField] private Button actionButton;

    private CharacterShopUI shop;
    private CharacterDefinition character;
    private CharacterDatabase database;

    public void Setup(CharacterShopUI owner, CharacterDefinition definition, CharacterDatabase sourceDatabase)
    {
        shop = owner;
        character = definition;
        database = sourceDatabase;

        if (nameText != null)
        {
            nameText.text = character.DisplayName;
        }

        if (previewImage != null)
        {
            previewImage.sprite = character.PreviewSprite;
            previewImage.preserveAspect = true;
            previewImage.enabled = character.PreviewSprite != null;
        }

        if (actionButton != null)
        {
            actionButton.onClick.RemoveAllListeners();
            actionButton.onClick.AddListener(HandleAction);
        }

        RefreshState();
    }

    public void RefreshState()
    {
        bool unlocked = CharacterProgress.IsUnlocked(character);
        bool selected = unlocked && CharacterProgress.GetSelectedCharacterId(database) == character.Id;

        if (priceText != null)
        {
            priceText.text = unlocked ? "OWNED" : $"x {character.Price}";
        }

        if (actionText != null)
        {
            actionText.text = selected ? "SELECTED" : unlocked ? "SELECT" : "BUY";
        }

        if (actionButton != null)
        {
            actionButton.interactable = !selected;
        }

        if (background != null)
        {
            background.color = selected
                ? new Color(1f, 0.82f, 0.32f, 0.92f)
                : unlocked
                    ? new Color(0.23f, 0.18f, 0.12f, 0.9f)
                    : new Color(0.09f, 0.08f, 0.08f, 0.88f);
        }
    }

    private void HandleAction()
    {
        if (shop != null)
        {
            shop.HandleCardAction(character);
        }
    }
}
