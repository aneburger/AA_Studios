// Handles SFX and Music
// Access via AudioManager.Instance

using UnityEngine;
using System.Collections;
using UnityEngine.Audio;

public sealed class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Sources")]
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioSource musicSource;
    [SerializeField] public AudioClip musicClip; // TEMPORARY - this should not be public I am just lazy :) Ill fix later

    [Header("Mixer")]
    [SerializeField] private AudioMixer audioMixer;

    [Header("Footstep Sounds")]
    [SerializeField] private AudioClip[] footstepClips;
    [SerializeField] private float footstepVolume = 0.5f;

    private AudioSource runningSFX;

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
    }

    // -- PLAY SFX --
    public void PlaySFX(AudioClip clip, float volume = 1f)
    {
        if (clip == null) return;
        sfxSource.PlayOneShot(clip, volume);
    }

    // -- PLAY MUSIC --
    public void PlayMusic(AudioClip clip, float volume = 0.5f)
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

    // -- PLAY FOOTSTEP --
    public void PlayFootstep()
    {
        if (footstepClips == null || footstepClips.Length == 0) return;
        
        AudioClip clip = footstepClips[Random.Range(0, footstepClips.Length)];
        PlaySFXWithPitch(clip, footstepVolume, 0.1f);
    }

    // -- SET RUNNING SFX PITCH --
    public void SetRunningSFXPitch(float pitch)
    {
        if (runningSFX != null)
            runningSFX.pitch = pitch;
    }

    // -- DAMPEN AUDIO --
    public void DampenAudio(float duration)
    {
        StartCoroutine(DampenCoroutine(duration));
    }

    // -- DAMPEN COROUTINE --
    private IEnumerator DampenCoroutine(float duration)
    {
        audioMixer.SetFloat("MasterCutoff", 800f);
        yield return new WaitForSeconds(duration);
        audioMixer.SetFloat("MasterCutoff", 22000f);
    }

    // -- FADE IN --
    public void FadeInMusic(AudioClip clip, float duration, float targetVolume = 0.5f)
    {
        StartCoroutine(FadeInCoroutine(clip, duration, targetVolume));
    }

    // -- FADE OUT --
    public void FadeOutMusic(float duration)
    {
        StartCoroutine(FadeOutCoroutine(duration));
    }

    // -- FADE IN COROUTINE --
    private IEnumerator FadeInCoroutine(AudioClip clip, float duration, float targetVolume)
    {
        musicSource.clip = clip;
        musicSource.volume = 0f;
        musicSource.loop = true;
        musicSource.Play();

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            musicSource.volume = Mathf.Lerp(0f, targetVolume, elapsed / duration);
            yield return null;
        }
        musicSource.volume = targetVolume;
    }

    // -- FADE OUT COROUTINE --
    private IEnumerator FadeOutCoroutine(float duration)
    {
        float startVolume = musicSource.volume;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            musicSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / duration);
            yield return null;
        }
        musicSource.Stop();
    }

}