using System.Collections.Generic;
using UnityEngine;

public class MovementController : MonoBehaviour {
    [Header("References")]
    public Rigidbody2D body;
    public SpriteRenderer spriteRenderer;

    [Header("Player Settings")]
    public float walkSpeed;
    public float frameRate;

    [Header("Walking Sprites")]
    public List<Sprite> nWalkSprites;
    public List<Sprite> eWalkSprites;
    public List<Sprite> sWalkSprites;

    [Header("Idle Sprites")]
    public List<Sprite> nIdleSprites;
    public List<Sprite> eIdleSprites;
    public List<Sprite> sIdleSprites;

    // ----------------------------------------------------------- STATE ----------------------------------------------------------- 
    float idleTime; 

    Vector2 lastDirection;  
    Vector2 direction; 

    bool wasMoving;
    bool isMoving;

    enum FacingDirection
    {
        North,
        South,
        East
    }

    // ----------------------------------------------------------- START ----------------------------------------------------------- 
    void Start(){
        lastDirection = Vector2.down;
        spriteRenderer.sprite = sIdleSprites[0];
    }

    // ----------------------------------------------------------- UPDATE ----------------------------------------------------------- 
    void Update() 
    {
        // Movement based on key input
        direction = new Vector2(
            Input.GetAxisRaw("Horizontal"),
            Input.GetAxisRaw("Vertical")
        ).normalized;

        // Handle movement
        isMoving = direction.sqrMagnitude > 0.01f;
        if (isMoving)
        {
            lastDirection = direction;
        }

        // Handle direction change
        HandleFlip();

        // Getfacing direction
        FacingDirection facing = GetFacingDirection(isMoving ? direction : lastDirection);
        // Get sprites based on facing direction 
        List<Sprite> currentSprites = GetSprites(facing, isMoving);

        if (currentSprites != null && currentSprites.Count > 0)
        {
            int frame = (int)(Time.time * frameRate) % currentSprites.Count;
            spriteRenderer.sprite = currentSprites[frame];
        }
        
        // Reset idle time
        if (isMoving != wasMoving)
        {
            idleTime = Time.time;
        }

        wasMoving = isMoving;
    }

    // ---------------------------------------------------------- PHYSICS ----------------------------------------------------------- 
    void FixedUpdate()
    {
        body.linearVelocity = direction * walkSpeed;
    }


    // ------------------------------------------------------ HELPER FUNCTIONS ------------------------------------------------------ 

    // Flip the character if right or left is pressed
    void HandleFlip()
    {
        if(!spriteRenderer.flipX && direction.x < 0)
        {
            spriteRenderer.flipX = true;
        } 
        else if (spriteRenderer.flipX && direction.x > 0)
        {
            spriteRenderer.flipX = false;
        }
    }

    // Return Direction enum based on key input
    FacingDirection GetFacingDirection(Vector2 dir)
    {
        if (Mathf.Abs(dir.y) > Mathf.Abs(dir.x))
        {
            if (dir.y < 0)
                return FacingDirection.South;
            else
                return FacingDirection.North;
        }

        return FacingDirection.East;
    }

    // Get correct sprite list
    List<Sprite> GetSprites(FacingDirection dir, bool isMoving)
    {
        if (isMoving)
        {
            if (dir == FacingDirection.North) return nWalkSprites;
            if (dir == FacingDirection.South) return sWalkSprites;
            return eWalkSprites;
        }
        else
        {
            if (dir == FacingDirection.North) return nIdleSprites;
            if (dir == FacingDirection.South) return sIdleSprites;
            return eIdleSprites;
        }
    }
}
