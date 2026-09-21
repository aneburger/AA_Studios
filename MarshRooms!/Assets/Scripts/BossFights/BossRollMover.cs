// Physics for the boss's rolls.

using System;
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class BossRollMover : MonoBehaviour
{
   
    [SerializeField] private LayerMask bumpMask;
    [SerializeField] private float bumpEventCooldown = 0.08f;

    public event Action<Vector2, Vector2> Bumped;

    public bool IsRolling { get; private set; }
    public Vector2 Direction { get; private set; } = Vector2.down;
    public float Speed { get; set; }
    public float ActualSpeed => body != null ? body.linearVelocity.magnitude : 0f;

    private Rigidbody2D body;
    private bool stopOnBump;
    private bool sliding;
    private Vector2 slideVelocity;
    private float lastBumpTime = -999f;

    // -- AWAKE --
    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Dynamic;
        body.gravityScale = 0f;
        body.freezeRotation = true;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;
        body.sleepMode = RigidbodySleepMode2D.NeverSleep;
    }

    // -- START ROLLING --
    public void StartRolling(Vector2 direction, float speed, bool stopOnBump = false)
    {
        sliding = false;
        this.stopOnBump = stopOnBump;

        IsRolling = true;
        Speed = speed;
        SetDirection(direction);
    }

    // -- SET DIRECTION --
    public void SetDirection(Vector2 direction)
    {
        if (direction.sqrMagnitude > 0.001f)
            Direction = direction.normalized;
    }

    // -- STOP ROLLING --
    public void StopRolling()
    {
        IsRolling = false;
        sliding = false;
        stopOnBump = false;

        body.linearVelocity = Vector2.zero;
        body.angularVelocity = 0f;
    }

    // -- SLIDE --
    public IEnumerator Slide(Vector2 velocity, float duration)
    {
        IsRolling = false;
        stopOnBump = false;
        sliding = true;

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            slideVelocity = Vector2.Lerp(velocity, Vector2.zero, Mathf.Clamp01(t / duration));
            yield return null;
        }

        StopRolling();
    }

    // -- FIXED UPDATE --
    private void FixedUpdate()
    {
        if (IsRolling)
            body.linearVelocity = Direction * Speed;
        else if (sliding)
            body.linearVelocity = slideVelocity;
        else
            body.linearVelocity = Vector2.zero;
    }

    // -- COLLISION --
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!IsRolling) return;
        if (((1 << collision.collider.gameObject.layer) & bumpMask.value) == 0) return;

        Vector2 incoming = Direction;
        Vector2 normal = collision.GetContact(0).normal;
        if (Vector2.Dot(normal, incoming) > 0f) normal = -normal;

        if (stopOnBump)
        {
            IsRolling = false;
            body.linearVelocity = Vector2.zero;
            Bumped?.Invoke(normal, incoming);
            return;
        }

        Direction = Vector2.Reflect(incoming, normal).normalized;

        if (Time.time - lastBumpTime < bumpEventCooldown) return;
        lastBumpTime = Time.time;
        Bumped?.Invoke(normal, incoming);
    }
}