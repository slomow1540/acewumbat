using TMPro;
using UnityEngine;

public class GeneralUI : MonoBehaviour
{
    protected TextMeshProUGUI text;
    protected RectTransform rect;

    protected Vector2 basePos;
    protected Vector2 offsetPos;

    protected float alpha = 0f;

    protected bool isShowing = false;
    protected bool isHiding = false;
    protected bool isMoving = false;

    protected float speed = 8f;

    protected float delay = 0f;
    protected float timer = 0f;

    protected virtual void Awake()
    {
        text = GetComponent<TextMeshProUGUI>();
        rect = GetComponent<RectTransform>();

        basePos = rect.anchoredPosition;
        SetAlpha(0f);
    }

    protected virtual void Update()
    {
        if (delay > 0)
        {
            timer += Time.deltaTime;
            if (timer < delay)
                return;
        }

        if (isShowing)
            FadeIn();

        if (isHiding)
            FadeOut();

        if (isMoving)
            MoveToBase();
    }

    public virtual void Show(float delay = 0f, float offsetX = -100f)
    {
        this.delay = delay;
        timer = 0f;

        isShowing = true;
        isHiding = false;

        offsetPos = basePos + new Vector2(offsetX, 0);
        rect.anchoredPosition = offsetPos;

        gameObject.SetActive(true);
    }

    public virtual void Hide(float delay = 0f)
    {
        this.delay = delay;
        timer = 0f;

        isHiding = true;
        isShowing = false;
    }

    public virtual void Move(float offsetX = -50f)
    {
        offsetPos = basePos + new Vector2(offsetX, 0);
        rect.anchoredPosition = offsetPos;

        isMoving = true;
    }

    protected void FadeIn()
    {
        alpha = Mathf.Lerp(alpha, 1f, Time.deltaTime * speed);
        rect.anchoredPosition = Vector2.Lerp(
            rect.anchoredPosition,
            basePos,
            Time.deltaTime * speed
        );

        SetAlpha(alpha);

        if (alpha >= 0.95f)
        {
            isShowing = false;
        }
    }

    protected void FadeOut()
    {
        alpha = Mathf.Lerp(alpha, 0f, Time.deltaTime * speed);
        rect.anchoredPosition = Vector2.Lerp(
            rect.anchoredPosition,
            basePos + new Vector2(-150f, 0),
            Time.deltaTime * speed
        );

        SetAlpha(alpha);

        if (alpha <= 0.05f)
        {
            isHiding = false;
            gameObject.SetActive(false);
        }
    }

    protected void MoveToBase()
    {
        rect.anchoredPosition = Vector2.Lerp(
            rect.anchoredPosition,
            basePos,
            Time.deltaTime * speed
        );

        if (Vector2.Distance(rect.anchoredPosition, basePos) < 0.5f)
        {
            rect.anchoredPosition = basePos;
            isMoving = false;
        }
    }

    protected void SetAlpha(float a)
    {
        if (text != null)
        {
            Color c = text.color;
            c.a = a;
            text.color = c;
        }
    }
}
