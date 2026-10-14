using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [SerializeField] private EnemyScript enemyPrefab;
    [SerializeField] private Collider arena;
    [SerializeField, Min(0f)] private float initialDelay = 2f;
    [SerializeField, Min(0.1f)] private float spawnInterval = 2f;
    [SerializeField, Min(1)] private int maxEnemies = 15;
    [SerializeField, Min(0f)] private float minPlayerDistance = 5f;
    [SerializeField, Min(0f)] private float edgeMargin = 1f;

    private PlayerControls player;
    private SphereCollider enemyCollider;
    private float nextSpawn;
    private float radius;

    private void Start()
    {
        player = FindFirstObjectByType<PlayerControls>();
        enemyCollider = enemyPrefab != null ? enemyPrefab.GetComponent<SphereCollider>() : null;
        if (player == null || arena == null || enemyCollider == null)
        {
            Debug.LogError("EnemySpawner needs a Player, Arena collider, and Enemy prefab with a SphereCollider.", this);
            enabled = false;
            return;
        }
        Vector3 scale = enemyPrefab.transform.localScale;
        radius = enemyCollider.radius * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z));
        nextSpawn = Time.time + initialDelay;
    }

    private void Update()
    {
        if (player == null || player.IsDead || Time.timeScale == 0f || Time.time < nextSpawn) return;
        nextSpawn = Time.time + spawnInterval;
        if (FindObjectsByType<EnemyScript>(FindObjectsSortMode.None).Length >= maxEnemies) return;
        TrySpawn();
    }

    private void TrySpawn()
    {
        Bounds bounds = arena.bounds;
        float margin = Mathf.Max(edgeMargin, radius);
        if (bounds.size.x <= margin * 2f || bounds.size.z <= margin * 2f) return;
        Vector3 centerOffset = Vector3.Scale(enemyCollider.center, enemyPrefab.transform.localScale);

        // Retry a bounded number of times; a crowded map simply skips this spawn.
        for (int attempt = 0; attempt < 30; attempt++)
        {
            float x = Random.Range(bounds.min.x + margin, bounds.max.x - margin);
            float z = Random.Range(bounds.min.z + margin, bounds.max.z - margin);
            Ray ray = new Ray(new Vector3(x, bounds.max.y + 5f, z), Vector3.down);
            if (!arena.Raycast(ray, out RaycastHit ground, bounds.size.y + 10f)) continue;

            Vector3 center = ground.point + Vector3.up * (radius + 0.05f);
            Vector3 toPlayer = center - player.transform.position;
            toPlayer.y = 0f;
            if (toPlayer.sqrMagnitude < minPlayerDistance * minPlayerDistance) continue;

            bool blocked = false;
            foreach (Collider other in Physics.OverlapSphere(center, radius + 0.02f,
                         Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                if (other == arena) continue;
                blocked = true;
                break;
            }
            if (blocked) continue;

            Instantiate(enemyPrefab, center - centerOffset, Quaternion.identity);
            return;
        }
    }
}
