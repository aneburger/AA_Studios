// Describes a shape of bullets around the boss

using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "KnifePattern", menuName = "Boss/Knife Pattern")]
public class KnifePatternData : ScriptableObject
{
    public enum Shape { Circle = 0, Triangle = 2, Square = 3 }

    public struct PatternPoint
    {
        public Vector2 offset;
        public Vector2 direction;
        public float speedFactor;
    }

    [Header("Shape")]
    public Shape shape = Shape.Circle;
    public int knifeCount = 16;
    public float radius = 0.7f;
    public bool alignToPlayer = true;
    public float rotationOffset = 0f;
    public bool randomRotation = false;

    [Header("Motion")]
    public float speedMultiplier = 1f;
    public float hangTime = 0.5f;
    public float launchStagger = 0f;

    [Header("Tracking")]
    public bool trackWhileHanging = true;
    public float trackTurnRate = 180f;
    public float trackLockTime = 0.15f;

    // -- BUILD POINTS --
    public void BuildPoints(Vector2 aimDirection, float countMultiplier, List<PatternPoint> result)
    {
        result.Clear();

        float aimAngle = aimDirection.sqrMagnitude > 0.0001f
            ? Mathf.Atan2(aimDirection.y, aimDirection.x) * Mathf.Rad2Deg
            : 0f;

        float baseAngle = (alignToPlayer ? aimAngle : 0f) + rotationOffset;
        if (randomRotation) baseAngle += Random.Range(0f, 360f);

        int count = Mathf.Max(1, Mathf.RoundToInt(knifeCount * countMultiplier));

        switch (shape)
        {   
            // Circle
            case Shape.Circle:
                for (int i = 0; i < count; i++)
                    AddRadial(result, baseAngle + 360f * i / count);
                break;

            // Triangle
            case Shape.Triangle:
                AddPolygon(result, 3, count, baseAngle);
                break;

            // Square
            case Shape.Square:
                AddPolygon(result, 4, count, baseAngle);
                break;
        }
    }

    // -- ADD RADIAL --
    private void AddRadial(List<PatternPoint> result, float angleDegrees)
    {
        float rad = angleDegrees * Mathf.Deg2Rad;
        Vector2 dir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));

        result.Add(new PatternPoint
        {
            offset = dir * radius,
            direction = dir,
            speedFactor = 1f
        });
    }

    // -- ADD POLYGON --
    private void AddPolygon(List<PatternPoint> result, int sides, int requested, float baseAngle)
    {
        int count = Mathf.Max(sides, Mathf.RoundToInt(requested / (float)sides) * sides);

        Vector2[] corners = new Vector2[sides];
        for (int k = 0; k < sides; k++)
        {
            float rad = (baseAngle + 360f * k / sides) * Mathf.Deg2Rad;
            corners[k] = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * radius;
        }

        for (int i = 0; i < count; i++)
        {
            float along = (float)i * sides / count;
            int edge = Mathf.FloorToInt(along);
            float f = along - edge;

            Vector2 pos = Vector2.Lerp(corners[edge % sides], corners[(edge + 1) % sides], f);
            Vector2 dir = pos.sqrMagnitude > 0.0001f ? pos.normalized : Vector2.right;

            result.Add(new PatternPoint
            {
                offset = pos,
                direction = dir,
                speedFactor = radius > 0f ? pos.magnitude / radius : 1f
            });
        }
    }
}