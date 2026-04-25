// Controls player movement + dodging

using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TopDown.Movement
{
    [RequireComponent(typeof(PlayerInput))]
    
    public class PlayerMover : Mover //Inherits from the Mover class
    {
        [Header("Dodge Settings")]
        [SerializeField] private float dodgeForce;
        [SerializeField] private float dodgeDuration;
        [SerializeField] private float dodgeCooldown;

        [SerializeField] private DirectionalAnimator directionalAnimator;
        [SerializeField] private PlayerAim aim;
        [SerializeField] private Shooter shooter;

        private Animator anim;
        private Vector2 lastDirection = Vector2.down;

        private bool isDodging;
        private float dodgeCooldownTimer;

        // -- AWAKE --
        protected override void Awake()
        {
            base.Awake();

            anim = GetComponentInChildren<Animator>();
            directionalAnimator = GetComponentInChildren<DirectionalAnimator>();
        }

        // -- UPDATE -- 
        private void Update()
        {
            if (!isDodging)
            {
                bool isMoving = moveInput.sqrMagnitude > 0.01f;
                if (isMoving) lastDirection = moveInput;

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

            if (!isDodging && dodgeCooldownTimer <= 0f)
                StartCoroutine(Dodge());
        }

        // -- DODGE --
        private IEnumerator Dodge()
        {
            isDodging = true;

            anim.SetTrigger("Dodge");

            body.AddForce(lastDirection * dodgeForce, ForceMode2D.Impulse);
            yield return new WaitForSeconds(dodgeDuration);

            isDodging = false;
            dodgeCooldownTimer = dodgeCooldown;
        }
    }   
}