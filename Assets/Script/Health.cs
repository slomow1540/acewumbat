using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

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
        public GameObject effectObject;

        public void Activate()
        {
            if (effectObject != null)
            {
                effectObject.SetActive(true);
            }
        }

        public void Deactivate()
        {
            if (effectObject != null)
            {
                effectObject.SetActive(false);
            }
        }

        public bool IsActivated => effectObject != null && effectObject.activeInHierarchy;
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

    [Header("Death Effects")]
    [Tooltip("Plane model child object to disable on death")]
    public GameObject planeModelObject;

    [Tooltip("Particle emitters to stop on death")]
    public List<ParticleSystem> damageEmitters = new List<ParticleSystem>();

    [Header("Events")]
    public UnityEvent onDeath;
    public UnityEvent<float> onDamage; // Passes damage amount
    public UnityEvent<float> onHeal; // Passes heal amount

    [Header("Terrain Collision")]
    [Tooltip("Layer name that should cause instant death on collision")]
    public string terrainLayerName = "Terrain";

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
            healthEffects.Sort(
                (a, b) => b.hpPercentageThreshold.CompareTo(a.hpPercentageThreshold)
            );
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

    /// Setup child parts for multipart objects
    private void SetupChildParts()
    {
        if (childParts == null || childParts.Count == 0)
            return;

        for (int i = 0; i < childParts.Count; i++)
        {
            if (childParts[i] == null)
                continue;

            Health childHealth = childParts[i].GetComponent<Health>();
            if (childHealth != null)
            {
                childHealth.isChildPart = true;
                childHealth.parentObject = gameObject;
            }
            else
            {
                Debug.LogWarning($"Child part {childParts[i].name} doesn't have Health component!");
            }
        }
    }

    /// Apply damage duh
    public void TakeDamage(float damage, GameObject attacker = null)
    {
        if (isInvulnerable || currentHealth <= 0)
            return;

        float actualDamage = damage * damageMultiplier;
        currentHealth -= actualDamage;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        onDamage?.Invoke(actualDamage);

        CheckHealthEffects();

        if (currentHealth <= 0)
        {
            Die(attacker);
        }
    }

    public void Heal(float amount)
    {
        if (currentHealth <= 0)
            return;

        float actualHeal = Mathf.Min(amount, maxHealth - currentHealth);
        currentHealth += actualHeal;

        onHeal?.Invoke(actualHeal);
    }

    private void CheckHealthEffects()
    {
        if (healthEffects == null || healthEffects.Count == 0)
            return;

        float currentHealthPercent = GetHealthPercent() * 100f;

        for (int i = 0; i < healthEffects.Count; i++)
        {
            HealthEffect effect = healthEffects[i];

            if (currentHealthPercent <= effect.hpPercentageThreshold)
            {
                effect.Activate();
            }
        }
    }

    public void NotifyChildPartDead(float damageMultIncrease)
    {
        if (!isMultipartObject)
            return;

        partsDead++;
        damageMultiplier += damageMultIncrease;

        Debug.Log(
            $"{gameObject.name} lost a part! Total parts lost: {partsDead}, New damage multiplier: {damageMultiplier}"
        );
    }

    /// Notify parent that this child part
    private void NotifyParentOfDeath()
    {
        if (!isChildPart || parentObject == null)
            return;

        Health parentHealth = parentObject.GetComponent<Health>();
        if (parentHealth != null)
        {
            parentHealth.NotifyChildPartDead(damageMultiplierPerPartLost);
        }
    }

    /// Main death function
    public void Die(GameObject attacker = null)
    {
        if (isDead)
            return;
        isDead = true;

        if (isChildPart)
        {
            NotifyParentOfDeath();
        }

        if (isMultipartObject)
        {
            KillAllChildParts(attacker);
        }

        if (!isStationary && planeController != null)
        {
            ForceCrashControls();
        }

        bool willBeInstant = Random.value < instantDeathChance;

        if (willBeInstant)
        {
            ExecuteDeath();
        }
        else
        {
            Invoke(nameof(ExecuteDeath), destroyDelay);
        }

        if (gameController != null)
        {
            gameController.NotifyEntityDeath(this, gameObject.tag, isPlayer, attacker);
        }
    }

    private void StopDamageEmitters()
    {
        if (damageEmitters == null || damageEmitters.Count == 0)
            return;

        foreach (ParticleSystem emitter in damageEmitters)
        {
            if (emitter != null)
            {
                emitter.Stop();
            }
        }
    }

    private void KillAllChildParts(GameObject attacker)
    {
        if (childParts == null || childParts.Count == 0)
            return;

        foreach (GameObject part in childParts)
        {
            if (part == null)
                continue;

            Health partHealth = part.GetComponent<Health>();
            if (partHealth != null && partHealth.IsAlive())
            {
                partHealth.Die(attacker);
            }
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (isStationary || isDead)
            return;

        if (collision.gameObject.layer == LayerMask.NameToLayer(terrainLayerName))
        {
            DieInstantlyFromTerrain(collision.gameObject);
        }
    }

    private void DieInstantlyFromTerrain(GameObject attacker = null)
    {
        if (isDead)
            return;
        isDead = true;

        // Cancel any delayed death that may already be scheduled
        CancelInvoke(nameof(ExecuteDeath));

        if (isChildPart)
        {
            NotifyParentOfDeath();
        }

        if (isMultipartObject)
        {
            KillAllChildParts(attacker);
        }

        if (!isStationary && planeController != null)
        {
            ForceCrashControls();
        }

        if (gameController != null)
        {
            gameController.NotifyEntityDeath(this, gameObject.tag, isPlayer, attacker);
        }

        ExecuteDeath();
    }

    private void ExecuteDeath()
    {
        if (planeModelObject != null)
        {
            planeModelObject.SetActive(false);
            StopDamageEmitters();
        }

        if (explodeOnDeath && explosionPrefab != null)
        {
            Instantiate(explosionPrefab, transform.position, Quaternion.identity);
        }

        if (deathSound != null)
        {
            AudioSource.PlayClipAtPoint(deathSound, transform.position);
        }

        onDeath?.Invoke();

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

    private void ForceCrashControls()
    {
        if (!isStationary && planeController != null)
        {
            planeController.thrust = 0f;

            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.AddForce(Vector3.down * 10000f);
                rb.AddForce(transform.forward * 10000f);
                rb.AddTorque(Random.insideUnitSphere * 300f);
            }
        }
    }

    public bool IsAlive()
    {
        return currentHealth > 0;
    }

    public float GetHealthPercent()
    {
        return currentHealth / maxHealth;
    }

    public float GetCurrentHealth()
    {
        return currentHealth;
    }

    public float GetMaxHealth()
    {
        return maxHealth;
    }

    public int GetPartsDestroyed()
    {
        return partsDead;
    }

    public int GetTotalParts()
    {
        if (childParts == null)
            return 0;
        return childParts.Count;
    }

    private void OnDestroy()
    {
        if (healthEffects != null)
        {
            foreach (HealthEffect effect in healthEffects)
            {
                effect.Deactivate();
            }
        }
    }
}
