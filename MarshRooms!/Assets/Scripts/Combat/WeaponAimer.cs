// Rotates the weapon pivot to face aim direction
// Handles weapon flip and front/back sorting order

using UnityEngine;

public class WeaponAimer : MonoBehaviour
{
    [SerializeField] private Transform weaponPivot;
    [SerializeField] private SpriteRenderer weaponRenderer;
    [SerializeField] private SpriteRenderer characterRenderer;
    [SerializeField] private float rotationSpeed;

    private float recoilAmount;
    private float recoilDecay;
    private float currentRecoil;

    private Vector2 aimDirection;
    
    public Vector2 AimDirection => aimDirection;

    // -- SET DIRECTION -- (call from PlayerAim or Enemy AI)
    public void SetAimDirection(Vector2 direction)
    {
        aimDirection = direction;
    }

    // -- APPLY RECOIL -- (Called by Shooter)
    public void ApplyRecoil(float amount, float decay)
    {
        recoilAmount = amount;
        recoilDecay = decay;
        currentRecoil += recoilAmount;
    }

    // -- UPDATE --
    private void Update()
    {
        if (aimDirection.sqrMagnitude < 0.001f)
            return;

        // -- RECOIL --
        currentRecoil = Mathf.Lerp(currentRecoil, 0f, 1f - Mathf.Exp(-recoilDecay * Time.deltaTime));

        // -- ROTATION --
        float angle = Mathf.Atan2(aimDirection.y, aimDirection.x) * Mathf.Rad2Deg;

        Quaternion target = Quaternion.Euler(0, 0, angle + currentRecoil);

        weaponPivot.rotation = Quaternion.Lerp(
            weaponPivot.rotation,
            target,
            1f - Mathf.Exp(-rotationSpeed * Time.deltaTime)
        );

        // -- FLIP WEAPON --
        weaponRenderer.flipY = aimDirection.x < 0f;

        // --- FRONT / BACK SORTING ---
        bool isBack = angle >= 50f && angle <= 130f;
        int baseOrder = characterRenderer.sortingOrder;
        weaponRenderer.sortingOrder = isBack ? baseOrder - 10 : baseOrder + 10;

        // -- DEBUG --
        Debug.DrawRay(weaponPivot.position, aimDirection * 2f, Color.green);
    }
}