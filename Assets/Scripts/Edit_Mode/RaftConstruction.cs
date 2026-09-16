using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public enum BuildType 
{ 
    Expansion, 
    OnTop      
}

[System.Serializable]
public class BuildableObject
{
    public string objectName;          
    public GameObject ghostPrefab;     
    public GameObject realPrefab;      
    public ItemSO requiredItem;        
    public int blocksRequired = 1;     
    public BuildType buildType;        
    public Vector3 pivotOffset = Vector3.zero; 
}

public class RaftConstruction : MonoBehaviour
{
    [Header("Inventory Settings")]
    public Inventory inventory;

    [Header("Buildable Objects List")]
    public BuildableObject[] buildableObjects; 
    private int currentIndex = 0; 

    [Header("Prefab Settings")]
    public Transform raftManager;

    [Header("Feedback Materials")]
    public Material matWhite;
    public Material matGreen;
    public Material matRed;

    [Header("Placement Parameters")]
    public float buildDistance = 6f;
    public float gridSize = 5.9f;
    public float yHeight = 0.48f;
    public float ghostYOffset = 0.05f;

    [Header("UI Selection Elements")]
    public GameObject buildParent;
    public TextMeshProUGUI selectedNameText;
    public RectTransform selectorFrame;
    public RectTransform[] iconSlots;
    public GameObject gameUI;
    
    [Header("Warning UI")]
    public TextMeshProUGUI warningText; 
    public TMP_FontAsset warningTextFont;

    private GameObject currentGhost;
    public static bool isBuildingMode = false;
    private Transform playerTransform;
    private bool isPhysicallyValid = false;
    private PlayerBlockPlaceFMOD blockPlaceFMOD;
    private Coroutine warningCoroutine; 

