using System.Collections;
using UnityEngine;

public class Hologram : MonoBehaviour
{
    public float duration = 0.5f;
    public Vector3 hiddenOffset = new Vector3(0, -2f, 0);
    public AnimationCurve ease = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private Vector3 shownPos;
    private Vector3 hiddenPos;

    void Awake()
    {
        shownPos = transform.localPosition;
        hiddenPos = shownPos + hiddenOffset;

        transform.localPosition = hiddenPos;
        transform.localScale = Vector3.zero;
    }

    public void Show()
    {
        StopAllCoroutines();
        StartCoroutine(Animate(show: true));
    }

    public void Hide()
    {
        StopAllCoroutines();
        StartCoroutine(Animate(show: false));
    }

    IEnumerator Animate(bool show)
    {
        float time = 0f;

        Vector3 startPos = show ? hiddenPos : shownPos;
        Vector3 endPos = show ? shownPos : hiddenPos;

        Vector3 startScale = show ? Vector3.zero : Vector3.one;
        Vector3 endScale = show ? Vector3.one : Vector3.zero;

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
