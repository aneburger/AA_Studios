using UnityEngine;
public sealed class MusicPlayer : MonoBehaviour
{
    [SerializeField] private AudioClip musicClip;
    [SerializeField, Range(0f, 1f)] private float volume = 0.35f;
    [SerializeField] private bool loop = true;
    [SerializeField] private bool persistBetweenScenes = true;

    private AudioSource _audioSource;

    private void Awake()
    {
        if (persistBetweenScenes)
        {
            MusicPlayer[] players = FindObjectsOfType<MusicPlayer>();
            if (players.Length > 1)
            {
                Destroy(gameObject);
                return;
            }

            DontDestroyOnLoad(gameObject);
        }

        _audioSource = GetComponent<AudioSource>();
        if (_audioSource == null)
        {
            _audioSource = gameObject.AddComponent<AudioSource>();
        }

        _audioSource.playOnAwake = false;
        _audioSource.loop = loop;
        _audioSource.volume = volume;
        _audioSource.clip = musicClip;
    }

    private void Start()
    {
        if (_audioSource.clip != null)
        {
            _audioSource.Play();
        }
        else
        {
            Debug.LogWarning($"{nameof(MusicPlayer)} has no musicClip assigned.", this);
        }
    }
}