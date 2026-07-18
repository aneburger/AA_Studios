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
    [SerializeField] private AudioClip pickupClip;
    [Range(0f, 1f)] public float pickupVolume;
    [SerializeField] private AudioClip emptyClip; 
    [Range(0f, 1f)] public float emptyVolume;

    public bool IsArmed => currentWeapon != null;
    protected float nextFireTime = 0f;

    private Coroutine squishCoroutine;
    private Vector3 defaultScale;
    
    private bool isFirstEquip = true;
    private bool weaponHidden = false;

    private int wallMask;

    private float bulletSpeedMultiplier = 1f;
    protected float shakeMultiplier = 1f;

    // Fire Rate Multipiers
    private float permanentFireRateMultiplier = 1f;
    private float temporaryFireRateMultiplier = 1f;

    // Damage Multiplier
    private float permanentDamageMultiplier = 1f;
    private float temporaryDamageMultiplier = 1f;
    private float critChance = 0f;

    private int bulletCountOverride = -1;
    private int permanentBulletCountBonus = 0;

    private float spreadAngleOverride = -1f;

    private Collider2D playerCollider;

    // -1 means infinite ammo
    private int currentAmmo = -1;

     // -- AWAKE --
    protected virtual void Awake()
    {
        if (weaponSprite != null)
            defaultScale = weaponSprite.transform.localScale;

        wallMask = LayerMask.GetMask("Walls");
        playerCollider = GetComponent<Collider2D>();
    }

    // -- BULLET SPEED MULTIPLIER --
    public void SetBulletSpeedMultiplier(float multiplier)
    {
        bulletSpeedMultiplier = multiplier;
    }

    // -- SHAKE MULTIPLIER --
    public void SetShakeMultiplier(float multiplier)
    {
        shakeMultiplier = multiplier;
    }

    // -- SET FIRE RATE MULTIPLIER --
    public void SetFireRateMultiplier(float multiplier)
    {
        temporaryFireRateMultiplier = multiplier;
    }

    // -- SET PERMANENT FIRE RATE MULTIPLIER --
    public void SetPermanentFireRateMultiplier(float multiplier)
    {
        permanentFireRateMultiplier = multiplier;
    }

    // -- GET COMBINED FIRE RATE MULTIPLIER --
    public float GetFireRateMultiplier()
    {
        return permanentFireRateMultiplier * temporaryFireRateMultiplier;
    }

    // -- SET DAMAGE MULTIPLIER --
    public void SetDamageMultiplier(float multiplier)
    {
        temporaryDamageMultiplier = multiplier;
    }

    // -- SET PERMANENT DAMAGE MULTIPLIER --
    public void SetPermanentDamageMultiplier(float multiplier)
    {
        permanentDamageMultiplier = multiplier;
    }

    // -- GET COMBINED DAMAGE MULTIPLIER --
    public float GetDamageMultiplier()
    {
        return permanentDamageMultiplier * temporaryDamageMultiplier;
    }

    // -- SET CRIT CHANCE (boons) --
    public void SetCritChance(float chance)
    {
        critChance = chance;
    }

    // -- SET PERMANENT BULLET COUNT BONUS --
    public void SetPermanentBulletCountBonus(int bonus)
    {
        permanentBulletCountBonus = bonus;
    }

    // -- GET FIRE POSITION --
    protected Vector3 GetFirePosition()
    {
        if (firePoint == null) return transform.position;
        if (currentWeapon == null) return firePoint.position;

        Vector2 offset = currentWeapon.firePointOffset;

        if (currentWeapon.flipOffsetOnAimFlip && weaponAimer != null && weaponAimer.AimDirection.x < 0f)
            offset.y = -offset.y;

        return firePoint.position + firePoint.TransformDirection(offset);
    }

    // -- EQUIP WEAPON --
    public void EquipWeapon(WeaponData weapon, bool isPickup = false, bool playSound = true)
    {
        currentWeapon = weapon;
        nextFireTime = Time.time + 0.2f;
        UpdateWeaponVisuals();

        if (isPickup)
        {
            if (playSound) AudioManager.Instance.PlaySFXWithPitch(pickupClip, pickupVolume);
        }
        else if (!isFirstEquip)
        {
            if (squishCoroutine != null) StopCoroutine(squishCoroutine);
            squishCoroutine = StartCoroutine(SquishWeapon());
            weaponAimer?.ApplyRecoil(currentWeapon.recoilAmount, currentWeapon.recoilDecay);
            AudioManager.Instance.PlaySFXWithPitch(equipClip, equipVolume);
        }

        isFirstEquip = false;
    }

    // -- GET SHOOT DIRECTION (implemented by subclasses) --
    protected abstract Vector2 GetShootDirection();

    // -- SHOOT --
    public BaseBullet Shoot()
    {
        if (currentWeapon == null) return null;

        if (!UseAmmo())
        {
            OnEmptyShootEffetcs();
            return null;
        }

        Vector2 direction = GetShootDirection();
        Vector3 spawnPosition = GetFirePosition();

        BaseBullet lastBullet = null;

        int count = (bulletCountOverride > 0 ? bulletCountOverride : currentWeapon.bulletCount) + permanentBulletCountBonus;
        float spread = spreadAngleOverride >= 0 ? spreadAngleOverride : currentWeapon.spreadAngle;
        float startAngle = -(spread * (count - 1) / 2f);

        Vector2 origin = playerCollider.bounds.center;
        Vector2 colliderExtents = playerCollider.bounds.extents;
        float totalDistance = Vector2.Distance(origin, spawnPosition);

        for (int i = 0; i < count; i++)
        {
            float angle = startAngle + spread * i;
            Vector2 spreadDirection = RotateVector(direction, angle);

            float tx = Mathf.Abs(spreadDirection.x) > 0.0001f ? colliderExtents.x / Mathf.Abs(spreadDirection.x) : float.MaxValue;
            float ty = Mathf.Abs(spreadDirection.y) > 0.0001f ? colliderExtents.y / Mathf.Abs(spreadDirection.y) : float.MaxValue;
            float edgeDistance = Mathf.Min(tx, ty);

            Vector2 castOrigin = origin + spreadDirection * edgeDistance;
            float remainingDistance = Mathf.Max(0f, totalDistance - edgeDistance);

            RaycastHit2D wallHit = Physics2D.CircleCast(castOrigin, 0.05f, spreadDirection, remainingDistance, wallMask);

            if (wallHit.collider != null)
                continue;

            if (Physics2D.OverlapPoint(spawnPosition, wallMask) != null)
                continue;

            // Roll a random damage
            float rolledDamage = Random.Range(currentWeapon.minDamage, currentWeapon.maxDamage);
            rolledDamage *= GetDamageMultiplier();

            // Apply crit damage
            if (Random.value <= critChance)
                rolledDamage *= 2f;

            GameObject bullet = Instantiate(currentWeapon.bulletPrefab, spawnPosition, Quaternion.identity);
            BaseBullet b = bullet.GetComponent<BaseBullet>();
            b.SetDirection(spreadDirection);
            b.SetAimOrigin(firePoint.position);
            b.SetBullet(currentWeapon.bulletSpeed * bulletSpeedMultiplier, rolledDamage, currentWeapon.hitKnockback, currentWeapon.hitPrefab, weaponSprite.sortingOrder, currentWeapon.wallHitClip, currentWeapon.wallHitVolume);
            lastBullet = b;
        }

        OnShootEffects(direction);
        return lastBullet;
    }

    private Vector2 RotateVector(Vector2 v, float degrees)
    {
        float rad = degrees * Mathf.Deg2Rad;
        float cos = Mathf.Cos(rad);
        float sin = Mathf.Sin(rad);
        return new Vector2(v.x * cos - v.y * sin, v.x * sin + v.y * cos);
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

    // -- SQUISH EFFECT --
    public virtual void SquishEffect()
    {
        // Squish weapon sprite
        if (squishCoroutine != null) StopCoroutine(squishCoroutine);
            squishCoroutine = StartCoroutine(SquishWeapon());
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

    public void SetBulletOverrides(int count, float spread)
    {
        bulletCountOverride = count;
        spreadAngleOverride = spread;
    }

    public void ClearBulletOverrides()
    {
        bulletCountOverride = -1;
        spreadAngleOverride = -1f;
    }

    // -- GIZMOS --
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(GetFirePosition(), 0.05f);
    }
}