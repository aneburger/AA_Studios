using UnityEngine;

public class EnemyLightReceiver : MonoBehaviour
{
    private int sortingOrderOffset = 5;

    [SerializeField] private float sortingOffset = 0f;

    private SpriteRenderer parentRenderer;
    private SpriteRenderer lightRenderer;
    private Transform sortReference;

    private void Awake()
    {
        parentRenderer = transform.parent.GetComponent<SpriteRenderer>();
        lightRenderer = GetComponent<SpriteRenderer>();
        lightRenderer.material = new Material(Shader.Find("Universal Render Pipeline/2D/Sprite-Lit-Default"));
        sortReference = transform.parent;
    }

    private void LateUpdate()
    {
        lightRenderer.sprite = parentRenderer.sprite;
        lightRenderer.flipX = parentRenderer.flipX;
        lightRenderer.flipY = parentRenderer.flipY;
        lightRenderer.color = parentRenderer.color;
        lightRenderer.enabled = parentRenderer.enabled;
        lightRenderer.sortingLayerID = parentRenderer.sortingLayerID;

        lightRenderer.sortingOrder = -(int)(sortReference.position.y * 100) + (int)sortingOffset + sortingOrderOffset;
    }
}