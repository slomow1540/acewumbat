using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class TargetingSystem : MonoBehaviour
{
    [Header("Targeting Settings")]
    [Tooltip("Maximum range for target detection")]
    public float maxTargetRange = 2000f;
    [Tooltip("Field of view angle for target detection")]
    [Range(0f, 360f)]
    public float targetingFOV = 60f;
    [Tooltip("Tag to identify enemies")]
    public string enemyTag = "Enemy";
    [Tooltip("Layer mask for raycasting")]
    public LayerMask targetLayers;

    [Header("Target Lock")]
    [Tooltip("Current locked target")]
    public GameObject currentTarget;
    [Tooltip("Key to cycle to next target")]
    public KeyCode nextTargetKey = KeyCode.R;
    [Tooltip("Key to cycle to previous target")]
    public KeyCode previousTargetKey = KeyCode.T;
    [Tooltip("Key to clear target")]
    public KeyCode clearTargetKey = KeyCode.Y;

    [Header("Auto-Targeting")]
    [Tooltip("Automatically lock onto nearest target in front")]
    public bool autoTarget = true;
    [Tooltip("How often to update auto-target (seconds)")]
    public float autoTargetUpdateRate = 0.5f;

    private List<GameObject> availableTargets = new List<GameObject>();
    private float lastAutoTargetUpdate;
    private Transform cameraTransform;

    private void Start()
    {
        cameraTransform = Camera.main?.transform;
        if (cameraTransform == null)
            cameraTransform = transform;
    }

    private void Update()
    {
        HandleTargetInput();

        if (autoTarget && Time.time - lastAutoTargetUpdate > autoTargetUpdateRate)
        {
            UpdateAutoTarget();
            lastAutoTargetUpdate = Time.time;
        }

        // Only clear if the target object was destroyed or is dead.
        // Going out of FOV or range does NOT clear the lock.
        if (currentTarget != null && !IsAliveAndExists(currentTarget))
        {
            ClearTarget();
        }
    }

    private void HandleTargetInput()
    {
        if (Input.GetKeyDown(nextTargetKey))
            CycleTarget(1);

        if (Input.GetKeyDown(previousTargetKey))
            CycleTarget(-1);

        if (Input.GetKeyDown(clearTargetKey))
            ClearTarget();
    }

    private void UpdateAutoTarget()
    {
        RefreshAvailableTargets();

        // Only auto-acquire if we have no target yet
        if (currentTarget == null && availableTargets.Count > 0)
        {
            currentTarget = GetBestAutoTarget();
        }
    }

    private void CycleTarget(int direction)
    {
        RefreshAvailableTargets();

        if (availableTargets.Count == 0)
        {
            ClearTarget();
            return;
        }

        int currentIndex = availableTargets.IndexOf(currentTarget);

        if (currentIndex == -1)
        {
            currentTarget = availableTargets[0];
        }
        else
        {
            currentIndex += direction;

            if (currentIndex < 0)
                currentIndex = availableTargets.Count - 1;
            else if (currentIndex >= availableTargets.Count)
                currentIndex = 0;

            currentTarget = availableTargets[currentIndex];
        }
    }

    private void ClearTarget()
    {
        currentTarget = null;
    }

    /// <summary>
    /// Rebuilds the available target list. Validity is range + alive only — FOV is NOT a filter here.
    /// </summary>
    private void RefreshAvailableTargets()
    {
        GameObject[] potentialTargets = GameObject.FindGameObjectsWithTag(enemyTag);
        availableTargets.Clear();

        foreach (GameObject target in potentialTargets)
        {
            if (IsValidTarget(target))
                availableTargets.Add(target);
        }
    }

    /// <summary>
    /// A target is valid if it exists, is alive, and is within range.
    /// FOV is intentionally NOT checked here — a locked target stays locked even outside FOV.
    /// </summary>
    private bool IsValidTarget(GameObject target)
    {
        if (target == null) return false;

        if (!IsAliveAndExists(target)) return false;

        float distance = Vector3.Distance(transform.position, target.transform.position);
        if (distance > maxTargetRange) return false;

        return true;
    }

    /// <summary>
    /// Returns true if the target is not null and its Health component (if any) reports alive.
    /// </summary>
    private bool IsAliveAndExists(GameObject target)
    {
        if (target == null) return false;

        Health targetHealth = target.GetComponent<Health>();
        if (targetHealth != null && !targetHealth.IsAlive()) return false;

        return true;
    }

    /// <summary>
    /// Picks the best initial target:
    /// 1. Lowest angle within FOV (most centered in the crosshair).
    /// 2. If nothing is in FOV, falls back to the closest target in range.
    /// </summary>
    private GameObject GetBestAutoTarget()
    {
        GameObject bestInFOV = null;
        float lowestAngle = float.MaxValue;

        GameObject closestOverall = null;
        float closestDistance = float.MaxValue;

        foreach (GameObject target in availableTargets)
        {
            Vector3 directionToTarget = (target.transform.position - cameraTransform.position).normalized;
            float angle = Vector3.Angle(cameraTransform.forward, directionToTarget);
            float distance = Vector3.Distance(transform.position, target.transform.position);

            // Track the most centered target inside the FOV cone
            if (angle <= targetingFOV && angle < lowestAngle)
            {
                lowestAngle = angle;
                bestInFOV = target;
            }

            // Track the closest target regardless of FOV (fallback)
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestOverall = target;
            }
        }

        // FOV target wins; fallback to closest if FOV is empty
        return bestInFOV != null ? bestInFOV : closestOverall;
    }

    // ─── Public API ───────────────────────────────────────────────────────────

    public Vector3 GetTargetPosition()
    {
        return currentTarget != null ? currentTarget.transform.position : Vector3.zero;
    }

    public bool HasTarget()
    {
        return currentTarget != null;
    }

    public float GetTargetDistance()
    {
        return currentTarget != null
            ? Vector3.Distance(transform.position, currentTarget.transform.position)
            : -1f;
    }

    public float GetTargetAngle()
    {
        if (currentTarget == null) return -1f;

        Vector3 directionToTarget = (currentTarget.transform.position - transform.position).normalized;
        return Vector3.Angle(transform.forward, directionToTarget);
    }
}