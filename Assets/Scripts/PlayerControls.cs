using UnityEngine;
using UnityEngine.InputSystem; // Needed to allow the player to move

public class PlayerControls : MonoBehaviour
{
    public float playerSpeed;
    public InputAction controller;
    public Vector2 dir2D;
    private Rigidbody playerBox; // Will be used for collision detection for enemy entities
    public float jumpHeight = 10.0f;
    private bool isDead;
    private bool isSafe = false;
    private bool isSlippery = false;
    private Vector3 currentVelocity = Vector3.zero;
    public float slipperyAcceleration = 2f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        controller.Enable();
        playerBox = GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    void Update()
    {
        dir2D = controller.ReadValue<Vector2>();

        Vector3 targetVelocity = new Vector3(
            dir2D.x,
            0f,
            dir2D.y
        ) * playerSpeed;

        if (isSlippery)
        {
            currentVelocity = Vector3.MoveTowards(
                currentVelocity,
                targetVelocity,
                slipperyAcceleration * Time.deltaTime
            );
        }
        else
        {
            currentVelocity = targetVelocity;
        }

        transform.Translate(currentVelocity * Time.deltaTime, Space.World);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (IsFatalObject(collision.gameObject))
        {
            Die();
        }
        else if (collision.gameObject.CompareTag("Obstacle"))
        {
            Debug.Log("Collided with an obstacle object");
        }
        else if (collision.gameObject.CompareTag("Dummy"))
        {
            Debug.Log("Collided with a dummy object");
        }
        else if (collision.gameObject.CompareTag("Bouncy"))
        {
            Debug.Log("Bounced off a bouncy object!");
            playerBox.AddForce(Vector3.up * jumpHeight, ForceMode.Impulse);
        }
    }

    private void OnTriggerEnter(Collider collision)
    {
        if (collision.gameObject.CompareTag("Powerup"))
        {
            Debug.Log("Picked up a powerup object");
        }
        else if (collision.gameObject.CompareTag("Safe"))
        {
            isSafe = true;
            Debug.Log("Standing on the safe zone");
        }
        else if (IsFatalObject(collision.gameObject))
        {
            Die();
        }
    }

    private void OnTriggerExit(Collider collision)
    {
        if (collision.gameObject.CompareTag("Safe"))
        {
            isSafe = false;
            Debug.Log("Left the safe zone");
        }
    }

    private bool IsFatalObject(GameObject other)
    {
        return other.CompareTag("Enemy") || other.name == "sunlight";
    }

    public void SetSlippery(bool slippery)
    {
        isSlippery = slippery;
    }

    public void Kill()
    {
        Die();
    }

    private void Die()
    {
        if (isDead || isSafe)
        {
            return;
        }

        isDead = true;
        Debug.Log("You died!");
        controller.Disable();
        GameOverUI.Show();
        Time.timeScale = 0f;
    }
}
