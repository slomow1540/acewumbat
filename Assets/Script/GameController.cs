using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Manages game state, entity tracking, win/lose conditions, mission time, triggers, and music
/// </summary>
public class GameController : MonoBehaviour
{
    #region Nested Classes

    [System.Serializable]
    public class EntityRecord
    {
        public Health healthSystem;
        public GameObject gameObject;
        public string tag;
        public bool isPlayer;
    }

    [System.Serializable]
    public class MissionTrigger
    {
        [Header("Trigger Info")]
        public string triggerName = "New Trigger";
        public bool isActive = true;
        public bool hasTriggered = false;

        [Header("Trigger Condition")]
        public TriggerCondition condition = TriggerCondition.TimeElapsed;

        [Tooltip("For TimeElapsed: seconds to wait")]
        public float timeThreshold = 60f;

        [Tooltip("For EnemiesKilled: number of enemies")]
        public int enemyCountThreshold = 5;

        [Tooltip("For CheckpointReached: checkpoint ID")]
        public string checkpointID = "Checkpoint1";

        [Header("Trigger Actions")]
        public List<TriggerAction> actions = new List<TriggerAction>();

        [Header("Debug")]
        public bool showDebugMessages = true;
    }

    public enum TriggerCondition
    {
        TimeElapsed,        // After X seconds
        EnemiesKilled,      // After X enemies killed
        CheckpointReached,  // When checkpoint called
        PlayerHealthBelow,  // When player HP < X%
        AllEnemiesDead      // When all enemies defeated
    }

    [System.Serializable]
    public class TriggerAction
    {
        public ActionType actionType = ActionType.SpawnEnemies;

        [Header("Spawn Settings")]
        [Tooltip("For SpawnEnemies: prefab to spawn")]
        public GameObject enemyPrefab;
        [Tooltip("Spawn position")]
        public Vector3 spawnPosition;
        [Tooltip("Spawn rotation")]
        public Vector3 spawnRotation;
        [Tooltip("Number to spawn")]
        public int spawnCount = 1;
        [Tooltip("Spread radius for multiple spawns")]
        public float spawnSpread = 10f;

        [Header("Point Settings")]
        [Tooltip("For GivePoints: amount")]
        public int pointAmount = 100;

        [Header("Message Settings")]
        [Tooltip("For ShowMessage: message text")]
        public string messageText = "Objective Complete!";
        [Tooltip("Message duration (seconds)")]
        public float messageDuration = 3f;

        [Header("Audio Settings")]
        [Tooltip("For PlaySound: audio clip")]
        public AudioClip audioClip;
    }

    public enum ActionType
    {
        SpawnEnemies,
        GivePoints,
        ShowMessage,
        PlaySound,
        ChangeMusic
    }

    #endregion

    [Header("Audio")]
    [Tooltip("Sound played when player wins")]
    public AudioClip victorySound;

    [Tooltip("Sound played when player loses")]
    public AudioClip defeatSound;

    [Header("Background Music")]
    [Tooltip("List of background music tracks")]
    public List<AudioClip> musicTracks = new List<AudioClip>();

    [Tooltip("Current music track index")]
    public int currentMusicIndex = 0;

    [Tooltip("Music volume (0-1)")]
    [Range(0f, 1f)]
    public float musicVolume = 0.5f;

    [Tooltip("Loop music tracks")]
    public bool loopMusic = true;

    [Tooltip("Fade in duration (seconds)")]
    public float musicFadeInDuration = 2f;

    [Tooltip("Fade out duration (seconds)")]
    public float musicFadeOutDuration = 1.5f;

    private AudioSource musicSource;
    private Coroutine musicFadeCoroutine;

    [Header("Mission Timer")]
    [Tooltip("Track mission time")]
    public bool trackMissionTime = true;

    [Tooltip("Format: mm:ss or mm:ss.ff")]
    public bool showMilliseconds = false;

    private float missionStartTime;
    private float missionEndTime;
    private float missionDuration;

