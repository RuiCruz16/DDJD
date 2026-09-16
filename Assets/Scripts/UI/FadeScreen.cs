using UnityEngine;
using System.Collections;

public class FadeScreen : MonoBehaviour
{
    public CanvasGroup fadeGroup;
    public float fadeDuration = 2f;

    private void Start()
    {
        fadeGroup.alpha = 1f;
        StartCoroutine(FadeIn());
    }

    private IEnumerator FadeIn()
    {
        float elapsedTime = 0f;

        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            fadeGroup.alpha = 1f - Mathf.Clamp01(elapsedTime / fadeDuration);
            yield return null; 
        }

        fadeGroup.alpha = 0f;
        
        fadeGroup.gameObject.SetActive(false);
    }
}