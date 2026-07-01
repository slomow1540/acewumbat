using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 3D missile warning system that shows arrows pointing at incoming missiles
/// Features: beeping, miss detection, visual urgency, smart filtering
/// </summary>
public class MissileWarning3D : MonoBehaviour
{
    [Header("Arrow Settings")]
    [Tooltip("Prefab for warning arrows")]
    public GameObject arrowPrefab;
    [Tooltip("Distance from player to place arrows")]
    public float radius = 5f;
    [Tooltip("Only show arrows for missiles within this range")]
    public float maxWarningDistance = 1000f;

    [Header("Missile Detection")]
    [Tooltip("Tag for enemy missiles")]
    public string missileTag = "EnemyMissile";
    [Tooltip("Only warn about missiles targeting this object")]
    public bool onlyShowTargetedMissiles = true;

    [Header("Warning Levels")]
    [Tooltip("Distance at which missile is considered dangerous (starts beeping)")]
    public float dangerDistance = 300f;
    [Tooltip("Distance at which missile is critical (fast beeping)")]
    public float criticalDistance = 100f;

    [Header("Miss Detection")]
    [Tooltip("Remove warning when missile passes this distance behind you")]
    public float missedDistanceBehind = 50f;
    [Tooltip("Angle behind you to consider missile as 'missed' (degrees)")]
    public float missedAngle = 120f;
    [Tooltip("Remove warning if missile tracking is lost")]
    public bool removeOnTrackingLoss = true;

    [Header("Audio")]
    [Tooltip("Beep sound for warnings")]
    public AudioClip beepSound;
    [Tooltip("Audio source for beeps")]
    public AudioSource audioSource;
    [Tooltip("Normal beep interval (far away)")]
    public float normalBeepInterval = 1f;
    [Tooltip("Danger beep interval (getting close)")]
    public float dangerBeepInterval = 0.5f;
    [Tooltip("Critical beep interval (very close)")]
    public float criticalBeepInterval = 0.15f;
    [Tooltip("Beep volume")]
    [Range(0f, 1f)]
    public float beepVolume = 0.5f;

    [Header("Visual Feedback")]
    [Tooltip("Arrow color when far")]
    public Color normalColor = Color.yellow;
    [Tooltip("Arrow color when danger")]
    public Color dangerColor = Color.orange;
    [Tooltip("Arrow color when critical")]
    public Color criticalColor = Color.red;
    [Tooltip("Pulse arrows when critical")]
    public bool pulseWhenCritical = true;
    [Tooltip("Pulse speed")]
    public float pulseSpeed = 5f;

    // Private tracking
    private Dictionary<GameObject, ArrowData> activeWarnings = new Dictionary<GameObject, ArrowData>();
    private float nextBeepTime = 0f;
    private GameObject closestMissile = null;

    private class ArrowData
    {
        public GameObject arrow;
        public float lastDistance;
        public bool wasTargeted;

        public ArrowData(GameObject arrow)
        {
            this.arrow = arrow;
            this.lastDistance = float.MaxValue;
            this.wasTargeted = true;
        }
    }

