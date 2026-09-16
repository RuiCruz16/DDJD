using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class FishingMiniGameUI : MonoBehaviour
{
    [Header("UI References")]
    public Canvas canvas;
    public RectTransform trackTransform;     // The static white background bar
    public Image trackImage;
    public RectTransform greenZoneTransform; // The green success zone (child of track)
    public Image greenZoneImage;
    public RectTransform indicatorTransform; // The small moving bar (child of track)
    public Image indicatorImage;
    public TMP_FontAsset statusFont;

    [Header("Optional Status Text")]
    [Tooltip("Optional UI text to display progress (hits required / misses left). " +
             "If left null, a default one will be auto-created.")]
    public TextMeshProUGUI statusText;

    [Header("Track Layout")]
    [Tooltip("Total width of the white bar in UI pixels.")]
    public float trackWidth = 600f;
    [Tooltip("Height of the white bar in UI pixels.")]
    public float trackHeight = 50f;
    [Tooltip("Width of the moving indicator in UI pixels.")]
    public float indicatorWidth = 10f;
    [Tooltip("Height of the moving indicator (slightly taller than the bar so it pokes out).")]
    public float indicatorHeight = 50f;

    [Header("Mini-Game Settings")]
    [Tooltip("Indicator sweep speed in normalized units per second (1 = full track per second).")]
    public float indicatorSpeed = 1.2f;
    [Tooltip("Lowest difficulty level (inclusive). Level N requires N successful hits.")]
    public int minLevel = 1;
    [Tooltip("Highest difficulty level (inclusive). Level N requires N successful hits.")]
    public int maxLevel = 3;
    [Tooltip("Maximum total misses allowed before the fish escapes.")]
    public int maxMisses = 3;
    [Range(0f, 1f)]
    [Tooltip("Width of the green zone (in normalized track units) on the FIRST hit.")]
    public float initialGreenZoneWidth = 0.25f;
    [Range(0.1f, 0.95f)]
    [Tooltip("Multiplier applied to the green zone width after each successful hit.")]
    public float greenZoneShrinkFactor = 0.55f;
    [Range(0.02f, 0.5f)]
    [Tooltip("Smallest the green zone is allowed to shrink to.")]
    public float minGreenZoneWidth = 0.06f;

    [Header("Visual Settings")]
    public Color trackColor = Color.white;
    public Color greenZoneColor = new Color(0.2f, 0.85f, 0.2f, 1f);
    public Color indicatorColor = Color.black;

    [Header("Custom Art (optional)")]
    [Tooltip("Sprite used for the white background bar. Leave null to keep a solid color.")]
    public Sprite trackSprite;
    [Tooltip("Material used for the white background bar. Leave null to keep the default UI material.")]
    public Material trackMaterial;
    [Tooltip("How the track sprite is drawn (Simple / Sliced / Tiled / Filled). " +
             "Use Sliced if your sprite has 9-slice borders.")]
    public Image.Type trackImageType = Image.Type.Sliced;

    [Tooltip("Sprite used for the green success zone. Leave null to keep a solid color.")]
    public Sprite greenZoneSprite;
    [Tooltip("Material used for the green success zone. Leave null to keep the default UI material.")]
    public Material greenZoneMaterial;
    [Tooltip("How the green zone sprite is drawn (Simple / Sliced / Tiled / Filled).")]
    public Image.Type greenZoneImageType = Image.Type.Sliced;

    [Tooltip("Sprite used for the moving indicator. Leave null to keep a solid color.")]
    public Sprite indicatorSprite;
    [Tooltip("Material used for the moving indicator. Leave null to keep the default UI material.")]
    public Material indicatorMaterial;
    [Tooltip("How the indicator sprite is drawn (Simple / Sliced / Tiled / Filled).")]
    public Image.Type indicatorImageType = Image.Type.Simple;

    [Tooltip("If true, sprite/material/color/type changes made in the Inspector at runtime " +
             "will be re-applied every frame. Useful while tweaking. Turn off for builds.")]
    public bool liveRefreshArt = false;

    // Current green zone bounds (normalized 0..1).
    private float greenZoneStart = 0.40f;
    private float greenZoneEnd = 0.60f;

    // Indicator position along the track in [0, 1].
    private float indicatorPos = 0f;
    private bool isActive = false;
    private bool isMovingForward = true;
    private System.Action<bool> onCatchAttempt;  // Callback for success/failure

    // Difficulty state for the current attempt.
    private int requiredHits = 1;
    private int hitsCompleted = 0;
    private int missesUsed = 0;
    private float currentGreenZoneWidth = 0.25f;

    void Start()
    {
        EnsureUI();

        // Hide the mini-game initially.
        if (canvas != null)
            canvas.gameObject.SetActive(false);
    }

    private void EnsureUI()
    {
        if (canvas == null)
            canvas = GetComponentInChildren<Canvas>(true);

        if (canvas == null
            || trackTransform == null
            || greenZoneTransform == null
            || indicatorTransform == null)
        {
            CreateUIElements();
        }

        ApplyArt();
        ApplyGreenZoneLayout();
    }

    /// <summary>
    /// Applies the Inspector-assigned sprites, materials and colors to the
    /// auto-built (or hand-built) Image components. Safe to call at any time.
    /// </summary>
    private void ApplyArt()
    {
        ApplyImageArt(trackImage, trackSprite, trackMaterial, trackColor, trackImageType);
        ApplyImageArt(greenZoneImage, greenZoneSprite, greenZoneMaterial, greenZoneColor, greenZoneImageType);
        ApplyImageArt(indicatorImage, indicatorSprite, indicatorMaterial, indicatorColor, indicatorImageType);
    }

    private static void ApplyImageArt(Image img, Sprite sprite, Material material, Color color, Image.Type type)
    {
        if (img == null) return;

        img.sprite = sprite;                                     // null -> solid colored quad
        img.material = material;                                 // null -> default UI material
        img.color = color;
        img.type = sprite != null ? type : Image.Type.Simple;    // Sliced needs a sprite with borders
        img.preserveAspect = false;
    }

    void Update()
    {
        if (liveRefreshArt)
            ApplyArt();

        if (!isActive)
            return;

        // Move the indicator back and forth between 0 and 1.
        if (isMovingForward)
        {
            indicatorPos += indicatorSpeed * Time.deltaTime;
            if (indicatorPos >= 1f)
            {
                indicatorPos = 1f;
                isMovingForward = false;
            }
        }
        else
        {
            indicatorPos -= indicatorSpeed * Time.deltaTime;
            if (indicatorPos <= 0f)
            {
                indicatorPos = 0f;
                isMovingForward = true;
            }
        }

        UpdateIndicatorVisual();

        // H to attempt the catch.
        if (Input.GetKeyDown(KeyCode.H))
        {
            AttemptHit();
        }
    }

    public void StartMiniGame(System.Action<bool> callback)
    {
        EnsureUI();

        if (trackTransform != null) trackTransform.gameObject.SetActive(true);
        if (greenZoneTransform != null) greenZoneTransform.gameObject.SetActive(true);
        if (indicatorTransform != null) indicatorTransform.gameObject.SetActive(true);

        onCatchAttempt = callback;

        int loLevel = Mathf.Max(1, Mathf.Min(minLevel, maxLevel));
        int hiLevel = Mathf.Max(loLevel, Mathf.Max(minLevel, maxLevel));
        requiredHits = Random.Range(loLevel, hiLevel + 1);

        hitsCompleted = 0;
        missesUsed = 0;
        currentGreenZoneWidth = Mathf.Clamp(initialGreenZoneWidth, minGreenZoneWidth, 1f);

        RandomizeGreenZone(currentGreenZoneWidth);

        indicatorPos = 0f;
        isMovingForward = true;
        UpdateIndicatorVisual();

        UpdateStatusText();

        isActive = true;

        if (canvas != null)
            canvas.gameObject.SetActive(true);
    }

    public void StopMiniGame()
    {
        isActive = false;
        if (canvas != null)
            canvas.gameObject.SetActive(false);
    }

    private void AttemptHit()
    {
        bool inZone = IsInGreenZone();

        if (inZone)
        {
            hitsCompleted++;
            Debug.Log($"Hit! ({hitsCompleted}/{requiredHits}) at {indicatorPos:F2}");

            if (hitsCompleted >= requiredHits)
            {
                // All required hits done -> success.
                FinishMiniGame(true);
                return;
            }

            // Shrink and relocate the green zone for the next hit.
            currentGreenZoneWidth = Mathf.Max(
                minGreenZoneWidth,
                currentGreenZoneWidth * greenZoneShrinkFactor);
            RandomizeGreenZone(currentGreenZoneWidth);

            // Reset indicator sweep so the next hit isn't trivial.
            indicatorPos = 0f;
            isMovingForward = true;
            UpdateIndicatorVisual();

            UpdateStatusText();
        }
        else
        {
            missesUsed++;
            Debug.Log($"Miss! ({missesUsed}/{maxMisses}) at {indicatorPos:F2}");

            if (missesUsed >= maxMisses)
            {
                // Out of misses -> fish escapes.
                FinishMiniGame(false);
                return;
            }

            UpdateStatusText();
        }
    }

    private void FinishMiniGame(bool success)
    {
        Debug.Log(success
            ? $"Mini-game SUCCESS: caught the fish ({hitsCompleted}/{requiredHits} hits, " +
              $"{missesUsed} miss(es))."
            : $"Mini-game FAILED: fish escaped ({hitsCompleted}/{requiredHits} hits, " +
              $"{missesUsed}/{maxMisses} misses).");

        onCatchAttempt?.Invoke(success);
        StartCoroutine(ShowResultAndClose(success));
    }

    private IEnumerator ShowResultAndClose(bool success)
    {
        isActive = false;

        if (trackTransform != null) trackTransform.gameObject.SetActive(false);
        if (greenZoneTransform != null) greenZoneTransform.gameObject.SetActive(false);
        if (indicatorTransform != null) indicatorTransform.gameObject.SetActive(false);

        if (statusText != null)
        {
            if (success)
            {
                statusText.text = "<size=130%><color=#ffffff>Fish captured successfully!</color></size>";
            }
            else
            {
                statusText.text = "<size=130%><color=#ffffff>Failed to capture fish!</color></size>";
            }
        }

        yield return new WaitForSeconds(1.5f);

        StopMiniGame();
    }

    private bool IsInGreenZone()
    {
        float lo = Mathf.Min(greenZoneStart, greenZoneEnd);
        float hi = Mathf.Max(greenZoneStart, greenZoneEnd);
        return indicatorPos >= lo && indicatorPos <= hi;
    }

    /// <summary>
    /// Picks a new green zone of the given normalized width and places it at a
    /// random spot on the track (fully contained in [0, 1]).
    /// </summary>
    private void RandomizeGreenZone(float width)
    {
        width = Mathf.Clamp(width, minGreenZoneWidth, 1f);

        float maxStart = Mathf.Max(0f, 1f - width);
        float start = Random.Range(0f, maxStart);

        greenZoneStart = start;
        greenZoneEnd = start + width;

        ApplyGreenZoneLayout();
    }

    private void UpdateIndicatorVisual()
    {
        if (indicatorTransform == null) return;

        // Map [0, 1] to [-trackWidth/2, +trackWidth/2] across the track.
        float x = Mathf.Lerp(-trackWidth * 0.5f, trackWidth * 0.5f, indicatorPos);
        indicatorTransform.anchoredPosition = new Vector2(x, 0f);
    }

    private void ApplyGreenZoneLayout()
    {
        if (greenZoneTransform == null) return;

        float lo = Mathf.Clamp01(Mathf.Min(greenZoneStart, greenZoneEnd));
        float hi = Mathf.Clamp01(Mathf.Max(greenZoneStart, greenZoneEnd));

        greenZoneTransform.anchorMin = new Vector2(lo, 0f);
        greenZoneTransform.anchorMax = new Vector2(hi, 1f);
        greenZoneTransform.offsetMin = Vector2.zero;
        greenZoneTransform.offsetMax = Vector2.zero;
    }

    private void UpdateStatusText()
    {
        if (statusText == null) return;

        int missesLeft = Mathf.Max(0, maxMisses - missesUsed);
        statusText.text = $"Lvl {requiredHits}   Hits {hitsCompleted}/{requiredHits}" +
                          $"   Misses left {missesLeft}";
    }

    private void CreateUIElements()
    {
        // Canvas (as a child of this GameObject).
        GameObject canvasGO = new GameObject("FishingCanvas");
        canvasGO.transform.SetParent(this.transform, false);
        canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        canvasGO.AddComponent<GraphicRaycaster>();

        // Root container that holds all the bar parts. The Track, GreenZone and
        // Indicator are SIBLINGS under this root (not parented to the track) so
        // we can control their draw order: GreenZone (back), Indicator, Track
        // (front). The track can be a frame with a transparent middle, and the
        // green zone and indicator will be visible through it.
        GameObject rootGO = new GameObject("MiniGameRoot");
        RectTransform rootRT = rootGO.AddComponent<RectTransform>();
        rootRT.SetParent(canvasGO.transform, false);
        rootRT.anchorMin = new Vector2(0.5f, 0f);
        rootRT.anchorMax = new Vector2(0.5f, 0f);
        rootRT.pivot = new Vector2(0.5f, 0.5f);
        rootRT.anchoredPosition = new Vector2(0f, 180f);
        rootRT.sizeDelta = new Vector2(trackWidth, trackHeight);

        // 1) GreenZone -- drawn first (bottom). Anchor-stretched horizontally
        //    based on greenZoneStart/End; ApplyGreenZoneLayout() sets the anchors.
        GameObject greenGO = new GameObject("GreenZone");
        greenZoneTransform = greenGO.AddComponent<RectTransform>();
        greenZoneTransform.SetParent(rootRT, false);
        greenZoneImage = greenGO.AddComponent<Image>();
        // Visuals applied in ApplyArt().

        // 2) Indicator -- drawn above the green zone, below the track.
        GameObject indicatorGO = new GameObject("Indicator");
        indicatorTransform = indicatorGO.AddComponent<RectTransform>();
        indicatorTransform.SetParent(rootRT, false);
        indicatorTransform.anchorMin = new Vector2(0.5f, 0.5f);
        indicatorTransform.anchorMax = new Vector2(0.5f, 0.5f);
        indicatorTransform.pivot = new Vector2(0.5f, 0.5f);
        indicatorTransform.sizeDelta = new Vector2(indicatorWidth, indicatorHeight);
        indicatorTransform.anchoredPosition = new Vector2(-trackWidth * 0.5f, 0f);
        indicatorImage = indicatorGO.AddComponent<Image>();
        // Visuals applied in ApplyArt().

        // 3) Track -- drawn LAST so it appears on top. Stretched to fill the root.
        //    Use a sprite with a transparent middle (e.g. 9-sliced frame) so the
        //    green zone and indicator show through.
        GameObject trackGO = new GameObject("Track");
        trackTransform = trackGO.AddComponent<RectTransform>();
        trackTransform.SetParent(rootRT, false);
        trackTransform.anchorMin = Vector2.zero;
        trackTransform.anchorMax = Vector2.one;
        trackTransform.offsetMin = Vector2.zero;
        trackTransform.offsetMax = Vector2.zero;
        trackImage = trackGO.AddComponent<Image>();
        // Block raycasts only when the track itself is opaque; if you want clicks
        // to pass through the transparent middle, turn this off in the Inspector.
        trackImage.raycastTarget = true;
        // Visuals (sprite/material/color) are set in ApplyArt() right after creation.

        // Optional status text above the bar.
        if (statusText == null)
        {
            GameObject textGO = new GameObject("StatusText");
            RectTransform textRT = textGO.AddComponent<RectTransform>();
            textRT.SetParent(rootRT, false);
            textRT.anchorMin = new Vector2(0.5f, 1f);
            textRT.anchorMax = new Vector2(0.5f, 1f);
            textRT.pivot = new Vector2(0.5f, 0f);
            textRT.anchoredPosition = new Vector2(0f, 8f);
            textRT.sizeDelta = new Vector2(trackWidth, 40f);

            statusText = textGO.AddComponent<TextMeshProUGUI>();
            statusText.alignment = TextAlignmentOptions.Center;
            statusText.color = Color.white;
            statusText.fontSize = 29;
            statusText.textWrappingMode = TextWrappingModes.Normal;

            if (statusFont != null)
            {
                statusText.font = statusFont;
            }
        }
    }
}
