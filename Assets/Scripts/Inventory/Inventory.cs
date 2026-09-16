using System.Collections.Generic;
using System.Runtime.CompilerServices;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI; 
using System.Collections;
using UnityEngine.EventSystems;
public class Inventory : MonoBehaviour
{     
    [Header("Inventory")]
    public GameObject hotBarObject;
    public GameObject inventorySlotParent;
    public GameObject container;

    [Header("Player Hints")]
    public ItemControlsUI thoughtBubbleUI;
    private Coroutine thoughtBubbleCoroutine;

    [Header("Default Item")]
    public ItemSO startingItem;

    [Header("Default Hotbar Item")]
    public ItemSO startingKnife;
    public ItemSO startingFishingRod;

    [Header("Inventory Items Settings (Pick Up)")]
    public Image dragIcon;
    public float pickUpRange = 5f; 
    public Material highLightMaterial;
    public GameObject chestInteractionUI;
    public TextMeshProUGUI interactionText;
    public TextMeshProUGUI notificationText;

    [Header("Raycast Settings")]
    public Transform playerBody;
    public float chestHeightOffset = 1.5f;
    public float pickUpRadius = 0.5f;

    [Header("Hotbar Settings")]
    public Color equippedColor = Color.white; 
    public float equippedOpacity = 1f;
    public Color normalColor = Color.gray;    
    public float normalOpacity = 0.5f;

    [Header("Hand Equipment Settings")]
    public Transform hand;

    [Header("External References")]
    public HookThrower hookThrower;

    [Header("Item Description")]
    public GameObject itemDescriptionParent;
    public Image itemDescriptionImage;
    public TextMeshProUGUI descriptionItemNameText;
    public TextMeshProUGUI itemDescriptionText;

    [Header("Crafting")]
    public List<Recipe> handRecipes = new List<Recipe>();  // exclusively for crafting table recipe
    public List<Recipe> tableRecipes = new List<Recipe>(); // all recipes

    [Header("UI Panels")]
    public GameObject controlsCanvas;
    public GameObject progressBars;

    [Header("Crafting Details Panel (Right)")]
    public GameObject detailsPanel;
    public GameObject craftingPromptText;
    public Image detailsIcon;
    public TextMeshProUGUI detailsName;
    public TextMeshProUGUI detailsDescription;
    public Transform ingredientsContainer; 
    public GameObject itemNeededUIPrefab; 
    public Button executeCraftButton;
    public GameObject detailsIngredientPrefab;
    public TextMeshProUGUI craftingNotificationText;

    private Recipe currentlySelectedRecipe;

    public Transform craftingGrid;

    public GameObject craftingTableUIContainer;
    public Transform tableCraftingGrid;
    private bool isCraftingTableOpen = false;
    private Recipe hoveredCraftingRecipe = null;
    private Coroutine craftingNotifCoroutine;

    public GameObject craftingBtnPrefab;
    public Color craftableColor = Color.white;
    public Color lockedColor = new Color(0.3f, 0.3f, 0.3f, 1f);

    [Header("Chest")]
    public GameObject chestContainer;
    public GameObject chestSlotParent;
    private List<Slot> chestUISlots = new List<Slot>();
    private Chest currentOpenChest = null;

    [Header("Placement Preview")]
    public float placementRotationStep = 90f;
    private GameObject currentPreviewObject;
    private bool isPreviewValid;
    private Vector3 previewPosition;
    private Transform previewParent;
    private float placementRotationY = 0f;
    private int lastPlacementHotbarIndex = -1;
    private ItemControlsUI currentPreviewUI;

    [Header("Placement Preview Materials")]
    public Material validPreviewMaterial;
    public Material invalidPreviewMaterial;

    private int equippedHotbarIndex = 0; // 0-5 (6 slots)
    private GameObject currentHandItem;

    private Material[] originalMaterials = null;
    private Renderer[] lookAtRenderers = null;

    private ItemControlsUI currentLookAtUI = null;
    public ItemControlsUI CurrentLookAtUI => currentLookAtUI;

    private List<Slot> inventorySlots = new List<Slot>();
    private List<Slot> hotBarSlots = new List<Slot>();
    private List<Slot> allSlots = new List<Slot>();

    private Slot draggedSlot = null;
    private bool isDragging = false;

    public static bool isInventoryOpen = false;

    private Player playerScript;
    private PlayerItemDropFMOD itemDropFMOD;
    private PlayerItemPickupFMOD itemPickupFMOD;

    [Header("Camera Controls")]
    public MonoBehaviour cameraInputScript;

    private void Awake()
    {
        inventorySlots.AddRange(inventorySlotParent.GetComponentsInChildren<Slot>());
        hotBarSlots.AddRange(hotBarObject.GetComponentsInChildren<Slot>());

        if (chestSlotParent != null) {
            chestUISlots.AddRange(chestSlotParent.GetComponentsInChildren<Slot>());
        }

        allSlots.AddRange(inventorySlots);
        allSlots.AddRange(hotBarSlots);
        allSlots.AddRange(chestUISlots);

        RefreshAllCraftingGrids();
    }

    void Start()
    {
        if (playerBody != null)
        {
            playerScript = playerBody.GetComponent<Player>();
            itemPickupFMOD = playerBody.GetComponent<PlayerItemPickupFMOD>();
            itemDropFMOD = playerBody.GetComponent<PlayerItemDropFMOD>();
        }

        if (startingItem != null && hotBarSlots.Count > 0)
        {
            hotBarSlots[0].SetItem(startingItem, 1);
            equippedHotbarIndex = 0;
            EquipHandItem();
        }

        if (startingKnife != null)
        {
            GiveStartingHotbarItem(startingKnife);
        }

        if (startingFishingRod != null)
        {
            GiveStartingHotbarItem(startingFishingRod);
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            if (!isInventoryOpen)
            {
                if (!IsInPlacementMode())
                {
                    Chest targetChest = GetLookAtChest();
                    CraftingTable targetTable = GetLookAtCraftingTable();

                    if (targetChest != null)
                    {
                        OpenChest(targetChest);
                    }
                    else if (targetTable != null)
                    {
                        OpenCraftingTable();
                    }
                    else
                    {
                        controlsCanvas.SetActive(false);
                        container.SetActive(true);
                        progressBars.SetActive(false);
                        isInventoryOpen = true;
                        Time.timeScale = 0f;
                        UnityEngine.Cursor.lockState = CursorLockMode.None;
                        UnityEngine.Cursor.visible = true;

                        if (cameraInputScript != null)
                        {
                            cameraInputScript.enabled = false;
                        }
                    }
                }
            }
            else
            {
                CloseInventory();
            }
        }

        if (Input.GetKeyDown(KeyCode.Escape) && isInventoryOpen)
        {
            CloseInventory();
        }

        SyncPlacementRotationWithEquippedItem();
        UpdatePlacementPreview();
        HandlePlacementRotation();

        if (IsInPlacementMode())
        {
            ClearCurrentLookTarget();
        }
        else
        {
            DetectLookAtItem();
            PickUpItem();
        }

        StartDrag();
        UpdateDragItemPosition();
        EndDrag();

        HandleHotbarSelection();

        HandleItemUsage();
        UpdateHotbarOpacity();
        UpdateItemDescription();
    }

