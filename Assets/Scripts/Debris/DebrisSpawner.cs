using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Structure to hold independent spawn settings for each debris type
[System.Serializable]
public class DebrisSpawnInfo
{
    public string name;           
    public GameObject prefab;     
    
    [Header("Independent Timers")]
    public float minSpawnTime = 10f; // Minimum time before this specific item spawns again
    public float maxSpawnTime = 20f; // Maximum time
}

public class DebrisSpawner : MonoBehaviour
{
    [Header("Spawning Settings")]
    public List<DebrisSpawnInfo> spawnableDebris; 

    [Header("Spawn Area")]
    public float spawnRadius = 40f; 
    public float targetOffset = 4f; // Random offset so it doesn't always hit the center

    void Start()
    {
        // Start a separate, independent timer for EVERY item in the list
        foreach (var item in spawnableDebris)
        {
            if (item.prefab != null)
            {
                StartCoroutine(SpawnRoutine(item));
            }
        }
    }

    // This routine runs in parallel for each debris type
    IEnumerator SpawnRoutine(DebrisSpawnInfo itemInfo)
    {
        while (true) 
        {
            // Wait for this specific item's timer
            yield return new WaitForSeconds(Random.Range(itemInfo.minSpawnTime, itemInfo.maxSpawnTime));

            // Calculate random spawn position in a circle
            float angle = Random.Range(0f, Mathf.PI * 2f);
            Vector3 spawnOffset = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * spawnRadius;
            Vector3 spawnPosition = transform.position + spawnOffset;

            // Calculate target position near the raft to make it float towards the player
            Vector3 randomTargetDir = new Vector3(Random.Range(-targetOffset, targetOffset), 0, Random.Range(-targetOffset, targetOffset));
            Vector3 targetPosition = transform.position + randomTargetDir;
            Vector3 directionToRaft = (targetPosition - spawnPosition).normalized;

            // Spawn the specific debris
            GameObject newDebris = Instantiate(itemInfo.prefab, spawnPosition, Quaternion.identity);
            
            // Make debris not physically interact with raft - set colliders as triggers
            Collider[] debris_colliders = newDebris.GetComponentsInChildren<Collider>();
            foreach (Collider col in debris_colliders)
            {
                col.isTrigger = true;
            }
            
            if (newDebris.TryGetComponent<DebrisMovement>(out var moveScript))
            {
                moveScript.SetMoveDirection(directionToRaft);
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, spawnRadius);
    }
}
