using UnityEngine;

public class SlimeSpawnTrigger : MonoBehaviour
{
    public SlimeController slimePrefab;
    public int spawnCount = 5;
    public float triggerDistance = 8f;
    public float spawnRadius = 2.5f;

    PlayerController playerController;
    bool hasSpawned;

    void Start()
    {
        playerController = FindFirstObjectByType<PlayerController>();
    }

    void Update()
    {
        if (hasSpawned)
        {
            return;
        }

        if (playerController == null)
        {
            playerController = FindFirstObjectByType<PlayerController>();
            if (playerController == null)
            {
                return;
            }
        }

        float distanceToPlayer = Vector2.Distance(transform.position, playerController.transform.position);
        if (distanceToPlayer > triggerDistance)
        {
            return;
        }

        SpawnSlimes();
    }

    void SpawnSlimes()
    {
        if (slimePrefab == null)
        {
            return;
        }

        hasSpawned = true;
        float angleStep = 360f / Mathf.Max(1, spawnCount);

        for (int i = 0; i < spawnCount; i++)
        {
            float angle = angleStep * i * Mathf.Deg2Rad;
            Vector2 spawnOffset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * spawnRadius;
            Vector3 spawnPosition = transform.position + (Vector3)spawnOffset;
            Instantiate(slimePrefab, spawnPosition, Quaternion.identity);
        }
    }
}
