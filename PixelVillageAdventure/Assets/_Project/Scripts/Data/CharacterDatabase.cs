using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Pixel Village/Character Database")]
public sealed class CharacterDatabase : ScriptableObject
{
    [SerializeField] private CharacterDefinition[] characters;

    public IReadOnlyList<CharacterDefinition> Characters => characters ?? System.Array.Empty<CharacterDefinition>();

    public CharacterDefinition GetById(string id)
    {
        if (string.IsNullOrEmpty(id) || characters == null)
        {
            return null;
        }

        foreach (CharacterDefinition character in characters)
        {
            if (character != null && character.Id == id)
            {
                return character;
            }
        }

        return null;
    }

    public CharacterDefinition GetDefaultCharacter()
    {
        if (characters == null || characters.Length == 0)
        {
            return null;
        }

        foreach (CharacterDefinition character in characters)
        {
            if (character != null && character.UnlockedByDefault)
            {
                return character;
            }
        }

        return characters[0];
    }
}
