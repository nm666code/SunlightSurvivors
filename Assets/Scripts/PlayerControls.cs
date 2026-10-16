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
    private bool isSafe = false;
    private bool isSlippery = false;
    private Vector3 currentVelocity = Vector3.zero;
    public float slipperyAcceleration = 2f;

    [Header("Dash")]
    [SerializeField, Min(0.1f)] private float dashSpeed = 24f;
    [SerializeField, Min(0.02f)] private float dashDuration = 0.2f;
    private const float DashCooldown = 3f;
    private Vector3 lastMoveDirection = Vector3.forward;
    private Vector3 dashDirection;
    private float dashEndTime;
    private float nextDashTime;

    public bool IsDashing => !isDead && Time.time < dashEndTime;
    public float DashCooldownRemaining => Mathf.Max(0f, nextDashTime - Time.time);

    public const int MaxHealth = 10;
    public int CurrentHealth { get; private set; } = MaxHealth;

    public void TakeDamage(int damage)
    {
        if (isDead || isInvulnerable || isSafe || damage <= 0)
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
        Rect panel = new Rect(16f, 16f, width, 126f);
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

        float cooldown = DashCooldownRemaining;
        GUI.color = isDead ? Color.gray : new Color(0.4f, 0.8f, 1f);
        string dashLabel = isDead ? "DASH [SPACE]  --"
            : IsDashing ? "DASH [SPACE]  ACTIVE"
            : cooldown > 0f ? $"DASH [SPACE]  {cooldown:0.0}s"
            : "DASH [SPACE]  READY";
        GUI.Label(new Rect(panel.x + 12f, panel.y + 72f, width - 24f, 28f),
            dashLabel, labelStyle);
        GUI.color = new Color(0.22f, 0.25f, 0.3f);
        Rect cooldownBar = new Rect(panel.x + 12f, panel.y + 106f, width - 24f, 6f);
        GUI.DrawTexture(cooldownBar, Texture2D.whiteTexture);
        cooldownBar.width *= isDead ? 0f : 1f - cooldown / DashCooldown;
        GUI.color = new Color(0.3f, 0.75f, 1f);
        GUI.DrawTexture(cooldownBar, Texture2D.whiteTexture);

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
        playerBox.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
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

        dir2D = Vector2.ClampMagnitude(controller.ReadValue<Vector2>(), 1f);
        if (!IsDashing && dir2D.sqrMagnitude > 0.01f)
        {
            lastMoveDirection = new Vector3(dir2D.x, 0f, dir2D.y).normalized;
        }

        if (!IsDashing && DashCooldownRemaining <= 0f && Keyboard.current != null &&
            Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            dashDirection = lastMoveDirection;
            dashEndTime = Time.time + dashDuration;
            nextDashTime = Time.time + DashCooldown;
        }
    }

    private void FixedUpdate()
    {
        if (isDead || Time.timeScale == 0f) return;

        Vector3 targetVelocity = new Vector3(
            dir2D.x,
            0f,
            dir2D.y
        ) * playerSpeed;

        if (IsDashing)
        {
            // Dash overrides ice acceleration; normal/ice movement resumes afterwards.
            currentVelocity = dashDirection * dashSpeed;
        }
        else if (isSlippery)
        {
            currentVelocity = Vector3.MoveTowards(
                currentVelocity,
                targetVelocity,
                slipperyAcceleration * Time.fixedDeltaTime
            );
        }
        else
        {
            currentVelocity = targetVelocity;
        }

        // Rigidbody movement participates in physics; keep gravity and bounce velocity.
        playerBox.linearVelocity = new Vector3(
            currentVelocity.x, playerBox.linearVelocity.y, currentVelocity.z);
    }

    // Enemy attacks use TakeDamage; sunlight still uses the instant-death path.
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
            isSafe = true;
            Debug.Log("Standing on the safe zone");
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
        return other.name == "sunlight";
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

        CurrentHealth = 0;
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
