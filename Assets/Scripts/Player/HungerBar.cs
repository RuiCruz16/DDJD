using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HungerBar : MonoBehaviour
{
    [Header("Hunger Settings")]
    public TextMeshProUGUI hungerText;
    public Gradient textGradient;

    [Header("Pulse Animation Settings")]
    public Transform pulseGroup;
    public float pulseSpeed = 1.2f;  // lower is slower
    public float pulseSize = 0.35f;  // bigger is bigger 

    private int maxHungerValue = 100;
    private bool isCritical = false;

    public void SetMaxHunger(int hunger)
    {
        maxHungerValue = hunger;
        UpdateText(hunger);
    }

    public void SetHunger(int hunger)
    {
        UpdateText(hunger);
    }

    private void UpdateText(int currentHunger)
    {
        float normalizedValue = maxHungerValue > 0 ? (float)currentHunger / maxHungerValue : 0f;
        int percentage = Mathf.RoundToInt(normalizedValue * 100f);

        hungerText.text = $"{percentage}%";

        if (textGradient != null)
        {
            hungerText.color = textGradient.Evaluate(normalizedValue);
        }
        isCritical = (percentage <= 30);
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
