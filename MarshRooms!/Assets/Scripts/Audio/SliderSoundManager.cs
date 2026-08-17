using UnityEngine;
using UnityEngine.UI;

public class SliderSoundManager : MonoBehaviour
{
    private const string MusicVolumeKey = "musicVolume";
    private const string SfxVolumeKey = "sfxVolume";

    [SerializeField] private Slider musicVolumeSlider;
    [SerializeField] private Slider sfxVolumeSlider;

    private void Start()
    {
        float savedMusicVolume = PlayerPrefs.GetFloat(MusicVolumeKey, 1f);
        float savedSfxVolume = PlayerPrefs.GetFloat(SfxVolumeKey, 1f);

        if (musicVolumeSlider != null)
        {
            musicVolumeSlider.SetValueWithoutNotify(savedMusicVolume);
            musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
        }

        if (sfxVolumeSlider != null)
        {
            sfxVolumeSlider.SetValueWithoutNotify(savedSfxVolume);
            sfxVolumeSlider.onValueChanged.AddListener(OnSfxVolumeChanged);
        }

        ApplyMusicVolume(savedMusicVolume);
        ApplySfxVolume(savedSfxVolume);
    }

    private void OnDestroy()
    {
        if (musicVolumeSlider != null)
            musicVolumeSlider.onValueChanged.RemoveListener(OnMusicVolumeChanged);

        if (sfxVolumeSlider != null)
            sfxVolumeSlider.onValueChanged.RemoveListener(OnSfxVolumeChanged);
    }

    private void OnMusicVolumeChanged(float value)
    {
        PlayerPrefs.SetFloat(MusicVolumeKey, value);
        PlayerPrefs.Save();
        ApplyMusicVolume(value);
    }

    private void OnSfxVolumeChanged(float value)
    {
        PlayerPrefs.SetFloat(SfxVolumeKey, value);
        PlayerPrefs.Save();
        ApplySfxVolume(value);
    }

    private void ApplyMusicVolume(float value)
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.SetMusicVolume(value);
    }

    private void ApplySfxVolume(float value)
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.SetSFXVolume(value);
    }
}