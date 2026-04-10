using System.Collections;
using UnityEngine;

public class CreditButton : MonoBehaviour
{
    public CameraMover cameraMover;

    private int creditPos = 4;

    private RectTransform rect;

    public float animSpeed = 6f;
    public float offsetX = 300f;

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

    public void ResetPos()
    {
        creditPos = 4;
    }

    public void GoRight()
    {
        creditPos++;
        if (creditPos > 7)
            creditPos = 4;

        cameraMover.MoveTo(creditPos);
    }

    public void GoLeft()
    {
        creditPos--;
        if (creditPos < 4)
            creditPos = 7;

        cameraMover.MoveTo(creditPos);
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
        gameObject.SetActive(false);
    }

    IEnumerator AnimateEnter()
    {
        Vector2 startPos = targetPos + new Vector2(offsetX, 0);

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
}
