// Handles enemy AI states: Idle, Chase, Attack
// Temporary DUMB AI :(
using UnityEngine;
using TopDown.Movement;

public class EnemyAI : MonoBehaviour
{   
    private enum State { Idle, Chase, Attack }

    private State currentState = State.Idle;

    private EnemyController enemy;
    private EnemyMover mover;
    private EnemyShooter shooter;
    private WeaponAimer weaponAimer;
    private Transform player;

    // -- AWAKE --
    private void Awake()
    {   
        enemy = GetComponent<EnemyController>();
        mover = GetComponent<EnemyMover>();
        shooter = GetComponent<EnemyShooter>();
        weaponAimer = GetComponentInChildren<WeaponAimer>();

        player = GameObject.FindGameObjectWithTag("Player")?.transform;
    }

    // -- UPDATE --
    private void Update()
    {
        if (player == null) return;

        float distance = Vector2.Distance(transform.position, player.position);

        switch (currentState)
        {
            case State.Idle:   HandleIdle(distance);   break;
            case State.Chase:  HandleChase(distance);  break;
            case State.Attack: HandleAttack(distance); break;
        }
    }

    // -- DIRECTION TO PLAYER --
    private Vector2 DirectionToPlayer()
    {
        return ((Vector2)player.position - (Vector2)transform.position).normalized;
    }

    // -- AIM AT PLAYER --
    private void AimAtPlayer()
    {
        if (weaponAimer != null)
            weaponAimer.SetAimDirection(DirectionToPlayer());
    }

    // -- IDLE --
    private void HandleIdle(float distance)
    {   
        mover.Stop();

        if (distance <= enemy.Data.detectionRange)
            currentState = State.Chase;
    }

    // -- CHASE --
    private void HandleChase(float distance)
    {
        mover.Move(DirectionToPlayer());
        AimAtPlayer();

        if (distance <= enemy.Data.attackRange)
            currentState = State.Attack;
        else if (distance > enemy.Data.detectionRange)
            currentState = State.Idle;
    }

    // -- ATTACK --
    private void HandleAttack(float distance)
    {
        mover.Stop();
        AimAtPlayer();
        shooter?.TryShoot();

        if (distance > enemy.Data.attackRange)
            currentState = State.Chase;
    }
}