using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Missile Interceptor System - Launches counter-missiles to intercept incoming threats
/// Can be placed on any aircraft or object for defensive capabilities
/// </summary>
public class MissileInterceptor : MonoBehaviour
{
    [Header("Detection Settings")]
    [Tooltip("Maximum range to detect incoming missiles")]
    public float detectionRange = 1000f;

    [Tooltip("Detection angle (0 = behind only, 180 = all around)")]
    [Range(0f, 180f)]
    public float detectionAngle = 120f;

    [Tooltip("How often to scan for threats (seconds)")]
    public float scanInterval = 0.2f;

    [Tooltip("Tag of enemy missiles to intercept")]
    public string enemyMissileTag = "enemy missile";

    [Header("Lock Settings")]
    [Tooltip("Time required to lock onto a threat before firing")]
    public float lockTime = 1.5f;

    [Tooltip("Can lock multiple threats simultaneously")]
    public bool allowMultipleLocks = false;

    [Tooltip("Maximum number of simultaneous locks")]
    public int maxSimultaneousLocks = 2;

    [Header("Interceptor Settings")]
    [Tooltip("Interceptor missile prefab to launch")]
    public GameObject interceptorPrefab;

    [Tooltip("Launch points for interceptors (will cycle through them)")]
    public Transform[] launchPoints;

    [Tooltip("Maximum interceptor missiles")]
    public int maxInterceptors = 10;

    [Tooltip("Current interceptor count")]
    public int currentInterceptors = 10;

    [Tooltip("Cooldown between launches (seconds)")]
    public float launchCooldown = 0.5f;

    [Tooltip("Launch speed for interceptors")]
    public float interceptorLaunchSpeed = 150f;

    [Tooltip("Interceptor damage")]
    public float interceptorDamage = 30f;

    [Header("Prioritization")]
    [Tooltip("Prioritize closest threats")]
    public bool prioritizeClosest = true;

    [Tooltip("Prioritize threats with shortest time to impact")]
    public bool prioritizeTimeToImpact = false;

    [Tooltip("Auto-engage threats (if false, requires manual activation)")]
    public bool autoEngage = true;

    [Header("Audio")]
    [Tooltip("Sound when locking onto threat")]
    public AudioClip lockingSound;

    [Tooltip("Sound when locked and firing")]
    public AudioClip launchSound;

    [Tooltip("Sound when out of ammo")]
    public AudioClip emptySound;

    private AudioSource audioSource;

    [Header("Visual Feedback")]
    [Tooltip("Show debug lines to threats")]
    public bool showDebugLines = true;

    // Internal tracking
    private class ThreatInfo
    {
        public GameObject missile;
        public float lockProgress;
        public float timeSinceDetected;
        public float distanceWhenDetected;
        public bool engagedAlready; // NEW: Track if we've already shot at this

        public ThreatInfo(GameObject m)
        {
            missile = m;
            lockProgress = 0f;
            timeSinceDetected = 0f;
            distanceWhenDetected = 0f;
            engagedAlready = false;
        }
    }

    private List<ThreatInfo> detectedThreats = new List<ThreatInfo>();
    private float lastScanTime;
    private float lastLaunchTime;
    private int currentLaunchPoint = 0;

    private void Start()
    {
        // Setup audio
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        audioSource.spatialBlend = 1f;

        // Validate setup
        if (interceptorPrefab == null)
        {
            Debug.LogError("MissileInterceptor: No interceptor prefab assigned!");
        }

        if (launchPoints == null || launchPoints.Length == 0)
        {
            Debug.LogWarning(
                "MissileInterceptor: No launch points assigned! Using object position."
            );
        }

        currentInterceptors = maxInterceptors;
    }

    private void Update()
    {
        // Scan for threats periodically
        if (Time.time - lastScanTime >= scanInterval)
        {
            ScanForThreats();
            lastScanTime = Time.time;
        }

        // Update lock progress on detected threats
        UpdateThreatLocks();

        // Attempt to engage threats
        if (autoEngage)
        {
            TryEngageThreats();
        }
    }

