using UnityEngine;

[DisallowMultipleComponent]
public sealed class PlayerCharacterLoader : MonoBehaviour
{
    [SerializeField] private CharacterDatabase characterDatabase;
    [SerializeField] private PlayerController player;
    [SerializeField] private Transform characterVisualRoot;
    [SerializeField] private GameObject fallbackVisual;
    [SerializeField] private int sortingOrder = 20;

    private GameObject currentInstance;

    private void Awake()
    {
        if (player == null)
        {
            player = GetComponent<PlayerController>();
        }

        if (characterVisualRoot == null)
        {
            characterVisualRoot = transform.Find("CharacterVisualRoot");
        }

        if (characterVisualRoot == null)
        {
            GameObject root = new GameObject("CharacterVisualRoot");
            root.transform.SetParent(transform, false);
            characterVisualRoot = root.transform;
        }

        LoadSelectedCharacter();
    }

    public void LoadSelectedCharacter()
    {
        ClearCurrentInstance();

        if (characterDatabase == null)
        {
            SetFallbackVisualActive(true);
            return;
        }

        CharacterProgress.EnsureDefaults(characterDatabase);
        CharacterDefinition character = characterDatabase.GetById(CharacterProgress.GetSelectedCharacterId(characterDatabase));
        if (character == null || character.CharacterPrefab == null)
        {
            SetFallbackVisualActive(true);
            return;
        }

        currentInstance = Instantiate(character.CharacterPrefab, characterVisualRoot);
        currentInstance.name = "SelectedCharacterInstance";
        currentInstance.transform.localPosition = Vector3.zero;
        currentInstance.transform.localRotation = Quaternion.identity;
        currentInstance.transform.localScale = Vector3.one;

        SanitizeVisualInstance(currentInstance);
        CharacterVisualDriver driver = currentInstance.AddComponent<CharacterVisualDriver>();
        driver.Initialize(player, character.PreviewSprite, sortingOrder);
        SetFallbackVisualActive(false);
    }

    private void ClearCurrentInstance()
    {
        if (currentInstance != null)
        {
            Destroy(currentInstance);
            currentInstance = null;
        }

        if (characterVisualRoot == null)
        {
            return;
        }

        for (int i = characterVisualRoot.childCount - 1; i >= 0; i--)
        {
            Destroy(characterVisualRoot.GetChild(i).gameObject);
        }
    }

    private void SanitizeVisualInstance(GameObject instance)
    {
        foreach (Rigidbody2D body in instance.GetComponentsInChildren<Rigidbody2D>(true))
        {
            Destroy(body);
        }

        foreach (Collider2D collider in instance.GetComponentsInChildren<Collider2D>(true))
        {
            Destroy(collider);
        }

        foreach (MonoBehaviour behaviour in instance.GetComponentsInChildren<MonoBehaviour>(true))
        {
            Destroy(behaviour);
        }

        foreach (SpriteRenderer renderer in instance.GetComponentsInChildren<SpriteRenderer>(true))
        {
            renderer.sortingOrder = sortingOrder;
        }
    }

    private void SetFallbackVisualActive(bool active)
    {
        if (fallbackVisual != null)
        {
            fallbackVisual.SetActive(active);
        }
    }
}
