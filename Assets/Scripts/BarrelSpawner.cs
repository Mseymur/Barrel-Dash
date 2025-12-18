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