using UnityEngine;

public class EnemyScript : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
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
    private Rigidbody enemyBox;
    private GameObject playerObj;
    public float jumpHeight = 1.0f; // Let's make the enemy jump when they touch bouncy tiles, shall we?
    void Start()
    {
        enemyBox = GetComponent<Rigidbody>();
        playerObj = GameObject.Find("Player");
    }

    // Update is called once per frame
    private void FixedUpdate()
    {
        if (isDead || playerObj == null || enemyBox == null) return;
        Vector3 direction = playerObj.transform.position - transform.position;
        direction.y = 0f;

        Vector3 horizontalVelocity = direction.normalized * moveSpeed;

        enemyBox.linearVelocity = new Vector3(
            horizontalVelocity.x,
            enemyBox.linearVelocity.y,
            horizontalVelocity.z
        );
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Bouncy"))
        {
            enemyBox.AddForce(Vector3.up * jumpHeight, ForceMode.Impulse);
        }
    }
}
