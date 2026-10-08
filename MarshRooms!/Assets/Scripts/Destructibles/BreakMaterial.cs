using UnityEngine;

[CreateAssetMenu(menuName = "Destructibles/Break Material", fileName = "NewBreakMaterial")]
public class BreakMaterial : ScriptableObject
{
    [Header("Audio")]
    public AudioClip[] breakSounds;
    [Range(0f, 1f)] public float volume = 1f;
    [Range(0f, 0.5f)] public float pitchVariation = 0.1f;

    [Header("VFX")]
    public GameObject dustPrefab;

    [Header("Debris burst feel")]
    public float minSpeed = 1.5f;
    public float maxSpeed = 4f;
    public float minSettleTime = 0.25f;
    public float maxSettleTime = 0.5f;
    public float maxSpinDegPerSec = 540f;
}