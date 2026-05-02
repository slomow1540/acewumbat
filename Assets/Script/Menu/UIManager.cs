using System;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    public enum MenuState
    {
        Idle,
        Menu,
        Confirm,
    }

    public MenuState state = MenuState.Idle;

    [Header("Managers")]
    public MenuManager menuManager;
    public MissionManager missionManager;
    public SettingManager settingManager;
    public CreditManager creditManager;
    public GameManager gameManager;

    private int currentIndex = 0;
    private Action[] menuActions;
    private Action[] menuExitActions;

    void Start()
    {
        menuActions = new Action[]
        {
            OpenHangar,
            OpenMission,
            OpenSurvival,
            OpenSettings,
            OpenCredits,
            QuitGame,
        };

        menuExitActions = new Action[]
        {
            CloseHangar,
            CloseMission,
            CloseSurvival,
            CloseSettings,
            CloseCredits,
            null,
        };
        menuManager.Init();
    }

    void Update()
    {
        if (state == MenuState.Idle && Input.anyKeyDown)
        {
            menuManager.StartMenu();
            state = MenuState.Menu;
        }
        else if (state == MenuState.Menu)
        {
            HandleInput();
        }
        else if (state == MenuState.Confirm)
        {
            if (Input.GetKeyDown(KeyCode.Escape))
                ResetMenu();
        }
    }

    void HandleInput()
    {
        if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
        {
            currentIndex = menuManager.MoveUp(currentIndex);
        }

        if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow))
        {
            currentIndex = menuManager.MoveDown(currentIndex);
        }

        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space))
        {
            Confirm();
        }
    }

    void Confirm()
    {
        state = MenuState.Confirm;

        menuManager.Confirm(currentIndex);

        gameManager.ApplyMenu((GameManager.MenuType)(currentIndex + 1));

        menuActions[currentIndex]?.Invoke();
    }

    public void ResetMenu()
    {
        state = MenuState.Menu;

        menuExitActions[currentIndex]?.Invoke();

        menuManager.Reset(currentIndex);

        gameManager.ApplyMenu(GameManager.MenuType.Idle);
    }

    public void SetIndexFromMouse(int i)
    {
        if (state != MenuState.Menu)
            return;

        if (currentIndex != i)
        {
            currentIndex = i;
            menuManager.SetIndex(currentIndex);
        }
    }

    public void SelectFromMouse(int i)
    {
        if (state != MenuState.Menu)
            return;

        currentIndex = i;

        menuManager.SetIndex(currentIndex);

        Confirm();
    }

    // Show
    void OpenHangar() => Debug.Log("Hangar");

    void OpenMission() => missionManager.ShowAll();

    void OpenSurvival() => Debug.Log("Survival");

    void OpenSettings() => settingManager.Show();

    void OpenCredits()
    {
        creditManager.Show();
        creditManager.UpdateText();
    }

    void QuitGame() => Application.Quit();

    // Hide
    void CloseHangar() { }

    void CloseMission() => missionManager.HideAll();

    void CloseSurvival() { }

    void CloseSettings()
    {
        settingManager.Hide();
    }

    void CloseCredits()
    {
        creditManager.Hide();
        creditManager.ResetPos();
    }
}
