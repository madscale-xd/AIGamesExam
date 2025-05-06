using UnityEngine;

public class SimplePrefabSpawner : MonoBehaviour
{
    public GameObject prefabToSpawn;
    public GameObject boundsObject;  // Must have a BoxCollider
    public float yOffset = 5f;

    void Start()
    {
        BoxCollider box = boundsObject.GetComponent<BoxCollider>();
        if (box == null)
        {
            Debug.LogError("Bounds object must have a BoxCollider.");
            return;
        }

        Bounds bounds = box.bounds;

        // Generate random X and Z within bounds
        float randomX = Random.Range(bounds.min.x, bounds.max.x);
        float randomZ = Random.Range(bounds.min.z, bounds.max.z);
        float y = boundsObject.transform.position.y + yOffset;

        Vector3 spawnPos = new Vector3(randomX, y, randomZ);
        Instantiate(prefabToSpawn, spawnPos, Quaternion.identity);
    }
}
