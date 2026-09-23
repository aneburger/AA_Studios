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
        [SerializeField] private PlayerSporeActivator sporeActivator; 

        [Header("Dodge Settings")]
        [SerializeField] private float dodgeForce;
        [SerializeField] private float dodgeDuration;
        [SerializeField] private float dodgeCooldown;
        [SerializeField] private float stationaryDodgeForceMultiplier;

        [Header("VFX Settings")]
        [SerializeField] private float dustDistance;

        [Header("Audio")]
        [SerializeField] private AudioClip dodgeClip;
        [Range(0f, 1f)] public float dodgeVolume;
        [SerializeField] private AudioClip dodgeFallClip;
        [Range(0f, 1f)] public float dodgeFallVolume;

        [Header("Sleep Settings")]
        [SerializeField] private float sleepDelay = 30f;
        private float inactivityTimer = 0f;
        private bool isSleeping = false;

        private Animator anim;
        private PlayerHealth playerHealth;
        private PlayerWeaponSlot weaponSlot;

        private Vector2 lastDirection = Vector2.down;
        private Vector2 lastDustPosition;

        private float dodgeCooldownTimer;
        private bool isDodging;
    
        public bool canDodge = true;
        private bool canMove = true;

        public bool IsDodging => isDodging;
        public bool CanMove => canMove;

        // Reference-counted: each SetInputLocked(true) call must be matched by a SetInputLocked(false)
        // from the SAME caller. Whoever locks it last to unlock releases control - this stops one system
        // (e.g. the pause menu) from prematurely restoring input that another system (e.g. the boss room
        // intro) is still holding locked.
        private int inputLockCount = 0;
        private bool IsInputLocked => inputLockCount > 0;
        private bool preLockCanMove;
        private bool preLockCanDodge;
        private bool preLockCanShoot;
        private bool preLockCanActivateSpore;
        private bool preLockCanSwitchWeapons;
        
        private float scrollCooldown = 0.35f;
        private float scrollCooldownTimer;

        private PlayerInput playerInput;

        // -- AWAKE --
        protected override void Awake()
        {
            base.Awake();

            // Get References
            anim = GetComponentInChildren<Animator>();
            playerHealth = GetComponent<PlayerHealth>();
            weaponSlot = GetComponent<PlayerWeaponSlot>();
            playerInput = GetComponent<PlayerInput>();

            if (sporeActivator == null)
                sporeActivator = GetComponent<PlayerSporeActivator>();
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
                }
                else
                {
                    lastDustPosition = transform.position;
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

        // -- FORCE IDLE ANIMATION --
        public void ForceIdleAnimation()
        {
            anim.SetBool("isWalking", false);
            anim.Play("IdleBlend", 0, 0f);
        }

        // -- PLAY LOOK UP ANIMATION --
        public void PlayLookUpTrigger()
        {
            anim.SetTrigger("LookUp");
        }

        // STOP LOOK UP ANIMATION
        public void EndLookUpHold()
        {
            anim.SetTrigger("LookUpEnd");
        }

        // -- SET CAN MOVE --
        public void SetCanMove(bool value)
        {
            canMove = value;
            if (!canMove) moveInput = Vector2.zero;
        }

        // -- SET ALL INPUT LOCKED --
        public void SetInputLocked(bool locked)
        {
            if (locked)
            {
                inputLockCount++;
                if (inputLockCount > 1) return;

                preLockCanMove = canMove;
                preLockCanDodge = canDodge;
                preLockCanShoot = shooter == null || shooter.CanShoot;
                preLockCanActivateSpore = sporeActivator == null || sporeActivator.CanActivate;
                preLockCanSwitchWeapons = weaponSlot == null || weaponSlot.CanSwitch;

                SetCanMove(false);
                canDodge = false;
                shooter?.SetCanShoot(false);
                sporeActivator?.SetCanActivate(false);
                weaponSlot?.SetCanSwitch(false);
            }
            else
            {
                if (inputLockCount <= 0) return;
                inputLockCount--;
                if (inputLockCount > 0) return;

                if (canMove) moveInput = ReadHeldMoveInput();

                SetCanMove(preLockCanMove);
                canDodge = preLockCanDodge;
                shooter?.SetCanShoot(preLockCanShoot);
                sporeActivator?.SetCanActivate(preLockCanActivateSpore);
                weaponSlot?.SetCanSwitch(preLockCanSwitchWeapons);
            }
        }

        // -- READ HELD INPUT --
        private Vector2 ReadHeldMoveInput()
        {
            InputAction move = (playerInput != null && playerInput.actions != null)
                ? playerInput.actions.FindAction("Move") : null;
            return move != null ? move.ReadValue<Vector2>() : Vector2.zero;
        }

        // -- MOVE INPUT --
        public void OnMove(InputAction.CallbackContext context)
        {   
            if (!canMove) return;
            moveInput = context.ReadValue<Vector2>();
        }

        // -- DODGE INPUT --
        public void OnDodge(InputAction.CallbackContext context)
        {
            if(!canDodge) return;
            if (!context.started) return;
            if (DialogueManager.Instance != null && DialogueManager.Instance.IsRunning) return;

            if (!isDodging && dodgeCooldownTimer <= 0f)
            {
                StartCoroutine(Dodge());
                TutorialDirector.Instance?.OnPlayerDodged();
            }
        }

        // -- SCROLL INPUT --
        public void OnScroll(InputAction.CallbackContext context)
        {
            if (!context.performed) return;
            if (DialogueManager.Instance != null && DialogueManager.Instance.IsRunning) return;
            if (scrollCooldownTimer > 0f) return;

            float scroll = context.ReadValue<float>();
            if (scroll > 0f) weaponSlot.ScrollUp();
            else if (scroll < 0f) weaponSlot.ScrollDown();

            scrollCooldownTimer = scrollCooldown;
        }

        // -- INTERACT INPUT --
        public void OnInteract(InputAction.CallbackContext context){}

        // -- WEAPON SLOT INPUT --
        public void OnWeaponSlot1(InputAction.CallbackContext context)
        {
            if (!context.started) return;
            weaponSlot.EquipSlot(0);
        }

        public void OnWeaponSlot2(InputAction.CallbackContext context)
        {
            if (!context.started) return;
            weaponSlot.EquipSlot(1);
        }

        public void OnWeaponSlot3(InputAction.CallbackContext context)
        {
            if (!context.started) return;
            weaponSlot.EquipSlot(2);
        }

        // -- SET SLEEPING --
        public void SetSleeping(bool sleeping)
        {
            isSleeping = sleeping;
            anim.SetBool("isSleeping", sleeping);
            shooter.HideWeapon(sleeping);
            if (sleeping) inactivityTimer = sleepDelay;
        }

        // -- DODGE --
        private IEnumerator Dodge()
        {
            isDodging = true;
            shooter.HideWeapon(true);
            anim.SetTrigger("Dodge");
            AudioManager.Instance.PlaySFX(dodgeClip, dodgeVolume);

            // Determine dodge direction
            Vector2 dodgeDirection;
            bool wasStandingStill = moveInput.sqrMagnitude <= 0.01f;

            if (moveInput.sqrMagnitude > 0.01f)
                dodgeDirection = moveInput.normalized;
            else if (shooter.IsArmed)
                dodgeDirection = aim.AimDirection.normalized;
            else
                dodgeDirection = lastDirection.normalized;

            float appliedForce = wasStandingStill
                ? dodgeForce * stationaryDodgeForceMultiplier
                : dodgeForce;

            playerHealth.SetInvincible(true);
            body.AddForce(dodgeDirection * appliedForce, ForceMode2D.Impulse);
            yield return new WaitForSeconds(dodgeDuration * (5f / 6f)); // Invinsible for the first 5 frames

            AudioManager.Instance.PlaySFXWithPitch(dodgeFallClip, dodgeFallVolume, 0.1f);

            playerHealth.SetInvincible(false);
            yield return new WaitForSeconds(dodgeDuration * (1f / 6f)); // Vulnerable for the last 1 frames

            // Spawn dust cloud after landing dodge
            VFXManager.Instance.SpawnDodgeDust(transform.position);

            // Stop dodging
            isDodging = false;
            shooter.HideWeapon(false);
            dodgeCooldownTimer = dodgeCooldown;

            // Slow down speed slightly after dodging
            float originalSpeed = moveSpeed;

            moveSpeed *= 0.4f;
            SetAnimationSpeed(0.6f); 

            yield return new WaitForSeconds(dodgeCooldown);

            moveSpeed = originalSpeed;
            SetAnimationSpeed(1f);
        }
    } 
}