using UnityEngine;

/// <summary>
/// Barricade damage system. Tracks durability and provides visual feedback.
/// Destroyed after taking maxHealth damage.
/// </summary>
public class Barricade : MonoBehaviour
{
    [Header("Durability")]
    [SerializeField] private int maxHealth = 3;
    private int currentHealth;

    [Header("Visual Feedback")]
    [SerializeField] private float damageScaleReduction = 1.8f; // Scale reduction per hit
    [SerializeField] private float colorDarkenPercentage = 0.60f; // Darkening per hit (0-1)
    [SerializeField] private float wobbleAmount = 0.35f;
    [SerializeField] private float wobbleDuration = 0.3f;

    private Vector3 originalScale;
    private Color originalColor;
    private Renderer meshRenderer;
    private Material materialInstance;
    private float wobbleTimer = 0f;
    private Quaternion originalRotation;

    void Start()
    {
        currentHealth = maxHealth;
        originalScale = transform.localScale;
        originalRotation = transform.localRotation;

        meshRenderer = GetComponent<Renderer>();
        if (meshRenderer != null)
        {
            // Create instance so we don't affect other barricades
            materialInstance = new Material(meshRenderer.material);
            meshRenderer.material = materialInstance;
            originalColor = materialInstance.color;
        }

        gameObject.tag = "Barricade";
    }

    void Update()
    {
        // Handle wobble effect after hit
        if (wobbleTimer > 0)
        {
            wobbleTimer -= Time.deltaTime;
            float wobbleProgress = 1f - (wobbleTimer / wobbleDuration);
            float wobbleAmount_t = wobbleAmount * Mathf.Sin(wobbleProgress * Mathf.PI);
            
            transform.localRotation = originalRotation * Quaternion.Euler(wobbleAmount_t, wobbleAmount_t * 0.5f, 0);
            
            if (wobbleTimer <= 0)
            {
                transform.localRotation = originalRotation;
            }
        }
    }

    /// <summary>
    /// Called when the shark attacks this barricade.
    /// </summary>
    public void TakeDamage(int damage = 1)
    {
        currentHealth -= damage;
        UpdateVisuals();
        wobbleTimer = wobbleDuration;

        if (currentHealth <= 0)
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Get current health (useful for UI or debugging).
    /// </summary>
    public int GetCurrentHealth()
    {
        return Mathf.Max(0, currentHealth);
    }

    /// <summary>
    /// Get max health.
    /// </summary>
    public int GetMaxHealth()
    {
        return maxHealth;
    }

    /// <summary>
    /// Check if barricade is destroyed.
    /// </summary>
    public bool IsDestroyed()
    {
        return currentHealth <= 0;
    }

    void UpdateVisuals()
    {
        // Calculate health percentage (0 = destroyed, 1 = full health)
        float healthPercent = Mathf.Max(0, (float)currentHealth / maxHealth);

        // Scale: compress downward as damage increases
        float scaleReduction = (1f - healthPercent) * damageScaleReduction;
        transform.localScale = new Vector3(
            originalScale.x,
            originalScale.y * (1f - scaleReduction),
            originalScale.z
        );

        // Color: darken as damage increases
        if (materialInstance != null)
        {
            Color darkenedColor = originalColor;
            float darkenAmount = (1f - healthPercent) * colorDarkenPercentage;
            darkenedColor.r = Mathf.Max(0, darkenedColor.r * (1f - darkenAmount));
            darkenedColor.g = Mathf.Max(0, darkenedColor.g * (1f - darkenAmount));
            darkenedColor.b = Mathf.Max(0, darkenedColor.b * (1f - darkenAmount));
            materialInstance.color = darkenedColor;
        }
    }
}