    private void Start()
    {
        // Setup audio source if not assigned
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.spatialBlend = 0f; // 2D sound
            audioSource.playOnAwake = false;
        }
    }

    private void Update()
    {
        UpdateMissileWarnings();
        UpdateBeeping();
        UpdateArrowVisuals();
    }

    private void UpdateMissileWarnings()
    {
        // Find all missiles
        GameObject[] missiles = GameObject.FindGameObjectsWithTag(missileTag);

        // Track which missiles we've seen this frame
        HashSet<GameObject> currentMissiles = new HashSet<GameObject>();

        foreach (GameObject missile in missiles)
        {
            if (missile == null) continue;

            // Check if we should warn about this missile
            if (!ShouldWarnAboutMissile(missile))
            {
                // Remove warning if it exists
                if (activeWarnings.ContainsKey(missile))
                {
                    RemoveWarning(missile);
                }
                continue;
            }

            currentMissiles.Add(missile);

            // Create or update warning
            if (!activeWarnings.ContainsKey(missile))
            {
                CreateWarning(missile);
            }
            else
            {
                UpdateWarning(missile);
            }
        }

        // Remove warnings for missiles that no longer exist or should be removed
        List<GameObject> toRemove = new List<GameObject>();
        foreach (var kvp in activeWarnings)
        {
            if (!currentMissiles.Contains(kvp.Key) || kvp.Key == null)
            {
                toRemove.Add(kvp.Key);
            }
        }

        foreach (GameObject missile in toRemove)
        {
            RemoveWarning(missile);
        }
    }

    private bool ShouldWarnAboutMissile(GameObject missile)
    {
        float distance = Vector3.Distance(transform.position, missile.transform.position);

        // Too far away?
        if (distance > maxWarningDistance)
            return false;

        // Check if missile is targeting us
        if (onlyShowTargetedMissiles)
        {
            Missile missileScript = missile.GetComponent<Missile>();
            if (missileScript != null)
            {
                // Not targeting us?
                if (missileScript.target != gameObject)
                    return false;

                // Missile lost tracking?
                if (removeOnTrackingLoss && !missileScript.hasTarget)
                    return false;
            }
        }

        // Check if missile has "missed" (passed by us)
        if (HasMissileMissed(missile, distance))
            return false;

        return true;
    }

    private bool HasMissileMissed(GameObject missile, float distance)
    {
        Vector3 toMissile = missile.transform.position - transform.position;

        // Check if missile is behind us
        float angle = Vector3.Angle(transform.forward, toMissile);

        if (angle > missedAngle)
        {
            // Missile is behind us
            // Check if it's also far enough behind to be considered "missed"
            if (distance > missedDistanceBehind)
            {
                return true;
            }
        }

        // Check if missile is moving away from us
        Rigidbody missileRb = missile.GetComponent<Rigidbody>();
        if (missileRb != null)
        {
            Vector3 missileVelocity = missileRb.linearVelocity;
            Vector3 toPlayer = transform.position - missile.transform.position;

            // If missile is moving away (dot product negative)
            if (Vector3.Dot(missileVelocity.normalized, toPlayer.normalized) < -0.5f)
            {
                // And it's already past us
                if (angle > 90f && distance > missedDistanceBehind * 0.5f)
                {
                    return true;
                }
            }
        }

        return false;
    }

    private void CreateWarning(GameObject missile)
    {
        if (arrowPrefab == null) return;

        GameObject arrow = Instantiate(arrowPrefab);
        ArrowData data = new ArrowData(arrow);
        activeWarnings[missile] = data;

        UpdateWarning(missile);
    }

    private void UpdateWarning(GameObject missile)
    {
        if (!activeWarnings.ContainsKey(missile)) return;

        ArrowData data = activeWarnings[missile];
        if (data.arrow == null) return;

        Vector3 dirToMissile = (missile.transform.position - transform.position).normalized;
        float distance = Vector3.Distance(transform.position, missile.transform.position);

        // Position arrow
        data.arrow.transform.position = transform.position + dirToMissile * radius;
        data.arrow.transform.rotation = Quaternion.LookRotation(dirToMissile);

        // Track distance
        data.lastDistance = distance;
    }

    private void RemoveWarning(GameObject missile)
    {
        if (!activeWarnings.ContainsKey(missile)) return;

        ArrowData data = activeWarnings[missile];
        if (data.arrow != null)
        {
            Destroy(data.arrow);
        }

        activeWarnings.Remove(missile);
    }

    private void UpdateBeeping()
    {
        if (beepSound == null || audioSource == null) return;

        // Find closest missile
        closestMissile = null;
        float closestDistance = float.MaxValue;

        foreach (var kvp in activeWarnings)
        {
            if (kvp.Key == null) continue;

            float distance = kvp.Value.lastDistance;
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestMissile = kvp.Key;
            }
        }

        // No missiles? No beeping
        if (closestMissile == null)
        {
            nextBeepTime = 0f;
            return;
        }

        // Determine beep interval based on distance
        float beepInterval;
        if (closestDistance < criticalDistance)
        {
            beepInterval = criticalBeepInterval;
        }
        else if (closestDistance < dangerDistance)
        {
            // Interpolate between danger and critical
            float t = (closestDistance - criticalDistance) / (dangerDistance - criticalDistance);
            beepInterval = Mathf.Lerp(criticalBeepInterval, dangerBeepInterval, t);
        }
        else
        {
            beepInterval = normalBeepInterval;
        }

        // Play beep if time
        if (Time.time >= nextBeepTime)
        {
            audioSource.PlayOneShot(beepSound, beepVolume);
            nextBeepTime = Time.time + beepInterval;
        }
    }

    private void UpdateArrowVisuals()
    {
        foreach (var kvp in activeWarnings)
        {
            if (kvp.Key == null || kvp.Value.arrow == null) continue;

            float distance = kvp.Value.lastDistance;
            GameObject arrow = kvp.Value.arrow;

            // Get renderer
            Renderer renderer = arrow.GetComponent<Renderer>();
            if (renderer == null)
            {
                // Try children
                renderer = arrow.GetComponentInChildren<Renderer>();
            }

            if (renderer != null)
            {
                // Determine color based on distance
                Color targetColor;
                if (distance < criticalDistance)
                {
                    targetColor = criticalColor;

                    // Pulse when critical
                    if (pulseWhenCritical)
                    {
                        float pulse = (Mathf.Sin(Time.time * pulseSpeed) + 1f) * 0.5f;
                        float scale = Mathf.Lerp(0.8f, 1.2f, pulse);
                        arrow.transform.localScale = Vector3.one * scale;
                    }
                }
                else if (distance < dangerDistance)
                {
                    // Interpolate between danger and critical
                    float t = (distance - criticalDistance) / (dangerDistance - criticalDistance);
                    targetColor = Color.Lerp(criticalColor, dangerColor, t);
                    arrow.transform.localScale = Vector3.one;
                }
                else
                {
                    targetColor = normalColor;
                    arrow.transform.localScale = Vector3.one;
                }

                // Apply color
                renderer.material.color = targetColor;
            }
        }
    }

    private void OnDestroy()
    {
        // Clean up all arrows
        foreach (var kvp in activeWarnings)
        {
            if (kvp.Value.arrow != null)
            {
                Destroy(kvp.Value.arrow);
            }
        }
        activeWarnings.Clear();
    }

    private void OnDrawGizmos()
    {
        // Draw warning radius
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, radius);

        // Draw danger distance
        Gizmos.color = Color.orange;
        Gizmos.DrawWireSphere(transform.position, dangerDistance);

        // Draw critical distance
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, criticalDistance);

        // Draw missed angle
        if (Application.isPlaying)
        {
            Gizmos.color = Color.cyan;
            Vector3 leftBound = Quaternion.Euler(0, -missedAngle, 0) * transform.forward * 50f;
            Vector3 rightBound = Quaternion.Euler(0, missedAngle, 0) * transform.forward * 50f;
            Gizmos.DrawRay(transform.position, leftBound);
            Gizmos.DrawRay(transform.position, rightBound);
        }
    }
}