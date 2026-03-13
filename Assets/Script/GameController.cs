using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Manages game state, entity tracking, and win/lose conditions
/// </summary>
public class GameController : MonoBehaviour
{
    [System.Serializable]
    public class EntityRecord
    {
        public Health healthSystem;
        public GameObject gameObject;
        public string tag;
        public bool isPlayer;
    }

    [Header("Audio")]
    [Tooltip("Sound played when player wins")]
    public AudioClip victorySound;

    [Tooltip("Sound played when player loses")]
    public AudioClip defeatSound;

    [Header("Fade Settings")]
    [Tooltip("Time to wait before showing result screen after game end")]
    public float resultScreenDelay = 1f;

    [Tooltip("Duration of fade effect for each step")]
    public float fadeDuration = 1f;

    [Header("Scene Settings")]
    [Tooltip("Name of the main menu scene")]
    public string mainMenuSceneName = "MainMenu";

    [Header("UI References")]
    [Tooltip("Root GameObject of the result UI (should contain Canvas)")]
    public GameObject resultCanvasObject;

    [Tooltip("Background panel Image (will fade to black)")]
    public Image resultPanel; // should be black; alpha controlled by script

    [Tooltip("Result text (e.g. 'VICTORY' / 'DEFEATED')")]
    public TextMeshProUGUI resultText;

    [Tooltip("Stat text to show additional info")]
    public TextMeshProUGUI statText;

    [Tooltip("Restart button GameObject")]
    public Button restartButton;

    [Tooltip("Main menu button GameObject")]
    public Button mainMenuButton;

    [Tooltip("target kill bar")]
    public GameObject TGTkill;

    [Header("UI Colors")]
    [Tooltip("Alpha target for the black background (0..1)")]
    [Range(0f, 1f)]
    public float backgroundTargetAlpha = 0.85f;

    // Private variables
    private List<EntityRecord> allEntities = new List<EntityRecord>();
    private EntityRecord playerEntity;
    private int totalEnemies = 0;
    private int defeatedEnemies = 0;
    private bool gameEnded = false;

    // CanvasGroups for fading text & stat
    private CanvasGroup resultTextCanvasGroup;
    private CanvasGroup statTextCanvasGroup;

    private static GameController instance;

    private void Awake()
    {
        // Singleton pattern
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
    }

    private void Start()
    {
        PrepareResultUI();
        if (resultPanel != null)
        {
            Color c = resultPanel.color;
            c.a = 0f;
            resultPanel.color = c;
        }

        if (resultTextCanvasGroup != null)
            resultTextCanvasGroup.alpha = 0f;

        if (statTextCanvasGroup != null)
            statTextCanvasGroup.alpha = 0f;

        if (restartButton != null)
            restartButton.gameObject.SetActive(false);

        if (mainMenuButton != null)
            mainMenuButton.gameObject.SetActive(false);
    }

    /// <summary>
    /// Register an entity with the GameController
    /// </summary>
    public void RegisterEntity(Health healthSystem, string entityTag, bool isPlayer)
    {
        if (healthSystem == null) return;

        EntityRecord record = new EntityRecord
        {
            healthSystem = healthSystem,
            gameObject = healthSystem.gameObject,
            tag = entityTag,
            isPlayer = isPlayer
        };

        allEntities.Add(record);

        if (isPlayer)
        {
            playerEntity = record;
            Debug.Log($"[GameController] Player registered: {healthSystem.gameObject.name}");
        }
        else if (entityTag == "Enemy")
        {
            totalEnemies++;
            Debug.Log($"[GameController] Enemy registered: {healthSystem.gameObject.name} (Total: {totalEnemies})");
        }
    }

