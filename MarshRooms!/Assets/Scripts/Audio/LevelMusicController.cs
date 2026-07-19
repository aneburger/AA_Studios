using UnityEngine;

public class LevelMusicController : MonoBehaviour
{
    [Header("Music")]
    [SerializeField] private AudioClip floorMusic;
    [SerializeField] private float combatVolume = 0.5f;
    [SerializeField] private float calmVolume = 0.2f;
    [SerializeField] private float fadeDuration = 1f;

    private void Start()
    {
        AudioManager.Instance.CrossfadeMusic(floorMusic, fadeDuration, combatVolume);
    }

    private void OnEnable()
    {
        RoomManager.OnRoomCleared += OnWaveCleared;
    }

    private void OnDisable()
    {
        RoomManager.OnRoomCleared -= OnWaveCleared;
    }

    private void OnWaveCleared()
    {
        AudioManager.Instance.FadeMusicVolume(calmVolume, fadeDuration);
    }
}