    private void OpenChest(Chest chest)
    {
        currentOpenChest = chest;
        
        if (chest.storedItems.Length != chestUISlots.Count)
        {
            System.Array.Resize(ref chest.storedItems, chestUISlots.Count);
            System.Array.Resize(ref chest.storedAmounts, chestUISlots.Count);
        }

        for (int i = 0; i < chestUISlots.Count; i++)
        {
            if (chest.storedItems[i] != null)
                chestUISlots[i].SetItem(chest.storedItems[i], chest.storedAmounts[i]);
            else
                chestUISlots[i].ClearSlot();
        }

        container.SetActive(true); 
        chestContainer.SetActive(true); 
        isInventoryOpen = true;
        Time.timeScale = 0f;
        UnityEngine.Cursor.lockState = CursorLockMode.None;
        UnityEngine.Cursor.visible = true;

        if (cameraInputScript != null) 
            cameraInputScript.enabled = false;
    }

    private void OpenCraftingTable()
    {
        craftingTableUIContainer.SetActive(true);
        controlsCanvas.SetActive(false);
        progressBars.SetActive(false);
        isInventoryOpen = true;
        isCraftingTableOpen = true;

        if (craftingPromptText != null) craftingPromptText.SetActive(true);
        if (detailsPanel != null) detailsPanel.SetActive(false);
        
        RefreshAllCraftingGrids(); 

        Time.timeScale = 0f;
        UnityEngine.Cursor.lockState = CursorLockMode.None;
        UnityEngine.Cursor.visible = true;

        if (cameraInputScript != null) 
            cameraInputScript.enabled = false;
    }

    private void CloseInventory()
    {
        hoveredCraftingRecipe = null;
        if (itemDescriptionParent != null) {
            itemDescriptionParent.SetActive(false);
        }

        if (isCraftingTableOpen) {
            craftingTableUIContainer.SetActive(false);
            isCraftingTableOpen = false;
        }

        if (controlsCanvas) {
            controlsCanvas.SetActive(true);
        }

        if (progressBars) {
            progressBars.SetActive(true);
        }

        if (currentOpenChest != null)
        {
            for (int i = 0; i < chestUISlots.Count; i++)
            {
                currentOpenChest.storedItems[i] = chestUISlots[i].GetItem();
                currentOpenChest.storedAmounts[i] = chestUISlots[i].GetAmount();
                
                chestUISlots[i].ClearSlot(); 
            }
            chestContainer.SetActive(false);
            currentOpenChest = null;
        }

        container.SetActive(false);
        isInventoryOpen = false;

        Time.timeScale = 1f; 

        UnityEngine.Cursor.lockState = CursorLockMode.Locked;
        UnityEngine.Cursor.visible = false;

        if (cameraInputScript != null) 
            cameraInputScript.enabled = true;
    }

    public void AddItem(ItemSO itemToAdd, int amount)
    {
        if (itemPickupFMOD != null)
        {
            itemPickupFMOD.HandleItemPickup();
        }

        int remaining = amount;

        foreach (Slot slot in hotBarSlots)
        {
            if (slot.HasItem() && slot.GetItem() == itemToAdd)
            {
                int currentAmount = slot.GetAmount();
                int maxStack = itemToAdd.maxStackSize;

                if (currentAmount < maxStack)
                {
                    int spaceLeft = maxStack - currentAmount;
                    int amountToAdd = Mathf.Min(spaceLeft, remaining);

                    slot.SetItem(itemToAdd, currentAmount + amountToAdd);
                    remaining -= amountToAdd;

                    if (remaining <= 0)
                    {
                        RefreshAllCraftingGrids();
                        return;
                    }
                }
            }
        }

        foreach (Slot slot in inventorySlots)
        {
            if (slot.HasItem() && slot.GetItem() == itemToAdd)
            {
                int currentAmount = slot.GetAmount();
                int maxStack = itemToAdd.maxStackSize;

                if (currentAmount < maxStack)
                {
                    int spaceLeft = maxStack - currentAmount;
                    int amountToAdd = Mathf.Min(spaceLeft, remaining);

                    slot.SetItem(itemToAdd, currentAmount + amountToAdd);
                    remaining -= amountToAdd;

                    if (remaining <= 0)
                    {
                        RefreshAllCraftingGrids();
                        return;
                    }
                }
            }
        }

        foreach (Slot slot in hotBarSlots)
        {
            if (!slot.HasItem())
            {
                if (hotBarSlots.Contains(slot) && !itemToAdd.isEquippable)
                {
                    continue; 
                }

                int amountToPlace = Mathf.Min(itemToAdd.maxStackSize, remaining);
                slot.SetItem(itemToAdd, amountToPlace);
                remaining -= amountToPlace;

                if (remaining <= 0)
                {
                    RefreshAllCraftingGrids();
                    return;
                }
            }
        }

        foreach (Slot slot in inventorySlots)
        {
            if (!slot.HasItem())
            {
                if (hotBarSlots.Contains(slot) && !itemToAdd.isEquippable)
                {
                    continue; 
                }

                int amountToPlace = Mathf.Min(itemToAdd.maxStackSize, remaining);
                slot.SetItem(itemToAdd, amountToPlace);
                remaining -= amountToPlace;

                if (remaining <= 0)
                {
                    RefreshAllCraftingGrids();
                    return;
                }
            }
        }

        if (remaining > 0)
        {
            Debug.Log("Inventory is full. Could not add " + remaining + " of " + itemToAdd.itemName);
        }
        RefreshAllCraftingGrids();
    }

