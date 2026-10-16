using UnityEngine;

public class ProjectileScript : MonoBehaviour
{
    // The ProjectileScript C# file is similar to EnemyScript, but this time, it follows the player
    // based on its current position (similar to a homing spell or the like).
    public float moveSpeed;
    private Rigidbody projectileBox;
    private GameObject playerObj;
    private float origX, origZ;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        projectileBox = GetComponent<Rigidbody>();
        playerObj = GameObject.Find("Player");
        origX = transform.position.x;
        origZ = transform.position.z;
    }

    // Update is called once per frame
    void Update()
    {
        // Make the projectile follow the player by using MoveTowards.
        transform.position = Vector3.MoveTowards(transform.position, playerObj.transform.position, moveSpeed * Time.deltaTime);
        
        // In case the projectile falls off the stage, respawn that projectile.
        if (transform.position.y <= -10)
        {
            transform.position = new Vector3(origX, 0.5f, origZ);
        }
    }

    // If the player collides with an object, reset its position back to its original position.
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.name == "Player")
        {
            Debug.Log("Collided with a projectile");
            transform.position = new Vector3(origX, 0.5f, origZ);
        }
    }
}