    /// <summary>
    /// Scan for incoming missiles
    /// </summary>
    private void ScanForThreats()
    {
        // Find all enemy missiles
        GameObject[] missiles = GameObject.FindGameObjectsWithTag(enemyMissileTag);

        // Check each missile
        foreach (GameObject missileObj in missiles)
        {
            if (missileObj == null)
                continue;

            // Check if targeting us
            Missile missile = missileObj.GetComponent<Missile>();
            if (missile != null && missile.target == gameObject)
            {
                // Check if in detection range and angle
                if (IsThreatDetectable(missileObj))
                {
                    // Add to threats if not already tracking
                    if (!IsAlreadyTracking(missileObj))
                    {
                        ThreatInfo threat = new ThreatInfo(missileObj);
                        threat.distanceWhenDetected = Vector3.Distance(
                            transform.position,
                            missileObj.transform.position
                        );
                        detectedThreats.Add(threat);

                        Debug.Log(
                            $"New threat detected: {missileObj.name} at {threat.distanceWhenDetected:F0}m"
                        );
                    }
                }
            }
        }

        // Clean up dead/lost threats (null missiles or out of range)
        detectedThreats.RemoveAll(t => t.missile == null || !IsThreatDetectable(t.missile));
    }

    /// <summary>
    /// Check if threat is within detection parameters
    /// </summary>
    private bool IsThreatDetectable(GameObject threat)
    {
        if (threat == null)
            return false;

        float distance = Vector3.Distance(transform.position, threat.transform.position);

        // Check range
        if (distance > detectionRange)
            return false;

        // Check angle (threat behind us)
        Vector3 directionToThreat = (threat.transform.position - transform.position).normalized;
        float angle = Vector3.Angle(-transform.forward, directionToThreat);

        if (angle > detectionAngle)
            return false;

        return true;
    }

    /// <summary>
    /// Check if already tracking this threat
    /// </summary>
    private bool IsAlreadyTracking(GameObject threat)
    {
        foreach (ThreatInfo t in detectedThreats)
        {
            if (t.missile == threat)
                return true;
        }
        return false;
    }

    /// <summary>
    /// Update lock progress on all threats
    /// </summary>
    private void UpdateThreatLocks()
    {
        if (detectedThreats.Count == 0)
            return;

        // Determine how many threats to lock simultaneously
        int locksToProcess = allowMultipleLocks
            ? Mathf.Min(maxSimultaneousLocks, detectedThreats.Count)
            : 1;

        // Sort threats by priority
        SortThreatsByPriority();

        // Update lock on top priority threats (only ones we haven't engaged yet)
        int locksProcessed = 0;
        for (int i = 0; i < detectedThreats.Count && locksProcessed < locksToProcess; i++)
        {
            ThreatInfo threat = detectedThreats[i];

            // Skip threats we've already engaged
            if (threat.engagedAlready)
                continue;

            threat.timeSinceDetected += Time.deltaTime;

            // Increase lock progress
            threat.lockProgress += Time.deltaTime / lockTime;
            threat.lockProgress = Mathf.Clamp01(threat.lockProgress);

            // Play locking sound (only for highest priority unengaged threat)
            if (
                locksProcessed == 0
                && threat.lockProgress > 0.1f
                && threat.lockProgress < 0.95f
                && lockingSound != null
            )
            {
                if (!audioSource.isPlaying)
                {
                    audioSource.clip = lockingSound;
                    audioSource.loop = true;
                    audioSource.Play();
                }
            }

            locksProcessed++;
        }
    }