    private void StartDrag()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Slot hovered = GetHoveredSlot();
            if (hovered != null && hovered.HasItem())
            {
                draggedSlot = hovered;
                isDragging = true;

                dragIcon.sprite = hovered.GetItem().icon;
                dragIcon.color = new Color(1, 1, 1, 0.5f);
                dragIcon.enabled = true;
            }
        }
    }

    private void EndDrag()
    {
        if (Input.GetMouseButtonUp(0) && isDragging)
        {
            Slot hovered = GetHoveredSlot();

            if (hovered != null)
            {
                HandleDrop(draggedSlot, hovered);

                Slot currentlyEquippedSlot = hotBarSlots[equippedHotbarIndex];

                if (draggedSlot == currentlyEquippedSlot || hovered == currentlyEquippedSlot)
                {
                    EquipHandItem();
                }

                dragIcon.enabled = false;
                draggedSlot = null;
                isDragging = false;
            }
            else
            {
                dragIcon.enabled = false;
                draggedSlot = null;
                isDragging = false;
            }
        }
    }

    private Slot GetHoveredSlot()
    {
        foreach (Slot slot in allSlots)
        {
            if (slot.hovering && slot.gameObject.activeInHierarchy)
            {
                return slot;
            }
        }
        return null;
    }

    private void HandleDrop(Slot from, Slot to)
    {
        if (from == to) return;

        ItemSO itemFrom = from.GetItem();
        ItemSO itemTo = to.GetItem();

        if (hotBarSlots.Contains(to))
        {
            if (itemFrom != null && !itemFrom.isEquippable)
            {
                Debug.Log($"{itemFrom.itemName} is not equippable and cannot go in the hotbar.");
                return;
            }
        }

        if (hotBarSlots.Contains(from) && itemTo != null)
        {
            if (!itemTo.isEquippable)
            {
                Debug.Log($"Cannot swap: {itemTo.itemName} cannot enter the hotbar.");
                return;
            }
        }

        if (to.HasItem() && itemTo == itemFrom)
        {
            int max = itemTo.maxStackSize;
            int space = max - to.GetAmount();

            if (space > 0)
            {
                int move = Mathf.Min(space, from.GetAmount());
                to.SetItem(itemTo, to.GetAmount() + move);
                from.SetItem(itemFrom, from.GetAmount() - move);

                if (from.GetAmount() <= 0) from.ClearSlot();
                return;
            }
        }

        if (to.HasItem())
        {
            int tempAmount = to.GetAmount();
            to.SetItem(itemFrom, from.GetAmount());
            from.SetItem(itemTo, tempAmount);
        }
        else
        {
            to.SetItem(itemFrom, from.GetAmount());
            from.ClearSlot();
        }
    }

    private void UpdateDragItemPosition()
    {
        if (isDragging)
        {
            dragIcon.transform.position = Input.mousePosition;
        }
    }

    private void PickUpItem()
    {
        if (lookAtRenderers != null && Input.GetKeyDown(KeyCode.F))
        {
            Grill grill = lookAtRenderers[0].GetComponentInParent<Grill>();
            if (grill != null)
            {
                if (grill.CurrentState == Grill.State.Cooked)
                {
                    ItemSO cooked = grill.TryTakeCookedFish();
                    if (cooked != null)
                    {
                        AddItem(cooked, 1);
                        EquipHandItem();
                    }
                    return;
                }
                else if (grill.CurrentState == Grill.State.Cooking)
                {
                    ShowScreenNotification("Cannot pick up the Grill while it's cooking!");
                    return;
                }
            }

            Item item = lookAtRenderers[0].GetComponentInParent<Item>();

            if (item != null && item.canBePickedUp && !item.gameObject.CompareTag("Raft") && !item.gameObject.CompareTag("Barricade"))
            {
                AddItem(item.item, item.amount);
                Destroy(item.gameObject);
                EquipHandItem();

                lookAtRenderers = null;
                originalMaterials = null;
            }
        }
    }

    private bool TryGetInteractionRay(out Ray ray)
    {
        ray = default;

        if (playerBody == null || Camera.main == null)
            return false;

        Vector3 originPosition = playerBody.position + new Vector3(0, chestHeightOffset, 0);
        originPosition += Camera.main.transform.forward * 0.75f;

        ray = new Ray(originPosition, Camera.main.transform.forward);
        return true;
    }

    private void DetectLookAtItem()
    {
        if (lookAtRenderers != null)
        {
            for (int i = 0; i < lookAtRenderers.Length; i++)
            {
                if (lookAtRenderers[i] != null && originalMaterials != null && i < originalMaterials.Length)
                    lookAtRenderers[i].material = originalMaterials[i];
            }

            lookAtRenderers = null;
            originalMaterials = null;
        }

        currentLookAtUI = null;

        if (isInventoryOpen)
        {
            if (chestInteractionUI != null)
                chestInteractionUI.SetActive(false);
            return;
        }

        if (!TryGetInteractionRay(out Ray ray))
            return;

        bool isLookingAtInteractable = false;

        RaycastHit[] hits = Physics.SphereCastAll(ray, pickUpRadius, pickUpRange);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (RaycastHit hit in hits)
        {
            Item item = hit.collider.GetComponentInParent<Item>();
            Chest chest = hit.collider.GetComponentInParent<Chest>();
            Grill grill = hit.collider.GetComponentInParent<Grill>();
            WaterPurifier waterPurifier = hit.collider.GetComponentInParent<WaterPurifier>();
            CraftingTable craftingTable = hit.collider.GetComponentInParent<CraftingTable>();

            if (grill != null)
            {
                Renderer[] renderers = grill.GetComponentsInChildren<Renderer>();
                if (renderers != null && renderers.Length > 0)
                {
                    originalMaterials = new Material[renderers.Length];
                    for (int i = 0; i < renderers.Length; i++)
                    {
                        originalMaterials[i] = renderers[i].material;
                        renderers[i].material = highLightMaterial;
                    }
                    lookAtRenderers = renderers;
                }

                ItemControlsUI bubbleUI = grill.GetComponentInParent<ItemControlsUI>();
                if (bubbleUI != null)
                {
                    ItemSO held = (hotBarSlots.Count > 0 && hotBarSlots[equippedHotbarIndex].HasItem())
                        ? hotBarSlots[equippedHotbarIndex].GetItem()
                        : null;

                    string grillText = grill.GetInteractionText(held).Replace("\n\n", " ").Replace("\n", " ");

                    if (grill.CurrentState == Grill.State.Empty && item != null && item.canBePickedUp && item.item != null)
                        grillText += $" [F] Pick up {item.item.itemName}";

                    bubbleUI.SetText(grillText);
                    currentLookAtUI = bubbleUI;
                }

                break;
            }

            if (waterPurifier != null)
            {
                Renderer[] renderers = waterPurifier.GetComponentsInChildren<Renderer>();
                if (renderers != null && renderers.Length > 0)
                {
                    originalMaterials = new Material[renderers.Length];
                    for (int i = 0; i < renderers.Length; i++)
                    {
                        originalMaterials[i] = renderers[i].material;
                        renderers[i].material = highLightMaterial;
                    }
                    lookAtRenderers = renderers;
                }

                ItemControlsUI bubbleUI = waterPurifier.GetComponentInParent<ItemControlsUI>();
                if (bubbleUI != null)
                {
                    string pickupPrompt = (item != null && item.canBePickedUp && item.item != null)
                        ? $" [F] Pick up {item.item.itemName}"
                        : "";

                    if (waterPurifier.currentState == WaterPurifier.State.Empty)
                        bubbleUI.SetText($"[Right Click] Insert Salt Water{pickupPrompt}");
                    else if (waterPurifier.currentState == WaterPurifier.State.Purifying)
                        bubbleUI.SetText($"Purifying... {Mathf.RoundToInt(waterPurifier.GetProgressPercentage())}%{pickupPrompt}");
                    else if (waterPurifier.currentState == WaterPurifier.State.Ready)
                        bubbleUI.SetText($"[Right Click] Collect Fresh Water{pickupPrompt}");

                    currentLookAtUI = bubbleUI;
                }

                break;
            }

            if (craftingTable != null)
            {
                Renderer[] renderers = craftingTable.GetComponentsInChildren<Renderer>();
                if (renderers != null && renderers.Length > 0)
                {
                    originalMaterials = new Material[renderers.Length];
                    for (int i = 0; i < renderers.Length; i++)
                    {
                        originalMaterials[i] = renderers[i].material;
                        renderers[i].material = highLightMaterial;
                    }
                    lookAtRenderers = renderers;
                }

                ItemControlsUI bubbleUI = craftingTable.GetComponentInParent<ItemControlsUI>();
                if (bubbleUI != null)
                {
                    if (item != null && item.canBePickedUp && item.item != null)
                        bubbleUI.SetText($"[E] Use Crafting Table [F] Pick up {item.item.itemName}");
                    else
                        bubbleUI.SetText("[E] Use Crafting Table");

                    currentLookAtUI = bubbleUI;
                }

                break;
            }

            if (item != null && item.canBePickedUp && !item.gameObject.CompareTag("Raft") && !item.gameObject.CompareTag("Barricade"))
            {
                Renderer[] renderers = item.GetComponentsInChildren<Renderer>();
                if (renderers != null && renderers.Length > 0)
                {
                    originalMaterials = new Material[renderers.Length];
                    for (int i = 0; i < renderers.Length; i++)
                    {
                        originalMaterials[i] = renderers[i].material;
                        renderers[i].material = highLightMaterial;
                    }
                    lookAtRenderers = renderers;
                }

                ItemControlsUI bubbleUI = item.GetComponentInParent<ItemControlsUI>();
                if (bubbleUI != null)
                {
                    if (chest != null)
                        bubbleUI.SetText($"[E] Open Chest [F] Pick up {item.item.itemName}");
                    else
                        bubbleUI.SetText($"[F] Pick up {item.item.itemName}");

                    currentLookAtUI = bubbleUI;
                }

                break;
            }
            else if (chest != null)
            {
                Renderer[] renderers = chest.GetComponentsInChildren<Renderer>();
                if (renderers != null && renderers.Length > 0)
                {
                    originalMaterials = new Material[renderers.Length];
                    for (int i = 0; i < renderers.Length; i++)
                    {
                        originalMaterials[i] = renderers[i].material;
                        renderers[i].material = highLightMaterial;
                    }
                    lookAtRenderers = renderers;
                }

                ItemControlsUI bubbleUI = chest.GetComponentInParent<ItemControlsUI>();
                if (bubbleUI != null)
                {
                    bubbleUI.SetText("[E] Open Chest");
                    currentLookAtUI = bubbleUI;
                }

                break;
            }
        }

        if (chestInteractionUI != null)
            chestInteractionUI.SetActive(isLookingAtInteractable);
    }

    private Chest GetLookAtChest()
    {
        if (!TryGetInteractionRay(out Ray ray))
            return null;

        RaycastHit[] hits = Physics.SphereCastAll(ray, pickUpRadius, pickUpRange);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (RaycastHit hit in hits)
        {
            Chest chest = hit.collider.GetComponentInParent<Chest>();
            if (chest != null)
                return chest;
        }

        return null;
    }

    private CraftingTable GetLookAtCraftingTable()
    {
        if (!TryGetInteractionRay(out Ray ray))
            return null;

        RaycastHit[] hits = Physics.SphereCastAll(ray, pickUpRadius, pickUpRange);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (RaycastHit hit in hits)
        {
            CraftingTable table = hit.collider.GetComponentInParent<CraftingTable>();
            if (table != null)
                return table;
        }

        return null;
    }

    private Grill GetLookAtGrill()
    {
        if (!TryGetInteractionRay(out Ray ray))
            return null;

        RaycastHit[] hits = Physics.SphereCastAll(ray, pickUpRadius, pickUpRange);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (RaycastHit hit in hits)
        {
            Grill grill = hit.collider.GetComponentInParent<Grill>();
            if (grill != null)
                return grill;
        }

        return null;
    }

    private bool IsLookingAtPurifier()
    {
        if (!TryGetInteractionRay(out Ray ray))
            return false;

        RaycastHit[] hits = Physics.SphereCastAll(ray, pickUpRadius, pickUpRange);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (RaycastHit hit in hits)
        {
            WaterPurifier purifier = hit.collider.GetComponentInParent<WaterPurifier>();
            if (purifier != null)
                return true;
        }

        return false;
    }

    private void UpdateHotbarOpacity()
    {
        for (int i = 0; i < hotBarSlots.Count; i++)
        {
            Transform fillTransform = hotBarSlots[i].transform.Find("Fill");

            if (fillTransform != null && fillTransform.TryGetComponent<Image>(out var fillImage))
            {
                bool isSelected = (i == equippedHotbarIndex);

                Color targetColor = isSelected ? equippedColor : normalColor;
                targetColor.a = isSelected ? equippedOpacity : normalOpacity;

                fillImage.color = targetColor;
            }
        }
    }

    private void HandleHotbarSelection()
    {
        if (isInventoryOpen) return;
        if (isCraftingTableOpen) return;

        int maxKeySlots = Mathf.Min(9, hotBarSlots.Count);
        for (int i = 0; i < maxKeySlots; i++)
        {
            if (Input.GetKeyDown((i + 1).ToString()))
            {
                if (hookThrower != null && hookThrower.IsHookActive())
                {
                    Debug.LogWarning("Cannot change slots while the hook is thrown!");
                    return;
                }

                equippedHotbarIndex = i;
                UpdateHotbarOpacity();
                EquipHandItem();
            }
        }

        if (hotBarSlots.Count > 0)
        {
            float scroll = Input.mouseScrollDelta.y;
            if (Mathf.Abs(scroll) > 0.01f)
            {
                int dir = -(int)Mathf.Sign(scroll); 
                equippedHotbarIndex = (equippedHotbarIndex + dir + hotBarSlots.Count) % hotBarSlots.Count;
                UpdateHotbarOpacity();
                EquipHandItem();
            }
        }
    }

    private void EquipHandItem()
    {
        if (currentHandItem != null)
        {
            Destroy(currentHandItem);
        }

        if (currentPreviewObject != null)
        {
            Destroy(currentPreviewObject);
            currentPreviewObject = null;
            currentPreviewUI = null;
        }

        Slot equippedSlot = hotBarSlots[equippedHotbarIndex];
        
        if (!equippedSlot.HasItem()) 
        {
            if (thoughtBubbleUI != null) 
                thoughtBubbleUI.HideUI();
            return;
        }

        ItemSO item = equippedSlot.GetItem();
        
        if (!item.isEquippable || item.handItemPrefab == null)
        {
            if (thoughtBubbleUI != null) 
                thoughtBubbleUI.HideUI();
            return;
        }

        currentHandItem = Instantiate(item.handItemPrefab, hand, false);
        currentHandItem.transform.localPosition = Vector3.zero;
        currentHandItem.transform.localRotation = Quaternion.identity;

        if (thoughtBubbleUI != null)
        {
            if (!string.IsNullOrEmpty(item.equipHint))
            {
                thoughtBubbleUI.SetText(item.equipHint);
                thoughtBubbleUI.ShowUI();

                if (thoughtBubbleCoroutine != null) 
                    StopCoroutine(thoughtBubbleCoroutine);
                thoughtBubbleCoroutine = StartCoroutine(HideThoughtBubbleAfterDelay(4f));
            }
            else
            {
                thoughtBubbleUI.HideUI(); 
            }
        }
    }

    private void UpdateItemDescription()
    {
        if (hoveredCraftingRecipe != null && hoveredCraftingRecipe.result != null)
        {
            ItemSO hoveredItem = hoveredCraftingRecipe.result;
            itemDescriptionImage.sprite = hoveredItem.icon;
            itemDescriptionText.text = hoveredItem.description;
            descriptionItemNameText.text = hoveredItem.itemName; 
            return; 
        }

        foreach (Slot slot in allSlots)
        {
            if (slot.hovering && slot.HasItem())
            {
                ItemSO hoveredItem = slot.GetItem();
                itemDescriptionParent.SetActive(true);
                itemDescriptionImage.sprite = hoveredItem.icon;
                itemDescriptionText.text = hoveredItem.description;
                descriptionItemNameText.text = hoveredItem.itemName; 
                return;
            }
        }
        
        itemDescriptionParent.SetActive(false);
    }

    private WaterPurifier GetLookAtWaterPurifier()
    {
        if (!TryGetInteractionRay(out Ray ray))
            return null;

        RaycastHit[] hits = Physics.SphereCastAll(ray, pickUpRadius, pickUpRange);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (RaycastHit hit in hits)
        {
            WaterPurifier wp = hit.collider.GetComponentInParent<WaterPurifier>();
            if (wp != null)
                return wp;
        }

        return null;
    }

    private void ApplyPreviewMaterial(Material previewMat)
    {
        if (currentPreviewObject == null || previewMat == null)
            return;

        Renderer[] renderers = currentPreviewObject.GetComponentsInChildren<Renderer>();

        foreach (Renderer r in renderers)
        {
            Material[] mats = new Material[r.materials.Length];

            for (int i = 0; i < mats.Length; i++)
                mats[i] = previewMat;

            r.materials = mats;
        }
    }

    private void UpdatePlacementPreview()
    {
        Slot equippedSlot = hotBarSlots[equippedHotbarIndex];
        ItemSO item = equippedSlot.HasItem() ? equippedSlot.GetItem() : null;

        if (item == null || !item.isPlaceableStructure || item.itemPrefab == null)
        {
            HidePlacementPreviewUI();

            if (currentPreviewObject != null)
                Destroy(currentPreviewObject);

            currentPreviewObject = null;
            currentPreviewUI = null;
            return;
        }

        if (currentPreviewObject == null)
        {
            currentPreviewObject = Instantiate(item.itemPrefab);

            foreach (Collider c in currentPreviewObject.GetComponentsInChildren<Collider>())
                c.enabled = false;

            foreach (Rigidbody rb in currentPreviewObject.GetComponentsInChildren<Rigidbody>())
            {
                rb.isKinematic = true;
                rb.useGravity = false;
            }
        }

        UpdatePlacementPreviewUI(item);

        Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        float placementRange = 50f;

        Debug.DrawRay(ray.origin, ray.direction * placementRange, Color.red);

        isPreviewValid = false;
        bool shouldShowPreview = false;
        Vector3 previewTargetPosition = Vector3.zero;
        Transform targetRaftTransform = null;
        bool blockedBeforeRaft = false;

        RaycastHit[] hits = Physics.RaycastAll(ray, placementRange, ~0, QueryTriggerInteraction.Ignore);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        for (int i = 0; i < hits.Length; i++)
        {
            RaycastHit hit = hits[i];
            Collider c = hit.collider;

            if (c.transform.IsChildOf(transform.root))
                continue;

            bool isRaft = c.CompareTag("Raft") || c.gameObject.layer == LayerMask.NameToLayer("Raft");

            if (isRaft)
            {
                shouldShowPreview = true;
                Slot previewSlot = hotBarSlots[equippedHotbarIndex];
                ItemSO equippedItem = previewSlot != null && previewSlot.HasItem() ? previewSlot.GetItem() : null;

                float placementYOffset = equippedItem != null ? equippedItem.placementYOffset : 0f;
                previewTargetPosition = hit.point + Vector3.up * (0.05f + placementYOffset);
                targetRaftTransform = c.transform;

                if (!blockedBeforeRaft &&
                    c.GetComponentInParent<CollectionNet>() == null &&
                    hit.normal.y > 0.8f)
                {
                    Vector3 checkCenter = hit.point + new Vector3(0f, 0.2f, 0f);
                    float checkRadius = 0.5f;

                    Collider[] overlappingObjects = Physics.OverlapSphere(
                        checkCenter,
                        checkRadius,
                        ~0,
                        QueryTriggerInteraction.Ignore
                    );

                    bool isSpaceClear = true;

                    foreach (Collider overlap in overlappingObjects)
                    {
                        if (overlap.transform.IsChildOf(c.transform.root))
                            continue;

                        if (overlap.transform.IsChildOf(transform.root))
                            continue;

                        if (overlap.CompareTag("Raft") || overlap.gameObject.layer == LayerMask.NameToLayer("Raft"))
                            continue;

                        isSpaceClear = false;
                        break;
                    }

                    if (isSpaceClear)
                    {
                        previewPosition = previewTargetPosition;
                        previewParent = targetRaftTransform;
                        isPreviewValid = true;
                    }
                }

                break;
            }
            else
            {
                blockedBeforeRaft = true;
            }
        }

        if (!shouldShowPreview)
        {
            if (currentPreviewObject != null)
                currentPreviewObject.SetActive(false);

            HidePlacementPreviewUI();
            isPreviewValid = false;
            previewParent = null;
            return;
        }

        currentPreviewObject.SetActive(true);
        currentPreviewObject.transform.position = isPreviewValid ? previewPosition : previewTargetPosition;
        currentPreviewObject.transform.rotation = Quaternion.Euler(0f, placementRotationY, 0f);

        UpdatePlacementPreviewUI(item);
        ApplyPreviewMaterial(isPreviewValid ? validPreviewMaterial : invalidPreviewMaterial);
    }

    private void HandleItemUsage()
    {
        if (Input.GetMouseButtonDown(1))
        {
            Slot equippedSlot = hotBarSlots[equippedHotbarIndex];
            ItemSO item = equippedSlot.HasItem() ? equippedSlot.GetItem() : null;

            bool isPlacingStructure = item != null && item.isPlaceableStructure && item.itemPrefab != null;

            if (!isPlacingStructure)
            {
                CraftingTable lookTable = GetLookAtCraftingTable();
                if (lookTable != null)
                {
                    return;
                }

                Grill lookGrill = GetLookAtGrill();
                if (lookGrill != null)
                {
                    if (lookGrill.CurrentState == Grill.State.Cooking)
                    {
                        return;
                    }

                    if (lookGrill.CurrentState == Grill.State.Cooked)
                    {
                        return;
                    }

                    if (lookGrill.rawFishItem != null && item == lookGrill.rawFishItem)
                    {
                        if (lookGrill.TryPlaceRawFish(item))
                        {
                            equippedSlot.RemoveAmount(1);
                            EquipHandItem();
                            RefreshAllCraftingGrids();
                        }
                        return;
                    }

                    ShowScreenNotification(lookGrill.rawFishItem != null
                        ? $"You need a {lookGrill.rawFishItem.itemName} to use the Grill!"
                        : "Grill is not configured!");
                    return;
                }

                WaterPurifier waterPurifier = GetLookAtWaterPurifier();
                if (waterPurifier != null)
                {
                    if (waterPurifier.currentState == WaterPurifier.State.Empty)
                    {
                        if (item != null && item.purifiedVersion != null)
                        {
                            waterPurifier.InsertSaltWater();
                            equippedSlot.RemoveAmount(1);
                            EquipHandItem();
                            RefreshAllCraftingGrids();
                        }
                        else
                        {
                            ShowScreenNotification("Hold Salt Water first.");
                        }
                    }
                    else if (waterPurifier.currentState == WaterPurifier.State.Ready)
                    {
                        ItemSO collectedWater = waterPurifier.CollectWater();
                        if (collectedWater != null)
                        {
                            AddItem(collectedWater, 1);
                            EquipHandItem();
                            RefreshAllCraftingGrids();
                        }
                    }
                    return;
                }
            }

            if (item != null && item.isPlaceableStructure && item.itemPrefab != null)
            {
                if (isPreviewValid && currentPreviewObject != null)
                {
                    Vector3 finalPlacementPosition = previewPosition;
                    GameObject placedObject = Instantiate(item.itemPrefab, finalPlacementPosition, currentPreviewObject.transform.rotation);

                    if (previewParent != null)
                    {
                        placedObject.transform.SetParent(previewParent, true);
                    }

                    Rigidbody rb = placedObject.GetComponent<Rigidbody>();
                    if (rb != null)
                    {
                        rb.isKinematic = true;
                        rb.useGravity = false;
                    }

                    Item itemComponent = placedObject.GetComponent<Item>();
                    if (itemComponent == null)
                    {
                        itemComponent = placedObject.AddComponent<Item>();
                    }

                    itemComponent.item = item;
                    itemComponent.amount = 1;
                    itemComponent.canBePickedUp = true;

                    equippedSlot.RemoveAmount(1);

                    isPreviewValid = false;
                    previewParent = null;
                    HidePlacementPreviewUI();

                    EquipHandItem();
                    RefreshAllCraftingGrids();
                }
                return;
            }

            if (item == null) return;

            if (item.isEmptyWaterBottle && item.filledWater != null)
            {
                if (IsLookingAtWater())
                {
                    equippedSlot.SetItem(item.filledWater, 1);
                    EquipHandItem();
                    RefreshAllCraftingGrids();
                    ShowScreenNotification("Collected salt water.");
                }
            }
            else if (item.isConsumable && playerScript != null)
            {
                if (item.thirstRestoreAmount > 0 && playerScript.currentThirst >= playerScript.maxThirst) return;
                if (item.hungerRestoreAmount > 0 && playerScript.currentHunger >= playerScript.maxHunger) return;

                if (item.thirstRestoreAmount > 0) playerScript.RestoreThirst(item.thirstRestoreAmount);
                if (item.hungerRestoreAmount > 0) playerScript.RestoreHunger(item.hungerRestoreAmount);

                if (item.turnIntoItemAfterUse != null)
                {
                    equippedSlot.SetItem(item.turnIntoItemAfterUse, 1);
                }
                else
                {
                    equippedSlot.RemoveAmount(1);
                }

                EquipHandItem();
                RefreshAllCraftingGrids();
            }
        }
    }

    private bool IsLookingAtWater()
    {
        if (!TryGetInteractionRay(out Ray ray))
            return false;

        if (Physics.Raycast(ray, out RaycastHit hit, pickUpRange, Physics.AllLayers, QueryTriggerInteraction.Collide))
        {
            if (hit.collider.CompareTag("Ocean"))
                return true;
        }

        return false;
    }

    private void HandlePurification(Slot equippedSlot, ItemSO item)
    {
        if (item.purifiedVersion != null)
        {
            equippedSlot.SetItem(item.purifiedVersion, 1);
            
            EquipHandItem();
            RefreshAllCraftingGrids();
        }
    }

    private void RefreshAllCraftingGrids()
    {
        PopulateCraftingUI(craftingGrid, handRecipes, 4);

        if (isCraftingTableOpen && tableCraftingGrid != null)
        {
            PopulateCraftingUI(tableCraftingGrid, tableRecipes, 999);
        }
    }

    private void PopulateCraftingUI(Transform targetGrid, List<Recipe> recipesList, int maxLimit)
    {
        foreach (Transform child in targetGrid)
        {
            Destroy(child.gameObject);
        }

        List<Recipe> sortedRecipes = new List<Recipe>();

        foreach (Recipe recipe in recipesList) { if (CanCraft(recipe)) sortedRecipes.Add(recipe); }
        foreach (Recipe recipe in recipesList) { if (!CanCraft(recipe)) sortedRecipes.Add(recipe); }

        int recipesToShow = Mathf.Min(maxLimit, sortedRecipes.Count);

        for (int i = 0; i < recipesToShow; i++)
        {
            Recipe recipe = sortedRecipes[i];
            GameObject btnObject = Instantiate(craftingBtnPrefab, targetGrid);

            Transform fillTransform = btnObject.transform.Find("Fill");
            bool craftable = CanCraft(recipe);

            if (fillTransform != null && fillTransform.TryGetComponent<Image>(out var fillImage))
                fillImage.color = craftable ? craftableColor : lockedColor;

            Transform iconTransform = btnObject.transform.Find("Icon");
            if (iconTransform != null && iconTransform.TryGetComponent<Image>(out var iconImage))
            {
                iconImage.sprite = recipe.result.icon;
                iconImage.color = craftable ? Color.white : new Color(1f, 1f, 1f, 0.4f);
            }

            Button btn = btnObject.GetComponent<Button>();
            btn.interactable = true;
            btn.onClick.RemoveAllListeners();

            if (targetGrid == craftingGrid) // crafting table in inventory
            {
                btn.onClick.AddListener(() => Craft(recipe));
            }
            else // other recipes 
            {
                btn.onClick.AddListener(() => SelectRecipe(recipe));
            }

            EventTrigger trigger = btnObject.GetComponent<EventTrigger>();
            if (trigger == null) trigger = btnObject.AddComponent<EventTrigger>();
            trigger.triggers.Clear();

            EventTrigger.Entry enterEntry = new EventTrigger.Entry();
            enterEntry.eventID = EventTriggerType.PointerEnter;
            enterEntry.callback.AddListener((data) => { hoveredCraftingRecipe = recipe; });
            trigger.triggers.Add(enterEntry);

            EventTrigger.Entry exitEntry = new EventTrigger.Entry();
            exitEntry.eventID = EventTriggerType.PointerExit;
            exitEntry.callback.AddListener((data) => { 
                if (hoveredCraftingRecipe == recipe) hoveredCraftingRecipe = null; 
            });
            trigger.triggers.Add(exitEntry);
            
            Transform ingredientParent = btnObject.transform.Find("Grid");
            if (ingredientParent == null) ingredientParent = btnObject.transform.GetChild(1);

            foreach (Ingredient ingredient in recipe.ingredients)
            {
                GameObject neededItem = Instantiate(itemNeededUIPrefab, ingredientParent);

                Image ingredientImage = neededItem.GetComponentInChildren<Image>();
                if (ingredientImage != null)
                {
                    ingredientImage.sprite = ingredient.item.icon;
                }

                TextMeshProUGUI amountText = neededItem.GetComponentInChildren<TextMeshProUGUI>();
                if (amountText != null)
                {
                    amountText.text = "x" + ingredient.amount.ToString();
                }
            }
        }
    }

    public void SelectRecipe(Recipe recipe)
    {
        currentlySelectedRecipe = recipe;

        if (detailsPanel != null) 
            detailsPanel.SetActive(true); 

        if (craftingPromptText != null) 
            craftingPromptText.SetActive(false);

        detailsIcon.sprite = recipe.result.icon;
        detailsName.text = recipe.result.itemName;
        detailsDescription.text = recipe.result.description;

        foreach (Transform child in ingredientsContainer)
        {
            Destroy(child.gameObject);
        }

        bool canCraft = true;

        foreach (Ingredient ing in recipe.ingredients)
        {
            GameObject neededUI = Instantiate(detailsIngredientPrefab, ingredientsContainer);
            Image ingredientImage = neededUI.GetComponentInChildren<Image>();
            if (ingredientImage != null)
            {
                ingredientImage.sprite = ing.item.icon;
            }

            int amountInInventory = CountItemInInventory(ing.item); 
            TextMeshProUGUI ingredientText = neededUI.GetComponentInChildren<TextMeshProUGUI>();
            if (ingredientText == null && neededUI.transform.childCount > 0)
            {
                ingredientText = neededUI.transform.GetChild(0).GetComponent<TextMeshProUGUI>();
            }

            if (ingredientText != null)
            {
                if (amountInInventory >= ing.amount)
                {
                    ingredientText.text = $"<color=green>{amountInInventory}</color>/{ing.amount}";
                }
                else
                {
                    ingredientText.text = $"<color=red>{amountInInventory}</color>/{ing.amount}";
                    canCraft = false; 
                }
            }
            else
            {
                if (amountInInventory < ing.amount) canCraft = false;
            }
        }

        executeCraftButton.interactable = canCraft;
        executeCraftButton.onClick.RemoveAllListeners();
        executeCraftButton.onClick.AddListener(() => 
        {
            if (!CanCraft(recipe))
            {
                ShowCraftingNotification("<color=#801a00>Not enough materials!\nTry collecting some first.</color>");
                return;
            }

            Craft(recipe);
            SelectRecipe(recipe);
        });
    }


    public int CountItemInInventory(ItemSO item)
    {
        int totalCount = 0;

        if (inventorySlots != null)
        {
            foreach (Slot slot in inventorySlots)
            {
                if (slot.HasItem() && slot.GetItem() == item)
                {
                    totalCount += slot.GetAmount();
                }
            }
        }

        if (hotBarSlots != null)
        {
            foreach (Slot slot in hotBarSlots)
            {
                if (slot.HasItem() && slot.GetItem() == item)
                {
                    totalCount += slot.GetAmount();
                }
            }
        }

        return totalCount;
    }

    public void Craft(Recipe recipe)
    {
        if (!CanCraft(recipe))
        {
            ShowCraftingNotification("<color=#801a00>Not enough materials!\nTry collecting some first.</color>");
            return;
        }

        ConsumeIngredients(recipe);
        AddItem(recipe.result, recipe.resultAmount);

        bool shouldAutoPlace = recipe.result != null && recipe.result.isPlaceableStructure && recipe.result.itemPrefab != null;
        if (shouldAutoPlace && TryAutoEquipCraftedStructure(recipe.result))
        {
            ShowCraftingNotification($"<color=#66ff33>Crafted {recipe.resultAmount}x {recipe.result.itemName}!\nSelected for placement.</color>");
        }
        else
        {
            ShowCraftingNotification($"<color=#66ff33>Crafted {recipe.resultAmount}x {recipe.result.itemName}!\nAdded to inventory.</color>");
        }

        RefreshAllCraftingGrids();
    }

    private bool TryAutoEquipCraftedStructure(ItemSO item)
    {
        if (item == null || !item.isPlaceableStructure || item.itemPrefab == null)
            return false;

        for (int i = 0; i < hotBarSlots.Count; i++)
        {
            if (hotBarSlots[i].HasItem() && hotBarSlots[i].GetItem() == item)
            {
                equippedHotbarIndex = i;
                UpdateHotbarOpacity();
                EquipHandItem();
                return true;
            }
        }

        for (int i = 0; i < inventorySlots.Count; i++)
        {
            if (inventorySlots[i].HasItem() && inventorySlots[i].GetItem() == item)
            {
                for (int j = 0; j < hotBarSlots.Count; j++)
                {
                    if (!hotBarSlots[j].HasItem())
                    {
                        hotBarSlots[j].SetItem(item, inventorySlots[i].GetAmount());
                        inventorySlots[i].ClearSlot();
                        equippedHotbarIndex = j;
                        UpdateHotbarOpacity();
                        EquipHandItem();
                        return true;
                    }
                }
            }
        }

        for (int i = 0; i < hotBarSlots.Count; i++)
        {
            if (!hotBarSlots[i].HasItem())
            {
                hotBarSlots[i].SetItem(item, 1);
                equippedHotbarIndex = i;
                UpdateHotbarOpacity();
                EquipHandItem();
                return true;
            }
        }

        return false;
    }

    private void ConsumeIngredients(Recipe recipe)
    {
        foreach(Ingredient ingredient in recipe.ingredients)
        {
            int remaining = ingredient.amount;
            
            foreach(Slot slot in allSlots)
            {
                if (!slot.HasItem())
                    continue;

                if (slot.GetItem() != ingredient.item)
                    continue;

                int take = Mathf.Min(slot.GetAmount(), remaining);
                slot.SetItem(slot.GetItem(), slot.GetAmount() - take);

                if (slot.GetAmount() <= 0)
                {
                    slot.ClearSlot();
                    remaining -= take;
                    if (remaining <= 0)
                        break;
                }
            }
        }
    }

    public bool CanCraft(Recipe recipe)
    {
        foreach(Ingredient ingredient in recipe.ingredients)
        {
            int totalFound = 0;
            
            foreach(Slot slot in allSlots)
            {
                if (slot.HasItem() && slot.GetItem() == ingredient.item)
                {
                    totalFound += slot.GetAmount();
                }
            }

            if (totalFound < ingredient.amount)
            {
                return false;
            }
        }
        return true;
    }

    public bool InventoryHasItem(ItemSO itemToCheck, int amountNeeded)
    {
        int totalFound = 0;
        
        foreach(Slot slot in allSlots)
        {
            if (slot.HasItem() && slot.GetItem() == itemToCheck)
            {
                totalFound += slot.GetAmount();
            }
        }

        return totalFound >= amountNeeded;
    }

    public void RemoveItemFromInventory(ItemSO itemToRemove, int amountToRemove)
    {
        int remaining = amountToRemove;
        
        foreach(Slot slot in allSlots)
        {
            if (!slot.HasItem())
                continue;

            if (slot.GetItem() != itemToRemove)
                continue;

            int take = Mathf.Min(slot.GetAmount(), remaining);
            slot.SetItem(slot.GetItem(), slot.GetAmount() - take);

            if (slot.GetAmount() <= 0)
            {
                slot.ClearSlot();
            }

            remaining -= take;
            if (remaining <= 0)
                break;
        }
        RefreshAllCraftingGrids(); 
    }

    private void GiveStartingHotbarItem(ItemSO item)
    {
        foreach (Slot slot in hotBarSlots)
        {
            if (!slot.HasItem())
            {
                slot.SetItem(item, 1);
                RefreshAllCraftingGrids();
                return;
            }
        }

        Debug.LogWarning("No empty hotbar slot available for starting item: " + item.itemName);
    }

    public ItemSO GetEquippedItem()
    {
        Slot equippedSlot = hotBarSlots[equippedHotbarIndex];
        if (equippedSlot.HasItem())
        {
            return equippedSlot.GetItem();
        }
        return null;
    }

    public void ShowScreenNotification(string message)
    {
        if (notificationText != null)
        {
            notificationText.gameObject.SetActive(true);
            notificationText.text = message;

            CancelInvoke(nameof(HideNotification));
            Invoke(nameof(HideNotification), 1.5f);
        }
    }

    private void HideNotification()
    {
        if (notificationText != null)
        {
            notificationText.gameObject.SetActive(false);
        }
    }

    public void ShowCraftingNotification(string message)
    {
        if (craftingNotificationText != null)
        {
            if (craftingNotifCoroutine != null)
            {
                StopCoroutine(craftingNotifCoroutine);
            }

        craftingNotifCoroutine = StartCoroutine(CraftingNotificationSequence(message));
        }
    }

    private IEnumerator CraftingNotificationSequence(string message)
    {
        craftingNotificationText.text = message;
        craftingNotificationText.gameObject.SetActive(true);

        yield return new WaitForSecondsRealtime(1.5f);

        craftingNotificationText.gameObject.SetActive(false);
        craftingNotifCoroutine = null;
    }

    private bool IsInPlacementMode()
    {

        if (hotBarSlots == null || hotBarSlots.Count == 0)
            return false;

        Slot equippedSlot = hotBarSlots[equippedHotbarIndex];
        if (equippedSlot == null || !equippedSlot.HasItem())
            return false;

        ItemSO item = equippedSlot.GetItem();
        return item != null && item.isPlaceableStructure && item.itemPrefab != null;
    }

    private void ClearCurrentLookTarget()
    {
        if (lookAtRenderers != null)
        {
            for (int i = 0; i < lookAtRenderers.Length; i++)
            {
                if (lookAtRenderers[i] != null && originalMaterials != null && i < originalMaterials.Length)
                    lookAtRenderers[i].material = originalMaterials[i];
            }

            lookAtRenderers = null;
            originalMaterials = null;
        }

        currentLookAtUI = null;

        if (chestInteractionUI != null)
            chestInteractionUI.SetActive(false);
    }

    private void HandlePlacementRotation()
    {
        if (!IsInPlacementMode())
            return;

        if (Input.GetKeyDown(KeyCode.E))
            placementRotationY -= placementRotationStep;

        if (Input.GetKeyDown(KeyCode.R))
            placementRotationY += placementRotationStep;
    }

    private void ResetPlacementState(bool hidePreview = true)
    {
        isPreviewValid = false;
        previewParent = null;
        previewPosition = Vector3.zero;
        placementRotationY = 0f;
        lastPlacementHotbarIndex = -1;

        if (hidePreview && currentPreviewObject != null)
        {
            currentPreviewObject.SetActive(false);
        }
    }

    private void SyncPlacementRotationWithEquippedItem()
    {
        if (!IsInPlacementMode())
        {
            ResetPlacementState();
            return;
        }

        if (lastPlacementHotbarIndex != equippedHotbarIndex)
        {
            placementRotationY = 0f;
            lastPlacementHotbarIndex = equippedHotbarIndex;
        }
    }

    private void UpdatePlacementPreviewUI(ItemSO item)
    {
        if (currentPreviewObject == null || item == null)
            return;

        if (currentPreviewUI == null)
            currentPreviewUI = currentPreviewObject.GetComponentInChildren<ItemControlsUI>(true);

        if (currentPreviewUI == null)
            return;

        currentPreviewUI.SetText($"[E] Rotate -90 [R] Rotate +90 [Right Click] Place {item.itemName}");
        currentPreviewUI.ShowUI();
    }

    private void HidePlacementPreviewUI()
    {
        if (currentPreviewUI != null)
            currentPreviewUI.HideUI();
    }

    private IEnumerator HideThoughtBubbleAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (thoughtBubbleUI != null)
        {
            thoughtBubbleUI.HideUI();
        }
    }
}
