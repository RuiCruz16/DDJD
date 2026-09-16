using UnityEngine;
using TMPro;

/// <summary>
/// Optional: Displays barricade health/durability above the barricade.
/// Attach this to the Barricade prefab if you want a health indicator.
/// Make sure the barricade has a Canvas child with TextMeshProUGUI component.
/// </summary>
public class BarricadeHealthDisplay : MonoBehaviour
{
    private Barricade barricade;
    private TextMeshProUGUI healthText;
    private RectTransform canvasRect;
    private Transform mainCamera;

    [SerializeField] private bool showHealthNumber = true; // Show "2/3" format
    [SerializeField] private float displayDistance = 5f;   // How far to show indicator

    void Start()
    {
        barricade = GetComponent<Barricade>();
        mainCamera = Camera.main?.transform;

        // Try to find TextMeshProUGUI in children
        TextMeshProUGUI[] textComponents = GetComponentsInChildren<TextMeshProUGUI>();
        if (textComponents.Length > 0)
        {
            healthText = textComponents[0];
            canvasRect = healthText.GetComponent<RectTransform>();
        }

        if (healthText == null)
        {
            Debug.LogWarning("[BarricadeHealthDisplay] No TextMeshProUGUI found on " + gameObject.name + ". Health display disabled.");
            enabled = false;
        }
    }

    void Update()
    {
        if (barricade == null || healthText == null || mainCamera == null)
            return;

        // Check if barricade is in range
        float distance = Vector3.Distance(mainCamera.position, transform.position);
        if (distance > displayDistance)
        {
            healthText.enabled = false;
            return;
        }

        healthText.enabled = true;

        // Update text
        if (showHealthNumber)
        {
            int current = barricade.GetCurrentHealth();
            int max = barricade.GetMaxHealth();
            healthText.text = current + "/" + max;
        }
        else
        {
            // Just show health bars: [==]  [= ]  [ =]  [ ]
            int current = barricade.GetCurrentHealth();
            int max = barricade.GetMaxHealth();
            int fillPercentage = Mathf.RoundToInt((float)current / max * 3);
            string[] bars = new string[] { "☐ ☐ ☐", "☐ ☐ ☑", "☐ ☑ ☑", "☑ ☑ ☑" };
            healthText.text = fillPercentage < bars.Length ? bars[fillPercentage] : bars[bars.Length - 1];
        }

        // Color based on health
        float healthPercent = (float)barricade.GetCurrentHealth() / barricade.GetMaxHealth();
        if (healthPercent > 0.66f)
            healthText.color = Color.green;
        else if (healthPercent > 0.33f)
            healthText.color = Color.yellow;
        else
            healthText.color = Color.red;
    }
}
