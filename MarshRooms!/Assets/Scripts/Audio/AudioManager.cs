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
    public void PlayMusic(AudioClip clip, float volume = 0.8f)
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

    // --------------------------- AUDIO EFFECTS ---------------------------------

    // -- PLAY SFX WITH PITCH VARIATION --
    public void PlaySFXWithPitch(AudioClip clip, float volume = 1f, float pitchVariation = 0.1f)
    {
        if (clip == null) return;

        GameObject tempAudio = new GameObject("TempAudio");
        AudioSource source = tempAudio.AddComponent<AudioSource>();

        source.clip = clip; 
        source.volume = volume;
        source.pitch = 1f + Random.Range(-pitchVariation, pitchVariation);
 
        source.Play();

        Destroy(tempAudio, clip.length); 
    }

    // -- PLAY LOOPING SFX --
    public void PlayLoopingSFX(ref AudioSource source, AudioClip clip, float volume = 1f, float pitch = 1f)
    {
        if (clip == null || source != null) return;

        GameObject tempAudio = new GameObject("LoopingSFX");
        AudioSource newSource = tempAudio.AddComponent<AudioSource>();
        newSource.clip = clip;
        newSource.volume = volume;
        newSource.pitch = pitch;
        newSource.loop = true;
        newSource.Play();

        source = newSource;
    }

    // -- STOP LOOPING SFX --
    public void StopLoopingSFX(ref AudioSource source)
    {
        if (source == null) return;
        Destroy(source.gameObject);
        source = null;
    }

}