/* To change cursor sprite based on certain conditions */

using UnityEngine;

public class CursorManager : MonoBehaviour
{
    [SerializeField] private Texture2D cursorTexture;
    public Vector2 cursorHotspot = Vector2.zero;

    void Start()
    {
        // Set target to center of the texture (for aiming)
        cursorHotspot = new Vector2(cursorTexture.width / 2f, cursorTexture.height / 2f);
        Cursor.SetCursor(cursorTexture, cursorHotspot, CursorMode.Auto);
    }
}