    /// <summary>
    /// Called when an entity dies
    /// </summary>
    public void NotifyEntityDeath(Health healthSystem, string entityTag, bool isPlayer)
    {
        if (gameEnded) return;

        if (isPlayer)
        {
            Debug.Log("[GameController] Player has been defeated!");
            HandleGameLoss();
        }
        else if (entityTag == "Enemy")
        {
            defeatedEnemies++;
            PopUpKillConfirm();
            Debug.Log($"[GameController] Enemy defeated! ({defeatedEnemies}/{totalEnemies})");

            if (defeatedEnemies >= totalEnemies)
            {
                Debug.Log("[GameController] All enemies defeated!");
                HandleGameVictory();
            }
        }
    }

    public void PopUpKillConfirm()
    {
        StartCoroutine(ShowKillConfirm());
    }

    IEnumerator ShowKillConfirm()
    {
        TGTkill.SetActive(true);
        yield return new WaitForSeconds(1.5f);
        TGTkill.SetActive(false);
    }

    /// <summary>
    /// Handle victory condition
    /// </summary>
    private void HandleGameVictory()
    {
        gameEnded = true;

        if (victorySound != null)
        {
            AudioSource.PlayClipAtPoint(victorySound, Vector3.zero);
        }

        StartCoroutine(ShowResultSequence(true));
    }

    /// <summary>
    /// Handle loss condition
    /// </summary>
    private void HandleGameLoss()
    {
        gameEnded = true;

        if (defeatSound != null)
        {
            AudioSource.PlayClipAtPoint(defeatSound, Vector3.zero);
        }

        StartCoroutine(ShowResultSequence(false));
    }

    /// <summary>
    /// Shows result UI with sequence: background fade (black) -> result text fade -> stat text fade -> show buttons
    /// </summary>
    private IEnumerator ShowResultSequence(bool isVictory)
    {
        ImprovedPlaneController pc = playerEntity.gameObject.GetComponent<ImprovedPlaneController>();

        if (pc != null)
        {
            pc.allowMouseControl = false;
        }

        yield return new WaitForSeconds(resultScreenDelay);

        // Prepare texts
        if (resultText != null)
            resultText.text = isVictory ? "VICTORY!" : "DEFEATED!";
        if (statText != null)
            statText.text = $"Enemies: {defeatedEnemies}/{Mathf.Max(1, totalEnemies)}";

        // Ensure buttons hidden before fade
        if (restartButton != null)
            restartButton.gameObject.SetActive(false);
        if (mainMenuButton != null)
            mainMenuButton.gameObject.SetActive(false);

        // Fade background panel
        if (resultPanel != null)
        {
            Color col = resultPanel.color;
            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                col.a = Mathf.Lerp(0f, backgroundTargetAlpha, Mathf.Clamp01(elapsed / fadeDuration));
                resultPanel.color = col;
                yield return null;
            }
            col.a = backgroundTargetAlpha;
            resultPanel.color = col;
        }

        // Fade in result text
        if (resultTextCanvasGroup != null)
            yield return StartCoroutine(FadeCanvasGroup(resultTextCanvasGroup, 0f, 1f, fadeDuration));
        else if (resultText != null)
            yield return StartCoroutine(FadeTMPAlpha(resultText, 0f, 1f, fadeDuration));

        // Fade in stat text
        if (statTextCanvasGroup != null)
            yield return StartCoroutine(FadeCanvasGroup(statTextCanvasGroup, 0f, 1f, fadeDuration));
        else if (statText != null)
            yield return StartCoroutine(FadeTMPAlpha(statText, 0f, 1f, fadeDuration));

