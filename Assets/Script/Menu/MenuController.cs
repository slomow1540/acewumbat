using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuController : MonoBehaviour
{
    public enum MenuState
    {
        Idle,
        Transition,
        Menu,
        Confirm,
    }

    public MenuState state = MenuState.Idle;

    [Header("UI")]
    public GameObject pressAnyKeyText;
    public GameObject menuPanel;

    public GameManager gameManager;

    public MenuItemAnim[] items;
    public Pointer pointer;
    public BackButton backButton;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip hoverSound;
    public AudioClip clickSound;
    public AudioClip startSound;

    private int currentIndex = 0;

    void Start()
    {
        menuPanel.SetActive(false);
        pressAnyKeyText.SetActive(true);
    }

    void Update()
    {
        switch (state)
        {
            case MenuState.Idle:
                if (Input.anyKeyDown)
                    StartGame();
                break;

            case MenuState.Menu:
                HandleMenuInput();
                break;

            case MenuState.Confirm:
                HandleBackInput();
                break;
        }
    }

    void StartGame()
    {
        state = MenuState.Transition;

        pressAnyKeyText.SetActive(false);
        PlayStart();

        Invoke(nameof(ShowMenu), 0.5f);
    }

    void ShowMenu()
    {
        menuPanel.SetActive(true);

        for (int i = 0; i < items.Length; i++)
        {
            items[i].Show(i * 0.1f);
        }

        pointer.Show(0.2f);
        pointer.Follow(GetCurrentRect());

        state = MenuState.Menu;
    }

    void HandleMenuInput()
    {
        if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
        {
            MoveUp();
        }

        if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow))
        {
            MoveDown();
        }

        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space))
        {
            ConfirmSelection();
        }
    }

    void MoveUp()
    {
        currentIndex--;
        if (currentIndex < 0)
            currentIndex = items.Length - 1;

        PlayHover();
        UpdateSelection();
    }

    void MoveDown()
    {
        currentIndex++;
        if (currentIndex >= items.Length)
            currentIndex = 0;

        PlayHover();
        UpdateSelection();
    }

    void UpdateSelection()
    {
        pointer.Follow(GetCurrentRect());
    }

    void ConfirmSelection()
    {
        state = MenuState.Confirm;

        PlayClick();

        items[currentIndex].Confirm();

        for (int i = 0; i < items.Length; i++)
        {
            items[i].Hide(i * 0.05f);
        }

        pointer.Hide(0.05f);

        backButton.Show();

        StartCoroutine(DelayedSelect());

        gameManager.ApplyMenu((GameManager.MenuType)(currentIndex + 1));
    }

    System.Collections.IEnumerator DelayedSelect()
    {
        yield return new WaitForSeconds(0.3f);
        SelectOption();
    }

    void HandleBackInput()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ResetMenu();
        }
    }

    public void ResetMenu()
    {
        state = MenuState.Menu;

        backButton.Hide();

        for (int i = 0; i < items.Length; i++)
        {
            items[i].Show(i * 0.05f);
        }

        pointer.Show(0.1f);
        pointer.Follow(GetCurrentRect());

        gameManager.ApplyMenu(GameManager.MenuType.Idle);
    }

    void SelectOption()
    {
        switch (currentIndex)
        {
            case 0:
                Debug.Log("Hangar");
                break;

            case 1:
                Debug.Log("Mission");
                break;

            case 2:
                Debug.Log("Survival");
                break;

            case 3:
                Debug.Log("Settings");
                break;

            case 4:
                Debug.Log("Credits");
                break;

            case 5:
                Application.Quit();
                break;
        }
    }

    public void SetIndexFromMouse(int i)
    {
        if (state != MenuState.Menu)
            return;

        if (currentIndex != i)
        {
            currentIndex = i;
            PlayHover();
            UpdateSelection();
        }
    }

    public void SelectFromMouse(int i)
    {
        if (state != MenuState.Menu)
            return;

        currentIndex = i;
        UpdateSelection();

        ConfirmSelection();
    }

    RectTransform GetCurrentRect()
    {
        return items[currentIndex].GetComponent<RectTransform>();
    }

    void PlayHover()
    {
        audioSource.PlayOneShot(hoverSound);
    }

    void PlayClick()
    {
        audioSource.PlayOneShot(clickSound);
    }

    void PlayStart()
    {
        audioSource.PlayOneShot(startSound);
    }
}
