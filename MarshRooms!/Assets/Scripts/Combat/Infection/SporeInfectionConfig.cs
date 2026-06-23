using UnityEngine;

[CreateAssetMenu(fileName = "SporeInfectionConfig", menuName = "Spore/SporeInfectionConfig")]
public class SporeInfectionConfig : ScriptableObject
{
    [Header("Duration")]
    public float duration = 2.5f;

    [Header("Slows")]
    public float speedMultiplier = 0.7f;
    public float bulletSpeedMultiplier = 0.5f;

    [Header("Tick Damage")]
    public float tickDamage = 2f;
    public float tickInterval = 0.5f;

    [Header("Visuals")]
    public Color infectedTint = new Color(0.4f, 1f, 0.4f);
}