        // Show buttons after texts
        if (restartButton != null)
        {
            restartButton.gameObject.SetActive(true);
            restartButton.interactable = true;
        }
        if (mainMenuButton != null)
        {
            mainMenuButton.gameObject.SetActive(true);
            mainMenuButton.interactable = true;
        }
    }

    #region Helpers & UI Setup

    /// <summary>
    /// Prepare references: if inspector references exist, use them; otherwise create the minimal UI.
    /// Also ensures CanvasGroups for fading text.
    /// </summary>
    private void PrepareResultUI()
    {
        // If a resultCanvasObject is assigned, use it. Otherwise create one.
        if (resultCanvasObject == null)
        {
            // Create main canvas
            resultCanvasObject = new GameObject("ResultCanvas");
            Canvas canvas = resultCanvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 9999;

            CanvasScaler scaler = resultCanvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;

            resultCanvasObject.AddComponent<CanvasGroup>(); // keep for completeness

            // Create background panel
            GameObject panelObj = new GameObject("Panel");
            panelObj.transform.SetParent(resultCanvasObject.transform, false);

            RectTransform panelRect = panelObj.AddComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;

            resultPanel = panelObj.AddComponent<Image>();
            resultPanel.color = new Color(0, 0, 0, 0f); // start transparent black

            // Create result text
            GameObject resultTextObj = new GameObject("ResultText");
            resultTextObj.transform.SetParent(panelObj.transform, false);

            RectTransform resultTextRect = resultTextObj.AddComponent<RectTransform>();
            resultTextRect.anchoredPosition = new Vector2(0, 100);
            resultTextRect.sizeDelta = new Vector2(800, 200);

            resultText = resultTextObj.AddComponent<TextMeshProUGUI>();
            resultText.text = "";
            resultText.alignment = TextAlignmentOptions.Center;
            resultText.fontSize = 100;

            // Create stat text
            GameObject statTextObj = new GameObject("StatText");
            statTextObj.transform.SetParent(panelObj.transform, false);

            RectTransform statTextRect = statTextObj.AddComponent<RectTransform>();
            statTextRect.anchoredPosition = new Vector2(0, 0);
            statTextRect.sizeDelta = new Vector2(800, 80);

            statText = statTextObj.AddComponent<TextMeshProUGUI>();
            statText.text = "";
            statText.alignment = TextAlignmentOptions.Center;
            statText.fontSize = 40;

            // Create button container
            GameObject buttonContainerObj = new GameObject("ButtonContainer");
            buttonContainerObj.transform.SetParent(panelObj.transform, false);

            RectTransform containerRect = buttonContainerObj.AddComponent<RectTransform>();
            containerRect.anchoredPosition = new Vector2(0, -100);
            containerRect.sizeDelta = new Vector2(600, 150);

            HorizontalLayoutGroup layoutGroup = buttonContainerObj.AddComponent<HorizontalLayoutGroup>();
            layoutGroup.spacing = 20;
            layoutGroup.childForceExpandWidth = true;
            layoutGroup.childForceExpandHeight = true;

            // Create restart button
            restartButton = CreateButton(buttonContainerObj, "RestartButton", "RESTART", RestartGame);

            // Create main menu button
            mainMenuButton = CreateButton(buttonContainerObj, "MainMenuButton", "MAIN MENU", GoToMainMenu);
        }
        else
        {
            // If resultPanel is assigned, force it to have transparent alpha initially and set to black
            if (resultPanel != null)
            {
                Color c = resultPanel.color;
                c.r = 0f; c.g = 0f; c.b = 0f; // force black
                c.a = 0f;
                resultPanel.color = c;
            }

            // If resultText exists, ensure a CanvasGroup exists for fade control
            if (resultText != null)
            {
                resultTextCanvasGroup = resultText.GetComponent<CanvasGroup>();
                if (resultTextCanvasGroup == null)
                    resultTextCanvasGroup = resultText.gameObject.AddComponent<CanvasGroup>();
                resultTextCanvasGroup.alpha = 0f;
            }

            // If statText exists, ensure a CanvasGroup exists for fade control
            if (statText != null)
            {
                statTextCanvasGroup = statText.GetComponent<CanvasGroup>();
                if (statTextCanvasGroup == null)
                    statTextCanvasGroup = statText.gameObject.AddComponent<CanvasGroup>();
                statTextCanvasGroup.alpha = 0f;
            }

            // If buttons were assigned, start hidden & not interactable
            if (restartButton != null)
            {
                restartButton.interactable = false;
                restartButton.gameObject.SetActive(false);
            }
            if (mainMenuButton != null)
            {
                mainMenuButton.interactable = false;
                mainMenuButton.gameObject.SetActive(false);
            }
        }

        // If we created the typography elements above, ensure CanvasGroups exist for them too
        if (resultText != null && resultTextCanvasGroup == null)
        {
            resultTextCanvasGroup = resultText.GetComponent<CanvasGroup>() ?? resultText.gameObject.AddComponent<CanvasGroup>();
            resultTextCanvasGroup.alpha = 0f;
        }
        if (statText != null && statTextCanvasGroup == null)
        {
            statTextCanvasGroup = statText.GetComponent<CanvasGroup>() ?? statText.gameObject.AddComponent<CanvasGroup>();
            statTextCanvasGroup.alpha = 0f;
        }
    }

    /// <summary>
    /// Helper method to create a button (used only when creating fallback UI)
    /// </summary>
    private Button CreateButton(GameObject parent, string name, string label, UnityEngine.Events.UnityAction callback)
    {
        GameObject buttonObj = new GameObject(name);
        buttonObj.transform.SetParent(parent.transform, false);

        RectTransform buttonRect = buttonObj.AddComponent<RectTransform>();
        buttonRect.sizeDelta = new Vector2(200, 80);

        Image buttonImage = buttonObj.AddComponent<Image>();
        buttonImage.color = new Color(0.2f, 0.2f, 0.2f, 1f);

        Button button = buttonObj.AddComponent<Button>();
        button.onClick.AddListener(callback);

        // Add hover effect
        ColorBlock colors = button.colors;
        colors.normalColor = new Color(0.2f, 0.2f, 0.2f, 1f);
        colors.highlightedColor = new Color(0.4f, 0.4f, 0.4f, 1f);
        colors.pressedColor = new Color(0.1f, 0.1f, 0.1f, 1f);
        button.colors = colors;

        // Create text for button
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(buttonObj.transform, false);

        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        TextMeshProUGUI buttonText = textObj.AddComponent<TextMeshProUGUI>();
        buttonText.text = label;
        buttonText.alignment = TextAlignmentOptions.Center;
        buttonText.fontSize = 40;

        return button;
    }

    #endregion

    #region Fade Coroutines

    private IEnumerator FadeCanvasGroup(CanvasGroup cg, float from, float to, float duration)
    {
        float elapsed = 0f;
        cg.alpha = from;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            cg.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(elapsed / duration));
            yield return null;
        }
        cg.alpha = to;
    }

    // Fallback for TMP text if CanvasGroup was not present (rare because we add CanvasGroup)
    private IEnumerator FadeTMPAlpha(TextMeshProUGUI tmp, float from, float to, float duration)
    {
        float elapsed = 0f;
        Color c = tmp.color;
        c.a = from;
        tmp.color = c;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            c.a = Mathf.Lerp(from, to, Mathf.Clamp01(elapsed / duration));
            tmp.color = c;
            yield return null;
        }
        c.a = to;
        tmp.color = c;
    }

    #endregion

    /// <summary>
    /// Restart the current game scene
    /// </summary>
    private void RestartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    /// <summary>
    /// Go to main menu
    /// </summary>
    private void GoToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuSceneName);
    }

    /// <summary>
    /// Get game status
    /// </summary>
    public bool IsGameEnded()
    {
        return gameEnded;
    }

    /// <summary>
    /// Get player status
    /// </summary>
    public bool IsPlayerAlive()
    {
        return playerEntity != null && playerEntity.healthSystem != null && playerEntity.healthSystem.IsAlive();
    }

    /// <summary>
    /// Get enemy count
    /// </summary>
    public int GetTotalEnemies()
    {
        return totalEnemies;
    }

    /// <summary>
    /// Get defeated enemy count
    /// </summary>
    public int GetDefeatedEnemies()
    {
        return defeatedEnemies;
    }
}