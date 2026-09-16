using UnityEngine;

public class HookBehavior : MonoBehaviour
{
    [Header("State")]
    public bool isThrown = false;

    [Header("Water Settings")]
    public float waterLevel = 0f;          // The Y position of your ocean
    public float floatAmplitude = 0.15f;   // How high/low it bobs in the water
    public float floatSpeed = 1.5f;        // How fast it bobs

    [Header("Air Animation")]
    public float airRotationSpeed = 120f;
    public float maxRotationAngle = 90f;
    public Vector3 rotationAxis = new Vector3(1, 0, 0);

    private float currentRotatedAmount = 0f;

    [Header("Catch Settings")]
    public float catchRadius = 1.5f;       // The size of the magnetic catch sphere

    [HideInInspector] 
    public bool isTouchingRaft = false;    // Read by HookThrower to know when to lift it

    private Rigidbody rb;
    private bool isFloating = false;       
    private float randomOffset;            

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        randomOffset = Random.Range(0f, 100f);
    }

    void Update()
    {
        if (!isThrown)
            return;

        // LOGIC WHILE IN THE AIR
        if (!isFloating)
        {
            if (currentRotatedAmount < maxRotationAngle)
            {
                float step = airRotationSpeed * Time.deltaTime;
                
                if (currentRotatedAmount + step > maxRotationAngle)
                {
                    step = maxRotationAngle - currentRotatedAmount;
                }
                
                transform.Rotate(rotationAxis * step, Space.Self);
                currentRotatedAmount += step;
            }

            // CHECK FOR WATER IMPACT
            if (transform.position.y <= waterLevel)
            {
                StopAtWater();
            }
        }

        // BOBBING EFFECT
        if (isFloating && (rb == null || !rb.isKinematic))
        {
            float newY = waterLevel + Mathf.Sin(Time.time * floatSpeed + randomOffset) * floatAmplitude;
            transform.position = new Vector3(transform.position.x, newY, transform.position.z);
        }

        // MAGNETIC CATCH
        // Continuously scan for debris around the hook
        CatchDebrisInRadius();
    }

    void StopAtWater()
    {
        isFloating = true;
        
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

    void CatchDebrisInRadius()
    {
        // Creates an invisible sphere and gets everything inside it
        Collider[] collidersInRange = Physics.OverlapSphere(transform.position, catchRadius);

        foreach (Collider col in collidersInRange)
        {
            // If it's a Debris AND it's not already attached to our hook
            if (col.CompareTag("Debris") && col.transform.parent != this.transform)
            {
                Rigidbody debrisRb = col.GetComponent<Rigidbody>();
                if (debrisRb != null) debrisRb.isKinematic = true;

                // Make the plank a "child" of the hook
                col.transform.SetParent(this.transform);
                
                // Snap the plank exactly to the center of the hook to show they are linked
                col.transform.localPosition = Vector3.zero; 
            }
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (!isThrown)
            return;

        // RAFT EDGE DETECTION
        // Tells the HookThrower script that it's time to destroy the hook
        // Includes Barricades so it doesn't get stuck against them
        if (collision.gameObject.CompareTag("Raft") || collision.gameObject.CompareTag("Barricade"))
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
