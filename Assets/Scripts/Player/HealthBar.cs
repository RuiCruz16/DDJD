using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HealthBar : MonoBehaviour
{
    [Header("Health Settings")]
    public TextMeshProUGUI healthText;
    public Gradient textGradient;

    [Header("Pulse Animation Settings")]
    public Transform pulseGroup;
    public float pulseSpeed = 1.2f;  // lower is slower
    public float pulseSize = 0.35f;  // bigger is bigger 

    private int maxHealthValue = 100;
    private bool isCritical = false;

    public void SetMaxHealth(int health)
    {
        maxHealthValue = health;
        UpdateText(health);
    }

    public void SetHealth(int health)
    {
        UpdateText(health);
    }

    private void UpdateText(int currentHealth)
    {
        float normalizedValue = maxHealthValue > 0 ? (float)currentHealth / maxHealthValue : 0f;
        int percentage = Mathf.RoundToInt(normalizedValue * 100f);

        healthText.text = $"{percentage}%";

        if (textGradient != null)
        {
            healthText.color = textGradient.Evaluate(normalizedValue);
        }
        isCritical = (percentage <= 20);
    }

    private void Update()
    {
        if (pulseGroup == null) return;

        if (isCritical)
        {
            float pulse = 1f + Mathf.PingPong(Time.unscaledTime * pulseSpeed, pulseSize); 
            pulseGroup.localScale = new Vector3(pulse, pulse, 1f);
        }
        else
        {
            if (pulseGroup.localScale != Vector3.one)
            {
                pulseGroup.localScale = Vector3.one;
            }
        }
    }
}