    /// <summary>
    /// Sort threats by priority (with null safety)
    /// </summary>
    private void SortThreatsByPriority()
    {
        // First, remove any null/destroyed missiles
        detectedThreats.RemoveAll(t => t.missile == null);

        if (detectedThreats.Count == 0)
            return;

        if (prioritizeClosest)
        {
            // Sort by distance (closest first)
            detectedThreats.Sort(
                (a, b) =>
                {
                    // Safety check for null missiles (shouldn't happen after RemoveAll, but be safe)
                    if (a.missile == null)
                        return 1;
                    if (b.missile == null)
                        return -1;

                    float distA = Vector3.Distance(
                        transform.position,
                        a.missile.transform.position
                    );
                    float distB = Vector3.Distance(
                        transform.position,
                        b.missile.transform.position
                    );
                    return distA.CompareTo(distB);
                }
            );
        }
        else if (prioritizeTimeToImpact)
        {
            // Sort by estimated time to impact (shortest first)
            detectedThreats.Sort(
                (a, b) =>
                {
                    // Safety check for null missiles
                    if (a.missile == null)
                        return 1;
                    if (b.missile == null)
                        return -1;

                    float timeA = CalculateTimeToImpact(a.missile);
                    float timeB = CalculateTimeToImpact(b.missile);
                    return timeA.CompareTo(timeB);
                }
            );
        }
    }

    /// <summary>
    /// Calculate estimated time to impact
    /// </summary>
    private float CalculateTimeToImpact(GameObject threat)
    {
        if (threat == null)
            return float.MaxValue;

        Rigidbody rb = threat.GetComponent<Rigidbody>();
        if (rb == null)
            return float.MaxValue;

        float distance = Vector3.Distance(transform.position, threat.transform.position);
        float speed = rb.linearVelocity.magnitude;

        if (speed < 1f)
            return float.MaxValue;

        return distance / speed;
    }

    /// <summary>
    /// Attempt to engage locked threats
    /// </summary>
    private void TryEngageThreats()
    {
        if (currentInterceptors <= 0)
            return;
        if (Time.time - lastLaunchTime < launchCooldown)
            return;

        // Find locked threats that haven't been engaged yet
        // Use for loop to safely modify collection
        for (int i = 0; i < detectedThreats.Count; i++)
        {
            ThreatInfo threat = detectedThreats[i];

            // Skip if already engaged
            if (threat.engagedAlready)
                continue;

            // Check if locked
            if (threat.lockProgress >= 1f && threat.missile != null)
            {
                // Launch interceptor
                LaunchInterceptor(threat.missile);

                // Mark as engaged (don't shoot at it again!)
                threat.engagedAlready = true;

                // Stop locking sound
                if (audioSource.isPlaying && audioSource.clip == lockingSound)
                {
                    audioSource.Stop();
                }

                return; // Only launch one at a time
            }
        }
    }

    /// <summary>
    /// Launch an interceptor missile at the threat
    /// </summary>
    private void LaunchInterceptor(GameObject threat)
    {
        if (currentInterceptors <= 0)
        {
            // Out of ammo
            if (emptySound != null)
            {
                audioSource.PlayOneShot(emptySound);
            }
            Debug.Log("MissileInterceptor: Out of interceptors!");
            return;
        }

        if (interceptorPrefab == null)
        {
            Debug.LogError("MissileInterceptor: No interceptor prefab assigned!");
            return;
        }

        // Get launch point
        Vector3 launchPosition;
        Quaternion launchRotation;

        if (launchPoints != null && launchPoints.Length > 0)
        {
            Transform launchPoint = launchPoints[currentLaunchPoint];
            launchPosition = launchPoint.position;
            launchRotation = launchPoint.rotation;

            // Cycle to next launch point
            currentLaunchPoint = (currentLaunchPoint + 1) % launchPoints.Length;
        }
        else
        {
            launchPosition = transform.position;
            launchRotation = transform.rotation;
        }

        // Spawn interceptor
        GameObject interceptorObj = Instantiate(interceptorPrefab, launchPosition, launchRotation);

        // Setup interceptor missile - check for InterceptorMissile component first
        InterceptorMissile interceptorMissile = interceptorObj.GetComponent<InterceptorMissile>();
        if (interceptorMissile != null)
        {
            // Using simple InterceptorMissile component
            interceptorMissile.Initialize(gameObject, threat);
            interceptorMissile.speed = interceptorLaunchSpeed;
        }
        else
        {
            // Fallback: try regular Missile component
            Missile regularMissile = interceptorObj.GetComponent<Missile>();
            if (regularMissile != null)
            {
                regularMissile.Initialize(gameObject, threat, interceptorDamage);
                regularMissile.launchSpeed = interceptorLaunchSpeed;
                regularMissile.maxSpeed = interceptorLaunchSpeed * 1.5f;
            }
            else
            {
                Debug.LogWarning(
                    "Interceptor prefab has neither InterceptorMissile nor Missile component!"
                );
            }
        }

        // Tag as friendly missile (optional)
        if (gameObject.tag != "Untagged")
        {
            interceptorObj.tag = "interceptor missile";
        }

        // Decrease ammo
        currentInterceptors--;

        // Play launch sound
        if (launchSound != null)
        {
            audioSource.PlayOneShot(launchSound);
        }

        lastLaunchTime = Time.time;

        Debug.Log(
            $"Launched interceptor at {threat.name}! Remaining: {currentInterceptors}/{maxInterceptors}"
        );
    }

