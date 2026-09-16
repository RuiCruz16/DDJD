using UnityEngine;
using UnityEngine.UI;

public class FishingRodThrower : MonoBehaviour
{
    [Header("References")]
    public ItemSO fishingRodItemSO;
    public GameObject fishingRodPrefab;         
    public Transform spawnPoint;                
    
    public Transform hand;                

    [Header("Inventory")]
    public Inventory inventory;

    [Header("UI")]
    public Slider chargeBar;

    [Header("Throw Settings")]
    public float minThrowForce = 2f;      
    public float maxThrowForce = 40f;     
    public float upwardForce = 6f;        
    public float reelSpeed = 10f;         

    [Header("Charge Settings")]
    public float maxChargeTime = 5f;      
    private float currentChargeTime = 0f; 
    private bool isCharging = false;      

    [Header("Animation Angles (X Axis)")]
    public float restAngle = 0f;          
    public float maxChargeAngle = -25f;   
    public float thrownAngle = 75f;       

    [Header("Animation Speeds")]
    public float chargeRotationSpeed = 5f; 
    public float throwRotationSpeed = 15f;  
    public float returnRotationSpeed = 8f;  

    [Header("Rope Visuals")]
    public Color ropeColor = new Color(0.6f, 0.4f, 0.3f); 
    public Material ropeMaterial;
    
    private LineRenderer thrownRope;      
    private LineRenderer staticRope;      

    private GameObject currentActiveFishingRod; 
    private FishingRodBehaviour currentFishingRodBehavior; 
    private bool isReeling = false;
    private PlayerFishingRodThrowFMOD fishingRodThrowFMOD;

    void Start()
    {
        if (chargeBar != null) 
        {
            chargeBar.gameObject.SetActive(false);
        }

        if (inventory == null)
        {
            inventory = FindFirstObjectByType<Inventory>();
        }

        fishingRodThrowFMOD = GetComponent<PlayerFishingRodThrowFMOD>();

        GameObject thrownRopeObj = new GameObject("ThrownRope");
        thrownRopeObj.transform.SetParent(this.transform); 
        thrownRope = thrownRopeObj.AddComponent<LineRenderer>();
        SetupRope(thrownRope);

        GameObject staticRopeObj = new GameObject("StaticRope");
        staticRopeObj.transform.SetParent(this.transform); 
        staticRope = staticRopeObj.AddComponent<LineRenderer>();
        SetupRope(staticRope);
    }

    private void SetupRope(LineRenderer rope)
    {
        rope.startWidth = 0.02f; 
        rope.endWidth = 0.02f;
        rope.enabled = false; 
        if (ropeMaterial != null)
        {
            rope.material = ropeMaterial;
        }
        rope.startColor = ropeColor;
        rope.endColor = ropeColor;
    }

    void Update()
    {
        HandleInput();
        
        AnimateRodAndRopes();

        bool isPlayingMiniGame = currentFishingRodBehavior != null && currentFishingRodBehavior.isInMiniGame;

        if (!isReeling && !isPlayingMiniGame && currentFishingRodBehavior != null && currentFishingRodBehavior.requestAutoReel)
        {
            currentFishingRodBehavior.requestAutoReel = false;
            StartReeling();
        }

        if (isReeling && currentActiveFishingRod != null && !isPlayingMiniGame)
        {
            ReelFishingRodIn();
        }
    }

    void HandleInput()
    {
        ItemSO equippedItem = inventory.GetEquippedItem();
        bool hookIsEquipped = equippedItem != null && equippedItem == fishingRodItemSO;

        if (!hookIsEquipped && currentActiveFishingRod == null)
        {
            if (isCharging)
            {
                isCharging = false;
                if (chargeBar != null) chargeBar.gameObject.SetActive(false);
            }
            return;
        }

        bool throwInputDown = Input.GetKeyDown(KeyCode.H) || Input.GetMouseButtonDown(0);
        bool throwInputUp = Input.GetKeyUp(KeyCode.H) || Input.GetMouseButtonUp(0);

        if (currentActiveFishingRod == null)
        {
            if (throwInputDown)
            {
                isCharging = true;
                currentChargeTime = 0f; 

                if (chargeBar != null)
                {
                    chargeBar.gameObject.SetActive(true);
                    chargeBar.value = 0f;
                }
            }

            if (isCharging)
            {
                currentChargeTime += Time.deltaTime;
                currentChargeTime = Mathf.Clamp(currentChargeTime, 0f, maxChargeTime);
            
                if (chargeBar != null)
                {
                    chargeBar.value = currentChargeTime / maxChargeTime;
                }
            }

            if (throwInputUp && isCharging)
            {
                isCharging = false;

                if (chargeBar != null)
                {
                    chargeBar.gameObject.SetActive(false);
                }
                
                float chargePct = currentChargeTime / maxChargeTime;
                float finalForce = Mathf.Lerp(minThrowForce, maxThrowForce, chargePct);
                
                ThrowFishingRod(finalForce);
            }
        }
        else if (throwInputDown && !isReeling)
        {
            bool isPlayingMiniGame = currentFishingRodBehavior != null && currentFishingRodBehavior.isInMiniGame;
            if (!isPlayingMiniGame)
            {
                StartReeling();
            }
        }
    }

