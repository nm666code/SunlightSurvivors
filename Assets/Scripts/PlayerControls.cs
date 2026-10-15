using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem; // Needed to allow the player to move

public class PlayerControls : MonoBehaviour
{
    public float playerSpeed;
    public InputAction controller;
    private Vector2 dir2D;
    private Rigidbody playerBox; // Will be used for collision detection for enemy entities
    public float jumpHeight = 10.0f;
    [SerializeField, Min(0.1f)] private float fallDeathDistance = 5f;
    private float groundHeight;
    private bool isDead;
    public bool IsDead => isDead;
    public bool isInvulnerable; // If true, the player can't be harmed by an enemy or hazard.
    private Renderer playerRenderer;
    public Material normalColor, invulnerableAura;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        controller.Enable();
        playerBox = GetComponent<Rigidbody>();
        GameObject level = GameObject.Find("Level");
        Collider ground = level != null ? level.GetComponent<Collider>() : null;
        groundHeight = ground != null ? ground.bounds.min.y : transform.position.y;
        playerRenderer = GetComponent<Renderer>();
        playerRenderer.material = normalColor;
        isInvulnerable = false;
    }

    // Update is called once per frame
    void Update()
    {
        if (isDead || Time.timeScale == 0f) return;
        if (transform.position.y < groundHeight - fallDeathDistance)
        {
            Die();
            return;
        }

        dir2D = controller.ReadValue<Vector2>();
        transform.Translate(Vector3.forward * Time.deltaTime * playerSpeed * dir2D.y);
        transform.Translate(Vector3.right * Time.deltaTime * playerSpeed * dir2D.x);
    }

    // The player dies if it collides with an enemy, and the invulnerability powerup isn't active.
    private void OnCollisionEnter(Collision collision)
    {
        if (IsFatalObject(collision.gameObject) && !isInvulnerable)
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
        if (IsFatalObject(collision.gameObject))
        {
            Die();
        }
        // Don't pickup another invulnerability powerup if the player is currently invulnerable.
        else if (collision.gameObject.CompareTag("Powerup") && !isInvulnerable)
        {
            Debug.Log("Picked up a powerup object and became invincible!");
            isInvulnerable = true;
            playerRenderer.material = invulnerableAura;
            Destroy(collision.gameObject);
            StartCoroutine(InvulnerableCoroutine());
        }
        else if (collision.gameObject.CompareTag("Safe"))
        {
            Debug.Log("Standing on the safe zone");
        }
    }

    private bool IsFatalObject(GameObject other)
    {
        return other.CompareTag("Enemy") || other.name == "sunlight";
    }

    public void Kill()
    {
        Die();
    }

    private void Die()
    {
        if (isDead)
        {
            return;
        }

        isDead = true;
        Debug.Log("You died!");
        controller.Disable();
        GameOverUI.Show();
        Time.timeScale = 0f;
    }

    // After that number of seconds have passed in that coroutine, disable the invulnerability powerup.
    IEnumerator InvulnerableCoroutine()
    {
        yield return new WaitForSeconds(7.5f);
        isInvulnerable = false;
        playerRenderer.material = normalColor;
    }
}
