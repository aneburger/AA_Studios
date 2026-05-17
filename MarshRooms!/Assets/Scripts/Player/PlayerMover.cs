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
        [SerializeField] private float dodgeVolume = 1f;
        [SerializeField] private AudioClip runningClip;
        [SerializeField] private float runningVolume = 1f;

        private Animator anim;
        private PlayerHealth playerHealth;
        private PlayerWeaponSlot weaponSlot;
        private AudioSource runningAudioSource;

        private Vector2 lastDirection = Vector2.down;
        private Vector2 lastDustPosition;

        private float dodgeCooldownTimer;
        private bool isDodging;
        public bool IsDodging => isDodging;

        private bool isRunningAudioPlaying;

        // -- AWAKE --
        protected override void Awake()
        {
            base.Awake();

            // Get References
            anim = GetComponentInChildren<Animator>();
            playerHealth = GetComponent<PlayerHealth>();
            weaponSlot = GetComponent<PlayerWeaponSlot>();

            // Create audio source for running sound
            runningAudioSource = gameObject.AddComponent<AudioSource>();
            runningAudioSource.clip = runningClip;
            runningAudioSource.volume = runningVolume;
            runningAudioSource.pitch = 2.0f;
            runningAudioSource.loop = true;
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

                    if (!isRunningAudioPlaying)
                    {
                        runningAudioSource.Play();
                        isRunningAudioPlaying = true;
                    }
                }
                else
                {
                    lastDustPosition = transform.position;

                    if (isRunningAudioPlaying)
                    {
                        runningAudioSource.Stop();
                        isRunningAudioPlaying = false;
                    }
                }

                anim.SetBool("isWalking", isMoving);

                // Face aim direction if armed, otherise face movement direction
                Vector2 facingDir = shooter.IsArmed ? aim.AimDirection : lastDirection;
                base.UpdateFacing(facingDir);
            }

            if (dodgeCooldownTimer > 0f)
                dodgeCooldownTimer -= Time.deltaTime;
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

            float scroll = context.ReadValue<float>();
            if (scroll > 0f) weaponSlot.ScrollUp();
            else if (scroll < 0f) weaponSlot.ScrollDown();
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
            anim.SetTrigger("Dodge");

            // Stop running audio when dodging
            if (isRunningAudioPlaying)
            {
                runningAudioSource.Stop();
                isRunningAudioPlaying = false;
            }

            // Determine dodge direction
            Vector2 dodgeDirection;
            if (moveInput.sqrMagnitude > 0.01f)
                dodgeDirection = moveInput.normalized;
            else if (shooter.IsArmed)
                dodgeDirection = aim.AimDirection.normalized;
            else
                dodgeDirection = lastDirection.normalized;

            AudioManager.Instance.PlaySFX(dodgeClip, dodgeVolume);

            playerHealth.SetInvincible(true);
            body.AddForce(dodgeDirection * dodgeForce, ForceMode2D.Impulse);
            yield return new WaitForSeconds(dodgeDuration * (5f / 6f)); // Invinsible for the first 4 frames

            playerHealth.SetInvincible(false);
            yield return new WaitForSeconds(dodgeDuration * (1f / 6f)); // Vulnerable for the last 2 frames

            // Spawn dust cloud after landing dodge
            VFXManager.Instance.SpawnDodgeDust(transform.position);

            // Stop dodging
            isDodging = false;
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