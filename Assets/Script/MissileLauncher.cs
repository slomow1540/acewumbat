using UnityEngine;
using UnityEngine.Events;

public class MissileLauncher : MonoBehaviour
{
    [Header("Missile Settings")]
    [Tooltip("Missile prefab")]
    public GameObject missilePrefab;
    [Tooltip("Launch points for missiles")]
    public Transform[] launchPoints;
    [Tooltip("Damage per missile")]
    public float missileDamage = 50f;
    [Tooltip("Fire key")]
    public KeyCode fireKey = KeyCode.Mouse1;

    [Header("Lock-On Settings")]
    [Tooltip("Time required to fully lock onto target (seconds)")]
    public float lockOnTime = 2f;
    [Tooltip("Current lock-on progress (0-1)")]
    [Range(0f, 1f)]
    public float lockProgress = 0f;
    [Tooltip("Reference to targeting system")]
    public TargetingSystem targetingSystem;
    [Tooltip("Maximum lock-on range")]
    public float maxLockRange = 1500f;
    [Tooltip("Maximum angle for lock-on (degrees)")]
    public float maxLockAngle = 30f;

    [Header("Ammo")]
    [Tooltip("Current missile count")]
    public int currentMissiles = 10;
    [Tooltip("Maximum missiles")]
    public int maxMissiles = 10;

    [Header("Audio Clips")]
    [Tooltip("Lock-on beeping sound (plays while locking)")]
    public AudioClip lockingSound;
    [Tooltip("Locked sound (plays when fully locked)")]
    public AudioClip lockedSound;
    [Tooltip("Launch sound")]
    public AudioClip launchSound;
    [Tooltip("Lock lost sound")]
    public AudioClip lockLostSound;

    [Header("Audio Volume Settings")]
    [Tooltip("Volume for lock beeping sound")]
    [Range(0f, 1f)]
    public float lockingVolume = 0.5f;

    [Tooltip("Volume for locked confirmation sound")]
    [Range(0f, 1f)]
    public float lockedVolume = 0.7f;

    [Tooltip("Volume for missile launch sound")]
    [Range(0f, 1f)]
    public float launchVolume = 0.8f;

    [Tooltip("Volume for lock lost sound")]
    [Range(0f, 1f)]
    public float lockLostVolume = 0.4f;

    [Tooltip("Master volume for all missile launcher sounds")]
    [Range(0f, 1f)]
    public float masterVolume = 1f;

    [Header("Lock Audio Settings")]
    [Tooltip("Beep interval at 0% lock (seconds)")]
    public float slowBeepInterval = 0.5f;
    [Tooltip("Beep interval at 100% lock (seconds)")]
    public float fastBeepInterval = 0.1f;

    [Header("Events")]
    public UnityEvent<float> onLockProgressChanged; // Passes lock progress 0-1
    public UnityEvent onLockAchieved;
    public UnityEvent onLockLost;
    public UnityEvent onMissileFired;

    private AudioSource lockAudioSource;
    private AudioSource effectAudioSource;
    private float nextBeepTime;
    private bool wasLocked = false;
    private bool isLocking = false;
    private int currentLaunchPointIndex = 0;
    private GameObject currentLockTarget;

    private void Awake()
    {
        // Create two audio sources - one for lock beeping, one for effects
        lockAudioSource = gameObject.AddComponent<AudioSource>();
        lockAudioSource.spatialBlend = 0f; // 2D sound for UI feedback
        lockAudioSource.playOnAwake = false;
        lockAudioSource.loop = false;

        effectAudioSource = gameObject.AddComponent<AudioSource>();
        effectAudioSource.spatialBlend = 0f;
        effectAudioSource.playOnAwake = false;
        effectAudioSource.loop = false;

        // Get targeting system if not assigned
        if (targetingSystem == null)
        {
            targetingSystem = GetComponent<TargetingSystem>();
        }

        // Setup launch points if not assigned
        if (launchPoints == null || launchPoints.Length == 0)
        {
            GameObject launchPoint = new GameObject("MissileLaunchPoint");
            launchPoint.transform.parent = transform;
            launchPoint.transform.localPosition = Vector3.forward * 3f;
            launchPoint.transform.localRotation = Quaternion.identity;
            launchPoints = new Transform[] { launchPoint.transform };
        }
    }

    private void Update()
    {
        UpdateLockOn();
        HandleMissileInput();
        UpdateLockAudio();
    }

    private void UpdateLockOn()
    {
        if (targetingSystem == null || !targetingSystem.HasTarget())
        {
            // No target - reset lock
            if (lockProgress > 0f || isLocking)
            {
                LoseLock();
            }
            return;
        }

        GameObject target = targetingSystem.currentTarget;

        // Check if target is within lock parameters
        if (!CanLockOnTarget(target))
        {
            if (lockProgress > 0f || isLocking)
            {
                LoseLock();
            }
            return;
        }

        // Same target - increase lock progress
        if (target == currentLockTarget)
        {
            if (lockProgress < 1f)
            {
                isLocking = true;
                lockProgress += Time.deltaTime / lockOnTime;
                lockProgress = Mathf.Clamp01(lockProgress);

                onLockProgressChanged?.Invoke(lockProgress);

                // Check if just achieved full lock
                if (lockProgress >= 1f && !wasLocked)
                {
                    AchieveLock();
                }
            }
        }
        else
        {
            // New target - reset lock progress
            if (lockProgress > 0f)
            {
                LoseLock();
            }
            currentLockTarget = target;
        }
    }

