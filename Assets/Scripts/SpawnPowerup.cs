using Unity.AppUI.UI;
using UnityEngine;

public class SpawnPowerup : MonoBehaviour
{
    // Similar to spawning enemies, spawn a powerup only if no powerup exists.
    // This time, only one powerup can spawn at a time, and takes longer to spawn
    // once picked up.
    public GameObject powerupPrefab;
    [SerializeField] private Collider arena;
    [SerializeField, Min(0f)] private float initialDelay = 4f;
    [SerializeField, Min(0.1f)] private float spawnInterval = 10f;
    private float nextSpawn;
    private float rangeX = 6, rangeZ = 6;
    private PlayerControls player;
    public string prefabName;
    private string prefabTag;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        prefabTag = powerupPrefab.tag;
        nextSpawn = Time.time + initialDelay;
        player = FindFirstObjectByType<PlayerControls>();
        if (player == null || arena == null)
        {
            Debug.LogError("SpawnPowerup requires player and arena instances not to be null.", this);
            enabled = false;
            return;
        }
        nextSpawn = Time.time + spawnInterval;
    }

    // Update is called once per frame
    private void Update()
    {
        // Don't spawn another powerup prefab.
        if (player == null || player.IsDead || Time.timeScale == 0f || Time.time < nextSpawn)
        {
            return;
        }
        nextSpawn = Time.time + spawnInterval;
        if (GameObject.FindGameObjectsWithTag(prefabTag).Length > 0)
        {
            return;
        }

        Instantiate(powerupPrefab, SpawnRandomPosition(), powerupPrefab.transform.rotation);
    }
    private Vector3 SpawnRandomPosition()
    {
        float newX = Random.Range(-rangeX, rangeX), newZ = Random.Range(-rangeZ, rangeZ);
        return new Vector3(newX, 0.5f, newZ);
    }
}