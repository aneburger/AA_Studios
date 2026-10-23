using System.Collections;
using UnityEngine;
using Unity.Cinemachine;

public class BossRoomCameraReveal : MonoBehaviour
{
    [SerializeField] private CinemachinePositionComposer composer;

    [Header("Reveal")]
    [SerializeField] private float revealDistance = 3f;
    [SerializeField] private float revealDuration = 1.5f;

    [Header("Return")]
    [SerializeField] private float returnDuration = 0.8f;

    private Vector3 baseOffset;
    private bool displaced;
    private Coroutine routine;

    // -- BEGIN --
    public void Begin(Vector2 direction)
    {
        if (!FindComposer()) return;

        if (!displaced)
        {
            baseOffset = composer.TargetOffset;
            displaced = true;
        }

        Vector2 dir = direction.sqrMagnitude > 0.001f ? direction.normalized : Vector2.up;
        Vector3 target = baseOffset + (Vector3)(dir * revealDistance);
        Animate(target, revealDuration, endsDisplaced: true);
    }

    // -- END --
    public void End()
    {
        if (!displaced || !FindComposer()) return;

        Animate(baseOffset, returnDuration, endsDisplaced: false);
    }

    // -- RESET IMMEDIATE --
    public void ResetImmediate()
    {
        StopRoutine();

        if (displaced && composer != null)
            composer.TargetOffset = baseOffset;

        displaced = false;
    }

    // -- ON DESTROY --
    private void OnDestroy()
    {
        ResetImmediate();
    }

    // -- FIND COMPOSER --
    private bool FindComposer()
    {
        if (composer == null)
            composer = FindFirstObjectByType<CinemachinePositionComposer>();

        return composer != null;
    }

    // -- ANIMATE --
    private void Animate(Vector3 target, float duration, bool endsDisplaced)
    {
        StopRoutine();

        if (duration <= 0f)
        {
            composer.TargetOffset = target;
            displaced = endsDisplaced;
            return;
        }

        routine = StartCoroutine(AnimateRoutine(target, duration, endsDisplaced));
    }

    private IEnumerator AnimateRoutine(Vector3 target, float duration, bool endsDisplaced)
    {
        Vector3 from = composer.TargetOffset;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
            composer.TargetOffset = Vector3.Lerp(from, target, t);
            yield return null;
        }

        composer.TargetOffset = target;
        displaced = endsDisplaced;
        routine = null;
    }

    private void StopRoutine()
    {
        if (routine == null) return;

        StopCoroutine(routine);
        routine = null;
    }
}