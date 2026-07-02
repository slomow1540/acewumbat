using UnityEngine;

/// <summary>
/// Add this component to enemy planes to allow AI control
/// Works alongside ImprovedPlaneController
/// The controller will check for this component and use these inputs instead of Input.GetAxis
/// </summary>
public class AIPlaneInput : MonoBehaviour
{
    [Header("AI Control Inputs")]
    [Tooltip("AI-controlled pitch input (-1 to 1)")]
    [Range(-1f, 1f)]
    public float aiPitchInput = 0f;
    
    [Tooltip("AI-controlled roll input (-1 to 1)")]
    [Range(-1f, 1f)]
    public float aiRollInput = 0f;
    
    [Tooltip("AI-controlled yaw input (-1 to 1)")]
    [Range(-1f, 1f)]
    public float aiYawInput = 0f;
    
    [Tooltip("AI-controlled thrust (0-100)")]
    [Range(0f, 100f)]
    public float aiThrustTarget = 50f;
    
    [Tooltip("AI wants to use high-G mode")]
    public bool aiHighGMode = false;
    
    /// <summary>
    /// Set control inputs for the AI (-1 to 1 for each axis)
    /// </summary>
    public void SetControlInputs(float pitch, float roll, float yaw)
    {
        aiPitchInput = Mathf.Clamp(pitch, -1f, 1f);
        aiRollInput = Mathf.Clamp(roll, -1f, 1f);
        aiYawInput = Mathf.Clamp(yaw, -1f, 1f);
    }
    
    /// <summary>
    /// Set target thrust (0-100 percentage)
    /// </summary>
    public void SetThrust(float targetThrust)
    {
        aiThrustTarget = Mathf.Clamp(targetThrust, 0f, 100f);
    }
    
    /// <summary>
    /// Enable/disable high-G mode
    /// </summary>
    public void SetHighGMode(bool enabled)
    {
        aiHighGMode = enabled;
    }
    
    /// <summary>
    /// Get current speed
    /// </summary>
    public float GetSpeed()
    {
        Rigidbody rb = GetComponent<Rigidbody>();
        return rb != null ? rb.linearVelocity.magnitude : 0f;
    }
}