using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIEventPasser : MonoBehaviour, IScrollHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private ScrollRect parentScrollRect;

    void Start()
    {
        parentScrollRect = GetComponentInParent<ScrollRect>();
    }

    public void OnScroll(PointerEventData eventData)
    {
        if (parentScrollRect != null) parentScrollRect.OnScroll(eventData);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (parentScrollRect != null) parentScrollRect.OnBeginDrag(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (parentScrollRect != null) parentScrollRect.OnDrag(eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (parentScrollRect != null) parentScrollRect.OnEndDrag(eventData);
    }
}