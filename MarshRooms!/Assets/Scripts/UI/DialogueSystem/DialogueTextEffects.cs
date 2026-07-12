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

    [Header("Rainbow Settings")]
    [SerializeField] private float rainbowSpeed = 2f;
    [SerializeField] private float rainbowSpread = 0.1f;

    [Header("Pulse Settings")]
    [SerializeField] private float pulseSpeed = 3f;
    [SerializeField] private float pulseAmount = 3f;

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

    // -- SET EFFECT RANGES --
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

            Vector3[] verts = textInfo.meshInfo[matIndex].vertices;
            Color32[] colors = textInfo.meshInfo[matIndex].colors32;

            Vector3 offset = Vector3.zero;
            bool colorModified = false;
            Color32 newColor = colors[vertIndex];

            foreach (var effect in effects)
            {
                switch (effect)
                {
                    // -- WAVE --
                    case TextEffectType.Wave:
                        float waveY = Mathf.Sin(Time.unscaledTime * waveSpeed + i * waveFrequency) * waveAmplitude;
                        offset += new Vector3(0, waveY, 0);
                        break;

                    // -- SHAKE --
                    case TextEffectType.Shake:
                        if (i < shakeOffsets.Length)
                            offset += shakeOffsets[i];
                        break;

                    // -- RAINBOW --
                    case TextEffectType.Rainbow:
                        float hue = Mathf.Repeat(Time.unscaledTime * rainbowSpeed + i * rainbowSpread, 1f);
                        Color rainbowColor = Color.HSVToRGB(hue, 1f, 1f);
                        newColor = rainbowColor;
                        colorModified = true;
                        break;

                    // -- PULSE --
                    case TextEffectType.Pulse:
                        float scale = Mathf.Sin(Time.unscaledTime * pulseSpeed + i * 0.3f) * pulseAmount;

                        Vector3 charCenter = (verts[vertIndex + 0] +
                                             verts[vertIndex + 1] +
                                             verts[vertIndex + 2] +
                                             verts[vertIndex + 3]) / 4f;

                        verts[vertIndex + 0] += (verts[vertIndex + 0] - charCenter).normalized * scale;
                        verts[vertIndex + 1] += (verts[vertIndex + 1] - charCenter).normalized * scale;
                        verts[vertIndex + 2] += (verts[vertIndex + 2] - charCenter).normalized * scale;
                        verts[vertIndex + 3] += (verts[vertIndex + 3] - charCenter).normalized * scale;
                        break;
                }
            }

            // Apply position offset
            if (offset != Vector3.zero)
            {
                verts[vertIndex + 0] += offset;
                verts[vertIndex + 1] += offset;
                verts[vertIndex + 2] += offset;
                verts[vertIndex + 3] += offset;
            }

            // Apply color override (rainbow)
            if (colorModified)
            {
                colors[vertIndex + 0] = newColor;
                colors[vertIndex + 1] = newColor;
                colors[vertIndex + 2] = newColor;
                colors[vertIndex + 3] = newColor;
            }
        }

        // Push modified vertices and colors back to mesh
        for (int i = 0; i < textInfo.meshInfo.Length; i++)
        {
            Mesh mesh = textInfo.meshInfo[i].mesh;
            mesh.vertices = textInfo.meshInfo[i].vertices;
            mesh.colors32 = textInfo.meshInfo[i].colors32;
            tmp.UpdateGeometry(mesh, i);
            tmp.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);
        }
    }

    // -- REGENERATE SHAKE OFFSETS --
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