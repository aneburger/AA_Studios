// Plays a sprite sequence on an Image component

using UnityEngine;
using UnityEngine.UI;

public class ImageAnimation : MonoBehaviour
{
    [Header("Frames")]
    public Sprite[] sprites;
    public int spritePerFrame = 6;

    [Header("Playback")]
    public bool loop = true;
    public bool destroyOnEnd = false;

    private Image image;
    private int index = 0;
    private int frame = 0;

    // -- AWAKE --
    private void Awake()
    {
        image = GetComponent<Image>();
    }

    // -- UPDATE --
    private void Update()
    {
        if (!loop && index == sprites.Length) return;

        frame++;
        if (frame < spritePerFrame) return;

        image.sprite = sprites[index];
        frame = 0;
        index++;

        if (index >= sprites.Length)
        {
            if (loop) index = 0;
            if (destroyOnEnd) Destroy(gameObject);
        }
    }
}