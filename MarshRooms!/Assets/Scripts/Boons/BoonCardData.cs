using UnityEngine;

public enum BoonRarity { Normal, Rare, Epic }
public enum BoonCategory { Health, Mutation, Damage }

[CreateAssetMenu(fileName = "BoonCardData", menuName = "Boons/BoonCardData")]
public class BoonCardData : ScriptableObject
{
    [Header("Identity")]
    public string boonId;
    public string displayName;
    [TextArea] public string description;

    [Header("Visuals")]
    public Sprite icon;

    [Header("Classification")]
    public BoonRarity rarity;
    public BoonCategory category;

    [Header("Caps")]
    public int maxCopies = -1;
}