using UnityEngine;
using UnityEngine.UI;

public class MarshMenuAnimation : MonoBehaviour
{
    [SerializeField] private Sprite[] beatenFrames;
    [SerializeField] private Sprite[] inProgressFrames;
    [SerializeField] private bool inProgressBeatsBeaten = false;

    private ImageAnimation anim;
    private Image image;
    private Sprite[] defaultFrames;

    private void Awake()
    {
        anim = GetComponent<ImageAnimation>();
        image = GetComponent<Image>();

        if (anim != null)
            defaultFrames = anim.sprites;
    }

    private void OnEnable()
    {
        Refresh();
    }

    private void Start()
    {
        Refresh();
    }

    public void Refresh()
    {
        if (anim == null) return;

        bool beaten = GameProgress.IsBeaten;
        bool inProgress = LevelLoader.Instance != null && LevelLoader.Instance.HasSavedGame;

        Sprite[] chosen = null;
        if (inProgress && inProgressBeatsBeaten) chosen = inProgressFrames;
        if (IsEmpty(chosen) && beaten) chosen = beatenFrames;
        if (IsEmpty(chosen) && inProgress) chosen = inProgressFrames;
        if (IsEmpty(chosen)) chosen = defaultFrames;

        anim.SetFrames(chosen);


        if (image != null && chosen != null && chosen.Length > 0)
            image.sprite = chosen[0];
    }

    private static bool IsEmpty(Sprite[] frames)
    {
        return frames == null || frames.Length == 0;
    }
}