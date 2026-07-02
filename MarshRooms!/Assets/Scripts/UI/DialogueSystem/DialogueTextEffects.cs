using System.Collections.Generic;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(TextMeshProUGUI))]
public class DialogueTextEffects : MonoBehaviour
{
    [Header("Wave Settings")]
    [SerializeField] private float waveAmplitude = 4f;
    [SerializeField] private float waveFrequency = 2f;
    [SerializeField] private float waveSpeed = 3f;

    [Header("Shake Settings")]
    [SerializeField] private float shakeIntensity = 2f;
    [SerializeField] private float shakeSpeed = 30f;

    private TextMeshProUGUI tmp;
    private List<TextEffectRange> effectRanges = new List<TextEffectRange>();
    private bool hasEffects = false;

    private float shakeTimer = 0f;
    private Vector3[] shakeOffsets = new Vector3[0];

    // -- AWAKE --
    private void Awake()
    {
        tmp = GetComponent<TextMeshProUGUI>();
    }

    // -- SET EFFECTS RANGES --
    public void SetEffectRanges(List<TextEffectRange> ranges)
    {
        effectRanges = ranges;
        hasEffects = ranges != null && ranges.Count > 0;
    }

    // -- CLEAR EFFECTS --
    public void ClearEffects()
    {
        effectRanges = new List<TextEffectRange>();
        hasEffects = false;
    }

    // -- UPDATE --
    private void Update()
    {
        if (!hasEffects) return;

        tmp.ForceMeshUpdate();

        TMP_TextInfo textInfo = tmp.textInfo;
        if (textInfo.characterCount == 0) return;

        // Re-randomise shake offsets on a timer
        shakeTimer -= Time.unscaledDeltaTime;
        if (shakeTimer <= 0f)
        {
            shakeTimer = 1f / shakeSpeed;
            RegenerateShakeOffsets(textInfo.characterCount);
        }

        for (int i = 0; i < textInfo.characterCount; i++)
        {
            TMP_CharacterInfo charInfo = textInfo.characterInfo[i];
            if (!charInfo.isVisible) continue;

            var effects = DialogueTextParser.GetEffectsAt(i, effectRanges);
            if (effects.Count == 0) continue;

            int matIndex = charInfo.materialReferenceIndex;
            int vertIndex = charInfo.vertexIndex;

            // Copy original vertices
            Vector3[] verts = textInfo.meshInfo[matIndex].vertices;
            Vector3 offset = Vector3.zero;

            foreach (var effect in effects)
            {
                switch (effect)
                {
                    case TextEffectType.Wave:
                        float waveY = Mathf.Sin(Time.unscaledTime * waveSpeed + i * waveFrequency) * waveAmplitude;
                        offset += new Vector3(0, waveY, 0);
                        break;

                    case TextEffectType.Shake:
                        if (i < shakeOffsets.Length)
                            offset += shakeOffsets[i];
                        break;
                }
            }

            // Apply offset to all 4 corners of the character quad
            verts[vertIndex + 0] += offset;
            verts[vertIndex + 1] += offset;
            verts[vertIndex + 2] += offset;
            verts[vertIndex + 3] += offset;
        }

        // Push modified vertices back to the mesh
        for (int i = 0; i < textInfo.meshInfo.Length; i++)
        {
            Mesh mesh = textInfo.meshInfo[i].mesh;
            mesh.vertices = textInfo.meshInfo[i].vertices;
            tmp.UpdateGeometry(mesh, i);
        }
    }

    // -- SHAKE OFFSETS --
    private void RegenerateShakeOffsets(int count)
    {
        if (shakeOffsets.Length != count)
            shakeOffsets = new Vector3[count];

        for (int i = 0; i < count; i++)
        {
            shakeOffsets[i] = new Vector3(
                Random.Range(-shakeIntensity, shakeIntensity),
                Random.Range(-shakeIntensity, shakeIntensity),
                0
            );
        }
    }
}