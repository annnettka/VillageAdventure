using UnityEngine;

[CreateAssetMenu(menuName = "Pixel Village/Character Definition")]
public sealed class CharacterDefinition : ScriptableObject
{
    [SerializeField] private string id;
    [SerializeField] private string displayName;
    [SerializeField] private GameObject characterPrefab;
    [SerializeField] private Sprite previewSprite;
    [SerializeField] private int price = 25;
    [SerializeField] private bool unlockedByDefault;

    public string Id => id;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public GameObject CharacterPrefab => characterPrefab;
    public Sprite PreviewSprite => previewSprite;
    public int Price => Mathf.Max(0, price);
    public bool UnlockedByDefault => unlockedByDefault;
}
