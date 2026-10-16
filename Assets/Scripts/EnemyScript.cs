using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class EnemyScript : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField, Min(1)] private int health = 1;
    private bool isDead;

    public bool IsDead => isDead;

    public void TakeDamage(int damage)
    {
        if (isDead || damage <= 0) return;
        health -= damage;
        if (health <= 0)
        {
            isDead = true;
            gameObject.SetActive(false);
            Destroy(gameObject);
        }
    }
    [Header("Automatic Attack")]
    [SerializeField, Min(0.1f)] private float attackRange = 1.5f;
    [SerializeField, Min(0.1f)] private float attackInterval = 1f;

    private Rigidbody enemyBox;
    private PlayerControls player;
    private float nextAttackTime;
    public float jumpHeight = 1.0f;

    private void Start()
    {
        enemyBox = GetComponent<Rigidbody>();
        player = FindFirstObjectByType<PlayerControls>();
    }

    private void FixedUpdate()
    {
        if (isDead || enemyBox == null) return;
        // TimeFreeze only pins bodies that existed when it started, so enemies spawned
        // mid-freeze would still chase, and frozen ones would still attack, without this.
        if (TimeFreeze.IsFrozen) return;
        if (player == null || player.IsDead)
        {
            enemyBox.linearVelocity = new Vector3(0f, enemyBox.linearVelocity.y, 0f);
            return;
        }

        Vector3 offset = player.transform.position - transform.position;
        Vector3 direction = new Vector3(offset.x, 0f, offset.z);
        Vector3 horizontalVelocity = direction.normalized * moveSpeed;
        enemyBox.linearVelocity = new Vector3(
            horizontalVelocity.x, enemyBox.linearVelocity.y, horizontalVelocity.z);

        // Use full 3D distance so an enemy cannot hit a player far above it.
        if (offset.sqrMagnitude <= attackRange * attackRange && Time.time >= nextAttackTime)
        {
            nextAttackTime = Time.time + Mathf.Max(0.1f, attackInterval);
            player.TakeDamage(1);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Bouncy"))
        {
            enemyBox.AddForce(Vector3.up * jumpHeight, ForceMode.Impulse);
        }
    }
}
