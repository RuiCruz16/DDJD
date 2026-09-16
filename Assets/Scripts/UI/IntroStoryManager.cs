using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.UI;
public class IntroStoryManager : MonoBehaviour
{
    [Header("UI References")]
    public GameObject storyPanel;
    public TextMeshProUGUI storyText;
    public CanvasGroup storyCanvasGroup;

    [Header("Crossfade Image Setup")]
    public Image baseBackgroundImage;
    public Image fadeBackgroundImage;
    public CanvasGroup fadeImageCanvasGroup;
    
    [Header("Story Settings")]
    [TextArea(3, 5)]
    public string[] storyPages;
    public Sprite[] storyBackgrounds;
    private int currentPage = 0;

    [Header("Effects Settings")]
    public float fadeInDuration = 1f;
    public float typingSpeed = 0.05f; // lower is faster
    public float backgroundCrossfadeDuration = 0.8f;

    [Header("Main Menu Reference")]
    public MainMenu mainMenuScript;

    private Coroutine typingCoroutine;
    private Coroutine crossfadeCoroutine;
    private bool isTyping = false;

    public void StartStory()
    {
        storyPanel.SetActive(true);
        storyCanvasGroup.alpha = 0f;
        currentPage = 0;

        if (storyBackgrounds.Length > 0 && storyBackgrounds[0] != null && baseBackgroundImage != null)
        {
            baseBackgroundImage.sprite = storyBackgrounds[0];
        }
        
        if (fadeImageCanvasGroup != null)
        {
            fadeImageCanvasGroup.alpha = 0f;
        }

        StartCoroutine(FadeInStory());
    }

    private IEnumerator FadeInStory()
    {
        float elapsedTime = 0f;

        while (elapsedTime < fadeInDuration)
        {
            elapsedTime += Time.deltaTime;
            storyCanvasGroup.alpha = Mathf.Clamp01(elapsedTime / fadeInDuration);
            yield return null; 
        }

        storyCanvasGroup.alpha = 1f; 
        StartTypingLine();
    }

    public void NextPage()
    {
        if (isTyping)
        {
            if (typingCoroutine != null) StopCoroutine(typingCoroutine);
            storyText.text = storyPages[currentPage];
            isTyping = false;
        }
        else
        {
            currentPage++;

            if (currentPage < storyPages.Length)
            {
                StartTypingLine();
                TriggerBackgroundCrossfade();
            }
            else
            {
                mainMenuScript.PlayGame(); 
            }
        }
    }

    private void StartTypingLine()
    {
        if (typingCoroutine != null) 
            StopCoroutine(typingCoroutine);

        typingCoroutine = StartCoroutine(TypeLine());
    }

    private void TriggerBackgroundCrossfade()
    {
        if (crossfadeCoroutine != null) StopCoroutine(crossfadeCoroutine);
        crossfadeCoroutine = StartCoroutine(CrossfadeBackgroundsCoroutine());
    }

    private IEnumerator CrossfadeBackgroundsCoroutine()
    {
        if (baseBackgroundImage == null || fadeBackgroundImage == null || fadeImageCanvasGroup == null) yield break;
        if (currentPage >= storyBackgrounds.Length || storyBackgrounds[currentPage] == null) yield break;

        fadeBackgroundImage.sprite = storyBackgrounds[currentPage];
        fadeImageCanvasGroup.alpha = 0f;

        float elapsedTime = 0f;
        while (elapsedTime < backgroundCrossfadeDuration)
        {
            elapsedTime += Time.deltaTime;
            fadeImageCanvasGroup.alpha = Mathf.Clamp01(elapsedTime / backgroundCrossfadeDuration);
            yield return null;
        }
        fadeImageCanvasGroup.alpha = 1f;

        baseBackgroundImage.sprite = storyBackgrounds[currentPage];
        fadeImageCanvasGroup.alpha = 0f;
    }

    private IEnumerator TypeLine()
    {
        isTyping = true;
        storyText.text = ""; 

        foreach (char letter in storyPages[currentPage].ToCharArray())
        {
            storyText.text += letter;
            yield return new WaitForSeconds(typingSpeed); 
        }

        isTyping = false; 
    }
}