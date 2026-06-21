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
    [SerializeField] public WeaponData currentWeapon;

    [Header("Audio")]
    [SerializeField] private AudioClip equipClip;
    [Range(0f, 1f)] public float equipVolume;
    [SerializeField] private AudioClip emptyClip; 
    [Range(0f, 1f)] public float emptyVolume;

    public bool IsArmed => currentWeapon != null;
    protected float nextFireTime = 0f;

    private Coroutine squishCoroutine;
    private Vector3 defaultScale;
    
    private bool isFirstEquip = true;
    private bool weaponHidden = false;

    // -1 means infinite ammo
    private int currentAmmo = -1;

     // -- AWAKE --
    protected virtual void Awake()
    {
        if (weaponSprite != null)
            defaultScale = weaponSprite.transform.localScale;
    }

    // -- GET FIRE POSITION --
    protected Vector3 GetFirePosition()
    {
        if (firePoint == null) return transform.position;
        if (currentWeapon == null) return firePoint.position;

        return firePoint.position + firePoint.TransformDirection(currentWeapon.firePointOffset);
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
        
        if (!UseAmmo())
        {
            OnEmptyShootEffetcs();
            return;
        }

        Vector2 direction = GetShootDirection();
        Vector3 spawnPosition = GetFirePosition();

        GameObject bullet = Instantiate(currentWeapon.bulletPrefab, spawnPosition, Quaternion.identity);
        BaseBullet b = bullet.GetComponent<BaseBullet>();
        b.SetDirection(direction);
        b.SetAimOrigin(firePoint.position);
        b.SetBullet(currentWeapon.bulletSpeed, currentWeapon.damage, currentWeapon.hitKnockback, currentWeapon.hitPrefab, weaponSprite.sortingOrder);
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
        VFXManager.Instance.SpawnMuzzleFlash(currentWeapon.muzzleFlashPrefab, GetFirePosition(), angle, weaponSprite.sortingOrder);

        // Squish weapon sprite
        if (squishCoroutine != null) StopCoroutine(squishCoroutine);
            squishCoroutine = StartCoroutine(SquishWeapon());
    }

    // -- EMPTY SHOOT EFFECTS --
    protected virtual void OnEmptyShootEffetcs()
    {
        // Half strength recoil
        weaponAimer?.ApplyRecoil(currentWeapon.recoilAmount * 0.5f, currentWeapon.recoilDecay);

        // Empty click sound
        AudioManager.Instance.PlaySFX(emptyClip, emptyVolume);
    }

    // -- SHOW / HIDE WEAPON --
    protected void UpdateWeaponVisuals()
    {
        if (weaponSprite != null)
        {
            weaponSprite.enabled = IsArmed && !weaponHidden;
            if (IsArmed) weaponSprite.sprite = currentWeapon.sprite;
        }

        if (weaponAimer != null) 
            weaponAimer.enabled = IsArmed && !weaponHidden;
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

    // -- SET AMMO --
    public void SetAmmo(int ammo)
    {
        currentAmmo = ammo;
    }

    // -- GET AMMO --
    public int GetAmmo() => currentAmmo;

    // -- USE AMMO --
    public bool UseAmmo()
    {   
        // return true if there is ammo left, and false if not
        if (currentAmmo == -1) return true; // Check for infinite ammo
        if (currentAmmo <= 0) return false;

        currentAmmo--;
        return true;
    }

    // -- HIDE WEAPON --
    public void HideWeapon(bool hidden)
    {
        weaponHidden = hidden;
        
        if (weaponSprite != null)
            weaponSprite.enabled = !hidden && IsArmed;
        
        if (weaponAimer != null)
            weaponAimer.enabled = !hidden && IsArmed;
    }

     // -- GIZMOS --
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(GetFirePosition(), 0.05f);
    }
}
