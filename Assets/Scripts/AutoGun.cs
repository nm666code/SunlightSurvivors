using UnityEngine;

// Created on the player at runtime; no scene or prefab edits are required.
public class AutoGun : MonoBehaviour
{
    [SerializeField, Min(0.05f)] private float shotInterval = 0.6f;
    [SerializeField, Min(1f)] private float range = 15f;
    [SerializeField, Min(1f)] private float bulletSpeed = 25f;
    [SerializeField, Min(1)] private int damage = 1;

    private PlayerControls player;
    private Transform gun;
    private Material gunMaterial;
    private Material bulletMaterial;
    private float nextShot;

    private void Awake()
    {
        player = GetComponent<PlayerControls>();
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null) shader = Shader.Find("Unlit/Color");
        gunMaterial = new Material(shader) { color = Color.gray };
        bulletMaterial = new Material(shader) { color = Color.yellow };
        GameObject model = GameObject.CreatePrimitive(PrimitiveType.Cube);
        model.name = "Auto Gun";
        Collider modelCollider = model.GetComponent<Collider>();
        modelCollider.enabled = false;
        Destroy(modelCollider);
        gun = model.transform;
        gun.SetParent(transform, false);
        gun.localScale = new Vector3(0.18f, 0.18f, 0.8f);
        model.GetComponent<Renderer>().sharedMaterial = gunMaterial;
    }

    private void Update()
    {
        if (player == null || player.IsDead || Time.timeScale == 0f) return;

        Vector3 origin = transform.position;
        EnemyScript target = null;
        Vector3 aim = Vector3.zero;
        float nearest = range * range;
        foreach (EnemyScript enemy in FindObjectsByType<EnemyScript>(FindObjectsSortMode.None))
        {
            if (enemy.IsDead) continue;
            Collider collider = enemy.GetComponentInChildren<Collider>();
            if (collider == null || !collider.enabled) continue;
            Vector3 point = collider.bounds.center;
            float distance = (point - origin).sqrMagnitude;
            if (distance > nearest || !CanSee(origin, point, enemy)) continue;
            nearest = distance;
            target = enemy;
            aim = point;
        }

        if (target == null) return;
        Vector3 direction = (aim - origin).normalized;
        if (direction.sqrMagnitude < 0.01f) return;
        gun.rotation = Quaternion.LookRotation(direction);
        gun.position = origin + direction * 0.5f;
        if (Time.time < nextShot) return;
        nextShot = Time.time + shotInterval;

        // Start at the player center so close targets cannot be skipped by the muzzle.
        GameObject bullet = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        bullet.name = "Gun Bullet";
        bullet.transform.position = origin;
        bullet.transform.localScale = Vector3.one * 0.16f;
        Collider bulletCollider = bullet.GetComponent<Collider>();
        bulletCollider.enabled = false;
        Destroy(bulletCollider);
        bullet.GetComponent<Renderer>().sharedMaterial = bulletMaterial;
        bullet.AddComponent<GunBullet>().Initialize(transform, direction, bulletSpeed, range, damage);
    }

    private bool CanSee(Vector3 origin, Vector3 point, EnemyScript target)
    {
        Vector3 delta = point - origin;
        foreach (RaycastHit hit in Physics.RaycastAll(origin, delta.normalized, delta.magnitude,
                     Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
        {
            if (hit.transform.IsChildOf(transform)) continue;
            if (hit.collider.GetComponentInParent<EnemyScript>() != target) return false;
        }
        return true;
    }

    private void OnDestroy()
    {
        Destroy(gunMaterial);
        Destroy(bulletMaterial);
    }
}
