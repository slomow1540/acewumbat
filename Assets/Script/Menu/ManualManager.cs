using System.Collections;
using UnityEngine;

public class ManualManager : MonoBehaviour
{
    [Header("UI")]
    public SlideIn[] manuals;
    public GameObject scroll;

    [Header("Overlay")]
    public Overlay overlay;
    public float overlayFadeSpeed = 5f;

    void Start()
    {
        for (int i = 0; i < manuals.Length; i++)
        {
            manuals[i].Hide();
        }

        Hide();
    }

    public void Show()
    {
        overlay.FadeTo(0.7f);
        scroll.SetActive(true);

        for (int i = 0; i < manuals.Length; i++)
        {
            manuals[i].Show(i * 0.1f);
        }
    }

    public void Hide()
    {
        overlay.FadeTo(0f);

        for (int i = manuals.Length - 1; i >= 0; i--)
        {
            manuals[i].Hide(i * 0.1f);
        }
        scroll.SetActive(false);
    }
}
