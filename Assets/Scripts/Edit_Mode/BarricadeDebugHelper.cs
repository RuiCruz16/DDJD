using UnityEngine;

/// <summary>
/// Debug Helper: Simulates barricade damage for testing.
/// Add this to any GameObject, then use in the Inspector to trigger damage events.
/// </summary>
public class BarricadeDebugHelper : MonoBehaviour
{
    [Header("Debug Controls")]
    [SerializeField] private Barricade targetBarricade;
    [SerializeField] private float damageAmount = 1f;
    [SerializeField] private bool autoFindBarricades = true;

    void Update()
    {
        // Press 'B' to damage nearest barricade (for quick testing)
        if (Input.GetKeyDown(KeyCode.B))
        {
            Barricade barricade = GetNearestBarricade();
            if (barricade != null)
            {
                barricade.TakeDamage(Mathf.RoundToInt(damageAmount));
                Debug.Log("[BarricadeDebug] Damaged barricade. Health: " + barricade.GetCurrentHealth() + "/" + barricade.GetMaxHealth());
            }
            else
            {
                Debug.LogWarning("[BarricadeDebug] No barricades found nearby!");
            }
        }
    }

    /// <summary>
    /// Call this from the Inspector button to damage the target barricade.
    /// </summary>
    public void DamageTargetBarricade()
    {
        if (targetBarricade != null)
        {
            targetBarricade.TakeDamage(Mathf.RoundToInt(damageAmount));
            Debug.Log("[BarricadeDebug] Barricade health: " + targetBarricade.GetCurrentHealth() + "/" + targetBarricade.GetMaxHealth());
        }
        else
        {
            Debug.LogWarning("[BarricadeDebug] No target barricade assigned!");
        }
    }

    /// <summary>
    /// Finds and logs all barricades in the scene.
    /// </summary>
    public void LogAllBarricades()
    {
        Barricade[] allBarricades = FindObjectsByType<Barricade>(FindObjectsSortMode.None);
        Debug.Log("[BarricadeDebug] Found " + allBarricades.Length + " barricades:");
        foreach (Barricade b in allBarricades)
        {
            Debug.Log("  - " + b.name + ": " + b.GetCurrentHealth() + "/" + b.GetMaxHealth() + " health");
        }
    }

    Barricade GetNearestBarricade()
    {
        if (!autoFindBarricades) return targetBarricade;

        Barricade[] barricades = FindObjectsByType<Barricade>(FindObjectsSortMode.None);
        if (barricades.Length == 0) return null;

        Barricade nearest = barricades[0];
        float nearestDistance = Vector3.Distance(transform.position, nearest.transform.position);

        for (int i = 1; i < barricades.Length; i++)
        {
            float distance = Vector3.Distance(transform.position, barricades[i].transform.position);
            if (distance < nearestDistance)
            {
                nearest = barricades[i];
                nearestDistance = distance;
            }
        }

        return nearest;
    }
}
