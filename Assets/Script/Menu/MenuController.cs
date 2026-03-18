using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class MenuController : MonoBehaviour
{
    public enum MenuState { Idle, Transition, Menu }
    public MenuState state = MenuState.Idle;

    public GameObject pressAnyKeyText;
    public GameObject menuPanel;

    public TextMeshProUGUI[] pointers;
    public MenuItemAnim[] anims;

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
                {
                    StartGame();
                }
                break;

            case MenuState.Menu:
                HandleMenuInput();
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

        for (int i = 0; i < anims.Length; i++)
        {
            anims[i].PlayIntro(i * 0.1f);
        }

        state = MenuState.Menu;

        UpdateMenu();
    }

    void HandleMenuInput()
    {
        if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
        {
            currentIndex--;
            if (currentIndex < 0) currentIndex = anims.Length - 1;

            PlayHover();
            UpdateMenu();
        }

        if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow))
        {
            currentIndex++;
            if (currentIndex >= anims.Length) currentIndex = 0;

            PlayHover();
            UpdateMenu();
        }

        if (Input.GetKeyDown(KeyCode.Return))
        {
            SelectOption();
        }
    }

    void UpdateMenu()
    {
        for (int i = 0; i < anims.Length; i++)
        {
            pointers[i].gameObject.SetActive(i == currentIndex);
            anims[i].SetActive(i == currentIndex);
        }
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

    void SelectOption()
    {
        PlayClick();

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
        if (currentIndex != i)
        {
            currentIndex = i;
            PlayHover();
            UpdateMenu();
        }
    }

    public void SelectFromMouse(int i)
    {
        currentIndex = i;
        UpdateMenu();
        SelectOption();
    }
}

