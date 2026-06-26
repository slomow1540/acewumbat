using System;
using System.Collections;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    public enum MenuState
    {
        Idle,
        Menu,
        Confirm,
        Quit,
    }

    public MenuState state = MenuState.Idle;

    [Header("Managers")]
    public MenuManager menuManager;
    public HangarManager hangarManager;
    public MissionManager missionManager;
    public SettingManager settingManager;
    public CreditManager creditManager;
    public GameManager gameManager;

    private int currentIndex = 0;
    private int quitIndex;
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
            OpenQuit,
        };

        menuExitActions = new Action[]
        {
            CloseHangar,
            CloseMission,
            CloseSurvival,
            CloseSettings,
            CloseCredits,
            CloseQuit,
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
        else if (state == MenuState.Quit)
        {
            HandleQuit();
        }
    }

    void HandleInput()
    {
        HandleVerticalMenu(ref currentIndex, 6, i => menuManager.SetIndex(i), Confirm);
    }

    void HandleQuit()
    {
        HandleVerticalMenu(
            ref quitIndex,
            2,
            i => menuManager.SetQuitIndex(i),
            ConfirmQuit,
            CloseQuit
        );
    }

    void Confirm()
    {
        state = MenuState.Confirm;

        menuManager.HideMenuWithSound(currentIndex);

        gameManager.ApplyMenu((GameManager.MenuType)(currentIndex + 1));

        menuActions[currentIndex]?.Invoke();

        if (currentIndex == 0)
        {
            StartCoroutine(ShowBackAfterHangarEnter());
        }
        else
        {
            menuManager.ShowBack();
        }
    }

    void ConfirmQuit()
    {
        if (quitIndex == 0)
        {
            Application.Quit();
        }
        else
        {
            CloseQuit();
        }
    }

    IEnumerator ShowBackAfterHangarEnter()
    {
        yield return new WaitForSeconds(hangarManager.enterDuration);
        menuManager.ShowBack();
    }

    public void ResetMenu()
    {
        StartCoroutine(ResetMenuRoutine());
    }

    IEnumerator ResetMenuRoutine()
    {
        state = MenuState.Confirm;

        menuManager.HideBack();
        menuExitActions[currentIndex]?.Invoke();

        if (currentIndex == 0)
        {
            yield return new WaitForSeconds(hangarManager.exitDuration);
        }
        else
        {
            yield return new WaitForSeconds(0.4f);
        }

        bool usesCamera = currentIndex != 3 && currentIndex != 5;

        if (usesCamera)
        {
            gameManager.ApplyMenu(GameManager.MenuType.Idle);
            yield return new WaitForSeconds(gameManager.cameraMover.moveDuration);
        }

        state = MenuState.Menu;

        menuManager.Reset(currentIndex);
    }

    public void SetIndexFromMouse(int i)
    {
        if (state == MenuState.Menu)
        {
            if (currentIndex != i)
            {
                currentIndex = i;
                menuManager.SetIndex(i);
            }
        }
        else if (state == MenuState.Quit)
        {
            if (quitIndex != i)
            {
                quitIndex = i;
                menuManager.SetQuitIndex(i);
            }
        }
    }

    public void SelectFromMouse(int i)
    {
        if (state == MenuState.Menu)
        {
            currentIndex = i;

            menuManager.SetIndex(i);

            Confirm();
        }
        else if (state == MenuState.Quit)
        {
            quitIndex = i;

            menuManager.SetQuitIndex(i);

            ConfirmQuit();
        }
    }

    void OpenHangar()
    {
        hangarManager.Show();
    }

    void OpenMission() => missionManager.ShowAll();

    void OpenSurvival() => Debug.Log("Survival");

    void OpenSettings() => settingManager.Show();

    void OpenCredits()
    {
        creditManager.Show();
        creditManager.UpdateText();
    }

    void OpenQuit()
    {
        quitIndex = 0;

        menuManager.ShowQuit();

        state = MenuState.Quit;
    }

    // Hide
    void CloseHangar()
    {
        hangarManager.Hide();
    }

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

    void CloseQuit()
    {
        menuManager.HideQuit();

        state = MenuState.Menu;

        menuManager.SetIndex(currentIndex);
    }

    void HandleVerticalMenu(
        ref int index,
        int count,
        Action<int> onChanged,
        Action onConfirm,
        Action onCancel = null
    )
    {
        if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
        {
            index = (index - 1 + count) % count;
            onChanged?.Invoke(index);
        }

        if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow))
        {
            index = (index + 1) % count;
            onChanged?.Invoke(index);
        }

        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space))
        {
            onConfirm?.Invoke();
        }

        if (onCancel != null && Input.GetKeyDown(KeyCode.Escape))
        {
            onCancel.Invoke();
        }
    }
}
