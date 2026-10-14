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

    public const int MaxHealth = 10;
    public int CurrentHealth { get; private set; } = MaxHealth;
    public bool IsDead => isDead;

    public void TakeDamage(int damage)
    {
        if (isDead || damage <= 0)
        {
            return;
        }

        CurrentHealth = Mathf.Max(0, CurrentHealth - damage);
        if (CurrentHealth == 0)
        {
            Die();
        }
    }

    private void OnGUI()
    {
        Color previousColor = GUI.color;
        int previousDepth = GUI.depth;
        GUI.depth = 10; // Keep the Game Over overlay in front of the health bar.

        float width = Mathf.Min(300f, Screen.width - 32f);
        Rect panel = new Rect(16f, 16f, width, 78f);
        GUI.color = new Color(0.08f, 0.1f, 0.14f, 0.9f);
        GUI.DrawTexture(panel, Texture2D.whiteTexture);

        GUI.color = Color.white;
        GUIStyle labelStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 20,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleLeft
        };
        GUI.Label(new Rect(panel.x + 12f, panel.y + 6f, width - 24f, 28f),
            $"HP  {CurrentHealth} / {MaxHealth}", labelStyle);

        float segmentWidth = (width - 24f - (MaxHealth - 1) * 4f) / MaxHealth;
        for (int i = 0; i < MaxHealth; i++)
        {
            GUI.color = i < CurrentHealth
                ? (CurrentHealth <= 3 ? new Color(0.95f, 0.22f, 0.2f) : new Color(0.2f, 0.85f, 0.4f))
                : new Color(0.22f, 0.25f, 0.3f);
            GUI.DrawTexture(new Rect(panel.x + 12f + i * (segmentWidth + 4f),
                panel.y + 42f, segmentWidth, 22f), Texture2D.whiteTexture);
        }

        GUI.color = previousColor;
        GUI.depth = previousDepth;
    }

    private void OnDestroy()
    {
        controller.Disable();
    }

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
        transform.Translate(Vector3.forward * Time.deltaTime * playerSpeed * dir2D.y);
        transform.Translate(Vector3.right * Time.deltaTime * playerSpeed * dir2D.x);
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
        if (IsFatalObject(collision.gameObject))
        {
            Die();
        }
        else if (collision.gameObject.CompareTag("Powerup"))
        {
            Debug.Log("Picked up a powerup object");
        }
        else if (collision.gameObject.CompareTag("Safe"))
        {
            Debug.Log("Standing on the safe zone");
        }
    }

    private bool IsFatalObject(GameObject other)
    {
        return other.name == "sunlight";
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

        CurrentHealth = 0;
        isDead = true;
        Debug.Log("You died!");
        controller.Disable();
        GameOverUI.Show();
        Time.timeScale = 0f;
    }
}
