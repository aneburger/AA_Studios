// Reusable bobbing indicator

using UnityEngine;

public class InteractIndicator : MonoBehaviour
{
    [SerializeField] private float bobHeight = 0.1f;
    [SerializeField] private float bobSpeed = 3f;

    private Vector3 basePos;
    private float bobTime = 0f;

    // -- AWAKE --
    private void Awake()
    {
        basePos = transform.localPosition;
    }

    // -- UPDATE --
    private void Update()
    {
        bobTime += Time.deltaTime * bobSpeed;
        float yOffset = Mathf.Sin(bobTime) * bobHeight;
        transform.localPosition = basePos + new Vector3(0f, yOffset, 0f);
    }

    // -- SHOW --
    public void Show()
    {
        gameObject.SetActive(true);
        bobTime = 0f;
        transform.localPosition = basePos;
    }

    // -- HIDE --
    public void Hide()
    {
        gameObject.SetActive(false);
    }

    // -- SET BASE POSITION --
    public void SetBasePosition(Vector3 pos)
    {
        basePos = pos;
        transform.localPosition = pos;
    }
}