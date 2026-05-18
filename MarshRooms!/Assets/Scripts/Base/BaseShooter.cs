// Base class for all shooting logic
// Handles fire rate and bullet spawning

using UnityEngine;
using TopDown.Movement;
using System.Collections;

public abstract class BaseShooter : MonoBehaviour
{
    [Header("References")]
    [SerializeField] protected Transform firePoint; 
    [SerializeField] protected WeaponAimer weaponAimer;
    [SerializeField] protected BaseMover mover;
    [SerializeField] protected SpriteRenderer weaponSprite;

    [Header("Weapon")]
    [SerializeField] protected WeaponData currentWeapon;

    [Header("Audio")]
    [SerializeField] private AudioClip equipClip;
    [Range(0f, 1f)] public float equipVolume;

    public bool IsArmed => currentWeapon != null;
    protected float nextFireTime = 0f;

    private Coroutine squishCoroutine;
    private Vector3 defaultScale;
    private bool isFirstEquip = true;
    
     // -- AWAKE --
    protected virtual void Awake()
    {
        if (weaponSprite != null)
            defaultScale = weaponSprite.transform.localScale;
    }

    // -- EQUIP WEAPON --
    public void EquipWeapon(WeaponData weapon)
    {   
        // Set current weapon
        currentWeapon = weapon;
        nextFireTime = Time.time + 0.2f;
        UpdateWeaponVisuals();

        if (!isFirstEquip)
        {
            // Weapon equip effects
            if (squishCoroutine != null) StopCoroutine(squishCoroutine);
            squishCoroutine = StartCoroutine(SquishWeapon());
            weaponAimer?.ApplyRecoil(currentWeapon.recoilAmount, currentWeapon.recoilDecay);
            AudioManager.Instance.PlaySFX(equipClip, equipVolume);
        }

        isFirstEquip = false;
    }

    // -- GET SHOOT DIRECTION (implemented by subclasses) --
    protected abstract Vector2 GetShootDirection();

    // -- SHOOT --
    public void Shoot()
    {
        if (currentWeapon == null) return;

        Vector2 direction = GetShootDirection();

        GameObject bullet = Instantiate(currentWeapon.bulletPrefab, firePoint.position, Quaternion.identity);
        Bullet b = bullet.GetComponent<Bullet>();
        
        b.SetDirection(direction);

        // Pass weapon data to bullet
        b.damage = currentWeapon.damage;
        b.speed = currentWeapon.bulletSpeed;
        b.knockback = currentWeapon.hitKnockback;
        b.hitVFX = currentWeapon.hitPrefab;
        b.weaponSortingOrder = weaponSprite.sortingOrder;
        OnShootEffects(direction);
    }

    // -- SHOOT EFFECTS --
    protected virtual void OnShootEffects(Vector2 direction)
    {
        weaponAimer?.ApplyRecoil(currentWeapon.recoilAmount, currentWeapon.recoilDecay);
        mover?.ApplyKnockback(-direction * currentWeapon.knockbackForce);

        // Play weapon sound
        AudioManager.Instance.PlaySFXWithPitch(currentWeapon.shootClip, currentWeapon.shootVolume, 0.1f);

        // Spawn muzzle flash VFX
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        VFXManager.Instance.SpawnMuzzleFlash(currentWeapon.muzzleFlashPrefab, firePoint.position, angle, weaponSprite.sortingOrder);

        // Squish weapon sprite
        if (squishCoroutine != null) StopCoroutine(squishCoroutine);
        squishCoroutine = StartCoroutine(SquishWeapon());
    }

    // -- SHOW / HIDE WEAPON --
    protected void UpdateWeaponVisuals()
    {
        if (weaponSprite != null)
        {
            weaponSprite.enabled = IsArmed;
            if (IsArmed) weaponSprite.sprite = currentWeapon.sprite;
        }

        if (weaponAimer != null) weaponAimer.enabled = IsArmed;
        
    }

    // -- SQUISH --
    private IEnumerator SquishWeapon()
    {
        if (weaponSprite == null) yield break;

        Transform weaponTransform = weaponSprite.transform;
        weaponTransform.localScale = new Vector3(defaultScale.x * 0.7f, defaultScale.y * 1.2f, 1f);
        yield return new WaitForSeconds(0.05f);
        weaponTransform.localScale = defaultScale;
    }
}
