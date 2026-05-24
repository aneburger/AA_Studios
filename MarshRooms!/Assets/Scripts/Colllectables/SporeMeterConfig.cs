using UnityEngine;

[CreateAssetMenu(fileName = "SporeMeterConfig", menuName = "Spore/Spore Meter Config")]
public class SporeMeterConfig : ScriptableObject
{
    [Header("Spore Requirements")]
    [SerializeField] public int sporeCapacity = 10;

    [Header("Visual Settings")]
    [SerializeField] public Color fullColor = Color.green;
    [SerializeField] public Color halfColor = Color.yellow;
    [SerializeField] public Color lowColor = Color.red;

    [Header("Fill Settings")]
    [SerializeField] public float fillSmoothSpeed = 5f;
}