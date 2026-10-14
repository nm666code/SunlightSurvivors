using UnityEngine;

public class GunBullet : MonoBehaviour
{
    private Transform owner;
    private Vector3 direction;
    private float speed;
    private float remainingRange;
    private int damage;

    public void Initialize(Transform shooter, Vector3 heading, float velocity, float range, int hitDamage)
    {
        owner = shooter;
        direction = heading;
        speed = velocity;
        remainingRange = range;
        damage = hitDamage;
    }

    private void Update()
    {
        if (Time.timeScale == 0f) return;
        if (owner == null) { Destroy(gameObject); return; }
        float step = Mathf.Min(speed * Time.deltaTime, remainingRange);
        // Sweep the full travel distance to avoid passing through enemies between frames.
        RaycastHit[] hits = Physics.SphereCastAll(transform.position, 0.08f, direction, step,
            Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        foreach (RaycastHit hit in hits)
        {
            if (hit.transform.IsChildOf(owner)) continue;
            EnemyScript enemy = hit.collider.GetComponentInParent<EnemyScript>();
            if (enemy != null && enemy.IsDead) continue;
            if (enemy != null) enemy.TakeDamage(damage);
            Destroy(gameObject);
            return;
        }
        transform.position += direction * step;
        remainingRange -= step;
        if (remainingRange <= 0f) Destroy(gameObject);
    }
}
