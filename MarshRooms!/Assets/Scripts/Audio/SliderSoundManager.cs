using UnityEngine;
using UnityEngine.UI;

public class SliderSoundManager : MonoBehaviour
{
    private const string MusicVolumeKey = "musicVolume";
    private const string SfxVolumeKey = "sfxVolume";

    [SerializeField] private Slider musicVolumeSlider;
    [SerializeField] private Slider sfxVolumeSlider;

    private float confirmedMusicVolume = 1f;
    private float confirmedSfxVolume = 1f;

    private void Awake()
    {
        confirmedMusicVolume = PlayerPrefs.GetFloat(MusicVolumeKey, 1f);
        confirmedSfxVolume = PlayerPrefs.GetFloat(SfxVolumeKey, 1f);
    }

    private void Start()
    {
        if (musicVolumeSlider != null)
            musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);

        if (sfxVolumeSlider != null)
            sfxVolumeSlider.onValueChanged.AddListener(OnSfxVolumeChanged);

        LoadConfirmedValues();
    }

    private void OnDestroy()
    {
        if (musicVolumeSlider != null)
            musicVolumeSlider.onValueChanged.RemoveListener(OnMusicVolumeChanged);

        if (sfxVolumeSlider != null)
            sfxVolumeSlider.onValueChanged.RemoveListener(OnSfxVolumeChanged);
    }

    public void LoadConfirmedValues()
    {
        confirmedMusicVolume = PlayerPrefs.GetFloat(MusicVolumeKey, confirmedMusicVolume);
        confirmedSfxVolume = PlayerPrefs.GetFloat(SfxVolumeKey, confirmedSfxVolume);

        if (musicVolumeSlider != null)
            musicVolumeSlider.SetValueWithoutNotify(confirmedMusicVolume);

        if (sfxVolumeSlider != null)
            sfxVolumeSlider.SetValueWithoutNotify(confirmedSfxVolume);

        ApplyVolumes(confirmedMusicVolume, confirmedSfxVolume);
    }

    public void ConfirmChanges()
    {
        confirmedMusicVolume = musicVolumeSlider != null ? musicVolumeSlider.value : confirmedMusicVolume;
        confirmedSfxVolume = sfxVolumeSlider != null ? sfxVolumeSlider.value : confirmedSfxVolume;

        PlayerPrefs.SetFloat(MusicVolumeKey, confirmedMusicVolume);
        PlayerPrefs.SetFloat(SfxVolumeKey, confirmedSfxVolume);
        PlayerPrefs.Save();

        ApplyVolumes(confirmedMusicVolume, confirmedSfxVolume);
    }

    public void CancelChanges()
    {
        LoadConfirmedValues();
    }

    private void OnMusicVolumeChanged(float value)
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.SetMusicVolume(value);
    }

    private void OnSfxVolumeChanged(float value)
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.SetSFXVolume(value);
    }

    private void ApplyVolumes(float musicVolume, float sfxVolume)
    {
        if (AudioManager.Instance == null)
            return;

        AudioManager.Instance.SetMusicVolume(musicVolume);
        AudioManager.Instance.SetSFXVolume(sfxVolume);
    }
}