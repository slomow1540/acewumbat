using System.Collections;
using UnityEngine;

public class TabPanel : MonoBehaviour
{
    public float animSpeed = 6f;
    private Vector3 hiddenOffset = new Vector3(0, -50f, 0);

    private RectTransform rect;
    private CanvasGroup canvasGroup;

    private Vector3 shownPos;
    private Vector3 hiddenPos;

    void Awake()
    {
        rect = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();

        shownPos = rect.anchoredPosition;
        hiddenPos = shownPos + hiddenOffset;
    }

    public void HideInstant()
    {
        rect = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        rect.anchoredPosition = hiddenPos;
        canvasGroup.alpha = 0f;
        gameObject.SetActive(false);
    }

    public void Show()
    {
        gameObject.SetActive(true);
        StopAllCoroutines();
        StartCoroutine(Animate(true));
    }

    public void Hide()
    {
        if (!gameObject.activeInHierarchy)
        {
            rect.anchoredPosition = hiddenPos;
            if (canvasGroup != null)
                canvasGroup.alpha = 0f;

            gameObject.SetActive(false);
            return;
        }

        StopAllCoroutines();
        StartCoroutine(Animate(false));
    }

    IEnumerator Animate(bool show)
    {
        float time = 0f;

        Vector3 startPos = rect.anchoredPosition;
        Vector3 targetPos = show ? shownPos : hiddenPos;

        float startAlpha = canvasGroup.alpha;
        float targetAlpha = show ? 1f : 0f;

        while (time < 1f)
        {
            time += Time.deltaTime * animSpeed;
            float t = Mathf.SmoothStep(0f, 1f, time);

            rect.anchoredPosition = Vector3.Lerp(startPos, targetPos, t);
            canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);

            yield return null;
        }

        rect.anchoredPosition = targetPos;
        canvasGroup.alpha = targetAlpha;

        if (!show)
            gameObject.SetActive(false);
    }
}
