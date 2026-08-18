using UnityEngine;

public static class CharacterProgress
{
    public const string TotalFlowersKey = "TotalFlowers";
    public const string SelectedCharacterIdKey = "SelectedCharacterId";
    public const string CharacterUnlockedPrefix = "CharacterUnlocked_";

    public static int TotalFlowers => PlayerPrefs.GetInt(TotalFlowersKey, 0);

    public static void AddFlowers(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        PlayerPrefs.SetInt(TotalFlowersKey, TotalFlowers + amount);
        PlayerPrefs.Save();
    }

    public static bool TrySpendFlowers(int amount)
    {
        amount = Mathf.Max(0, amount);
        if (TotalFlowers < amount)
        {
            return false;
        }

        PlayerPrefs.SetInt(TotalFlowersKey, TotalFlowers - amount);
        PlayerPrefs.Save();
        return true;
    }

    public static bool IsUnlocked(CharacterDefinition character)
    {
        if (character == null)
        {
            return false;
        }

        return character.UnlockedByDefault || PlayerPrefs.GetInt(CharacterUnlockedPrefix + character.Id, 0) != 0;
    }

    public static void Unlock(CharacterDefinition character)
    {
        if (character == null)
        {
            return;
        }

        PlayerPrefs.SetInt(CharacterUnlockedPrefix + character.Id, 1);
        PlayerPrefs.Save();
    }

    public static string GetSelectedCharacterId(CharacterDatabase database)
    {
        string selectedId = PlayerPrefs.GetString(SelectedCharacterIdKey, string.Empty);
        if (!string.IsNullOrEmpty(selectedId))
        {
            CharacterDefinition selected = database != null ? database.GetById(selectedId) : null;
            if (database == null || (selected != null && IsUnlocked(selected)))
            {
                return selectedId;
            }
        }

        CharacterDefinition fallback = database != null ? database.GetDefaultCharacter() : null;
        if (fallback != null)
        {
            Unlock(fallback);
            Select(fallback);
            return fallback.Id;
        }

        return selectedId;
    }

    public static bool Select(CharacterDefinition character)
    {
        if (character == null || !IsUnlocked(character))
        {
            return false;
        }

        PlayerPrefs.SetString(SelectedCharacterIdKey, character.Id);
        PlayerPrefs.Save();
        return true;
    }

    public static void EnsureDefaults(CharacterDatabase database)
    {
        CharacterDefinition fallback = database != null ? database.GetDefaultCharacter() : null;
        if (fallback == null)
        {
            return;
        }

        Unlock(fallback);
        string selectedId = PlayerPrefs.GetString(SelectedCharacterIdKey, string.Empty);
        CharacterDefinition selected = database.GetById(selectedId);
        if (selected == null || !IsUnlocked(selected))
        {
            Select(fallback);
        }
    }

    public static void ResetCharacterShop(CharacterDatabase database)
    {
        if (database != null)
        {
            foreach (CharacterDefinition character in database.Characters)
            {
                if (character != null)
                {
                    PlayerPrefs.DeleteKey(CharacterUnlockedPrefix + character.Id);
                }
            }
        }

        PlayerPrefs.DeleteKey(SelectedCharacterIdKey);
        EnsureDefaults(database);
        PlayerPrefs.Save();
    }
}
