// Provides enemy pathing direction

using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyPathing : MonoBehaviour
{
    [SerializeField] private float destinationUpdateInterval = 0.2f;

    [Header("Off-Mesh Target Handling")]
    [SerializeField] private float offMeshSampleDistance = 2.5f;

    [Header("Separation")]
    [SerializeField] private LayerMask enemyMask;
    [SerializeField] private float separationRadius = 1.2f;
    [SerializeField] private float separationStrength = 1f;

    private NavMeshAgent agent;
    private float nextUpdateTime;

    private static readonly List<Collider2D> separationBuffer = new List<Collider2D>();

    // -- AWAKE --
    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.updatePosition = false;
        agent.updateRotation = false;
        agent.updateUpAxis = false;
    }

    // -- UPDATE --
    private void Update()
    {
        agent.nextPosition = transform.position;
    }

    [Header("Direction Smoothing")]
    [SerializeField] private float directionSmoothSpeed = 10f;

    private Vector2 smoothedDirection;

    // -- DIRECTION TO TARGET --
    public Vector2 GetDirectionToTarget(Vector2 targetPosition)
    {
        if (Time.time >= nextUpdateTime)
        {
            agent.SetDestination(ResolveDestination(targetPosition));
            nextUpdateTime = Time.time + destinationUpdateInterval;
        }

        Vector2 pathDirection = Vector2.zero;
        if (!agent.pathPending && agent.desiredVelocity.sqrMagnitude > 0.001f)
            pathDirection = ((Vector2)agent.desiredVelocity).normalized;

        Vector2 separation = GetSeparationForce();
        Vector2 combined = pathDirection + separation * separationStrength;
        Vector2 rawDirection = combined.sqrMagnitude > 0.001f ? combined.normalized : pathDirection;

        smoothedDirection = Vector2.Lerp(
            smoothedDirection,
            rawDirection,
            1f - Mathf.Exp(-directionSmoothSpeed * Time.deltaTime)
        );

        return smoothedDirection;
    }

    // -- RESOLVE DESTINATION --
    private Vector3 ResolveDestination(Vector2 targetPosition)
    {
        if (NavMesh.SamplePosition(targetPosition, out NavMeshHit hit, offMeshSampleDistance, NavMesh.AllAreas))
            return hit.position;

        return targetPosition;
    }

    // -- SEPARATION FORCE --
    private Vector2 GetSeparationForce()
    {
        ContactFilter2D filter = new ContactFilter2D();
        filter.SetLayerMask(enemyMask);
        filter.useTriggers = true;

        int count = Physics2D.OverlapCircle(transform.position, separationRadius, filter, separationBuffer);
        Vector2 force = Vector2.zero;

        for (int i = 0; i < count; i++)
        {
            Collider2D other = separationBuffer[i];
            if (other == null || other.transform == transform) continue;

            Vector2 offset = (Vector2)transform.position - (Vector2)other.transform.position;
            float dist = offset.magnitude;
            if (dist < 0.001f) continue;

            force += offset.normalized * (1f - dist / separationRadius);
        }

        return force;
    }

    private void OnDrawGizmos()
    {
        if (agent == null || agent.path == null) return;

        Gizmos.color = Color.yellow;
        var corners = agent.path.corners;
        for (int i = 0; i < corners.Length - 1; i++)
        {
            Gizmos.DrawLine(corners[i], corners[i + 1]);
            Gizmos.DrawSphere(corners[i], 0.05f);
        }
    }
}