using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ThirstBar : MonoBehaviour
{
    [Header("Thirst Settings")]
    public TextMeshProUGUI thirstText;
    public Gradient textGradient;

    [Header("Pulse Animation Settings")]
    public Transform pulseGroup;
    public float pulseSpeed = 1.2f;  // lower is slower
    public float pulseSize = 0.35f;  // bigger is bigger 

    private int maxThirstValue = 100;
    private bool isCritical = false;

    public void SetMaxThirst(int thirst)
    {
        maxThirstValue = thirst;
        UpdateText(thirst);
    }

    public void SetThirst(int thirst)
    {
        UpdateText(thirst);
    }

    private void UpdateText(int currentThirst)
    {
        float normalizedValue = maxThirstValue > 0 ? (float)currentThirst / maxThirstValue : 0f;
        int percentage = Mathf.RoundToInt(normalizedValue * 100f);

        thirstText.text = $"{percentage}%";

        if (textGradient != null)
        {
            thirstText.color = textGradient.Evaluate(normalizedValue);
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
