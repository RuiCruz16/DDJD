using UnityEngine;

public class CollectionNet : MonoBehaviour
{
    [Header("Settings")]
    public float catchHeightOffset = 0.2f; 
    public float scatterRadius = 1.5f;

    private void OnTriggerEnter(Collider other)
    {
        Item itemScript = other.GetComponentInParent<Item>();

        if (itemScript != null)
        {
            // Ignore Raft parts, Barricades, and items that are already on the raft
            if (itemScript.gameObject.CompareTag("Raft") || 
                itemScript.gameObject.CompareTag("Barricade") || 
                itemScript.transform.parent == transform.root)
            {
                return;
            }

            if (itemScript.transform.parent != null && itemScript.transform.parent.GetComponent<HookBehavior>() != null)
            {
                return;
            }

            GameObject caughtItem = itemScript.gameObject;

            // Disable water floating physics
            Floater floater = caughtItem.GetComponent<Floater>();
            if (floater != null) floater.enabled = false;

            // Disable the movement script so it stops despawning
            DebrisMovement moveScript = caughtItem.GetComponent<DebrisMovement>();
            if (moveScript != null) moveScript.enabled = false;

            // Freeze rigidbody physics
            Rigidbody rb = caughtItem.GetComponent<Rigidbody>();
            if (rb != null)
            {
                if (!rb.isKinematic) 
                {
                    rb.linearVelocity = Vector3.zero; 
                    rb.angularVelocity = Vector3.zero;
                }
                rb.isKinematic = true;
            }

            // Calculate a random position near the center
            // Get the exact geometric center of the collider, ignoring pivot offsets
            Vector3 realCenter = transform.position;
            BoxCollider netCollider = GetComponent<BoxCollider>();
            if (netCollider != null)
            {
                realCenter = netCollider.bounds.center;
            }

            float offsetX = Random.Range(-scatterRadius, scatterRadius);
            float offsetZ = Random.Range(-scatterRadius, scatterRadius);
            Vector3 randomPosition = realCenter + new Vector3(offsetX, catchHeightOffset, offsetZ);

            caughtItem.transform.position = randomPosition;
            caughtItem.transform.rotation = Quaternion.Euler(0, Random.Range(0f, 360f), 0);

            // Parent to the root (RaftManager) to prevent stretching
            caughtItem.transform.SetParent(transform.root);

            itemScript.canBePickedUp = true;
        }
    }
}
