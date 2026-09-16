using UnityEngine;
using UnityEngine.Rendering;

public class WaterBuoyancy : MonoBehaviour
{
    [Header("Water Settings")]
    public float upwardForce = 10f;   
    public float maxFloatSpeed = 6f;  // Safety limit to prevent shooting into the sky

    public float chestOffset = 1.2f;
    public float waterDrag = 3f; // Stop violent boucing

    [Header("Water Jump Settings")]
    public float waterJumpForce = 8f;    // Upward force applied when jumping out of water onto the raft
    public float raftCheckRadius = 1f; // Radius of the invisible sphere to detect the raft
    public float raftCheckHeight = 3f; // Extra vertical range so you can jump back while lower in the water
    
    private ThirdPersonMovement movementScript;

    public bool isInWater = false;

    private float originalGravity;
    private float floatPauseTimer = 0f;  // Pauses normal buoyancy briefly so the jump works

    void Start()
    {
        movementScript = GetComponent<ThirdPersonMovement>();
        
        if (movementScript != null)
        {
            originalGravity = movementScript.gravity; // Store the default gravity
        }
    }

    void Update()
    {
        if (isInWater && movementScript != null)
        {
            // JUMP LOGIC: If Space is pressed AND a collider with the "Raft" tag is near
            if (Input.GetButtonDown("Jump") && !movementScript.characterController.isGrounded && IsNearRaft())
            {
                movementScript.velocity.y = waterJumpForce;
                floatPauseTimer = 0.5f; // Pause normal water physics for half a second
            }

            // If we just jumped, countdown the timer and skip normal floating physics
            if (floatPauseTimer > 0)
            {
                floatPauseTimer -= Time.deltaTime;
                return; 
            }


            if (movementScript.characterController.isGrounded)
            {
                movementScript.gravity = originalGravity;
                return;
            }
            else
            {
                movementScript.gravity = -2f; // Ensure swimming gravity while in the water
            }

            float currentWaveHeight = 0f;
            if (LowPolyWater.LowPolyWater.instance != null)
            {
                currentWaveHeight = LowPolyWater.LowPolyWater.instance.GetWaveHeight(transform.position);
            }

            // BUOYANCY LOGIC: Calculate distance between player and water surface
            float playerChestY = transform.position.y + chestOffset;
            float depth = currentWaveHeight - playerChestY;

            if (depth > 0)
            {
                // Push player up   
                movementScript.velocity.y += upwardForce * depth * Time.deltaTime;
                // Vertical drag to smooth out the bouncing
                movementScript.velocity.y = Mathf.Lerp(movementScript.velocity.y, 0f, Time.deltaTime * waterDrag);

                // Clamp velocity to prevent launching into the air (only applied when floating normally)
                if (movementScript.velocity.y > maxFloatSpeed) 
                {
                    movementScript.velocity.y = maxFloatSpeed;
                }
            }
        }
    }

    // Creates an invisible sphere around the player to check for the raft's colliders
    private bool IsNearRaft()
    {
        Vector3 checkCenter = transform.position + new Vector3(0f, chestOffset * 0.5f, 0f);
        Vector3 halfExtents = new Vector3(raftCheckRadius, raftCheckHeight, raftCheckRadius);
        Collider[] hitColliders = Physics.OverlapBox(checkCenter, halfExtents);
        foreach (var col in hitColliders)
        {
            // Detects the tag directly on the collider it touches
            if (col.CompareTag("Raft"))
            {
                return true; 
            }
        }
        return false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Ocean"))
        {
            isInWater = true;
            if (movementScript != null) 
            {
                movementScript.gravity = -2f; // Reduce gravity to simulate water resistance
                movementScript.velocity.y /= 3f; // Slow down the falling impact
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Ocean"))
        {
            isInWater = false;
            if (movementScript != null) 
            {
                movementScript.gravity = originalGravity; // Restore normal gravity
            }
        }
    }

    // Este código desenha a esfera do radar na janela "Scene" do Unity!
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Vector3 checkCenter = transform.position + new Vector3(0f, chestOffset * 0.5f, 0f);
        Gizmos.DrawWireCube(checkCenter, new Vector3(raftCheckRadius * 2f, raftCheckHeight * 2f, raftCheckRadius * 2f));
    }
}
