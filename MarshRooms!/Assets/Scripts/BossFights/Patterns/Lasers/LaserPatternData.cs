// A fixed set of laser angles for however many statues are active.

using UnityEngine;

[CreateAssetMenu(fileName = "LaserPatternData", menuName = "Weapons/LaserPatternData")]
public class LaserPatternData : ScriptableObject
{
    public float[] anglesDegrees;
}