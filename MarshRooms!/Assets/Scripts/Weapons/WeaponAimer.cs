// This script rotates the weapon

using UnityEngine;

public class WeaponAimer : MonoBehaviour
{
    [SerializeField] private PlayerAim aim;
    [SerializeField] private Transform weaponPivot;
    [SerializeField] private SpriteRenderer weaponRenderer;
    [SerializeField] private SpriteRenderer playerRenderer;

    [SerializeField] private float rotationSpeed;

    private void Update()
    {
        Vector2 dir = aim.AimDirection;

        if (dir.sqrMagnitude < 0.001f)
            return;

        // -- ROTATION --
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

        Quaternion target = Quaternion.Euler(0, 0, angle);

        weaponPivot.rotation = Quaternion.Lerp(
            weaponPivot.rotation,
            target,
            1f - Mathf.Exp(-rotationSpeed * Time.deltaTime)
        );

        // -- FLIP WEAPON --
        weaponRenderer.flipY = dir.x < 0f;

        // --- WEAPON FRONT / BACK LOGIC ---
        bool isBack = false;

        float enterBackMin = 50f;
        float enterBackMax = 130f;
        
        float exitBackMin = 40f;
        float exitBackMax = 140f;

        // Weapon is at the back between 45 to 135 degrees
        if (!isBack)
        {
            if (angle >= enterBackMin && angle <= enterBackMax)
            {
                isBack = true;
            }
        }
        else
        {
            if (angle < exitBackMin || angle > exitBackMax)
            {
                isBack = false;
            }
        }

        int baseOrder = playerRenderer.sortingOrder;

        if (isBack)
        {
            weaponRenderer.sortingOrder = baseOrder - 10;
        }
        else
        {
            weaponRenderer.sortingOrder = baseOrder + 10;
        }

        // -- DEBUG --
        Debug.DrawRay(weaponPivot.position, dir * 2f, Color.green);
    }
}