    private bool CanLockOnTarget(GameObject target)
    {
        if (target == null) return false;

        // Check if target is alive
        Health targetHealth = target.GetComponent<Health>();
        if (targetHealth != null && !targetHealth.IsAlive())
            return false;

        // Check range
        float distance = Vector3.Distance(transform.position, target.transform.position);
        if (distance > maxLockRange)
            return false;

        // Check angle (target must be in front)
        Vector3 directionToTarget = (target.transform.position - transform.position).normalized;
        float angle = Vector3.Angle(transform.forward, directionToTarget);
        if (angle > maxLockAngle)
            return false;

        return true;
    }

    private void AchieveLock()
    {
        wasLocked = true;
        isLocking = false;

        // Play locked sound with volume control
        if (lockedSound != null && effectAudioSource != null)
        {
            effectAudioSource.PlayOneShot(lockedSound, lockedVolume * masterVolume);
        }

        onLockAchieved?.Invoke();
    }

    private void LoseLock()
    {
        bool hadLock = lockProgress > 0f;

        lockProgress = 0f;
        wasLocked = false;
        isLocking = false;
        currentLockTarget = null;

        if (hadLock)
        {
            // Play lock lost sound with volume control
            if (lockLostSound != null && effectAudioSource != null)
            {
                effectAudioSource.PlayOneShot(lockLostSound, lockLostVolume * masterVolume);
            }

            onLockLost?.Invoke();
        }
    }

    private void UpdateLockAudio()
    {
        // Only play beeping while actively locking (not when fully locked)
        if (isLocking && lockProgress < 1f && lockingSound != null)
        {
            // Calculate beep interval based on lock progress
            // More locked = faster beeping
            float currentInterval = Mathf.Lerp(slowBeepInterval, fastBeepInterval, lockProgress);

            if (Time.time >= nextBeepTime)
            {
                lockAudioSource.PlayOneShot(lockingSound, lockingVolume * masterVolume);
                nextBeepTime = Time.time + currentInterval;
            }
        }
    }

    private void HandleMissileInput()
    {
        // Check if we have AI input
        if (Input.GetKeyDown(fireKey))
        {
            TryFireMissile();
        }
    }

    private void TryFireMissile()
    {
        // Check if we can fire
        if (!CanFireMissile())
            return;

        // Fire missile
        FireMissile();
    }

    private bool CanFireMissile()
    {
        // Check ammo
        if (currentMissiles <= 0)
        {
            Debug.Log("Out of missiles!");
            return false;
        }

        // Check missile prefab
        if (missilePrefab == null)
        {
            Debug.LogWarning("No missile prefab assigned!");
            return false;
        }

        // Must have full lock
        if (lockProgress < 1f)
        {
            Debug.Log("Target not locked!");
            return false;
        }

        // Must have valid target
        if (!targetingSystem.HasTarget())
        {
            Debug.Log("No target!");
            return false;
        }

        return true;
    }

    private void FireMissile()
    {
        // Use ammo
        currentMissiles--;

        // Get launch point
        Transform launchPoint = launchPoints[currentLaunchPointIndex];
        currentLaunchPointIndex = (currentLaunchPointIndex + 1) % launchPoints.Length;

        // Spawn missile
        GameObject missileObj = Instantiate(missilePrefab, launchPoint.position, launchPoint.rotation);

        // Initialize missile
        Missile missile = missileObj.GetComponent<Missile>();
        if (missile != null)
        {
            missile.Initialize(gameObject, targetingSystem.currentTarget, missileDamage);
        }

        // Play launch sound with volume control
        if (launchSound != null && effectAudioSource != null)
        {
            effectAudioSource.PlayOneShot(launchSound, launchVolume * masterVolume);
        }

        // Reset lock after firing
        LoseLock();

        onMissileFired?.Invoke();
    }

    #region Public Methods

    /// <summary>
    /// Check if missile is locked and ready to fire
    /// </summary>
    public bool IsLocked()
    {
        return lockProgress >= 1f;
    }

    /// <summary>
    /// Get lock progress as percentage (0-1)
    /// </summary>
    public float GetLockProgress()
    {
        return lockProgress;
    }

    /// <summary>
    /// Reload missiles
    /// </summary>
    public void Reload()
    {
        currentMissiles = maxMissiles;
    }

    /// <summary>
    /// Add missiles
    /// </summary>
    public void AddMissiles(int amount)
    {
        currentMissiles = Mathf.Min(currentMissiles + amount, maxMissiles);
    }

    /// <summary>
    /// Set master volume for all sounds (0-1)
    /// </summary>
    public void SetMasterVolume(float volume)
    {
        masterVolume = Mathf.Clamp01(volume);
    }

    /// <summary>
    /// Set volume for specific sound type (0-1)
    /// </summary>
    public void SetSoundVolume(string soundType, float volume)
    {
        volume = Mathf.Clamp01(volume);

        switch (soundType.ToLower())
        {
            case "locking":
                lockingVolume = volume;
                break;
            case "locked":
                lockedVolume = volume;
                break;
            case "launch":
                launchVolume = volume;
                break;
            case "locklost":
                lockLostVolume = volume;
                break;
            default:
                Debug.LogWarning($"Unknown sound type: {soundType}");
                break;
        }
    }

    #endregion
}