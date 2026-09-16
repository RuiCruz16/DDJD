using System.Collections;
using UnityEngine;

public class FishSpawner : MonoBehaviour
{
    [Header("Spawning Settings")]
    public GameObject fishPrefab;
    // [10sec,60sec] to spawn
    public float minSpawnTime = 10f;
    public float maxSpawnTime = 20f;

    [Header("Spawn Area")]
    public float spawnRadius = 40f; 
    public float targetOffset = 4f; // Random offset so it doesn't always hit the center

    void Start()
    {
        StartCoroutine(SpawnRoutine());
    }

    IEnumerator SpawnRoutine()
    {
        while (true) 
        {
            yield return new WaitForSeconds(Random.Range(minSpawnTime, maxSpawnTime));

            // Calculate random position in a circle around the raft
            float angle = Random.Range(0f, Mathf.PI * 2f);
            Vector3 spawnOffset = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * spawnRadius;
            Vector3 spawnPosition = transform.position + spawnOffset;

            // Calculate a target point near the raft
            Vector3 randomTargetDir = new(Random.Range(-targetOffset, targetOffset), 0, Random.Range(-targetOffset, targetOffset));
            Vector3 targetPosition = transform.position + randomTargetDir;

            // Get direction from spawn to target
            Vector3 directionToRaft = (targetPosition - spawnPosition).normalized;

            // Spawn and send direction to the object
            GameObject newFish = Instantiate(fishPrefab, spawnPosition, Quaternion.identity);
            
            if (newFish.TryGetComponent<FishMovement>(out var moveScript))
                moveScript.SetMoveDirection(directionToRaft);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, spawnRadius);
    }
}
