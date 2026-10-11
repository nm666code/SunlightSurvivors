using UnityEngine;

public class SpawnEnemy : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public GameObject enemyPrefab, playerPrefab;
    private float rangeX = -13, rangeZ = -13;
    private int numEnemies;
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        // In case no enemy objects are present, randomly spawn some new ones.
        numEnemies = FindObjectsByType<EnemyScript>(FindObjectsSortMode.None).Length;
        if (numEnemies == 0)
        {
            int randEnemies = Random.Range(1, 5);
            for (int i = 1; i < randEnemies; i++)
            {
                Instantiate(enemyPrefab, SpawnRandomPosition(), enemyPrefab.transform.rotation);
            }
        }
    }

    // Place any enemy prefab in any random position, but make sure none of them fall off the stage accidentally.
    // Also, make sure that the enemy doesn't spawn too close to the player.
    private Vector3 SpawnRandomPosition()
    {
        float newX = 0, newZ = 0;
        float playerX = playerPrefab.transform.position.x, playerZ = playerPrefab.transform.position.z;
        while (Mathf.Abs(newX - playerX) < 2.5f && Mathf.Abs(newZ - playerZ) < 2.5f)
        {
            newX = Random.Range(-rangeX, rangeX);
            newZ = Random.Range(-rangeZ, rangeZ);
        }
        return new Vector3(newX, 0.5f, newZ);
    }
}
