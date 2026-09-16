using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class HookThrower : MonoBehaviour
{
    [Header("References")]
    public ItemSO hookItemSO;
    public GameObject hookPrefab;       
    public Transform spawnPoint;  
    public Animator characterAnimator;        
    
    public Transform hand;                

    private LineRenderer rope;           

    [Header("Inventory")]
    public Inventory inventory;

    [Header("UI")]
    public Slider chargeBar;

    [Header("Throw Settings")]
    public float minThrowForce = 2f;      // Minimum force (quick tap)
    public float maxThrowForce = 40f;     // Maximum force (holding for 5 seconds)
    public float upwardForce = 6f;        // Gives the throw an arc
    public float reelSpeed = 10f;         // How fast the hook comes back to the player
    public float throwDelay = 0.2f;

    [Header("Charge Settings")]
    public float maxChargeTime = 5f;      // Maximum seconds the player can hold 'H'
    private float currentChargeTime = 0f; // Internal timer
    private bool isCharging = false;      // Is the player holding 'H'?
    private bool isThrowingSequence = false;

    [Header("Rope Visuals")]
    public Color ropeColor = new Color(0.6f, 0.4f, 0.3f); // Light Brown Color

    // Internal State Variables
    private GameObject currentActiveHook; 
    private HookBehavior currentHookBehavior; // Cached to read 'isTouchingRaft'
    private bool isReeling = false;       

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

        if (characterAnimator == null)
        {
            characterAnimator = GetComponentInChildren<Animator>();
        }

        // Create a dedicated child object for the rope to avoid LineRenderer conflicts
        GameObject ropeObject = new GameObject(this.GetType().Name + "_Rope");
        ropeObject.transform.SetParent(this.transform); // Attach it to the Player
        
        rope = ropeObject.AddComponent<LineRenderer>();
        
        rope.startWidth = 0.05f;
        rope.endWidth = 0.05f;
        rope.enabled = false; 
        
        rope.material = new Material(Shader.Find("Sprites/Default"));
        rope.startColor = ropeColor;
        rope.endColor = ropeColor;
    }

    void Update()
    {
        HandleInput();

        bool touchedRaft = currentHookBehavior != null && currentHookBehavior.isTouchingRaft;
        
        // If we pressed 'H' to reel it in, and the hook still exists, pull it!
        if ((isReeling || touchedRaft) && currentActiveHook != null)
        {
            ReelHookIn();
        }

        // If the hook is currently out in the world, update the rope positions
        if (currentActiveHook != null)
        {
            DrawRope();
        }
    }

    void HandleInput()
    {
        ItemSO equippedItem = inventory.GetEquippedItem();
        bool hookIsEquipped = equippedItem != null && equippedItem == hookItemSO;

        if ((!hookIsEquipped && currentActiveHook == null) || isThrowingSequence)
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

        // IF THE HOOK IS IN OUR HAND (Not thrown yet)
        if (currentActiveHook == null)
        {
            // 1. Start charging the throw
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

            // 2. Increase the timer while holding the button
            if (isCharging)
            {
                currentChargeTime += Time.deltaTime;
                currentChargeTime = Mathf.Clamp(currentChargeTime, 0f, maxChargeTime);
            
                if (chargeBar != null)
                {
                    chargeBar.value = currentChargeTime / maxChargeTime;
                }
            }

            // 3. Throw the hook when the button is released
            if (throwInputUp && isCharging)
            {
                isCharging = false;

                if (chargeBar != null)
                {
                    chargeBar.gameObject.SetActive(false);
                }
                
                float chargePct = currentChargeTime / maxChargeTime;
                float finalForce = Mathf.Lerp(minThrowForce, maxThrowForce, chargePct);
                
                ThrowHook(finalForce);
            }
        }
        // IF THE HOOK IS IN THE WATER (Ready to be pulled back)
        else if (throwInputDown && !isReeling)
        {
            StartReeling();
        }
    }

    void ThrowHook(float force)
    {
        // 1. Instantly trigger the animation visual wind-up
        if (characterAnimator != null)
        {
            characterAnimator.SetTrigger("ThrowTrigger"); 
        }

        // 2. Start the background timer to delay the physics spawn
        StartCoroutine(ExecuteDelayedThrow(force));
    }

    private IEnumerator ExecuteDelayedThrow(float force)
    {
        isThrowingSequence = true;

        yield return new WaitForSeconds(throwDelay);

        if (hand != null && hand.childCount > 0)
        {
            hand.GetChild(0).gameObject.SetActive(false);
        }

        currentActiveHook = Instantiate(hookPrefab, spawnPoint.position, spawnPoint.rotation);
        
        Item hookLootScript = currentActiveHook.GetComponent<Item>();
        if (hookLootScript != null) Destroy(hookLootScript);
        
        currentHookBehavior = currentActiveHook.GetComponent<HookBehavior>();
        currentHookBehavior.isThrown = true;

        Rigidbody rb = currentActiveHook.GetComponent<Rigidbody>();
        rb.freezeRotation = true;

        Collider col = currentActiveHook.GetComponent<Collider>();
        if (col != null) col.isTrigger = true;

        Vector3 throwDir = (transform.forward * force) + (Vector3.up * upwardForce);
        rb.AddForce(throwDir, ForceMode.Impulse);

        rope.enabled = true;
        isThrowingSequence = false; 
    }

    void StartReeling()
    {
        isReeling = true;
        Rigidbody rb = currentActiveHook.GetComponent<Rigidbody>();
        if (rb != null) 
        {
            rb.isKinematic = true; 
        }

        Collider col = currentActiveHook.GetComponent<Collider>();
        if (col != null) 
        {
            col.isTrigger = true; 
        }
    }

    void ReelHookIn()
    {
        Vector3 targetPos = spawnPoint.position; 
        if (currentHookBehavior != null)
        {
            targetPos = new Vector3(spawnPoint.position.x, currentHookBehavior.waterLevel, spawnPoint.position.z);
        }

        currentActiveHook.transform.position = Vector3.MoveTowards(
            currentActiveHook.transform.position, 
            targetPos, 
            reelSpeed * Time.deltaTime
        );

        // CHECK RAFT COLLISION
        bool hitRaft = currentHookBehavior != null && currentHookBehavior.isTouchingRaft;

        if (Vector3.Distance(currentActiveHook.transform.position, spawnPoint.position) < 0.6f || hitRaft)
        {
            int debrisCount = 0;
            
            foreach (Transform child in currentActiveHook.transform)
            {
                if (child.CompareTag("Debris"))
                {
                    debrisCount++;

                    if (inventory != null && child.TryGetComponent<Item>(out var item))
                    {
                        inventory.AddItem(item.item, item.amount);
                    }
                }
            }

            // Only print if we actually caught something
            if (debrisCount > 0)
            {
                Debug.Log($"Got {debrisCount} debris!");
            }

            if (debrisCount > 0 && inventory == null)
            {
                Debug.LogWarning("Caught debris, but no Inventory was found to store it.");
            }

            Destroy(currentActiveHook); 
            currentActiveHook = null;   
            currentHookBehavior = null;
            isReeling = false;          
            rope.enabled = false;       
            
            if (hand != null && hand.childCount > 0)
            {
                hand.GetChild(0).gameObject.SetActive(true);
            }
        }
    }

    void DrawRope()
    {
        rope.SetPosition(0, spawnPoint.position);
        rope.SetPosition(1, currentActiveHook.transform.position);
    }

    public bool IsHookActive()
    {
        return currentActiveHook != null;
    }
}
