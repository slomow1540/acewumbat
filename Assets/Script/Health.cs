using UnityEngine;
using UnityEngine.Events;

public class Health : MonoBehaviour
{
    [Header("Health Settings")]
    [Tooltip("Maximum health")]
    public float maxHealth = 100f;
    [Tooltip("Current health")]
    public float currentHealth;
    [Tooltip("Is this object invulnerable?")]
    public bool isInvulnerable = false;
    
    [Header("Damage Settings")]
    [Tooltip("Damage multiplier for different damage types")]
    public float damageMultiplier = 1f;
    
    [Header("Events")]
    public UnityEvent onDeath;
    public UnityEvent<float> onDamage; // Passes damage amount
    public UnityEvent<float> onHeal; // Passes heal amount
    
    private void Awake()
    {
        currentHealth = maxHealth;
    }
    
    /// <summary>
    /// Apply damage to this object
    /// </summary>
    public void TakeDamage(float damage, GameObject attacker = null)
    {
        if (isInvulnerable || currentHealth <= 0) return;
        
        float actualDamage = damage * damageMultiplier;
        currentHealth -= actualDamage;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        
        onDamage?.Invoke(actualDamage);
        
        if (currentHealth <= 0)
        {
            Die();
        }
    }
    
    /// <summary>
    /// Heal this object
    /// </summary>
    public void Heal(float amount)
    {
        if (currentHealth <= 0) return;
        
        float actualHeal = Mathf.Min(amount, maxHealth - currentHealth);
        currentHealth += actualHeal;
        
        onHeal?.Invoke(actualHeal);
    }
    
    /// <summary>
    /// Kill this object instantly
    /// </summary>
    public void Die()
    {
        if (currentHealth <= 0)
        {
            onDeath?.Invoke();
            // Don't destroy by default - let other scripts handle it
        }
    }
    
    /// <summary>
    /// Check if object is alive
    /// </summary>
    public bool IsAlive()
    {
        return currentHealth > 0;
    }
    
    /// <summary>
    /// Get health as percentage (0-1)
    /// </summary>
    public float GetHealthPercent()
    {
        return currentHealth / maxHealth;
    }
}
