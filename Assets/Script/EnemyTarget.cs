using UnityEngine;

/// <summary>
/// Simple script for enemy targets. Attach this and Health component to any object.
/// Make sure to tag the object as "Enemy" in Unity Inspector.
/// </summary>
public class EnemyTarget : MonoBehaviour
{
    [Header("Enemy Settings")]
    [Tooltip("Should this enemy explode when destroyed?")]
    public bool explodeOnDeath = true;
    [Tooltip("Explosion effect prefab")]
    public GameObject explosionPrefab;
    [Tooltip("Delay before destroying object after death")]
    public float destroyDelay = 2f;
    
    [Header("Audio")]
    public AudioClip deathSound;
    
    private Health health;
    private bool isDead = false;
    
    private void Awake()
    {
        health = GetComponent<Health>();
        
        if (health == null)
        {
            Debug.LogError("EnemyTarget requires Health component!");
            return;
        }
        
        // Subscribe to death event
        health.onDeath.AddListener(OnDeath);
    }
    
    private void OnDeath()
    {
        if (isDead) return;
        isDead = true;
        
        // Spawn explosion
        if (explodeOnDeath && explosionPrefab != null)
        {
            Instantiate(explosionPrefab, transform.position, Quaternion.identity);
        }
        
        // Play death sound
        if (deathSound != null)
        {
            AudioSource.PlayClipAtPoint(deathSound, transform.position);
        }
        
        // Destroy after delay
        Destroy(gameObject, destroyDelay);
    }
    
    private void OnDestroy()
    {
        // Unsubscribe from events
        if (health != null)
        {
            health.onDeath.RemoveListener(OnDeath);
        }
    }
}
