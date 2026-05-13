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
        [SerializeField] private DirectionalAnimator directionalAnimator;

        [Header("Dodge Settings")]
        [SerializeField] private float dodgeForce;
        [SerializeField] private float dodgeDuration;
        [SerializeField] private float dodgeCooldown;

        [Header("VFX Settings")]
        [SerializeField] private float dustDistance;

        private Animator anim;
        private PlayerHealth playerHealth;

        private Vector2 lastDirection = Vector2.down;
        private Vector2 lastDustPosition;

        private bool isDodging;
        private float dodgeCooldownTimer;

        // -- GETTERS --
        public bool IsDodging
        {
            get { return isDodging; }
        }

        public DirectionalAnimator DirectionalAnimator
        {
            get { return directionalAnimator; }
        }

        // -- AWAKE --
        protected override void Awake()
        {
            base.Awake();

            // Get References
            anim = GetComponentInChildren<Animator>();
            directionalAnimator = GetComponentInChildren<DirectionalAnimator>();
            playerHealth = GetComponent<PlayerHealth>();
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
                directionalAnimator.SetDirection(facingDir);
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

        // -- DODGE --
        private IEnumerator Dodge()
        {
            isDodging = true;
            anim.SetTrigger("Dodge");

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
            dodgeCooldownTimer = dodgeCooldown;

            // Slow down speed slightly after dodging
            float originalSpeed = moveSpeed;

            moveSpeed *= 0.7f;
            directionalAnimator.SetAnimationSpeed(0.7f);

            yield return new WaitForSeconds(dodgeCooldown);

            moveSpeed = originalSpeed;
            directionalAnimator.SetAnimationSpeed(1f);
        }
    }   
}