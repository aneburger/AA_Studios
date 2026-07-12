using UnityEngine;

public class LightReceiver : MonoBehaviour
{
    private SpriteRenderer parentRenderer;
    private SpriteRenderer lightRenderer;

    private void Awake()
    {
        parentRenderer = transform.parent.GetComponent<SpriteRenderer>();
        lightRenderer = GetComponent<SpriteRenderer>();
        lightRenderer.material = new Material(Shader.Find("Universal Render Pipeline/2D/Sprite-Lit-Default"));
    }

    private void LateUpdate()
    {
        lightRenderer.sprite = parentRenderer.sprite;
        lightRenderer.flipX = parentRenderer.flipX;
        lightRenderer.color = parentRenderer.color;
    }
}