using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CrowdChatter : MonoBehaviour
{
    [Header("Bubble")]
    [SerializeField] private CrowdBubble bubblePrefab;
    [SerializeField] private CrowdBubble longBubblePrefab;
    [SerializeField] private int longLineMinChars = 12;
    [SerializeField] private Vector3 bubbleOffset = new Vector3(0f, 1.1f, 0f);

    [Header("Behaviour")]
    [SerializeField] private bool memberCheersWithBubble = true;
    [SerializeField] private int avoidRecentCount = 3;
    [SerializeField] private float cheerAllStaggerMin = 0.05f;
    [SerializeField] private float cheerAllStaggerMax = 0.2f;
    [SerializeField] private float everyoneHopDelayMax = 0.3f;

    [Header("Sustained Cheer (CheerFor)")]
    [SerializeField] private int bubblesPerWaveMin = 2;
    [SerializeField] private int bubblesPerWaveMax = 4;
    [SerializeField] private float waveGapMin = 0.5f;
    [SerializeField] private float waveGapMax = 1.3f;
    [Range(0f, 1f)] [SerializeField] private float waveHopChance = 0.6f;

    private string lastLine;

    private readonly List<CrowdMember> members = new List<CrowdMember>();
    private readonly Queue<CrowdMember> recent = new Queue<CrowdMember>();

    // -- MEMBERS --
    public void AddMember(CrowdMember m)
    {
        if (m != null && !members.Contains(m)) members.Add(m);
    }

    public void SetMembers(IEnumerable<CrowdMember> list)
    {
        members.Clear();
        recent.Clear();
        if (list == null) return;
        foreach (CrowdMember m in list) AddMember(m);
    }

    // -- CHEER (one bubble) --
    public CrowdMember Cheer(string line)
    {
        if (string.IsNullOrEmpty(line) || bubblePrefab == null) return null;

        CrowdMember who = PickMember();
        if (who == null) return null;

        Vector3 offset = bubbleOffset + who.BubbleOffsetExtra;
        CrowdBubble bubble = Instantiate(PrefabFor(line), who.transform.position + offset, Quaternion.identity);
        bubble.Show(line, who.transform, offset);

        if (memberCheersWithBubble) who.Cheer();
        return who;
    }

    private CrowdBubble PrefabFor(string line)
    {
        if (longBubblePrefab != null && line.Length >= longLineMinChars)
            return longBubblePrefab;
        return bubblePrefab;
    }

    public CrowdMember CheerRandom(string[] lines)
    {
        if (lines == null || lines.Length == 0) return null;
        return Cheer(lines[Random.Range(0, lines.Length)]);
    }

    // -- CHEER ALL --
    public float CheerAll(string[] lines, bool everyoneHops = true)
    {
        if (lines == null || lines.Length == 0 || bubblePrefab == null) return 0f;

        float[] delays = new float[lines.Length];
        float acc = 0f;
        for (int i = 0; i < lines.Length; i++)
        {
            acc += Random.Range(cheerAllStaggerMin, cheerAllStaggerMax);
            delays[i] = acc;
        }

        StartCoroutine(CheerAllRoutine(lines, delays));

        if (everyoneHops)
        {
            foreach (CrowdMember m in members)
                if (m != null) StartCoroutine(DelayedCheer(m, Random.Range(0f, everyoneHopDelayMax)));
        }

        return acc + bubblePrefab.TotalLifetime;
    }

    // -- CHEER FOR --
    public Coroutine CheerFor(float duration, string[] lines, float intensity = 1f)
    {
        return StartCoroutine(CheerForRoutine(duration, lines, intensity));
    }

    private IEnumerator CheerForRoutine(float duration, string[] lines, float intensity)
    {
        if (lines == null || lines.Length == 0 || bubblePrefab == null) yield break;

        intensity = Mathf.Max(0.1f, intensity);
        int minCount = Mathf.Max(1, Mathf.RoundToInt(bubblesPerWaveMin * intensity));
        int maxCount = Mathf.Max(minCount, Mathf.RoundToInt(bubblesPerWaveMax * intensity));
        float gapScale = 1f / Mathf.Max(0.25f, intensity);
        float hopChance = Mathf.Clamp01(waveHopChance * intensity);

        float elapsed = 0f;
        while (elapsed < duration)
        {
            int count = Random.Range(minCount, maxCount + 1);
            for (int i = 0; i < count && elapsed < duration; i++)
            {
                Cheer(PickLine(lines));

                float d = Random.Range(cheerAllStaggerMin, cheerAllStaggerMax);
                yield return new WaitForSecondsRealtime(d);
                elapsed += d;
            }

            // End of the wave: some of the crowd hops
            foreach (CrowdMember m in members)
                if (m != null && Random.value < hopChance)
                    StartCoroutine(DelayedCheer(m, Random.Range(0f, everyoneHopDelayMax)));

            float gap = Random.Range(waveGapMin, waveGapMax) * gapScale;
            yield return new WaitForSecondsRealtime(gap);
            elapsed += gap;
        }
    }

    // Random line, but not the same one twice in a row
    private string PickLine(string[] lines)
    {
        string line = lines[Random.Range(0, lines.Length)];
        if (lines.Length > 1)
        {
            int guard = 0;
            while (line == lastLine && guard++ < 10)
                line = lines[Random.Range(0, lines.Length)];
        }
        lastLine = line;
        return line;
    }

    private IEnumerator CheerAllRoutine(string[] lines, float[] delays)
    {
        float prev = 0f;
        for (int i = 0; i < lines.Length; i++)
        {
            yield return new WaitForSecondsRealtime(delays[i] - prev);
            prev = delays[i];
            Cheer(lines[i]);
        }
    }

    private IEnumerator DelayedCheer(CrowdMember m, float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
        if (m != null) m.Cheer();
    }

    // -- PICK --
    private CrowdMember PickMember()
    {
        List<CrowdMember> candidates = new List<CrowdMember>();
        foreach (CrowdMember m in members)
            if (m != null && m.isActiveAndEnabled && !recent.Contains(m))
                candidates.Add(m);

        if (candidates.Count == 0)
        {
            recent.Clear();
            foreach (CrowdMember m in members)
                if (m != null && m.isActiveAndEnabled) candidates.Add(m);
        }

        if (candidates.Count == 0) return null;

        CrowdMember pick = candidates[Random.Range(0, candidates.Count)];

        recent.Enqueue(pick);
        while (recent.Count > Mathf.Max(0, avoidRecentCount)) recent.Dequeue();

        return pick;
    }
}