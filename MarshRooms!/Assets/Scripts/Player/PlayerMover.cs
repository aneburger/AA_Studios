// Controls player movement + dodging

using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TopDown.Movement
{
    [RequireComponent(typeof(PlayerInput))]
    
    public class PlayerMover : BaseMover
    {
        [Header("References")]
        [SerializeField] private PlayerAimer aim;
        [SerializeField] private PlayerShooter shooter;

        [Header("Dodge Settings")]
        [SerializeField] private float dodgeForce;
        [SerializeField] private float dodgeDuration;
        [SerializeField] private float dodgeCooldown;

        [Header("VFX Settings")]
        [SerializeField] private float dustDistance;

        [Header("Audio")]
        [SerializeField] private AudioClip dodgeClip;
        [Range(0f, 1f)] public float dodgeVolume;
        [SerializeField] private AudioClip runningClip;
        [Range(0f, 1f)] public float runningVolume;

        [Header("Sleep Settings")]
        [SerializeField] private float sleepDelay = 30f
        ;
        private float inactivityTimer = 0f;
        private bool isSleeping = false;

        private Animator anim;
        private PlayerHealth playerHealth;
        private PlayerWeaponSlot weaponSlot;

        private Vector2 lastDirection = Vector2.down;
        private Vector2 lastDustPosition;

        private float dodgeCooldownTimer;
        private bool isDodging;
        public bool IsDodging => isDodging;

        private float scrollCooldown = 0.35f;
        private float scrollCooldownTimer;

        // -- AWAKE --
        protected override void Awake()
        {
            base.Awake();

            // Get References
            anim = GetComponentInChildren<Animator>();
            playerHealth = GetComponent<PlayerHealth>();
            weaponSlot = GetComponent<PlayerWeaponSlot>();
        }

        // -- UPDATE -- 
        private void Update()
        {
            if (!isDodging)
            {
                bool isMoving = moveInput.sqrMagnitude > 0.01f;
                if (isMoving)
                {
                    lastDirection = moveInput;

                    if (Vector2.Distance(transform.position, lastDustPosition) >= dustDistance)
                    {
                        VFXManager.Instance.SpawnWalkDust(transform.position);
                        lastDustPosition = transform.position;
                    }

                    // When moving starts
                    AudioManager.Instance.StartRunningSFX(runningClip, runningVolume, 2.0f);
                }
                else
                {
                    lastDustPosition = transform.position;

                    // When moving stops
                    AudioManager.Instance.StopRunningSFX();
                }

                anim.SetBool("isWalking", isMoving);

                // Face aim direction if armed, otherise face movement direction
                Vector2 facingDir = shooter.IsArmed ? aim.AimDirection : lastDirection;
                base.UpdateFacing(facingDir);
            }

            // -- CHECK INACTIVITY --
            bool isActive = moveInput.sqrMagnitude > 0.01f || isDodging || Mouse.current.leftButton.isPressed || Mouse.current.rightButton.isPressed;

            if (isActive)
            {
                inactivityTimer = 0f;
                if (isSleeping)
                {
                    // Wake up
                    isSleeping = false;
                    anim.SetBool("isSleeping", false);
                    shooter.HideWeapon(false);
                }
            }
            else
            {
                inactivityTimer += Time.deltaTime;
                if (inactivityTimer >= sleepDelay && !isSleeping)
                {
                    // Sleep
                    isSleeping = true;
                    anim.SetBool("isSleeping", true);
                    shooter.HideWeapon(true);
                }
            }

            // Dodge Cooldown
            if (dodgeCooldownTimer > 0f)
                dodgeCooldownTimer -= Time.deltaTime;
            
            // Scroll Cooldown
            if (scrollCooldownTimer > 0f)
                scrollCooldownTimer -= Time.deltaTime;
        }

        // -- FIXED UPDATE -- 
        protected override void FixedUpdate()
        {
            if (!isDodging)
                base.FixedUpdate();
        }

        // -- MOVE INPUT --
        public void OnMove(InputAction.CallbackContext context)
        {
            moveInput = context.ReadValue<Vector2>();
        }

        // -- DODGE INPUT --
        public void OnDodge(InputAction.CallbackContext context)
        {
            if (!context.started) return;

            // Cannot Dodge when standing still
            if (!isDodging && dodgeCooldownTimer <= 0f && moveInput.sqrMagnitude > 0.01f)
                StartCoroutine(Dodge());
        }

        
        // -- SCROLL INPUT --
        public void OnScroll(InputAction.CallbackContext context)
        {

            if (!context.performed) return;
            if (scrollCooldownTimer > 0f) return;

            float scroll = context.ReadValue<float>();
            if (scroll > 0f) weaponSlot.ScrollUp();
            else if (scroll < 0f) weaponSlot.ScrollDown();

            scrollCooldownTimer = scrollCooldown;
        }

        // -- INTERACT INPUT --
        public void OnInteract(InputAction.CallbackContext context)
        {
            if (!context.started) return;
            weaponSlot.PickupWeapon();
        }

        // -- DODGE --
        private IEnumerator Dodge()
        {
            isDodging = true;
            shooter.HideWeapon(true);
            anim.SetTrigger("Dodge");
            AudioManager.Instance.PlaySFX(dodgeClip, dodgeVolume);

            // Stop running audio when dodging:
            AudioManager.Instance.StopRunningSFX();

            // Determine dodge direction
            Vector2 dodgeDirection;
            if (moveInput.sqrMagnitude > 0.01f)
                dodgeDirection = moveInput.normalized;
            else if (shooter.IsArmed)
                dodgeDirection = aim.AimDirection.normalized;
            else
                dodgeDirection = lastDirection.normalized;

            playerHealth.SetInvincible(true);
            body.AddForce(dodgeDirection * dodgeForce, ForceMode2D.Impulse);
            yield return new WaitForSeconds(dodgeDuration * (5f / 6f)); // Invinsible for the first 4 frames

            playerHealth.SetInvincible(false);
            yield return new WaitForSeconds(dodgeDuration * (1f / 6f)); // Vulnerable for the last 2 frames

            // Spawn dust cloud after landing dodge
            VFXManager.Instance.SpawnDodgeDust(transform.position);

            // Stop dodging
            isDodging = false;
            shooter.HideWeapon(false);
            dodgeCooldownTimer = dodgeCooldown;

            // Slow down speed slightly after dodging
            float originalSpeed = moveSpeed;

            moveSpeed *= 0.6f;
            SetAnimationSpeed(0.6f);

            yield return new WaitForSeconds(dodgeCooldown);

            moveSpeed = originalSpeed;
            SetAnimationSpeed(1f);
        }
    } 
}