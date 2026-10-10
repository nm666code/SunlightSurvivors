using UnityEngine;

public class EnemyScript : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public float moveSpeed;
    private Rigidbody enemyBox;
    private GameObject playerObj;
    public float jumpHeight = 1.0f; // Let's make the enemy jump when they touch bouncy tiles, shall we?
    void Start()
    {
        enemyBox = GetComponent<Rigidbody>();
        playerObj = GameObject.Find("Player");
    }

    // Update is called once per frame
    void Update()
    {
        // Make the enemy object follow the player.
        Vector3 vectorDir = (playerObj.transform.position - transform.position).normalized;
        enemyBox.AddForce(vectorDir * moveSpeed);

        if (transform.position.y < -15)
        {
            Destroy(gameObject);
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
