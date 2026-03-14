using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

/// <summary>
/// Advanced Health System for all aircraft and entities
/// Combines plane control with HP management
/// Handles death sequences, damage effects, game completion, and multipart objects
/// </summary>
public class Health : MonoBehaviour
{
    [System.Serializable]
    public class HealthEffect
    {
        [Tooltip("HP percentage threshold to activate (0-100)")]
        [Range(0f, 100f)]
        public float hpPercentageThreshold = 50f;

        [Tooltip("Object to enable at this HP threshold")]
        public GameObject effectPrefab;

        [Tooltip("If true, all effects at this threshold activate. If false, only one activates")]
        public bool activateAllEffectsAtThreshold = true;

        [Tooltip("If false, only the first matching effect activates at this threshold")]
        public bool isActive = true;

        private GameObject spawnedEffect;

        public void Activate(Transform parent)
        {
            if (!isActive || effectPrefab == null) return;

            if (spawnedEffect == null)
            {
                spawnedEffect = Instantiate(effectPrefab, parent.position, parent.rotation, parent);
            }
            else if (!spawnedEffect.activeInHierarchy)
            {
                spawnedEffect.SetActive(true);
            }
        }

        public void Deactivate()
        {
            if (spawnedEffect != null)
            {
                spawnedEffect.SetActive(false);
            }
        }

        public bool IsActivated => spawnedEffect != null && spawnedEffect.activeInHierarchy;
    }

    [Header("Health Settings")]
    [Tooltip("Maximum health")]
    public float maxHealth = 100f;

    [Tooltip("Point reward for killing this object")]
    public int point = 100;

    [Tooltip("Current health")]
    [SerializeField]
    public float currentHealth;

    [Tooltip("Is this object invulnerable?")]
    public bool isInvulnerable = false;

    [Header("Entity Type")]
    [Tooltip("Is this the player aircraft?")]
    public bool isPlayer = false;

    [Tooltip("Is this entity stationary (doesn't need control override)?")]
    public bool isStationary = false;

    [Header("Multipart System")]
    [Tooltip("Is this object a multipart object with destructible parts?")]
    public bool isMultipartObject = false;

    [Tooltip("Child parts with Health scripts (parent only)")]
    public List<GameObject> childParts = new List<GameObject>();

    [Tooltip("Damage multiplier increase when a child part dies")]
    [Range(0f, 2f)]
    public float damageMultiplierPerPartLost = 0.5f;

    [Tooltip("Is this a child part of a multipart object?")]
    public bool isChildPart = false;

    [Tooltip("Parent object (if this is a child part)")]
    public GameObject parentObject;

    [Header("Damage Settings")]
    [Tooltip("Damage multiplier for different damage types")]
    public float damageMultiplier = 1f;

    [Header("Death Settings")]
    [Tooltip("Should this entity explode when destroyed?")]
    public bool explodeOnDeath = true;

    [Tooltip("Explosion effect prefab")]
    public GameObject explosionPrefab;

    [Tooltip("Time delay before destroying object after death")]
    public float destroyDelay = 2f;

    [Tooltip("Chance of instant death (0-1, where 1 is 100% instant)")]
    [Range(0f, 1f)]
    public float instantDeathChance = 0.3f;

    [Header("Audio")]
    public AudioClip deathSound;

    [Header("Health Effects")]
    [Tooltip("List of effects to enable at specific HP thresholds")]
    public List<HealthEffect> healthEffects = new List<HealthEffect>();

    [Header("Events")]
    public UnityEvent onDeath;
    public UnityEvent<float> onDamage; // Passes damage amount
    public UnityEvent<float> onHeal; // Passes heal amount

    // References
    private GameController gameController;
    private ImprovedPlaneController planeController;
    private bool isDead = false;
    private List<float> activatedThresholds = new List<float>();
    private int partsDead = 0;

    private void Awake()
    {
        currentHealth = maxHealth;

        // Find GameController
        gameController = FindObjectOfType<GameController>();

        // Get plane controller if not stationary
        if (!isStationary)
        {
            planeController = GetComponent<ImprovedPlaneController>();
        }

        // Sort health effects by threshold for easier management
        if (healthEffects != null && healthEffects.Count > 0)
        {
            healthEffects.Sort((a, b) => b.hpPercentageThreshold.CompareTo(a.hpPercentageThreshold));
        }

        // Setup multipart system
        if (isMultipartObject)
        {
            SetupChildParts();
        }
    }

    private void Start()
    {
        // Register with GameController
        if (gameController != null)
        {
            gameController.RegisterEntity(this, gameObject.tag, isPlayer);
        }
    }

    private void Update()
    {
        // Check health effects each frame
        CheckHealthEffects();
    }

