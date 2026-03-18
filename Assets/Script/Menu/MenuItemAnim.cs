using UnityEngine;
using TMPro;
using UnityEngine.EventSystems;

public class MenuItemAnim : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler
{
    private TextMeshProUGUI text;
    private RectTransform rect;

    private Vector2 startPos;
    private Vector2 targetPos;

    private float alpha = 0f;
    private float speed = 8f;

    private bool isActive = false;
    private bool isHovered = false;

    private bool introPlaying = false;
    private float introDelay = 0f;
    private float timer = 0f;

    public int index;
    public MenuController controller;

    void Awake()
    {
        text = GetComponent<TextMeshProUGUI>();
        rect = GetComponent<RectTransform>();

        targetPos = rect.anchoredPosition;
        startPos = targetPos + new Vector2(-100f, 0);

        rect.anchoredPosition = startPos;

        SetAlpha(0f);
    }

    void Update()
    {
        if (introPlaying)
        {
            timer += Time.deltaTime;

            if (timer >= introDelay)
            {
                FadeIn();
            }
        }

        if (isHovered && !IsMouseOver())
        {
            isHovered = false;
        }

        HandleVisual();
    }

    bool IsMouseOver()
    {
        PointerEventData pointerData = new PointerEventData(EventSystem.current);
        pointerData.position = Input.mousePosition;

        var results = new System.Collections.Generic.List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerData, results);

        foreach (var result in results)
        {
            if (result.gameObject == gameObject)
                return true;
        }

        return false;
    }

    public void PlayIntro(float delay)
    {
        introPlaying = true;
        introDelay = delay;
        timer = 0f;
    }

    void FadeIn()
    {
        alpha = Mathf.Lerp(alpha, 1f, Time.deltaTime * speed);
        rect.anchoredPosition = Vector2.Lerp(rect.anchoredPosition, targetPos, Time.deltaTime * speed);

        SetAlpha(alpha);

        if (alpha >= 0.95f)
        {
            introPlaying = false;
        }
    }

    public void SetActive(bool active)
    {
        isActive = active;
    }

    void HandleVisual()
    {
        float targetScale = 1f;

        if (isActive)
            targetScale = 1.07f;
        else if (isHovered)
            targetScale = 1.15f;

        transform.localScale = Vector3.Lerp(transform.localScale, Vector3.one * targetScale, Time.deltaTime * 10f);

        // if (isActive)
        //     text.color = Color.cyan;
        // else if (isHovered)
        //     text.color = new Color(1f, 1f, 1f, 0.9f);
        // else
        //     text.color = new Color(1f, 1f, 1f, 0.6f);
    }

    void SetAlpha(float a)
    {
        Color c = text.color;
        c.a = a;
        text.color = c;
    }


    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovered = true;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        controller.SelectFromMouse(index);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
    }
}