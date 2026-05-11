// Handles SFX and Music
// Access via AudioManager.Instance

using UnityEngine;

public sealed class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Sources")]
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioClip musicClip; // TEMPORARY!

    // -- AWAKE --
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        PlayMusic(musicClip);  // TEMPORARY!
    }

    // -- PLAY SFX --
    public void PlaySFX(AudioClip clip, float volume = 1f)
    {
        if (clip == null) return;
        sfxSource.PlayOneShot(clip, volume);
    }

    // -- PLAY MUSIC --
    public void PlayMusic(AudioClip clip, float volume = 1f)
    {
        if (clip == null || musicSource.clip == clip) return;
        musicSource.clip = clip;
        musicSource.volume = volume;
        musicSource.loop = true;
        musicSource.Play();
    }

    // -- STOP MUSIC --
    public void StopMusic()
    {
        musicSource.Stop();
    }

    // -- SET MUSIC VOLUME --
    public void SetMusicVolume(float volume)
    {
        musicSource.volume = volume;
    }

    // -- SET SFX VOLUME --
    public void SetSFXVolume(float volume)
    {
        sfxSource.volume = volume;
    }

}