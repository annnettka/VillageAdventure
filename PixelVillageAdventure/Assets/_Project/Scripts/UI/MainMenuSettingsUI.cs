using UnityEngine;
using UnityEngine.UI;

public sealed class MainMenuSettingsUI : MonoBehaviour
{
    private const string MusicKey = "MusicEnabled";
    private const string SoundKey = "SoundEnabled";

    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private Text musicButtonText;
    [SerializeField] private Text soundButtonText;

    private void Awake()
    {
        EnsureDefaults();
        RefreshLabels();
        CloseSettings();
    }

    public void OpenSettings()
    {
        EnsureDefaults();
        RefreshLabels();

        if (settingsPanel != null)
        {
            settingsPanel.SetActive(true);
        }
    }

    public void CloseSettings()
    {
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }
    }

    public void ToggleMusic()
    {
        SetEnabled(MusicKey, !GetEnabled(MusicKey));
        RefreshLabels();
    }

    public void ToggleSound()
    {
        SetEnabled(SoundKey, !GetEnabled(SoundKey));
        RefreshLabels();
    }

    private void RefreshLabels()
    {
        if (musicButtonText != null)
        {
            musicButtonText.text = GetEnabled(MusicKey) ? "MUSIC ON" : "MUSIC OFF";
        }

        if (soundButtonText != null)
        {
            soundButtonText.text = GetEnabled(SoundKey) ? "SOUND ON" : "SOUND OFF";
        }
    }

    private static void EnsureDefaults()
    {
        if (!PlayerPrefs.HasKey(MusicKey))
        {
            PlayerPrefs.SetInt(MusicKey, 1);
        }

        if (!PlayerPrefs.HasKey(SoundKey))
        {
            PlayerPrefs.SetInt(SoundKey, 1);
        }

        PlayerPrefs.Save();
    }

    private static bool GetEnabled(string key)
    {
        return PlayerPrefs.GetInt(key, 1) != 0;
    }

    private static void SetEnabled(string key, bool enabled)
    {
        PlayerPrefs.SetInt(key, enabled ? 1 : 0);
        PlayerPrefs.Save();
    }
}
