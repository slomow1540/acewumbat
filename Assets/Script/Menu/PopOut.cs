using System.Collections;
using UnityEngine;

public class PopOut : MonoBehaviour
{
    public float duration = 0.5f;

    [Header("Offset (arah muncul)")]
    public Vector3 offset = new Vector3(0, -2f, 0);

    [Header("Scale")]
    public Vector3 hiddenScale = Vector3.zero;
    public Vector3 shownScale = Vector3.one;

    [Header("Animation")]
    public AnimationCurve ease = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private Vector3 shownPos;
    private Vector3 hiddenPos;

    protected void Awake()
    {
        shownPos = transform.localPosition;
        hiddenPos = shownPos + offset;

        transform.localPosition = hiddenPos;
        transform.localScale = hiddenScale;
    }

    public void Show()
    {
        StopAllCoroutines();
        StartCoroutine(Animate(true));
    }

    public void Hide()
    {
        StopAllCoroutines();
        StartCoroutine(Animate(false));
    }

    public void ShowInstant()
    {
        StopAllCoroutines();

        transform.localPosition = shownPos;
        transform.localScale = shownScale;
    }

    public void HideInstant()
    {
        StopAllCoroutines();

        transform.localPosition = hiddenPos;
        transform.localScale = hiddenScale;
    }

    IEnumerator Animate(bool show)
    {
        float time = 0f;

        Vector3 startPos = show ? hiddenPos : shownPos;
        Vector3 endPos = show ? shownPos : hiddenPos;

        Vector3 startScale = show ? hiddenScale : shownScale;
        Vector3 endScale = show ? shownScale : hiddenScale;

        while (time < duration)
        {
            time += Time.deltaTime;
            float t = time / duration;
            float e = ease.Evaluate(t);

            transform.localPosition = Vector3.Lerp(startPos, endPos, e);
            transform.localScale = Vector3.Lerp(startScale, endScale, e);

            yield return null;
        }

        transform.localPosition = endPos;
        transform.localScale = endScale;
    }
}