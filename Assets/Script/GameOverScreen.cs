using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Handles the game over screen UI and navigation
/// </summary>
public class GameOverScreen : MonoBehaviour
{
    [Header("UI Elements")]
    public TextMeshProUGUI resultText;
    public TextMeshProUGUI statsText;
    public Button restartButton;
    public Button mainMenuButton;

    [Header("Scene Names")]
    [Tooltip("Name of the main game scene")]
    public string gameSceneName = "GameScene";

    [Tooltip("Name of the main menu scene")]
    public string mainMenuSceneName = "MainMenu";

    private void Awake()
    {
        // Setup button listeners
        if (restartButton != null)
        {
            restartButton.onClick.AddListener(RestartGame);
        }

        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.AddListener(GoToMainMenu);
        }

        // Display appropriate message
        DisplayGameOverInfo();
    }

    private void DisplayGameOverInfo()
    {
        // You can pass game data through a static reference or singleton
        // For now, we'll display generic text

        if (resultText != null)
        {
            // This could be customized based on victory/defeat conditions
            resultText.text = "Game Over";
        }

        if (statsText != null)
        {
            statsText.text = "Check your performance!";
        }
    }

    public void RestartGame()
    {
        Time.timeScale = 1f; // Ensure time is running
        SceneManager.LoadScene(gameSceneName);
    }

    public void GoToMainMenu()
    {
        Time.timeScale = 1f; // Ensure time is running
        SceneManager.LoadScene(mainMenuSceneName);
    }

    private void OnDestroy()
    {
        if (restartButton != null)
        {
            restartButton.onClick.RemoveListener(RestartGame);
        }

        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.RemoveListener(GoToMainMenu);
        }
    }
}