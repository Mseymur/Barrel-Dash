using System.Collections;
using UnityEngine;

public class BarrelSpawner : MonoBehaviour
{
    public GameObject barrelPrefab;
    public float spawnInterval = 3f;

    public Transform leftSpawnPoint;
    public Transform rightSpawnPoint;

    private bool spawnOnLeft = true;
    private bool stopSpawning = false;

    void Start()
    {
        StartCoroutine(SpawnBarrels());
    }

    private IEnumerator SpawnBarrels()
    {
        while (!stopSpawning)
        {
            // Wait until the game is explicitly active (User detected)
            // If GameManager is missing, we WAIT (Pause) and warn, rather than running wildly.
            if (GameManager.Instance == null)
            {
                Debug.LogWarning("[BarrelSpawner] GameManager not found! Pausing spawning until GameManager is present.");
            }

            while (GameManager.Instance == null || !GameManager.Instance.isGameActive)
            {
                yield return null;
            }

            Transform spawnPoint = spawnOnLeft ? leftSpawnPoint : rightSpawnPoint;
            Instantiate(barrelPrefab, spawnPoint.position, Quaternion.identity);
            spawnOnLeft = !spawnOnLeft;
            yield return new WaitForSeconds(spawnInterval);
        }
    }

    public void StopSpawning()
    {
        stopSpawning = true;
    }
}