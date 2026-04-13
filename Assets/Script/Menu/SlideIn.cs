using System.Collections;
using UnityEngine;

public class SlideIn : MonoBehaviour
{
    public float animSpeed = 6f;
    public Vector2 offset;

    private RectTransform rect;
    private Vector2 targetPos;

    void Awake()
    {
        rect = GetComponent<RectTransform>();
    }

    void Start()
    {
        targetPos = rect.anchoredPosition;
        gameObject.SetActive(false);
    }

    public void Show()
    {
        gameObject.SetActive(true);

        StopAllCoroutines();
        StartCoroutine(AnimateEnter());
    }

    public void Hide()
    {
        StopAllCoroutines();
        StartCoroutine(AnimateExit());
    }

    IEnumerator AnimateEnter()
    {
        Vector2 startPos = targetPos + offset;

        rect.anchoredPosition = startPos;

        float time = 0f;

        while (time < 1f)
        {
            time += Time.deltaTime * animSpeed;
            float t = Mathf.SmoothStep(0f, 1f, time);

            rect.anchoredPosition = Vector2.Lerp(startPos, targetPos, t);

            yield return null;
        }

        rect.anchoredPosition = targetPos;
    }

    IEnumerator AnimateExit()
    {
        Vector2 startPos = rect.anchoredPosition;
        Vector2 endPos = targetPos + offset;

        float time = 0f;

        while (time < 1f)
        {
            time += Time.deltaTime * animSpeed;
            float t = Mathf.SmoothStep(0f, 1f, time);

            rect.anchoredPosition = Vector2.Lerp(startPos, endPos, t);

            yield return null;
        }

        rect.anchoredPosition = endPos;

        gameObject.SetActive(false);
    }
}
