using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerControlsUI : MonoBehaviour
{
    [Header("UI Customization")]
    public TMP_FontAsset customFont;
    public Color textColor = new Color(0.15f, 0.08f, 0.04f, 1f);

    [Header("Layout Settings")]
    public Vector2 margins = new Vector2(-50f, -40f);
    public float textWidth = 1400f;

    public GameObject controlsCanvasObj;

    void Start()
    {
        if (controlsCanvasObj == null && GameObject.Find("PlayerControlsCanvas") == null)
        {
            GenerateControlsUI();
        }
    }

    [ContextMenu("Generate Controls UI Now")]
    void GenerateControlsUI()
    {
        if (GameObject.Find("PlayerControlsCanvas") != null)
        {
            return;
        }

        controlsCanvasObj = new GameObject("PlayerControlsCanvas");
        Canvas canvas = controlsCanvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50; 

        CanvasScaler scaler = controlsCanvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        
        controlsCanvasObj.AddComponent<GraphicRaycaster>();

        GameObject textObj = new GameObject("ControlsText");
        textObj.transform.SetParent(controlsCanvasObj.transform, false);
        
        RectTransform textRT = textObj.AddComponent<RectTransform>();
        textRT.anchorMin = new Vector2(1f, 1f);
        textRT.anchorMax = new Vector2(1f, 1f);
        textRT.pivot = new Vector2(1f, 1f);
        textRT.sizeDelta = new Vector2(textWidth, 60f); 
        textRT.anchoredPosition = margins; 

        TextMeshProUGUI bodyText = textObj.AddComponent<TextMeshProUGUI>();
        bodyText.text = "<b>H</b> - Hook / Fishing<space=60><b>E</b> - Inventory<space=60><b>L</b> - Build Mode<space=60><b>F</b> - Pick Items";
        bodyText.alignment = TextAlignmentOptions.TopRight; 
        bodyText.fontSize = 30;
        bodyText.color = textColor;
        
        if (customFont != null) bodyText.font = customFont;
    }

    private void OnDestroy()
    {
        if (Application.isPlaying && controlsCanvasObj != null)
        {
            Destroy(controlsCanvasObj);
        }
    }
}
