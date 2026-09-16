using UnityEngine;

public class SharkSpawner : MonoBehaviour
{
    public GameObject sharkPrefab;
    public Transform raft;
    public float spawnDelay = 10f;
    public float spawnDistance = 100f;
    public float minSpawnDistance = 20f;

    void Start()
    {
        Invoke(nameof(SpawnShark), spawnDelay);
    }

    void SpawnShark()
    {
        if (raft == null)
        {
            Debug.LogError("Raft transform not set in SharkSpawner!");
            return;
        }

        float actualSpawnDistance = Mathf.Max(spawnDistance, minSpawnDistance);
        Vector2 randomCircle = Random.insideUnitCircle.normalized * actualSpawnDistance;
        Vector3 spawnPosition = raft.position + new Vector3(randomCircle.x, -5f, randomCircle.y);

        Instantiate(sharkPrefab, spawnPosition, Quaternion.identity);

        Debug.Log($"[SharkSpawner] Spawned shark at {spawnPosition}");
    }
}
