using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MissionManager : MonoBehaviour
{
    [Header("Level Data")]
    public LevelData[] levels;

    [Header("Prefab")]
    public LevelItem itemPrefab;

    [Header("UI")]
    public RectTransform content;
    public Pointer pointer;
    public GeneralUI header;
    public TabletManager tablet;
    public GameObject panel;

    [Header("Scroll")]
    public float itemSpacing = 80f;
    public int visibleCount = 8;

    [Header("Audio")]
    public AudioClip moveSound;
    public AudioClip selectSound;
    private AudioManager audioManager;

    private LevelItem[] items;

    private int currentIndex = 0;
    private int topIndex = 0;
    private int panelOffest = 290;
    private bool isUsingMouse = true;
    private float mouseCooldown = 0.2f;
    private float lastKeyboardTime;

    void Start()
    {
        audioManager = AudioManager.Instance;

        GenerateItems();

        currentIndex = GetDefaultIndex();
        topIndex = Mathf.Clamp(currentIndex, 0, Mathf.Max(0, levels.Length - visibleCount));

        content.anchoredPosition = new Vector2(0, topIndex * itemSpacing);

        UpdateSelection();

        pointer.isBlinking = true;
        panel.SetActive(false);
    }

    void Update()
    {
        HandleInput();
        HandleMouseScroll();

        if (!isUsingMouse && Time.time - lastKeyboardTime > mouseCooldown)
        {
            isUsingMouse = true;
        }
    }

    int GetDefaultIndex()
    {
        int lastPlayed = 0;

        for (int i = 0; i < levels.Length; i++)
        {
            int score = ProgressManager.GetScore(i);

            if (score > 0)
                lastPlayed = i;
        }

        return lastPlayed;
    }

    void GenerateItems()
    {
        items = new LevelItem[levels.Length];

        for (int i = 0; i < levels.Length; i++)
        {
            LevelItem item = Instantiate(itemPrefab, content);

            item.index = i;
            item.manager = this;

            item.SetText("Operation " + i.ToString("00"));

            RectTransform rect = item.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);

            rect.anchoredPosition = new Vector2(20, -15 - i * itemSpacing);

            items[i] = item;

            item.RefreshBasePosition();
        }
    }

    void HandleInput()
    {
        if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
            Move(-1);

        if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow))
            Move(1);

        if (Input.GetKeyDown(KeyCode.Return))
            Confirm();
    }

    void Move(int dir)
    {
        isUsingMouse = false;
        lastKeyboardTime = Time.time;

        currentIndex += dir;

        if (currentIndex < 0)
            currentIndex = levels.Length - 1;

        if (currentIndex >= levels.Length)
            currentIndex = 0;

        audioManager.Play(moveSound);

        if (currentIndex < topIndex)
            topIndex = currentIndex;
        else if (currentIndex > topIndex + visibleCount - 1)
            topIndex = currentIndex - (visibleCount - 1);

        ScrollToTopIndex();
        UpdateSelection();
    }

    void HandleScroll()
    {
        int targetTop = topIndex;

        if (currentIndex < topIndex)
            targetTop = currentIndex;
        else if (currentIndex > topIndex + visibleCount - 1)
            targetTop = currentIndex - (visibleCount - 1);

        if (targetTop != topIndex)
        {
            topIndex = targetTop;
            ScrollToTopIndex();
        }
    }

    IEnumerator SmoothScroll(Vector2 target)
    {
        Vector2 start = content.anchoredPosition;
        float time = 0f;

        while (time < 1f)
        {
            time += Time.deltaTime * 8f;
            content.anchoredPosition = Vector2.Lerp(start, target, time);
            yield return null;
        }

        content.anchoredPosition = target;
    }

    void UpdateSelection()
    {
        for (int i = 0; i < items.Length; i++)
        {
            items[i].SetSelected(i == currentIndex);
        }

        float y = panelOffest - currentIndex * itemSpacing;

        RectTransform ptr = pointer.GetComponent<RectTransform>();
        Vector2 pos = ptr.anchoredPosition;
        pos.y = y;
        ptr.anchoredPosition = pos;

        tablet.SetData(levels[currentIndex], currentIndex);
    }

    void Confirm()
    {
        audioManager.Play(selectSound);

        Debug.Log("Start Level: " + levels[currentIndex].title);

        SceneManager.LoadScene("Level" + currentIndex);
    }

    void HandleMouseScroll()
    {
        Vector2 localMousePos;
        RectTransform viewport = content.parent as RectTransform;

        if (
            !RectTransformUtility.ScreenPointToLocalPointInRectangle(
                viewport,
                Input.mousePosition,
                null,
                out localMousePos
            )
        )
            return;

        float height = viewport.rect.height;

        if (localMousePos.y > height * 0.4f && currentIndex <= topIndex)
        {
            if (topIndex > 0)
            {
                topIndex--;
                ScrollToTopIndex();
            }
        }
        else if (localMousePos.y < -height * 0.4f && currentIndex >= topIndex + visibleCount - 1)
        {
            if (topIndex < levels.Length - visibleCount)
            {
                topIndex++;
                ScrollToTopIndex();
            }
        }
    }

    void ScrollToTopIndex()
    {
        Vector2 target = new Vector2(0, topIndex * itemSpacing);

        StopAllCoroutines();
        StartCoroutine(SmoothScroll(target));
    }

    public void SetIndexFromMouse(int i)
    {
        if (!isUsingMouse)
            return;

        if (currentIndex == i)
            return;

        currentIndex = i;

        if (currentIndex < topIndex)
            topIndex = currentIndex;
        else if (currentIndex > topIndex + visibleCount - 1)
            topIndex = currentIndex - (visibleCount - 1);

        ScrollToTopIndex();
        UpdateSelection();
    }

    public void SelectFromMouse(int i)
    {
        currentIndex = i;

        UpdateSelection();
        Confirm();
    }

    public void ShowAll()
    {
        panel.SetActive(true);

        tablet.Show();
        header.Show();

        for (int i = 0; i < items.Length; i++)
        {
            items[i].Show(i * 0.05f, -100f);
        }
    }

    public void HideAll()
    {
        tablet.Hide();
        header.Hide();

        for (int i = 0; i < items.Length; i++)
        {
            items[i].Hide(i * 0.02f);
        }

        panel.SetActive(false);
    }
}
