using UnityEngine;

/// <summary>
/// Checkpoint trigger for GameController trigger system
/// Place in level to trigger events when player reaches location
/// </summary>
public class Checkpoint : MonoBehaviour
{
    [Header("Checkpoint Settings")]
    [Tooltip("Unique ID for this checkpoint (used in GameController triggers)")]
    public string checkpointID = "Checkpoint1";
    
    [Tooltip("Only trigger once")]
    public bool triggerOnce = true;
    
    [Tooltip("Deactivate GameObject after triggering")]
    public bool deactivateAfterTrigger = false;
    
    [Header("Visual Feedback")]
    [Tooltip("Play sound when triggered")]
    public AudioClip triggerSound;
    
    [Tooltip("Particle effect when triggered")]
    public GameObject particleEffect;
    
    [Tooltip("Show debug messages")]
    public bool showDebugMessages = true;
    
    private bool hasTriggered = false;
    private GameController gameController;

    private void Start()
    {
        gameController = FindObjectOfType<GameController>();
        
        if (gameController == null)
        {
            Debug.LogWarning($"Checkpoint '{checkpointID}': No GameController found in scene!");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Check if already triggered
        if (triggerOnce && hasTriggered) return;
        
        // Check if player entered
        Health health = other.GetComponent<Health>();
        if (health != null && health.isPlayer)
        {
            TriggerCheckpoint();
        }
    }

    private void TriggerCheckpoint()
    {
        hasTriggered = true;
        
        if (showDebugMessages)
        {
            Debug.Log($"[Checkpoint] '{checkpointID}' reached!");
        }
        
        // Trigger in GameController
        if (gameController != null)
        {
            gameController.TriggerCheckpoint(checkpointID);
        }
        
        // Play sound
        if (triggerSound != null)
        {
            AudioSource.PlayClipAtPoint(triggerSound, transform.position);
        }
        
        // Spawn particle effect
        if (particleEffect != null)
        {
            GameObject fx = Instantiate(particleEffect, transform.position, Quaternion.identity);
            Destroy(fx, 5f);
        }
        
        // Deactivate if requested
        if (deactivateAfterTrigger)
        {
            gameObject.SetActive(false);
        }
    }

    private void OnDrawGizmos()
    {
        // Visualize checkpoint in editor
        Gizmos.color = hasTriggered ? Color.green : Color.yellow;
        Gizmos.DrawWireSphere(transform.position, 2f);
        
        // Show trigger radius if collider exists
        SphereCollider sphereCol = GetComponent<SphereCollider>();
        if (sphereCol != null)
        {
            Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
            Gizmos.DrawWireSphere(transform.position, sphereCol.radius);
        }
        
        BoxCollider boxCol = GetComponent<BoxCollider>();
        if (boxCol != null)
        {
            Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(boxCol.center, boxCol.size);
        }
    }
}
