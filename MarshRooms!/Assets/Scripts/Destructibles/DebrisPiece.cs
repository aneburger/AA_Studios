using UnityEngine;

public class DebrisPiece : MonoBehaviour
{
    Vector2 start;
    Vector2 end;
    float spin;
    float duration;
    float elapsed;

    public void Launch(Vector2 startPos, Vector2 endPos, float spinDegPerSec, float settleTime)
    {
        start = startPos;
        end = endPos;
        spin = spinDegPerSec;
        duration = Mathf.Max(0.05f, settleTime);
    }

    void Update()
    {
        elapsed += Time.deltaTime;
        float t = Mathf.Clamp01(elapsed / duration);

        float inv = 1f - t;
        float eased = 1f - inv * inv * inv;
        transform.position = Vector2.Lerp(start, end, eased);
        transform.Rotate(0f, 0f, spin * inv * inv * Time.deltaTime);

        if (t >= 1f) enabled = false;
    }
}