using UnityEngine;

public class EnemyScript : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    [SerializeField] private float moveSpeed = 3f;
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