    /// <summary>
    /// Manually trigger interception (for manual control mode)
    /// </summary>
    public void ManualEngage()
    {
        if (!autoEngage)
        {
            TryEngageThreats();
        }
    }

    /// <summary>
    /// Reload interceptors
    /// </summary>
    public void Reload(int amount = -1)
    {
        if (amount < 0)
        {
            currentInterceptors = maxInterceptors;
        }
        else
        {
            currentInterceptors = Mathf.Min(currentInterceptors + amount, maxInterceptors);
        }

        Debug.Log($"Interceptors reloaded: {currentInterceptors}/{maxInterceptors}");
    }

    /// <summary>
    /// Get number of detected threats
    /// </summary>
    public int GetThreatCount()
    {
        return detectedThreats.Count;
    }

    /// <summary>
    /// Get highest lock progress
    /// </summary>
    public float GetHighestLockProgress()
    {
        float highest = 0f;
        foreach (ThreatInfo threat in detectedThreats)
        {
            if (threat.lockProgress > highest)
                highest = threat.lockProgress;
        }
        return highest;
    }

    /// <summary>
    /// Check if currently tracking any threats
    /// </summary>
    public bool HasThreats()
    {
        return detectedThreats.Count > 0;
    }

    private void OnDrawGizmos()
    {
        if (!showDebugLines)
            return;

        // Draw detection range
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        // Draw detection cone
        Vector3 backDirection = -transform.forward;
        Vector3 left = Quaternion.Euler(0, -detectionAngle, 0) * backDirection;
        Vector3 right = Quaternion.Euler(0, detectionAngle, 0) * backDirection;

        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(transform.position, left * detectionRange);
        Gizmos.DrawRay(transform.position, right * detectionRange);

        // Draw lines to threats
        if (Application.isPlaying && detectedThreats != null)
        {
            foreach (ThreatInfo threat in detectedThreats)
            {
                if (threat.missile != null)
                {
                    // Color based on lock progress and engagement status
                    if (threat.engagedAlready)
                        Gizmos.color = Color.gray; // Already engaged
                    else if (threat.lockProgress >= 1f)
                        Gizmos.color = Color.red; // Locked
                    else if (threat.lockProgress > 0f)
                        Gizmos.color = Color.yellow; // Locking
                    else
                        Gizmos.color = Color.white; // Detected

                    Gizmos.DrawLine(transform.position, threat.missile.transform.position);
                }
            }
        }

        // Draw launch points
        if (launchPoints != null)
        {
            Gizmos.color = Color.green;
            foreach (Transform launchPoint in launchPoints)
            {
                if (launchPoint != null)
                {
                    Gizmos.DrawWireSphere(launchPoint.position, 0.5f);
                    Gizmos.DrawRay(launchPoint.position, launchPoint.forward * 2f);
                }
            }
        }
    }
}
