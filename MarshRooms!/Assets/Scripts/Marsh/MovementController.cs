using System.Collections;
using UnityEngine;

public class MovementController : MonoBehaviour {
    [Header("References")]
    public Rigidbody2D body;
    public SpriteRenderer spriteRenderer;
    private Animator anim;

    [Header("Player Settings")]
    public float walkSpeed;
    public float dodgeForce;
    public float dodgeDuration;
    public float dodgeCooldown;

    private Vector2 lastDirection;  
    private Vector2 direction; 

    private bool isMoving = false;
    private bool isDodging = false;

    private float dodgeCooldownTimer;

    // ----------------------------------------------------------- START ----------------------------------------------------------- 
    void Start(){
        anim = GetComponent<Animator>();
        lastDirection = Vector2.down;
    }

    // ----------------------------------------------------------- UPDATE ----------------------------------------------------------- 
    void Update() 
    {
        // DODGE INPUT (I set it in Input Manager to spacebar so we can use keybinding later)
        if (Input.GetButtonDown("Dodge") && !isDodging && dodgeCooldownTimer <= 0f)
        {
            StartCoroutine(Dodge());
        }

        // Only allow movement when not dodging
        if (!isDodging)
        {
            // MOVEMENT INPUT
            direction = new Vector2(
                Input.GetAxisRaw("Horizontal"),
                Input.GetAxisRaw("Vertical")
            ).normalized;

            // Handle movement
            isMoving = direction.sqrMagnitude > 0.01f;
            if (isMoving)
                lastDirection = direction;
            
            // Send state to animator
            anim.SetBool("isWalking", isMoving);

            Vector2 animDirection = isMoving ? direction : lastDirection;

            anim.SetFloat("x", animDirection.x);
            anim.SetFloat("y", animDirection.y); 
        }

        // Reduce dodge cooldown
        if (dodgeCooldownTimer > 0)
        {
            dodgeCooldownTimer -= Time.deltaTime;
        }
    }

    // ---------------------------------------------------------- PHYSICS ----------------------------------------------------------- 
    void FixedUpdate()
    {   
        if (!isDodging)
        {
            body.linearVelocity = direction * walkSpeed;
        }
    }

    // ----------------------------------------------------------- DODGE ------------------------------------------------------------ 
    IEnumerator Dodge()
    {
        isDodging = true;

        // Determine dodge direction 
        Vector2 dodgeDir = lastDirection;

        // Set animator direction
        anim.SetFloat("x", dodgeDir.x);
        anim.SetFloat("y", dodgeDir.y);

        // Trigger animation
        anim.SetTrigger("Dodge");

        body.linearVelocity = dodgeDir * dodgeForce;
        yield return new WaitForSeconds(dodgeDuration);

        isDodging = false;

        //start cooldown timer
        dodgeCooldownTimer = dodgeCooldown;
    }
}