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
        // Find camera for targeting calculations
        cameraTransform = Camera.main?.transform;
        if (cameraTransform == null)
        {
            cameraTransform = transform; // Fallback to plane's transform
        }
    }
    
    private void Update()
    {
        HandleTargetInput();
        
        // Auto-target update
        if (autoTarget && Time.time - lastAutoTargetUpdate > autoTargetUpdateRate)
        {
            UpdateAutoTarget();
            lastAutoTargetUpdate = Time.time;
        }
        
        // Clear target if it's destroyed or out of range
        if (currentTarget != null)
        {
            if (!IsValidTarget(currentTarget))
            {
                ClearTarget();
            }
        }
    }
    
    private void HandleTargetInput()
    {
        // Cycle to next target
        if (Input.GetKeyDown(nextTargetKey))
        {
            CycleTarget(1);
        }
        
        // Cycle to previous target
        if (Input.GetKeyDown(previousTargetKey))
        {
            CycleTarget(-1);
        }
        
        // Clear target
        if (Input.GetKeyDown(clearTargetKey))
        {
            ClearTarget();
        }
    }
    
    private void UpdateAutoTarget()
    {
        // Find all potential targets
        GameObject[] potentialTargets = GameObject.FindGameObjectsWithTag(enemyTag);
        availableTargets.Clear();
        
        foreach (GameObject target in potentialTargets)
        {
            if (IsValidTarget(target))
            {
                availableTargets.Add(target);
            }
        }
        
        // If no current target, lock onto nearest
        if (currentTarget == null && availableTargets.Count > 0)
        {
            currentTarget = GetClosestTargetInFOV();
        }
    }
    
    private void CycleTarget(int direction)
    {
        UpdateAutoTarget(); // Refresh target list
        
        if (availableTargets.Count == 0)
        {
            ClearTarget();
            return;
        }
        
        int currentIndex = availableTargets.IndexOf(currentTarget);
        
        if (currentIndex == -1)
        {
            // No current target, select first
            currentTarget = availableTargets[0];
        }
        else
        {
            // Cycle through targets
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
    
    private bool IsValidTarget(GameObject target)
    {
        if (target == null) return false;
        
        // Check if target is alive
        Health targetHealth = target.GetComponent<Health>();
        if (targetHealth != null && !targetHealth.IsAlive())
            return false;
        
        // Check range
        float distance = Vector3.Distance(transform.position, target.transform.position);
        if (distance > maxTargetRange)
            return false;
        
        // Check field of view
        Vector3 directionToTarget = (target.transform.position - cameraTransform.position).normalized;
        float angle = Vector3.Angle(cameraTransform.forward, directionToTarget);
        if (angle > targetingFOV)
            return false;
        
        return true;
    }
    
    private GameObject GetClosestTargetInFOV()
    {
        GameObject closest = null;
        float closestDistance = float.MaxValue;
        
        foreach (GameObject target in availableTargets)
        {
            float distance = Vector3.Distance(transform.position, target.transform.position);
            
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closest = target;
            }
        }
        
        return closest;
    }
    
    /// <summary>
    /// Get current target position
    /// </summary>
    public Vector3 GetTargetPosition()
    {
        if (currentTarget != null)
            return currentTarget.transform.position;
        
        return Vector3.zero;
    }
    
    /// <summary>
    /// Check if we have a valid target
    /// </summary>
    public bool HasTarget()
    {
        return currentTarget != null;
    }
    
    /// <summary>
    /// Get distance to current target
    /// </summary>
    public float GetTargetDistance()
    {
        if (currentTarget != null)
            return Vector3.Distance(transform.position, currentTarget.transform.position);
        
        return -1f;
    }
    
    /// <summary>
    /// Get angle to current target
    /// </summary>
    public float GetTargetAngle()
    {
        if (currentTarget != null)
        {
            Vector3 directionToTarget = (currentTarget.transform.position - transform.position).normalized;
            return Vector3.Angle(transform.forward, directionToTarget);
        }
        
        return -1f;
    }
}
