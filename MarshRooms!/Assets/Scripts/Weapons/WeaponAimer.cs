// This script rotates the weapon

using UnityEngine;

public class WeaponAimer : MonoBehaviour
{
    [SerializeField] private PlayerAim aim;
    [SerializeField] private Transform weaponPivot;

    [SerializeField] private float rotationSpeed;

    private void Update()
    {
        Vector2 dir = aim.AimDirection;

        if (dir.sqrMagnitude < 0.001f)
            return;

        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

        Quaternion target = Quaternion.Euler(0, 0, angle);

        weaponPivot.rotation = Quaternion.Lerp(
            weaponPivot.rotation,
            target,
            1f - Mathf.Exp(-rotationSpeed * Time.deltaTime)
        );
 
        // Debug.DrawRay(weaponPivot.position, dir * 2f, Color.green);
    }
}