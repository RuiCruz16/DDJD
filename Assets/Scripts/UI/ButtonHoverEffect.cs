using UnityEngine;
using UnityEngine.EventSystems; // Required for detecting mouse hover

// We add interfaces to tell Unity to listen for the mouse entering and exiting
public class ButtonHoverEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Hover Settings")]
    public float hoverScaleMultiplier = 1.1f; // 1.1 means it gets 10% bigger
    
    private Vector3 originalScale;

    private void Start()
    {
        // Remember the starting size so we can shrink it back down later
        originalScale = transform.localScale;
    }

    // This runs the exact moment the mouse touches the button
    public void OnPointerEnter(PointerEventData eventData)
    {
        transform.localScale = originalScale * hoverScaleMultiplier;
    }

    // This runs the exact moment the mouse leaves the button
    public void OnPointerExit(PointerEventData eventData)
    {
        transform.localScale = originalScale;
    }
}