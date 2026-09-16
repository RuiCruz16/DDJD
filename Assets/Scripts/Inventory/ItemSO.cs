using UnityEngine;
using UnityEngine.Rendering;

[CreateAssetMenu(fileName = "Item", menuName = "NewItem")]

public class ItemSO : ScriptableObject
{
    [Header ("Items Attributes")]
    public string itemName;
    public Sprite icon;
    public int maxStackSize;
    public GameObject itemPrefab;
    public GameObject handItemPrefab;
    public string description;

    [Header("Placement Settings")]
    public float placementYOffset = 0f;

    [Header("Consumable Items")]
    public bool isConsumable;
    public int thirstRestoreAmount;
    public int hungerRestoreAmount;
    public ItemSO turnIntoItemAfterUse;

    [Header("Water Bottle Settings")]
    public bool isEmptyWaterBottle;
    public ItemSO filledWater;
    public ItemSO purifiedVersion;

    [Header("Equip Settings")]
    public bool isEquippable = true;

    [Header("Weapon Settings")]
    public bool isWeapon;

    [Header("Placement Settings")]
    public bool isPlaceableStructure; 

    [Header("UI Hints")]
    [Tooltip("Text to show in the thought bubble when equipped (leave empty for no bubble)")]
    public string equipHint;
}