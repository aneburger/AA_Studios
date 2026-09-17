// /* To change cursor sprite based on certain conditions */

// using UnityEngine;

// public class CursorManager : MonoBehaviour
// {
//     [SerializeField] private Texture2D cursorTexture;
//     public Vector2 cursorHotspot = Vector2.zero;

//     void Start()
//     {
//         // Set target to center of the texture (for aiming)
//         cursorHotspot = new Vector2(cursorTexture.width / 2f, cursorTexture.height / 2f);
//         Cursor.SetCursor(cursorTexture, cursorHotspot, CursorMode.Auto);
//     }
// }



using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class CursorManager : MonoBehaviour
{
    public static CursorManager Instance { get; private set; }

    [Header("Cursor Textures")]
    [SerializeField] private Texture2D aimCursorTexture;
    [SerializeField] private Texture2D arrowCursorTexture;
    [SerializeField] private Texture2D pointerCursorTexture;

    [Header("Hotspots")]
    [Tooltip("Leave at (-1, -1) to auto-center the hotspot on the aim texture (needed for accurate aiming).")]
    [SerializeField] private Vector2 aimCursorHotspot = new Vector2(-1f, -1f);
    [Tooltip("Usually the top-left tip of the arrow, e.g. (0, 0).")]
    [SerializeField] private Vector2 arrowCursorHotspot = Vector2.zero;
    [Tooltip("Usually the tip of the pointing finger.")]
    [SerializeField] private Vector2 pointerCursorHotspot = Vector2.zero;

    private enum CursorState { Aim, Arrow, Pointer }
    private CursorState currentState;
    private bool stateInitialized = false;

    private readonly List<RaycastResult> raycastResults = new List<RaycastResult>();

    // -- AWAKE --
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (aimCursorHotspot.x < 0f || aimCursorHotspot.y < 0f)
            aimCursorHotspot = CenterHotspot(aimCursorTexture);

        ApplyCursor(CursorState.Aim);
    }

    // -- UPDATE --
    private void Update()
    {
        bool menuActive = Time.timeScale <= 0f;

        CursorState desired = !menuActive
            ? CursorState.Aim
            : (IsPointerOverSelectable() ? CursorState.Pointer : CursorState.Arrow);

        if (!stateInitialized || desired != currentState)
            ApplyCursor(desired);
    }

    // -- IS POINTER OVER SELECTABLE --
    private bool IsPointerOverSelectable()
    {
        if (EventSystem.current == null) return false;

        Vector2 pointerPosition = Mouse.current != null
            ? Mouse.current.position.ReadValue()
            : (Vector2)Input.mousePosition;

        PointerEventData pointerData = new PointerEventData(EventSystem.current)
        {
            position = pointerPosition
        };

        raycastResults.Clear();
        EventSystem.current.RaycastAll(pointerData, raycastResults);

        foreach (RaycastResult result in raycastResults)
        {
            if (result.gameObject.GetComponentInParent<Selectable>() != null)
                return true;
        }

        return false;
    }

    // -- APPLY CURSOR --
    private void ApplyCursor(CursorState state)
    {
        currentState = state;
        stateInitialized = true;

        switch (state)
        {
            case CursorState.Aim:
                Cursor.SetCursor(aimCursorTexture, aimCursorHotspot, CursorMode.Auto);
                break;

            case CursorState.Arrow:
                Cursor.SetCursor(arrowCursorTexture, arrowCursorHotspot, CursorMode.Auto);
                break;

            case CursorState.Pointer:
                Cursor.SetCursor(pointerCursorTexture, pointerCursorHotspot, CursorMode.Auto);
                break;
        }
    }

    // -- CENTER HOTSPOT --
    private Vector2 CenterHotspot(Texture2D texture)
    {
        if (texture == null) return Vector2.zero;
        return new Vector2(texture.width / 2f, texture.height / 2f);
    }
}