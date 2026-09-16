using UnityEngine;

public class Grill : MonoBehaviour
{
    public enum State { Empty, Cooking, Cooked }

    [Header("Cooking Setup")]
    public ItemSO rawFishItem;
    public ItemSO cookedFishItem;
    [Tooltip("Seconds it takes to fully cook a fish")]
    public float cookTime = 8f;

    [Header("Fish Visual")]
    [Tooltip("Where the fish is anchored on the grill. If null, the grill transform is used.")]
    public Transform fishMountPoint;
    [Tooltip("Optional override prefab. If null, the rawFishItem.handItemPrefab is used.")]
    public GameObject fishVisualPrefab;
    public Vector3 fishLocalPosition = Vector3.zero;
    public Vector3 fishLocalEuler = Vector3.zero;
    public Vector3 fishLocalScale = Vector3.one;

    [Header("Cooking Colors (applied to the fish visual)")]
    public Color rawColor = new Color(1f, 0.5f, 0.45f);
    public Color cookingColor = new Color(0.85f, 0.55f, 0.2f);
    public Color cookedColor = new Color(0.4f, 0.22f, 0.12f);

    [Header("Optional Indicators")]
    [Tooltip("GameObject toggled ON while cooking (e.g. a smoke particle system).")]
    public GameObject cookingIndicator;
    [Tooltip("GameObject toggled ON when the fish is done (e.g. a green light).")]
    public GameObject cookedIndicator;

    public State CurrentState { get; private set; } = State.Empty;
    public float CookProgress01 => cookTime > 0f ? Mathf.Clamp01(cookProgress / cookTime) : 0f;

    private GameObject fishInstance;
    private Renderer[] fishRenderers;
    private MaterialPropertyBlock propBlock;
    private float cookProgress;

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");

    private void Awake()
    {
        propBlock = new MaterialPropertyBlock();
        SetIndicator(cookingIndicator, false);
        SetIndicator(cookedIndicator, false);
    }

    private void Update()
    {
        if (CurrentState != State.Cooking) return;

        cookProgress += Time.deltaTime;
        float t = CookProgress01;

        Color current = t < 0.5f
            ? Color.Lerp(rawColor, cookingColor, t * 2f)
            : Color.Lerp(cookingColor, cookedColor, (t - 0.5f) * 2f);
        ApplyFishColor(current);

        if (cookProgress >= cookTime)
        {
            CurrentState = State.Cooked;
            ApplyFishColor(cookedColor);
            SetIndicator(cookingIndicator, false);
            SetIndicator(cookedIndicator, true);
        }
    }

    public bool TryPlaceRawFish(ItemSO fish)
    {
        if (CurrentState != State.Empty) return false;
        if (rawFishItem == null || fish != rawFishItem) return false;

        SpawnFishVisual();
        cookProgress = 0f;
        ApplyFishColor(rawColor);
        CurrentState = State.Cooking;
        SetIndicator(cookingIndicator, true);
        SetIndicator(cookedIndicator, false);
        return true;
    }

    public ItemSO TryTakeCookedFish()
    {
        if (CurrentState != State.Cooked) return null;

        ClearFishVisual();
        CurrentState = State.Empty;
        cookProgress = 0f;
        SetIndicator(cookingIndicator, false);
        SetIndicator(cookedIndicator, false);
        return cookedFishItem;
    }

    public string GetInteractionText(ItemSO heldItem)
    {
        switch (CurrentState)
        {
            case State.Empty:
                if (rawFishItem != null && heldItem == rawFishItem)
                    return $"[Right Click] Place {rawFishItem.itemName} on Grill";
                return rawFishItem != null
                    ? $"Hold a {rawFishItem.itemName} to cook it on the grill"
                    : "Grill is empty";
            case State.Cooking:
                int pct = Mathf.RoundToInt(CookProgress01 * 100f);
                return $"Cooking... {pct}%";
            case State.Cooked:
                return cookedFishItem != null
                    ? $"[F] Pick up {cookedFishItem.itemName}"
                    : "[F] Pick up cooked fish";
        }
        return string.Empty;
    }

    private void SpawnFishVisual()
    {
        ClearFishVisual();

        GameObject prefab = fishVisualPrefab != null
            ? fishVisualPrefab
            : (rawFishItem != null ? rawFishItem.handItemPrefab : null);
        if (prefab == null) return;

        Transform parent = fishMountPoint != null ? fishMountPoint : transform;
        fishInstance = Instantiate(prefab, parent);
        fishInstance.transform.localPosition = fishLocalPosition;
        fishInstance.transform.localEulerAngles = fishLocalEuler;
        fishInstance.transform.localScale = fishLocalScale;

        // Make sure the visual is inert (no physics, no triggers).
        foreach (var rb in fishInstance.GetComponentsInChildren<Rigidbody>())
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }
        foreach (var col in fishInstance.GetComponentsInChildren<Collider>())
        {
            col.enabled = false;
        }

        fishRenderers = fishInstance.GetComponentsInChildren<Renderer>();
    }

    private void ClearFishVisual()
    {
        if (fishInstance != null) Destroy(fishInstance);
        fishInstance = null;
        fishRenderers = null;
    }

    private void ApplyFishColor(Color c)
    {
        if (fishRenderers == null) return;
        foreach (var r in fishRenderers)
        {
            if (r == null) continue;
            r.GetPropertyBlock(propBlock);
            propBlock.SetColor(BaseColorId, c);
            propBlock.SetColor(ColorId, c);
            r.SetPropertyBlock(propBlock);
        }
    }

    private static void SetIndicator(GameObject go, bool active)
    {
        if (go != null && go.activeSelf != active) go.SetActive(active);
    }
}
