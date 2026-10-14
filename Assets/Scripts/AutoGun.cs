using UnityEngine;

// Attach to a gun prefab placed under the player.
public class AutoGun : MonoBehaviour
{
    [SerializeField, Min(0.05f)] private float shotInterval = 0.6f;
    [SerializeField, Min(1f)] private float range = 15f;
    [SerializeField, Min(1f)] private float bulletSpeed = 25f;
    [SerializeField, Min(1)] private int damage = 1;
    [SerializeField] private Transform muzzle;
    [SerializeField] private GunBullet bulletPrefab;

    private PlayerControls player;
    private float nextShot;

    private void Awake()
    {
        player = GetComponentInParent<PlayerControls>();
    }

    private void Start()
    {
        if (player == null || muzzle == null || bulletPrefab == null)
        {
            Debug.LogError("AutoGun needs a Player parent, Muzzle, and Bullet Prefab.", this);
            enabled = false;
        }
    }

    private void Update()
    {
        if (player == null || player.IsDead || Time.timeScale == 0f) return;

        Vector3 origin = player.transform.position;
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
        transform.rotation = Quaternion.LookRotation(direction);
        // Do not shoot past a close target or through an obstacle between player and muzzle.
        if (Vector3.Dot(aim - muzzle.position, direction) <= 0f ||
            !CanSee(origin, muzzle.position, target) ||
            !CanSee(muzzle.position, aim, target)) return;
        if (Time.time < nextShot) return;
        nextShot = Time.time + shotInterval;

        Vector3 shotDirection = (aim - muzzle.position).normalized;
        GunBullet bullet = Instantiate(bulletPrefab, muzzle.position,
            Quaternion.LookRotation(shotDirection));
        bullet.Initialize(player.transform, shotDirection, bulletSpeed, range, damage);
    }

    private bool CanSee(Vector3 origin, Vector3 point, EnemyScript target)
    {
        Vector3 delta = point - origin;
        foreach (RaycastHit hit in Physics.RaycastAll(origin, delta.normalized, delta.magnitude,
                     Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
        {
            if (hit.transform.IsChildOf(player.transform)) continue;
            if (hit.collider.GetComponentInParent<EnemyScript>() != target) return false;
        }
        return true;
    }

}
