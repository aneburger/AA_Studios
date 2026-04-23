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

        private Animator anim;
        private Vector2 lastDirection = Vector2.down;
        private bool isDodging;
        private float dodgeCooldownTimer;

        // -- AWAKE --
        protected override void Awake()
        {
            base.Awake();
            anim = GetComponent<Animator>();
        }

        // -- UPDATE -- 
        private void Update()
        {
            if (!isDodging)
            {
                bool isMoving = moveInput.sqrMagnitude > 0.01f;
                if (isMoving) lastDirection = moveInput;

                anim.SetBool("isWalking", isMoving);

                Vector2 animDir = isMoving ? moveInput : lastDirection;
                anim.SetFloat("x", animDir.x);
                anim.SetFloat("y", animDir.y);
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
        private void OnMove(InputValue value)
        {
            moveInput = value.Get<Vector2>();
        }

        // -- DODGE INPUT --
        private void OnDodge()
        {
            if (!isDodging && dodgeCooldownTimer <= 0f)
                StartCoroutine(Dodge());
        }

        // -- DODGE --
        private IEnumerator Dodge()
        {
            isDodging = true;

            anim.SetFloat("x", lastDirection.x);
            anim.SetFloat("y", lastDirection.y);
            anim.SetTrigger("Dodge");

            body.AddForce(lastDirection * dodgeForce, ForceMode2D.Impulse);
            yield return new WaitForSeconds(dodgeDuration);

            isDodging = false;
            dodgeCooldownTimer = dodgeCooldown;
        }
    }   
}