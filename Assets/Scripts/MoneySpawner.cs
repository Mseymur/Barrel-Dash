using UnityEngine;
using System.Collections.Generic;

public class MoneySpawner : MonoBehaviour
{
    public GameObject moneyPrefab; // The money prefab to spawn
    public int numberOfMoney = 10; // Number of money collectibles to spawn
    public BoxCollider spawnArea;  // The spawn area defined by a BoxCollider
    public float minDistance = 1.0f; // Minimum distance between spawned money items

    private List<Vector3> spawnPositions = new List<Vector3>(); // Track positions of spawned money

    void Start()
    {
        SpawnMoney();
    }

void SpawnMoney()
{
    for (int i = 0; i < numberOfMoney; i++)
    {
        Vector3 spawnPosition;
        bool positionValid;
        int attempts = 0;
        int maxAttempts = 100;

        do
        {
            spawnPosition = GetRandomPosition();
            positionValid = IsPositionValid(spawnPosition);
            attempts++;
        }
        while (!positionValid && attempts < maxAttempts);

        if (positionValid)
        {
            // Adjust the Y position to the ground using a raycast
            spawnPosition = AlignToGround(spawnPosition);

            // Instantiate the money prefab
            GameObject money = Instantiate(moneyPrefab, spawnPosition, Quaternion.identity);

            // Add constraints to prevent unnecessary movement
            Rigidbody rb = money.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.constraints = RigidbodyConstraints.FreezeRotation;
            }

            // Save the position to prevent overlap
            spawnPositions.Add(spawnPosition);
        }
    }
}

Vector3 AlignToGround(Vector3 spawnPosition)
{
    RaycastHit hit;

    // Cast a ray downward from above the spawn position
    if (Physics.Raycast(new Vector3(spawnPosition.x, spawnArea.bounds.max.y, spawnPosition.z), Vector3.down, out hit, Mathf.Infinity))
    {
        // If the ray hits the ground, adjust the Y position to the hit point
        spawnPosition.y = hit.point.y;
    }
    else
    {
        Debug.LogWarning("Spawn position did not hit the ground. Using default Y position.");
    }

    return spawnPosition;
}

    Vector3 GetRandomPosition()
    {
        // Get bounds of the spawn area
        Bounds bounds = spawnArea.bounds;

        // Generate random positions within the bounds
        float randomX = Random.Range(bounds.min.x, bounds.max.x);
        float randomY = Random.Range(bounds.min.y, bounds.max.y);
        float randomZ = Random.Range(bounds.min.z, bounds.max.z);

        return new Vector3(randomX, randomY, randomZ);
    }

    bool IsPositionValid(Vector3 position)
    {
        // Check if the position is far enough from existing positions
        foreach (Vector3 existingPosition in spawnPositions)
        {
            if (Vector3.Distance(existingPosition, position) < minDistance)
            {
                return false;
            }
        }
        return true;
    }
}