using UnityEngine;

public class Carousel : MonoBehaviour
{
    [Header("Carousel")]
    public RectTransform content;
    public RectTransform[] pages;

    [Header("Animation")]
    public float animationSpeed = 10f;

    private int currentPage = 0;
    private Vector2 targetPosition;

    void Start()
    {
        if (pages.Length == 0)
            return;

        GoTo(0);
        content.anchoredPosition = targetPosition;
    }

    void Update()
    {
        content.anchoredPosition = Vector2.Lerp(
            content.anchoredPosition,
            targetPosition,
            Time.deltaTime * animationSpeed
        );
    }

    public void GoTo(int index)
    {
        if (pages.Length == 0)
            return;

        currentPage = Mathf.Clamp(index, 0, pages.Length - 1);

        targetPosition = new Vector2(
            -pages[currentPage].anchoredPosition.x,
            content.anchoredPosition.y
        );
    }

    public void Next()
    {
        currentPage++;

        if (currentPage >= pages.Length)
            currentPage = 0;

        GoTo(currentPage);
    }

    public void Previous()
    {
        currentPage--;

        if (currentPage < 0)
            currentPage = pages.Length - 1;

        GoTo(currentPage);
    }

    public int GetCurrentPage()
    {
        return currentPage;
    }

    public void Show()
    {
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }
}