    void Start()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerTransform = playerObj.transform;
            blockPlaceFMOD = playerObj.GetComponent<PlayerBlockPlaceFMOD>();
        }
        else
        {
            Debug.LogError("Player not found! Make sure your player object has the 'Player' tag.");
        }

        if (inventory == null) 
        {
            Debug.LogWarning("Inventory reference is missing in Raft Construction.");
        }

        EnsureWarningUI();
    }

    void EnsureWarningUI()
    {
        if (warningText == null)
        {
            GameObject textGO = new GameObject("BuildWarningText");
            RectTransform textRT = textGO.AddComponent<RectTransform>();
            
            if (buildParent != null)
            {
                textRT.SetParent(buildParent.transform, false);
            }
            
            textRT.anchorMin = new Vector2(0.5f, -1.0f);
            textRT.anchorMax = new Vector2(0.5f, -1.0f);
            textRT.pivot = new Vector2(0.5f, 0.5f);
            textRT.anchoredPosition = new Vector2(0f, 30f);
            textRT.sizeDelta = new Vector2(800f, 50f);

            warningText = textGO.AddComponent<TextMeshProUGUI>();

            if (warningTextFont != null)
            {
                warningText.font = warningTextFont;
            }

            warningText.alignment = TextAlignmentOptions.Center;
            warningText.color = Color.red;
            warningText.fontSize = 45;
            
            textGO.SetActive(false);
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.L))
        {
            ToggleBuildingMode();
        }

        if (isBuildingMode)
        {
            HandleObjectCycling();

            if (currentGhost != null)
            {
                MoveGhostToGrid();
                CheckPlacementLogic();

                if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Return))
                {
                    TryPlaceRaft();
                }
            }
        }
    }

    public void CancelConstruction()
    {
        if (isBuildingMode)
        {
            ToggleBuildingMode(); 
        }
    }

    void ToggleBuildingMode()
    {
        isBuildingMode = !isBuildingMode;

        if (buildParent != null) {
            buildParent.SetActive(isBuildingMode);
        }

        if (gameUI != null) {
            gameUI.SetActive(!isBuildingMode);
        }

        if (isBuildingMode)
        {
            currentIndex = 0; 
            RefreshGhost();
            UpdateUI();
        }
        else
        {
            if (currentGhost != null) Destroy(currentGhost);
        }
    }

    void HandleObjectCycling()
    {
        if (buildableObjects == null || buildableObjects.Length <= 1) return;

        bool hasChanged = false;

        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            currentIndex++;
            if (currentIndex >= buildableObjects.Length) currentIndex = 0;
            hasChanged = true;
        }
        else if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            currentIndex--;
            if (currentIndex < 0) currentIndex = buildableObjects.Length - 1;
            hasChanged = true;
        }

        if (hasChanged)
        {
            RefreshGhost();
            UpdateUI();
            Debug.Log($"Switched to -> {buildableObjects[currentIndex].objectName}");
        }
    }

    void UpdateUI()
    {
        if (buildableObjects.Length == 0) return;

        if (selectedNameText != null)
        {
            selectedNameText.text = buildableObjects[currentIndex].objectName;
        }

        if (selectorFrame != null && iconSlots != null && iconSlots.Length > currentIndex)
        {
            selectorFrame.position = iconSlots[currentIndex].position;
        }
    }

    void RefreshGhost()
    {
        if (currentGhost != null) Destroy(currentGhost);
        
        if (buildableObjects.Length > 0 && buildableObjects[currentIndex].ghostPrefab != null)
        {
            currentGhost = Instantiate(buildableObjects[currentIndex].ghostPrefab);
        }
    }

    void MoveGhostToGrid()
    {
        if (playerTransform == null || currentGhost == null) return;

        Transform camTransform = Camera.main.transform;
        Vector3 buildDirection = camTransform.forward;

        buildDirection.y = 0;
        buildDirection.Normalize();

        Vector3 targetPosWorld = playerTransform.position + (buildDirection * buildDistance);
        
        if (raftManager != null)
        {
            Vector3 localPos = raftManager.InverseTransformPoint(targetPosWorld);
            localPos.x = Mathf.Round(localPos.x / gridSize) * gridSize;
            localPos.z = Mathf.Round(localPos.z / gridSize) * gridSize;
            localPos.y = yHeight + ghostYOffset; 

            currentGhost.transform.position = raftManager.TransformPoint(localPos);
            
            BuildableObject currentObj = buildableObjects[currentIndex];

            if (currentObj.buildType == BuildType.OnTop)
            {
                Vector3 dirToPlayer = playerTransform.position - currentGhost.transform.position;
                dirToPlayer.y = 0;
                
                Vector3 localDir = raftManager.InverseTransformDirection(dirToPlayer.normalized);
                float angle = Mathf.Atan2(localDir.x, localDir.z) * Mathf.Rad2Deg;
                float snappedAngle = Mathf.Round(angle / 90f) * 90f;
                
                currentGhost.transform.rotation = raftManager.rotation * Quaternion.Euler(0, snappedAngle, 0);
            }
            else 
            {
                currentGhost.transform.rotation = raftManager.rotation;
            }

            currentGhost.transform.position += raftManager.rotation * currentObj.pivotOffset;
        }
        else
        {
            float x = Mathf.Round(targetPosWorld.x / gridSize) * gridSize;
            float z = Mathf.Round(targetPosWorld.z / gridSize) * gridSize;
            currentGhost.transform.position = new Vector3(x, yHeight + ghostYOffset, z);
        }
    }

    void CheckPlacementLogic()
    {
        if (currentGhost == null) return;

        BuildableObject currentObj = buildableObjects[currentIndex];
        
        Vector3 gridCenter = currentGhost.transform.position - (currentGhost.transform.rotation * currentObj.pivotOffset);
        float checkRadius = gridSize * 0.1f; 

        bool isOverlappingRaft = false;
        bool isOverlappingBarricade = false;
        bool isOverlappingNet = false; 

        Collider[] hitColliders = Physics.OverlapSphere(gridCenter, checkRadius);
        foreach (var hit in hitColliders)
        {
            if (hit.CompareTag("Raft"))
            {
                isOverlappingRaft = true;
                
                if (hit.GetComponentInParent<CollectionNet>() != null)
                {
                    isOverlappingNet = true;
                }
            }
            if (hit.CompareTag("Barricade")) isOverlappingBarricade = true;
        }

        if (currentObj.buildType == BuildType.Expansion)
        {
            bool isAdjacent = false;
            Vector3[] directions = { 
                Vector3.forward, Vector3.back, Vector3.left, Vector3.right,           
                new Vector3(1, 0, 1), new Vector3(1, 0, -1),                          
                new Vector3(-1, 0, 1), new Vector3(-1, 0, -1)                         
            };
            
            foreach (Vector3 dir in directions)
            {
                Vector3 rotatedDir = currentGhost.transform.TransformDirection(dir);
                Collider[] adjacentColliders = Physics.OverlapSphere(gridCenter + (rotatedDir * gridSize), checkRadius);
                foreach (var hit in adjacentColliders)
                {
                    if (hit.CompareTag("Raft"))
                    {
                        isAdjacent = true;
                        break;
                    }
                }
                if (isAdjacent) break; 
            }

            isPhysicallyValid = isAdjacent && !isOverlappingRaft && !isOverlappingBarricade;
        }
        else if (currentObj.buildType == BuildType.OnTop)
        {
            isPhysicallyValid = isOverlappingRaft && !isOverlappingBarricade && !isOverlappingNet;
        }

        bool hasEnoughItems = inventory != null && inventory.InventoryHasItem(currentObj.requiredItem, currentObj.blocksRequired);

        if (isPhysicallyValid && hasEnoughItems)
        {
            UpdateGhostMaterial(matGreen); 
        }
        else
        {
            UpdateGhostMaterial(matRed);   
        }
    }

    void UpdateGhostMaterial(Material targetMat)
    {
        if (currentGhost == null) return;

        MeshRenderer[] renderers = currentGhost.GetComponentsInChildren<MeshRenderer>();
        foreach (var r in renderers)
        {
            r.material = targetMat;
        }
    }

    void TryPlaceRaft()
    {
        BuildableObject currentObj = buildableObjects[currentIndex];
        bool hasEnoughItems = inventory != null && inventory.InventoryHasItem(currentObj.requiredItem, currentObj.blocksRequired);

        if (!hasEnoughItems)
        {
            ShowWarning("You don't have the block material to build!");
            return;
        }

        if (!isPhysicallyValid)
        {
            if (currentObj.buildType == BuildType.Expansion)
            {
                ShowWarning("Must be placed adjacent to a raft block!");
            }
            else if (currentObj.buildType == BuildType.OnTop)
            {
                ShowWarning("Must be placed on top of a raft block!");
            }
            return; 
        }

        inventory.RemoveItemFromInventory(currentObj.requiredItem, currentObj.blocksRequired);
        
        if (blockPlaceFMOD != null)
        {
            blockPlaceFMOD.HandleBlockPlace();
        }
        
        GameObject newPiece = Instantiate(currentObj.realPrefab, currentGhost.transform.position, currentGhost.transform.rotation);
        if (raftManager != null)
        {
            newPiece.transform.SetParent(raftManager);

            Vector3 localPos = newPiece.transform.localPosition;
            localPos.y = yHeight + currentObj.pivotOffset.y; 
            newPiece.transform.localPosition = localPos;

            newPiece.transform.rotation = currentGhost.transform.rotation;
        }
    }

    void ShowWarning(string message)
    {
        if (warningText != null)
        {
            warningText.text = message;
            warningText.gameObject.SetActive(true);
            
            if (warningCoroutine != null) StopCoroutine(warningCoroutine);
            warningCoroutine = StartCoroutine(HideWarningRoutine());
        }
    }

    System.Collections.IEnumerator HideWarningRoutine()
    {
        yield return new WaitForSeconds(2.0f);
        if (warningText != null)
        {
            warningText.gameObject.SetActive(false);
        }
    }
}