    void AnimateRodAndRopes()
    {
        Transform equippedRod = null;
        Transform rodBase = null;
        Transform rodTip = null;

        if (hand != null && hand.childCount > 0)
        {
            equippedRod = hand.GetChild(0);
            rodBase = equippedRod.Find("Rod_Base");
            rodTip = equippedRod.Find("Rod_Tip");
        }

        if (equippedRod != null)
        {
            float targetAngle = restAngle;
            float rotSpeed = returnRotationSpeed; 

            if (isCharging)
            {
                targetAngle = maxChargeAngle;
                rotSpeed = chargeRotationSpeed;
            }
            else if (isReeling)
            {
                targetAngle = thrownAngle; 
                rotSpeed = throwRotationSpeed;
            }
            else if (currentActiveFishingRod != null)
            {
                targetAngle = thrownAngle;
                rotSpeed = throwRotationSpeed; 
            }

            Quaternion targetRotation = Quaternion.Euler(targetAngle, 0, 0);
            equippedRod.localRotation = Quaternion.Lerp(equippedRod.localRotation, targetRotation, Time.deltaTime * rotSpeed);
        }

        if (rodBase != null && rodTip != null)
        {
            staticRope.enabled = true;
            staticRope.SetPosition(0, rodBase.position);
            staticRope.SetPosition(1, rodTip.position);
        }
        else
        {
            staticRope.enabled = false;
        }

        if (currentActiveFishingRod != null)
        {
            thrownRope.enabled = true;
            
            Vector3 ropeStartPos = (rodTip != null) ? rodTip.position : spawnPoint.position;
            
            thrownRope.SetPosition(0, ropeStartPos);
            thrownRope.SetPosition(1, currentActiveFishingRod.transform.position);
        }
        else
        {
            thrownRope.enabled = false;
        }
    }

    void ThrowFishingRod(float force)
    {
        currentActiveFishingRod = Instantiate(fishingRodPrefab, spawnPoint.position, spawnPoint.rotation);
        currentFishingRodBehavior = currentActiveFishingRod.GetComponent<FishingRodBehaviour>();

        Item lootScript = currentActiveFishingRod.GetComponent<Item>();
        if (lootScript != null) Destroy(lootScript);

        if (currentFishingRodBehavior != null)
        {
            currentFishingRodBehavior.isThrown = true;

            // Play the throw/splash sound when the rod actually hits the water,
            // not the instant the player releases it.
            if (fishingRodThrowFMOD != null)
                currentFishingRodBehavior.onWaterImpact += fishingRodThrowFMOD.HandleFishingRodThrow;
        }

        Rigidbody rb = currentActiveFishingRod.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.freezeRotation = true;

            Vector3 throwDir = (transform.forward * force) + (Vector3.up * upwardForce);
            rb.AddForce(throwDir, ForceMode.Impulse);
        }
    }

    void StartReeling()
    {
        isReeling = true;
        Rigidbody rb = currentActiveFishingRod.GetComponent<Rigidbody>();
        if (rb != null) 
        {
            rb.isKinematic = true; 
        }

        Collider col = currentActiveFishingRod.GetComponent<Collider>();
        if (col != null) col.isTrigger = true;
    }

    void ReelFishingRodIn()
    {
        float targetY = currentActiveFishingRod.transform.position.y;
        if (currentFishingRodBehavior != null)
        {
            targetY = currentFishingRodBehavior.waterLevel;
        }

        Vector3 targetPos = new Vector3(spawnPoint.position.x, targetY, spawnPoint.position.z);

        currentActiveFishingRod.transform.position = Vector3.MoveTowards(
            currentActiveFishingRod.transform.position, 
            targetPos, 
            reelSpeed * Time.deltaTime
        );

        float distanceToPlayerXZ = Vector2.Distance(
            new Vector2(currentActiveFishingRod.transform.position.x, currentActiveFishingRod.transform.position.z),
            new Vector2(spawnPoint.position.x, spawnPoint.position.z)
        );

        bool hitRaft = false;
        Collider[] hits = Physics.OverlapSphere(currentActiveFishingRod.transform.position, 0.5f);
        foreach (Collider hit in hits)
        {
            if (hit.CompareTag("Raft") || hit.CompareTag("Barricade"))
            {
                hitRaft = true;
                break;
            }
        }

        if (hitRaft || distanceToPlayerXZ <= 1.5f)
        {
            int fishCount = 0;
            
            foreach (Transform child in currentActiveFishingRod.transform)
            {
                if (child.CompareTag("Fish"))
                {
                    fishCount++;
                    if (inventory != null && child.TryGetComponent<Item>(out var item))
                    {
                        inventory.AddItem(item.item, item.amount);
                    }
                }
            }

            if (fishCount > 0) Debug.Log($"Got {fishCount} Fish!");

            Destroy(currentActiveFishingRod); 
            currentActiveFishingRod = null;   
            currentFishingRodBehavior = null;
            isReeling = false;          
            thrownRope.enabled = false;       
        }
    }

    public bool IsHookActive()
    {
        return currentActiveFishingRod != null;
    }
}