using System.Collections;
using UnityEngine;

public class FishingRodBehaviour : MonoBehaviour
{
    [Header("State")]
    public bool isThrown = false;

    [Header("Water Settings")]
    public float waterLevel = 0f;          // The Y position of your ocean
    public float floatAmplitude = 0.15f;   // How high/low it bobs in the water
    public float floatSpeed = 1.5f;        // How fast it bobs

    [Header("Catch Settings")]
    public float catchRadius = 1.5f;       // The size of the magnetic catch sphere

    [Header("Mini-Game Settings")]
    public FishingMiniGameUI miniGameUI;   // Reference to the mini-game UI
    [Tooltip("Seconds after a failed catch during which no new fish can be hooked. " +
             "Prevents the just-escaped fish from being instantly re-caught.")]
    public float failedCatchCooldown = 2f;

    [HideInInspector] 
    public bool isTouchingRaft = false;    // Read by HookThrower to know when to lift it

    [HideInInspector]
    public bool requestAutoReel = false;   // Set after a failed mini-game so the thrower auto-reels

    // Fired exactly once, the moment the rod first touches the water surface.
    // The thrower subscribes to this so the splash sound plays on impact, not on release.
    public System.Action onWaterImpact;

    private Rigidbody rb;
    private bool isFloating = false;       
    private float randomOffset;
    private Collider currentCaughtFish;    // The fish currently being caught
    public bool isInMiniGame = false;      // Are we currently in the mini-game?            
    private float catchCooldownRemaining = 0f;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        randomOffset = Random.Range(0f, 100f);
    }

    void Update()
    {
        if (!isThrown)
            return;

        // CHECK FOR WATER IMPACT
        if (!isFloating && transform.position.y <= waterLevel)
        {
            StopAtWater();
        }

        // BOBBING EFFECT
        if (isFloating && (rb == null || !rb.isKinematic))
        {
            float newY = waterLevel + Mathf.Sin(Time.time * floatSpeed + randomOffset) * floatAmplitude;
            transform.position = new Vector3(transform.position.x, newY, transform.position.z);
        }

        // Tick down the post-failure cooldown so a just-escaped fish has time to swim away.
        if (catchCooldownRemaining > 0f)
            catchCooldownRemaining -= Time.deltaTime;

        // MAGNETIC CATCH
        // Continuously scan for debris around the hook
        CatchFishesInRadius();
    }

    void StopAtWater()
    {
        isFloating = true;

        // Notify listeners (e.g. the FMOD splash sound) that the rod just hit the water.
        onWaterImpact?.Invoke();
        
        if (rb != null)
        {
            rb.useGravity = false;
            if (!rb.isKinematic) 
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
        }
    }

    void CatchFishesInRadius()
    {
        // Skip if we're already in a mini-game
        if (isInMiniGame)
            return;

        if (currentCaughtFish != null)
            return;

        // Skip while the post-failure cooldown is active so we don't instantly
        // re-catch the fish that just escaped.
        if (catchCooldownRemaining > 0f)
            return;

        // Creates an invisible sphere and gets everything inside it
        Collider[] collidersInRange = Physics.OverlapSphere(transform.position, catchRadius);

        foreach (Collider col in collidersInRange)
        {
            // If it's a Fish AND it's not already attached to our hook
            if (col.CompareTag("Fish") && col.transform.parent != this.transform)
            {
                // Start the mini-game for this fish
                currentCaughtFish = col;
                isInMiniGame = true;

                CompleteCatch(currentCaughtFish); // Temporarily attach the fish to the hook while in the mini-game

                // Find the mini-game UI if not already set
                if (miniGameUI == null)
                    miniGameUI = FindFirstObjectByType<FishingMiniGameUI>(FindObjectsInactive.Include);

                if (miniGameUI == null)
                {
                    GameObject uiGO = new GameObject("FishingMiniGameUI (Auto)");
                    miniGameUI = uiGO.AddComponent<FishingMiniGameUI>();
                    StartCoroutine(StartMiniGameNextFrame());
                }
                else if (miniGameUI != null)
                {
                    miniGameUI.StartMiniGame(OnCatchAttemptResult);
                }
                else
                {
                    Debug.LogWarning("FishingRodBehaviour: FishingMiniGameUI could not be created. Releasing fish.");
                    // Release the fish so the rod isn't permanently stuck
                    ReleaseCurrentFish();
                    isInMiniGame = false;
                }

                // Only catch one fish at a time
                break;
            }
        }
    }

    private void CompleteCatch(Collider fishCollider)
    {
        Rigidbody fishRb = fishCollider.GetComponent<Rigidbody>();
        if (fishRb != null) fishRb.isKinematic = true;

        // Make the fish a "child" of the hook
        fishCollider.transform.SetParent(this.transform);
        
        // Snap the fish exactly to the center of the hook to show they are linked
        fishCollider.transform.localPosition = Vector3.zero;
    }

    private void OnCatchAttemptResult(bool success)
    {
        isInMiniGame = false;

        if (success && currentCaughtFish != null)
        {
            // Successfully caught the fish!
            CompleteCatch(currentCaughtFish);
            Debug.Log("Fish caught successfully!");
        }
        else
        {
            // Failed to catch - the fish escapes and the rod should reel back empty.
            ReleaseCurrentFish();
            catchCooldownRemaining = Mathf.Max(0f, failedCatchCooldown);
            requestAutoReel = true;
            Debug.Log("Fish escaped!");
        }

        if (!success)
        {
            currentCaughtFish = null;
        }
    }

    private void ReleaseCurrentFish()
    {
        if (currentCaughtFish == null) return;

        // Unparent so FishMovement (which idles while parented) takes back control
        // and the fish keeps its normal swimming behavior.
        currentCaughtFish.transform.SetParent(null);

        // Leave the rigidbody kinematic: FishMovement drives the transform directly,
        // so giving it a physics velocity would just fight that script. Keeping it
        // kinematic preserves the fish's original movement.
        Rigidbody fishRb = currentCaughtFish.GetComponent<Rigidbody>();
        if (fishRb != null)
        {
            fishRb.linearVelocity = Vector3.zero;
            fishRb.angularVelocity = Vector3.zero;
        }
    }

    private IEnumerator StartMiniGameNextFrame()
    {
        // Wait one frame so the freshly-added FishingMiniGameUI's Start() runs
        // (it hides the canvas on Start) BEFORE we call StartMiniGame.
        yield return null;

        if (miniGameUI != null && isInMiniGame)
        {
            miniGameUI.StartMiniGame(OnCatchAttemptResult);
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (!isThrown)
            return;

        // RAFT EDGE DETECTION
        // Tells the HookThrower script that it's time to destroy the hook
        if (collision.gameObject.CompareTag("Raft"))
        {
            isTouchingRaft = true;
        }
    }

    // RAFT EDGE DETECTION (For the reeling phase)
    // This triggers the moment the ghost hook touches the raft
    void OnTriggerEnter(Collider other)
    {
        if (!isThrown)
            return;

        // Includes Barricades so it knows it reached the base
        if (other.CompareTag("Raft") || other.CompareTag("Barricade"))
        {
            isTouchingRaft = true;
        }
    }

    // VISUAL DEBUGGER IN EDITOR
    // Draws a yellow wireframe sphere in the Scene view so you can see the catch radius
    void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, catchRadius);
    }
}