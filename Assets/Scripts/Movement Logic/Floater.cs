using UnityEngine;
using UnityEngine.Rendering;

public class Floater : MonoBehaviour
{
    [Header("Floating parameters")]
    public Rigidbody rigidBody;
    public float depthBeforeSubmerged = 1f; // How deep the object can go before being fully submerged
    public float displacementAmount = 3f; // Strength of upward buoyancy force
    public int floaterCount = 1; // Floating points affecting the object

    [Header("Water parameters")]
    public float waterDrag = 0.99f; // Resistance applied to movement in water 
    public float waterAngularDrag = 0.5f; // Resistance applied to rotation in water 

    private void FixedUpdate()
    {
        if (Inventory.isInventoryOpen) 
            return;

        // Apply gravity
        rigidBody.AddForceAtPosition(Physics.gravity / floaterCount, transform.position, ForceMode.Acceleration);
        // Current water surface height
        float waveHeight = LowPolyWater.LowPolyWater.instance.GetWaveHeight(transform.position);
        
        // If object is below the water surface
        if (transform.position.y < waveHeight)
        {
            // How submerged the object is
            float displacementMultiplier = Mathf.Clamp01((waveHeight - transform.position.y) / depthBeforeSubmerged) * displacementAmount;
            // Apply upward buoyancy 
            rigidBody.AddForceAtPosition(new Vector3(0f, Mathf.Abs(Physics.gravity.y) * displacementMultiplier, 0f), transform.position, ForceMode.Acceleration);
            // Apply drag to slow down movement
            rigidBody.AddForce(displacementMultiplier * Time.fixedDeltaTime * waterDrag * -rigidBody.linearVelocity, ForceMode.VelocityChange);
            // Apply angular drag to reduce rotation 
            rigidBody.AddTorque(displacementMultiplier * Time.fixedDeltaTime * waterAngularDrag * -rigidBody.angularVelocity, ForceMode.VelocityChange);
        }
    }
}