    [Header("Mission Triggers")]
    [Tooltip("List of mission triggers")]
    public List<MissionTrigger> missionTriggers = new List<MissionTrigger>();

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
    public Image resultPanel;

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

    [Tooltip("Message display text")]
    public TextMeshProUGUI messageText;

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
    private int PointObtained = 0;

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
        // Start mission timer
        if (trackMissionTime)
        {
            missionStartTime = Time.time;
        }

        // Setup music
        SetupBackgroundMusic();

        // Start playing music
        if (musicTracks.Count > 0 && currentMusicIndex >= 0 && currentMusicIndex < musicTracks.Count)
        {
            PlayMusic(currentMusicIndex);
        }

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

        if (messageText != null)
            messageText.gameObject.SetActive(false);
    }

    private void Update()
    {
        // Update triggers
        if (!gameEnded)
        {
            UpdateTriggers();
        }
    }

    #region Background Music

    private void SetupBackgroundMusic()
    {
        musicSource = gameObject.GetComponent<AudioSource>();
        if (musicSource == null)
        {
            musicSource = gameObject.AddComponent<AudioSource>();
        }

        musicSource.loop = loopMusic;
        musicSource.volume = musicVolume;
        musicSource.playOnAwake = false;
    }

    public void PlayMusic(int trackIndex, bool fadeIn = true)
    {
        if (trackIndex < 0 || trackIndex >= musicTracks.Count)
        {
            Debug.LogWarning($"Invalid music track index: {trackIndex}");
            return;
        }

        // Stop any ongoing fade
        if (musicFadeCoroutine != null)
        {
            StopCoroutine(musicFadeCoroutine);
            musicFadeCoroutine = null;
        }

        currentMusicIndex = trackIndex;
        musicSource.clip = musicTracks[trackIndex];

        if (fadeIn)
        {
            musicSource.volume = 0f;
            musicSource.Play();
            musicFadeCoroutine = StartCoroutine(FadeMusic(0f, musicVolume, musicFadeInDuration));
            Debug.Log($"Playing music track (fade in): {musicTracks[trackIndex].name}");
        }
        else
        {
            musicSource.volume = musicVolume;
            musicSource.Play();
            Debug.Log($"Playing music track: {musicTracks[trackIndex].name}");
        }
    }

    public void StopMusic(bool fadeOut = true)
    {
        if (musicSource != null)
        {
            // Stop any ongoing fade
            if (musicFadeCoroutine != null)
            {
                StopCoroutine(musicFadeCoroutine);
                musicFadeCoroutine = null;
            }

            if (fadeOut && musicSource.isPlaying)
            {
                musicFadeCoroutine = StartCoroutine(FadeMusicAndStop());
            }
            else
            {
                musicSource.Stop();
            }
        }
    }

    private IEnumerator FadeMusic(float fromVolume, float toVolume, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            musicSource.volume = Mathf.Lerp(fromVolume, toVolume, elapsed / duration);
            yield return null;
        }
        musicSource.volume = toVolume;
        musicFadeCoroutine = null;
    }

    private IEnumerator FadeMusicAndStop()
    {
        yield return StartCoroutine(FadeMusic(musicSource.volume, 0f, musicFadeOutDuration));
        musicSource.Stop();
        musicSource.volume = musicVolume; // Reset for next play
        musicFadeCoroutine = null;
    }

    public void SetMusicVolume(float volume)
    {
        musicVolume = Mathf.Clamp01(volume);
        if (musicSource != null)
        {
            musicSource.volume = musicVolume;
        }
    }

    public void ChangeMusicWithCrossfade(int newTrackIndex)
    {
        if (newTrackIndex < 0 || newTrackIndex >= musicTracks.Count)
        {
            Debug.LogWarning($"Invalid music track index: {newTrackIndex}");
            return;
        }

        // If music not playing, just play new track
        if (!musicSource.isPlaying)
        {
            PlayMusic(newTrackIndex, fadeIn: true);
            return;
        }

        // Crossfade
        StartCoroutine(CrossfadeMusic(newTrackIndex));
    }

    private IEnumerator CrossfadeMusic(int newTrackIndex)
    {
        // Fade out current track
        yield return StartCoroutine(FadeMusic(musicSource.volume, 0f, musicFadeOutDuration));

        // Switch track
        musicSource.clip = musicTracks[newTrackIndex];
        currentMusicIndex = newTrackIndex;
        musicSource.Play();

        // Fade in new track
        yield return StartCoroutine(FadeMusic(0f, musicVolume, musicFadeInDuration));

        Debug.Log($"Crossfaded to music track: {musicTracks[newTrackIndex].name}");
    }

    #endregion

    #region Mission Timer

    public float GetMissionTime()
    {
        if (!trackMissionTime) return 0f;

        if (gameEnded)
        {
            return missionDuration;
        }
        else
        {
            return Time.time - missionStartTime;
        }
    }

    public string GetFormattedMissionTime()
    {
        float time = GetMissionTime();
        int minutes = Mathf.FloorToInt(time / 60f);
        int seconds = Mathf.FloorToInt(time % 60f);
        int milliseconds = Mathf.FloorToInt((time * 100f) % 100f);

        if (showMilliseconds)
        {
            return $"{minutes:00}:{seconds:00}.{milliseconds:00}";
        }
        else
        {
            return $"{minutes:00}:{seconds:00}";
        }
    }

    #endregion

    #region Trigger System

    private void UpdateTriggers()
    {
        foreach (MissionTrigger trigger in missionTriggers)
        {
            if (!trigger.isActive || trigger.hasTriggered)
                continue;

            if (CheckTriggerCondition(trigger))
            {
                ExecuteTrigger(trigger);
            }
        }
    }

    private bool CheckTriggerCondition(MissionTrigger trigger)
    {
        switch (trigger.condition)
        {
            case TriggerCondition.TimeElapsed:
                return GetMissionTime() >= trigger.timeThreshold;

            case TriggerCondition.EnemiesKilled:
                return defeatedEnemies >= trigger.enemyCountThreshold;

            case TriggerCondition.PlayerHealthBelow:
                if (playerEntity != null && playerEntity.healthSystem != null)
                {
                    float healthPercent = playerEntity.healthSystem.GetHealthPercent();
                    return healthPercent <= (trigger.timeThreshold / 100f); // Reuse timeThreshold as percentage
                }
                return false;

            case TriggerCondition.AllEnemiesDead:
                return defeatedEnemies >= totalEnemies && totalEnemies > 0;

            case TriggerCondition.CheckpointReached:
                // This is called manually via TriggerCheckpoint()
                return false;

            default:
                return false;
        }
    }

    private void ExecuteTrigger(MissionTrigger trigger)
    {
        trigger.hasTriggered = true;

        if (trigger.showDebugMessages)
        {
            Debug.Log($"[GameController] Trigger '{trigger.triggerName}' executed!");
        }

        foreach (TriggerAction action in trigger.actions)
        {
            ExecuteAction(action);
        }
    }

    private void ExecuteAction(TriggerAction action)
    {
        switch (action.actionType)
        {
            case ActionType.SpawnEnemies:
                SpawnEnemies(action);
                break;

            case ActionType.GivePoints:
                GivePoints(action.pointAmount);
                break;

            case ActionType.ShowMessage:
                ShowMessage(action.messageText, action.messageDuration);
                break;

            case ActionType.PlaySound:
                if (action.audioClip != null)
                {
                    //musicSource.PlayClipAtPoint(action.audioClip, Camera.main.transform.position);
                    musicSource.PlayOneShot(action.audioClip);
                }
                break;

            case ActionType.ChangeMusic:
                // Use spawnCount as music index, with crossfade
                ChangeMusicWithCrossfade(action.spawnCount);
                break;
        }
    }

    private void SpawnEnemies(TriggerAction action)
    {
        if (action.enemyPrefab == null)
        {
            Debug.LogWarning("SpawnEnemies: No enemy prefab assigned!");
            return;
        }

        for (int i = 0; i < action.spawnCount; i++)
        {
            // Calculate spawn position with spread
            Vector3 spawnPos = action.spawnPosition;
            if (action.spawnSpread > 0 && action.spawnCount > 1)
            {
                Vector2 randomOffset = Random.insideUnitCircle * action.spawnSpread;
                spawnPos += new Vector3(randomOffset.x, 0, randomOffset.y);
            }

            Quaternion spawnRot = Quaternion.Euler(action.spawnRotation);
            GameObject enemy = Instantiate(action.enemyPrefab, spawnPos, spawnRot);

            Debug.Log($"Spawned enemy: {enemy.name} at {spawnPos}");
        }
    }

    private void GivePoints(int points)
    {
        PointObtained += points;
        Debug.Log($"Points awarded: +{points} (Total: {PointObtained})");
    }

    private void ShowMessage(string message, float duration)
    {
        if (messageText != null)
        {
            StartCoroutine(DisplayMessage(message, duration));
        }
        else
        {
            Debug.Log($"[MESSAGE] {message}");
        }
    }

    private IEnumerator DisplayMessage(string message, float duration)
    {
        messageText.text = message;
        messageText.gameObject.SetActive(true);

        // Fade in
        CanvasGroup cg = messageText.GetComponent<CanvasGroup>();
        if (cg == null)
        {
            cg = messageText.gameObject.AddComponent<CanvasGroup>();
        }

        float fadeTime = 0.5f;
        yield return StartCoroutine(FadeCanvasGroup(cg, 0f, 1f, fadeTime));

        // Display
        yield return new WaitForSeconds(duration - (fadeTime * 2));

        // Fade out
        yield return StartCoroutine(FadeCanvasGroup(cg, 1f, 0f, fadeTime));

        messageText.gameObject.SetActive(false);
    }

    /// <summary>
    /// Manually trigger a checkpoint (called from external scripts)
    /// </summary>
    public void TriggerCheckpoint(string checkpointID)
    {
        Debug.Log($"[GameController] Checkpoint triggered: {checkpointID}");

        foreach (MissionTrigger trigger in missionTriggers)
        {
            if (!trigger.isActive || trigger.hasTriggered)
                continue;

            if (trigger.condition == TriggerCondition.CheckpointReached &&
                trigger.checkpointID == checkpointID)
            {
                ExecuteTrigger(trigger);
            }
        }
    }

    #endregion

    #region Entity Management

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
    public void NotifyEntityDeath(Health healthSystem, string entityTag, bool isPlayer, GameObject attacker = null)
    {
        if (gameEnded) return;

        if (isPlayer)
        {
            Debug.Log("[GameController] Player has been defeated!");
            HandleGameLoss();
        }
        else if (entityTag == "Enemy")
        {
            if (attacker != null)
            {
                Health attackerHealth = attacker.GetComponent<Health>();
                if (attackerHealth != null && attackerHealth.isPlayer == true)
                {
                    PopUpKillConfirm();
                    PointObtained += healthSystem.point;
                }
            }

            defeatedEnemies++;
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
        if (TGTkill != null)
        {
            TGTkill.SetActive(true);
            yield return new WaitForSeconds(1.5f);
            TGTkill.SetActive(false);
        }
    }

    #endregion

    #region Game End

    /// <summary>
    /// Handle victory condition
    /// </summary>
    private void HandleGameVictory()
    {
        gameEnded = true;

        // Record mission end time
        if (trackMissionTime)
        {
            missionEndTime = Time.time;
            missionDuration = missionEndTime - missionStartTime;
        }

        StartCoroutine(HandleGameEnd(true));
    }

    private void HandleGameLoss()
    {
        gameEnded = true;

        // Record mission end time
        if (trackMissionTime)
        {
            missionEndTime = Time.time;
            missionDuration = missionEndTime - missionStartTime;
        }

        StartCoroutine(HandleGameEnd(false));
    }

    private IEnumerator HandleGameEnd(bool isVictory)
    {
        // Fade out background music first
        StopMusic(fadeOut: true);

        // Wait for music to fade
        yield return new WaitForSeconds(musicFadeOutDuration);

        // Play victory/defeat sound (now music is silent)
        if (isVictory && victorySound != null)
        {
            //musicSource.PlayClipAtPoint(victorySound, Camera.main.transform.position);
            musicSource.PlayOneShot(victorySound);
        }
        else if (!isVictory && defeatSound != null)
        {
            //musicSource.PlayClipAtPoint(defeatSound, Camera.main.transform.position);
            musicSource.PlayOneShot(defeatSound);
        }

        // Show results screen
        StartCoroutine(ShowResultSequence(isVictory));
    }

    /// <summary>
    /// Shows result UI with sequence: background fade (black) -> result text fade -> stat text fade -> show buttons
    /// </summary>
    /// 
    /// howard (add point to global variable)
    private IEnumerator ShowResultSequence(bool isVictory)
    {
        ImprovedPlaneController pc = playerEntity?.gameObject.GetComponent<ImprovedPlaneController>();

        if (pc != null)
        {
            pc.allowMouseControl = false;
        }

        yield return new WaitForSeconds(resultScreenDelay);

        // Prepare texts
        if (resultText != null)
            resultText.text = isVictory ? "VICTORY!" : "DEFEATED!";

        if (statText != null)
        {
            string timeString = trackMissionTime ? $"\nTime: {GetFormattedMissionTime()}" : "";
            statText.text = $"Enemies: {defeatedEnemies}/{Mathf.Max(1, totalEnemies)}\nPoints: {PointObtained}{timeString}";
        }

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

    #endregion

    #region UI Helpers

    private void PrepareResultUI()
    {
        if (resultCanvasObject == null)
        {
            resultCanvasObject = new GameObject("ResultCanvas");
            Canvas canvas = resultCanvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 9999;

            CanvasScaler scaler = resultCanvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;

            resultCanvasObject.AddComponent<CanvasGroup>();

            // Create background panel
            GameObject panelObj = new GameObject("Panel");
            panelObj.transform.SetParent(resultCanvasObject.transform, false);

            RectTransform panelRect = panelObj.AddComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;

            resultPanel = panelObj.AddComponent<Image>();
            resultPanel.color = new Color(0, 0, 0, 0f);

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
            if (resultPanel != null)
            {
                Color c = resultPanel.color;
                c.r = 0f; c.g = 0f; c.b = 0f;
                c.a = 0f;
                resultPanel.color = c;
            }

            if (resultText != null)
            {
                resultTextCanvasGroup = resultText.GetComponent<CanvasGroup>();
                if (resultTextCanvasGroup == null)
                    resultTextCanvasGroup = resultText.gameObject.AddComponent<CanvasGroup>();
                resultTextCanvasGroup.alpha = 0f;
            }

            if (statText != null)
            {
                statTextCanvasGroup = statText.GetComponent<CanvasGroup>();
                if (statTextCanvasGroup == null)
                    statTextCanvasGroup = statText.gameObject.AddComponent<CanvasGroup>();
                statTextCanvasGroup.alpha = 0f;
            }

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

        ColorBlock colors = button.colors;
        colors.normalColor = new Color(0.2f, 0.2f, 0.2f, 1f);
        colors.highlightedColor = new Color(0.4f, 0.4f, 0.4f, 1f);
        colors.pressedColor = new Color(0.1f, 0.1f, 0.1f, 1f);
        button.colors = colors;

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

    #region Scene Management

    private void RestartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private void GoToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuSceneName);
    }

    #endregion

    #region Public Getters

    public bool IsGameEnded()
    {
        return gameEnded;
    }

    public bool IsPlayerAlive()
    {
        return playerEntity != null && playerEntity.healthSystem != null && playerEntity.healthSystem.IsAlive();
    }

    public int GetTotalEnemies()
    {
        return totalEnemies;
    }

    public int GetDefeatedEnemies()
    {
        return defeatedEnemies;
    }

    public int GetPoints()
    {
        return PointObtained;
    }

    #endregion
}