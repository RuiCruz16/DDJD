using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ItemControlsUI : MonoBehaviour
{
    [Header("UI Settings")]
    public Vector3 floatingOffset = new Vector3(0f, 2.2f, 0f);
    public string interactionText = ""; 
    public TMP_FontAsset customFont;

    [Header("Text Settings")]
    public float fontSize = 60f;
    public Color textColor = new Color(1f, 1f, 1f, 0.95f);

    [Header("Bubble Colors")]
    public Color borderColor = new Color(0.36f, 0.22f, 0.13f, 1f);
    public Color innerColor = new Color(0.6830188f, 0.5312209f, 0.4214097f, 1f);

    [Header("Bubble Layout")]
    public float horizontalPadding = 120f;
    public float boxHeight = 120f;
    public float canvasScale = 0.005f;

    [Header("Generated Shape")]
    public int textureHeight = 320;
    public int borderThickness = 15;
    public int cornerRadius = 52;
    public int tailWidth = 120;
    public int tailHeight = 70;
    public int tailJoinInset = 16;

    [Header("Text Margins")]
    public float textPaddingLeft = 40f;
    public float textPaddingRight = 40f;
    public float textPaddingTop = 18f;
    public float textPaddingBottom = 18f;

    private GameObject canvasObj;
    private Image bubbleImage;
    private RectTransform bubbleRT;
    private TextMeshProUGUI uiText;
    private RectTransform textRT;

    void Start()
    {
        CreateWorldUI();
        HideUI();
    }

    void LateUpdate()
    {
        if (canvasObj != null && canvasObj.activeSelf && Camera.main != null)
        {
            canvasObj.transform.forward = Camera.main.transform.forward;
        }
    }

    void CreateWorldUI()
    {
        canvasObj = new GameObject("FloatingCanvas");
        canvasObj.transform.SetParent(transform, false);

        Vector3 finalOffset = new Vector3(
            floatingOffset.x / transform.lossyScale.x,
            floatingOffset.y / transform.lossyScale.y,
            floatingOffset.z / transform.lossyScale.z
        );
        canvasObj.transform.localPosition = finalOffset;

        Vector3 finalScale = new Vector3(
            canvasScale / transform.lossyScale.x,
            canvasScale / transform.lossyScale.y,
            canvasScale / transform.lossyScale.z
        );
        canvasObj.transform.localScale = finalScale;

        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 10;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.dynamicPixelsPerUnit = 10f;

        GameObject bubbleObj = new GameObject("Bubble");
        bubbleObj.transform.SetParent(canvasObj.transform, false);

        bubbleRT = bubbleObj.AddComponent<RectTransform>();
        bubbleImage = bubbleObj.AddComponent<Image>();
        bubbleImage.raycastTarget = false;
        bubbleImage.color = Color.white;

        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(bubbleObj.transform, false);

        textRT = textObj.AddComponent<RectTransform>();
        textRT.anchorMin = new Vector2(0f, 0f);
        textRT.anchorMax = new Vector2(1f, 1f);

        uiText = textObj.AddComponent<TextMeshProUGUI>();
        uiText.text = interactionText;
        uiText.alignment = TextAlignmentOptions.Center;
        uiText.fontSize = fontSize;
        uiText.color = textColor;
        uiText.textWrappingMode = TextWrappingModes.NoWrap;
        uiText.overflowMode = TextOverflowModes.Overflow;
        uiText.raycastTarget = false;

        if (customFont != null)
            uiText.font = customFont;

        UpdateBoxSize();
    }

    public void ShowUI()
    {
        if (canvasObj != null)
        {
            canvasObj.SetActive(true);
        }
    }

    public void HideUI()
    {
        if (canvasObj != null)
        {
            canvasObj.SetActive(false);
        }
    }

    public void SetText(string newText)
    {
        if (interactionText == newText) return;

        interactionText = newText;

        if (uiText != null)
        {
            uiText.text = newText;
            UpdateBoxSize();
        }
    }

    void UpdateBoxSize()
    {
        if (uiText == null || bubbleRT == null || textRT == null)
            return;

        if (string.IsNullOrEmpty(uiText.text) || uiText.text.Trim() == "")
        {
            if (bubbleImage != null) bubbleImage.enabled = false;
            return;
        }
        else
        {
            if (bubbleImage != null) bubbleImage.enabled = true;
        }

        Vector2 preferred = uiText.GetPreferredValues(uiText.text);
        float width = preferred.x + horizontalPadding;

        if (bubbleRT.sizeDelta.x > 0 && Mathf.Abs(bubbleRT.sizeDelta.x - width) < 30f)
        {
            return;
        }

        bubbleRT.sizeDelta = new Vector2(width, boxHeight);

        float normalizedTailHeight = boxHeight * ((float)tailHeight / textureHeight);
        float bodyBottomPadding = textPaddingBottom + normalizedTailHeight;
        float bodyTopPadding = textPaddingTop;

        textRT.offsetMin = new Vector2(textPaddingLeft, bodyBottomPadding);
        textRT.offsetMax = new Vector2(-textPaddingRight, -bodyTopPadding);

        if (bubbleImage != null)
        {
            if (bubbleImage.sprite != null)
            {
                if (bubbleImage.sprite.texture != null)
                    Destroy(bubbleImage.sprite.texture);
                Destroy(bubbleImage.sprite);
            }
            
            bubbleImage.sprite = GenerateBubbleSprite(width);
        }
    }

    Sprite GenerateBubbleSprite(float uiWidth)
    {
        float scale = (float)textureHeight / boxHeight;
        int texWidth = Mathf.RoundToInt(uiWidth * scale);
        int texHeight = textureHeight;

        Texture2D tex = new Texture2D(texWidth, texHeight, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;

        Color clear = new Color(0f, 0f, 0f, 0f);
        Color[] pixels = new Color[texWidth * texHeight];

        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = clear;

        for (int y = 0; y < texHeight; y++)
        {
            for (int x = 0; x < texWidth; x++)
            {
                bool insideOuter = IsInsideBubbleShape(
                    x, y,
                    texWidth, texHeight,
                    cornerRadius,
                    tailWidth, tailHeight,
                    0,
                    tailJoinInset
                );

                if (!insideOuter)
                    continue;

                bool insideInner = IsInsideBubbleShape(
                    x, y,
                    texWidth, texHeight,
                    Mathf.Max(2, cornerRadius - borderThickness),
                    Mathf.Max(4, tailWidth - borderThickness * 2),
                    tailHeight,
                    borderThickness,
                    tailJoinInset
                );

                pixels[y * texWidth + x] = insideInner ? innerColor : borderColor;
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();

        return Sprite.Create(
            tex,
            new Rect(0, 0, texWidth, texHeight),
            new Vector2(0.5f, 0f),
            100f
        );
    }

    bool IsInsideBubbleShape(
        int px, int py,
        int width, int height,
        int radius,
        int tailW, int tailH,
        int inset,
        int joinInset)
    {
        int left = inset;
        int right = width - inset;
        int bottom = tailH + inset;
        int top = height - inset;

        radius = Mathf.Max(1, radius);

        if (IsInsideRoundedRect(px, py, left, bottom, right, top, radius))
            return true;

        int tailCenterX = width / 2;
        float tailBaseY = tailH + inset;

        Vector2 a = new Vector2(tailCenterX - tailW * 0.5f, tailBaseY);
        Vector2 b = new Vector2(tailCenterX + tailW * 0.5f, tailBaseY);
        Vector2 c = new Vector2(tailCenterX, inset);

        return IsInsideTriangle(px, py, a, b, c);
    }

    bool IsInsideRoundedRect(int px, int py, int left, int bottom, int right, int top, int radius)
    {
        if (px >= left + radius && px < right - radius && py >= bottom && py < top)
            return true;

        if (px >= left && px < right && py >= bottom + radius && py < top - radius)
            return true;

        Vector2 bl = new Vector2(left + radius, bottom + radius);
        Vector2 br = new Vector2(right - radius - 1, bottom + radius);
        Vector2 tl = new Vector2(left + radius, top - radius - 1);
        Vector2 tr = new Vector2(right - radius - 1, top - radius - 1);

        if (InsideCircle(px, py, bl, radius)) return true;
        if (InsideCircle(px, py, br, radius)) return true;
        if (InsideCircle(px, py, tl, radius)) return true;
        if (InsideCircle(px, py, tr, radius)) return true;

        return false;
    }

    bool InsideCircle(int px, int py, Vector2 center, int radius)
    {
        float dx = px - center.x;
        float dy = py - center.y;
        return dx * dx + dy * dy <= radius * radius;
    }

    bool IsInsideTriangle(int px, int py, Vector2 a, Vector2 b, Vector2 c)
    {
        Vector2 p = new Vector2(px, py);

        float d1 = Sign(p, a, b);
        float d2 = Sign(p, b, c);
        float d3 = Sign(p, c, a);

        bool hasNeg = (d1 < 0) || (d2 < 0) || (d3 < 0);
        bool hasPos = (d1 > 0) || (d2 > 0) || (d3 > 0);

        return !(hasNeg && hasPos);
    }

    float Sign(Vector2 p1, Vector2 p2, Vector2 p3)
    {
        return (p1.x - p3.x) * (p2.y - p3.y) - (p2.x - p3.x) * (p1.y - p3.y);
    }
}