    /// <summary>
    /// Setup child parts for multipart objects
    /// </summary>
    private void SetupChildParts()
    {
        if (childParts == null || childParts.Count == 0) return;

        // Iterate through child parts and setup references
        for (int i = 0; i < childParts.Count; i++)
        {
            if (childParts[i] == null) continue;

            Health childHealth = childParts[i].GetComponent<Health>();
            if (childHealth != null)
            {
                // Mark as child part
                childHealth.isChildPart = true;
                childHealth.parentObject = gameObject;
            }
            else
            {
                Debug.LogWarning($"Child part {childParts[i].name} doesn't have Health component!");
            }
        }
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
            Die(attacker);
        }
    }

    public void Heal(float amount)
    {
        if (currentHealth <= 0) return;

        float actualHeal = Mathf.Min(amount, maxHealth - currentHealth);
        currentHealth += actualHeal;

        onHeal?.Invoke(actualHeal);
    }

    private void CheckHealthEffects()
    {
        if (healthEffects == null || healthEffects.Count == 0) return;

        float currentHealthPercent = GetHealthPercent() * 100f;

        for (int i = 0; i < healthEffects.Count; i++)
        {
            HealthEffect effect = healthEffects[i];

            if (currentHealthPercent <= effect.hpPercentageThreshold)
            {
                if (!activatedThresholds.Contains(effect.hpPercentageThreshold))
                {
                    effect.Activate(transform);
                    activatedThresholds.Add(effect.hpPercentageThreshold);

                    // If not activating all, break after first
                    if (!effect.activateAllEffectsAtThreshold)
                    {
                        break;
                    }
                }
            }
        }
    }

    /// <summary>
    /// Called when a child part dies (for multipart objects)
    /// </summary>
    public void NotifyChildPartDead(float damageMultIncrease)
    {
        if (!isMultipartObject) return;

        partsDead++;
        damageMultiplier += damageMultIncrease;

        Debug.Log($"{gameObject.name} lost a part! Total parts lost: {partsDead}, New damage multiplier: {damageMultiplier}");
    }

    /// <summary>
    /// Notify parent that this child part died
    /// </summary>
    private void NotifyParentOfDeath()
    {
        if (!isChildPart || parentObject == null) return;

        Health parentHealth = parentObject.GetComponent<Health>();
        if (parentHealth != null)
        {
            parentHealth.NotifyChildPartDead(damageMultiplierPerPartLost);
        }
    }

    /// <summary>
    /// Main death function
    /// </summary>
    public void Die(GameObject attacker = null)
    {
        if (isDead) return;
        isDead = true;

        // If this is a child part, notify parent
        if (isChildPart)
        {
            NotifyParentOfDeath();
        }

        // If this is a multipart parent, kill all children with same attacker
        if (isMultipartObject)
        {
            KillAllChildParts(attacker);
        }

        // Force controls to act as if crashing (if not stationary)
        if (!isStationary && planeController != null)
        {
            ForceCrashControls();
        }

        // Determine if instant death or delayed
        bool willBeInstant = Random.value < instantDeathChance;

        if (willBeInstant)
        {
            ExecuteDeath();
        }
        else
        {
            Invoke(nameof(ExecuteDeath), destroyDelay);
        }

        // Notify GameController
        if (gameController != null)
        {
            gameController.NotifyEntityDeath(this, gameObject.tag, isPlayer, attacker);
        }
    }

    /// <summary>
    /// Kill all child parts when parent dies (propagates attacker for scoring)
    /// </summary>
    private void KillAllChildParts(GameObject attacker)
    {
        if (childParts == null || childParts.Count == 0) return;

        foreach (GameObject part in childParts)
        {
            if (part == null) continue;

            Health partHealth = part.GetComponent<Health>();
            if (partHealth != null && partHealth.IsAlive())
            {
                // Kill part with same attacker (for scoring)
                partHealth.Die(attacker);
            }
        }
    }

    /// <summary>
    /// Execute the actual death sequence
    /// </summary>
    private void ExecuteDeath()
    {
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

        // Invoke death event
        onDeath?.Invoke();

        // Destroy object (player plane is handled by GameController)
        if (!isPlayer)
        {
            Destroy(gameObject);
        }
        else
        {
            if (planeController != null)
            {
                planeController.maxThrust = 0f;
                planeController.thrust = 0f;
            }
        }
    }

    /// <summary>
    /// Force plane controls to crash (zeroes input, induces downward spiral)
    /// </summary>
    private void ForceCrashControls()
    {
        if (!isStationary && planeController != null)
        {
            // Set thrust to zero
            planeController.thrust = 0f;

            // Add strong downward and random rotational forces to simulate crash
            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.AddForce(Vector3.down * 10000f);
                rb.AddForce(transform.forward * 10000f);
                rb.AddTorque(Random.insideUnitSphere * 300f);
            }
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

    /// <summary>
    /// Get current health value
    /// </summary>
    public float GetCurrentHealth()
    {
        return currentHealth;
    }

    /// <summary>
    /// Get max health value
    /// </summary>
    public float GetMaxHealth()
    {
        return maxHealth;
    }

    /// <summary>
    /// Get number of parts destroyed (for multipart objects)
    /// </summary>
    public int GetPartsDestroyed()
    {
        return partsDead;
    }

    /// <summary>
    /// Get total number of parts (for multipart objects)
    /// </summary>
    public int GetTotalParts()
    {
        if (childParts == null) return 0;
        return childParts.Count;
    }

    private void OnDestroy()
    {
        // Cleanup health effects
        if (healthEffects != null)
        {
            foreach (HealthEffect effect in healthEffects)
            {
                effect.Deactivate();
            }
        }
    }
}