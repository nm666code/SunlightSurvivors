using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// The player's active abilities, ported from the 2D JAM prototype (JAMSkill) to the 3D arena:
///
///   1 - Teleport:    blink toward the cursor on the ground, clamped to range, the Level
///                    bounds and the first wall in the way.
///   2 - Time Freeze: everything except the player holds still for a few seconds (TimeFreeze).
///
/// Lives on the Player next to PlayerControls. If the Player prefab does not have it yet,
/// it is added automatically on every scene load, so tuning values are only kept when the
/// component is added to the prefab by hand.
/// </summary>
[RequireComponent(typeof(PlayerControls))]
[RequireComponent(typeof(Rigidbody))]
public class PlayerAbilities : MonoBehaviour
{
    [Header("1 - Teleport")]
    [Tooltip("Wind-up before the jump. The player is rooted during it.")]
    public float teleportChargeTime = 0.25f;

    [Tooltip("Maximum distance. A cursor further out is clamped to this range.")]
    public float teleportMaxRange = 6f;

    public float teleportCooldown = 3f;

    [Tooltip("Clearance needed along the path and at the destination. Roughly the player's collider radius.")]
    public float teleportClearRadius = 0.5f;

    [Tooltip("Sweep the path and stop at the first solid obstacle. Turn off for a phase-through blink.")]
    public bool stopAtWalls = true;

    [Tooltip("Optional marker spawned at the destination during the wind-up. A flat disc is used when empty.")]
    public GameObject teleportMarkerPrefab;

    [Header("2 - Time Freeze")]
    public float freezeDuration = 4f;

    [Tooltip("Measured from the moment the freeze ends.")]
    public float freezeCooldown = 10f;

    [Header("Debug")]
    public bool showHud = true;

    /// <summary>True during the teleport wind-up: the player is rooted.</summary>
    public bool IsCharging => isTeleporting;

    public bool IsFreezing => isFreezing;

    private PlayerControls controls;
    private Rigidbody body;
    private Camera mainCamera;
    private Collider levelCollider;

    // Reused by the teleport sweep so a jump allocates nothing.
    private readonly RaycastHit[] teleportHits = new RaycastHit[16];

    private float nextTeleportTime;
    private float nextFreezeTime;
    private float freezeEndTime;
    private bool isTeleporting;
    private bool isFreezing;
    private GameObject activeMarker;
    private Material markerMaterial;

    private static bool attachHooked;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        attachHooked = false;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void HookAttach()
    {
        if (!attachHooked)
        {
            // GameOverUI restarts by reloading the scene, which recreates the Player without
            // a runtime-added component, so attach again after every load.
            SceneManager.sceneLoaded += (scene, mode) => AttachToPlayer();
            attachHooked = true;
        }
        AttachToPlayer();
    }

    private static void AttachToPlayer()
    {
        PlayerControls player = FindFirstObjectByType<PlayerControls>();
        if (player != null && player.GetComponent<PlayerAbilities>() == null)
        {
            player.gameObject.AddComponent<PlayerAbilities>();
        }
    }

    private void Awake()
    {
        controls = GetComponent<PlayerControls>();
        body = GetComponent<Rigidbody>();
        mainCamera = Camera.main;

        // Same convention as SunlightSweep3D: the arena is the "Level" object's collider.
        GameObject level = GameObject.Find("Level");
        levelCollider = level != null ? level.GetComponent<Collider>() : null;
    }

    private void Update()
    {
        if (Time.timeScale == 0f) return; // game over
        if (mainCamera == null) mainCamera = Camera.main;

        Keyboard keyboard = Keyboard.current;
        if (keyboard == null) return;

        if (keyboard.digit1Key.wasPressedThisFrame || keyboard.numpad1Key.wasPressedThisFrame) TryTeleport();
        if (keyboard.digit2Key.wasPressedThisFrame || keyboard.numpad2Key.wasPressedThisFrame) TryFreezeTime();
    }

    public float GetTeleportCooldownRemaining() => Mathf.Max(0f, nextTeleportTime - Time.time);

    public float GetFreezeCooldownRemaining() => Mathf.Max(0f, nextFreezeTime - Time.time);

    // ------------------------------------------------------------ 1: teleport

    public bool TryTeleport()
    {
        if (isTeleporting || Time.time < nextTeleportTime) return false;
        if (!TryGetCursorGroundPoint(out Vector3 cursorPoint)) return false;

        StartCoroutine(TeleportRoutine(cursorPoint));
        return true;
    }

    private IEnumerator TeleportRoutine(Vector3 cursorPoint)
    {
        isTeleporting = true;

        // The destination is locked in on the key press rather than after the wind-up,
        // so the marker the player sees is the spot they actually land on.
        Vector3 destination = ClampToWalls(ClampToLevel(ResolveTeleportDestination(cursorPoint)));
        activeMarker = SpawnMarker(destination);

        // Rooted through the wind-up so nothing invalidates the swept path. Collision
        // messages still reach a disabled script, so enemies can still kill the player.
        controls.enabled = false;
        body.linearVelocity = Vector3.zero;

        yield return new WaitForSeconds(teleportChargeTime);

        Destroy(activeMarker);
        controls.enabled = true;
        MoveTo(destination);

        nextTeleportTime = Time.time + teleportCooldown;
        isTeleporting = false;
    }

    /// <summary>Cursor projected onto the ground (the Level's top surface, or the player's feet).</summary>
    private bool TryGetCursorGroundPoint(out Vector3 point)
    {
        point = default;

        Mouse mouse = Mouse.current;
        if (mouse == null || mainCamera == null) return false;

        Ray ray = mainCamera.ScreenPointToRay(mouse.position.ReadValue());
        var ground = new Plane(Vector3.up, new Vector3(0f, GroundHeight(), 0f));
        if (!ground.Raycast(ray, out float enter)) return false;

        point = ray.GetPoint(enter);
        return true;
    }

    private float GroundHeight()
    {
        return levelCollider != null ? levelCollider.bounds.max.y : transform.position.y - teleportClearRadius;
    }

    private Vector3 ResolveTeleportDestination(Vector3 cursorPoint)
    {
        Vector3 origin = body.position;
        Vector3 offset = cursorPoint - origin;
        offset.y = 0f; // stay at the current height; only the ground position matters

        return origin + Vector3.ClampMagnitude(offset, teleportMaxRange);
    }

    /// <summary>Keeps the destination on the Level, so a blink cannot drop the player off the edge.</summary>
    private Vector3 ClampToLevel(Vector3 destination)
    {
        if (levelCollider == null) return destination;

        Bounds bounds = levelCollider.bounds;
        destination.x = Mathf.Clamp(destination.x, bounds.min.x + teleportClearRadius, bounds.max.x - teleportClearRadius);
        destination.z = Mathf.Clamp(destination.z, bounds.min.z + teleportClearRadius, bounds.max.z - teleportClearRadius);
        return destination;
    }

    /// <summary>
    /// Sweeps a player-sized sphere along the jump and pulls the destination back to the
    /// first solid obstacle in the way, so a blink cannot pass through walls.
    ///
    /// Not counted as obstacles: triggers (pickups, the safe zone), floor-like surfaces
    /// (the Level and floor tiles), and dynamic bodies (enemies and dummies, which move).
    /// </summary>
    private Vector3 ClampToWalls(Vector3 destination)
    {
        if (!stopAtWalls) return destination;

        Vector3 origin = body.position;
        Vector3 delta = destination - origin;
        float distance = delta.magnitude;
        if (distance < 0.0001f) return destination;

        Vector3 direction = delta / distance;

        // Slightly under the player's radius: standing flush against a wall (or on the floor)
        // would otherwise start the sweep already overlapping it.
        float castRadius = teleportClearRadius * 0.9f;
        int count = Physics.SphereCastNonAlloc(
            origin, castRadius, direction, teleportHits, distance, ~0, QueryTriggerInteraction.Ignore);

        float nearest = distance;
        for (int i = 0; i < count; i++)
        {
            RaycastHit hit = teleportHits[i];
            if (hit.collider == null) continue;
            if (hit.collider.transform.IsChildOf(transform)) continue;              // the player's own body
            if (hit.distance <= 0f) continue;                                       // already overlapping at the start
            if (hit.normal.y > 0.5f) continue;                                      // floor, not a wall
            if (hit.rigidbody != null && !hit.rigidbody.isKinematic) continue;      // enemies, dummies
            if (hit.distance < nearest) nearest = hit.distance;
        }

        return origin + direction * nearest;
    }

    private GameObject SpawnMarker(Vector3 destination)
    {
        Vector3 markerPosition = new Vector3(destination.x, GroundHeight() + 0.02f, destination.z);

        if (teleportMarkerPrefab != null)
        {
            return Instantiate(teleportMarkerPrefab, markerPosition, Quaternion.identity);
        }

        GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        marker.name = "Teleport Marker";
        Destroy(marker.GetComponent<Collider>());
        marker.transform.position = markerPosition;
        marker.transform.localScale = new Vector3(teleportClearRadius * 2f, 0.01f, teleportClearRadius * 2f);

        Renderer markerRenderer = marker.GetComponent<Renderer>();
        if (markerMaterial == null)
        {
            markerMaterial = new Material(markerRenderer.sharedMaterial) { color = new Color(0.3f, 0.8f, 1f) };
        }
        markerRenderer.sharedMaterial = markerMaterial;

        return marker;
    }

    // ---------------------------------------------------------- 2: time freeze

    public bool TryFreezeTime()
    {
        if (isFreezing || TimeFreeze.IsFrozen || Time.time < nextFreezeTime) return false;

        StartCoroutine(FreezeRoutine());
        return true;
    }

    private IEnumerator FreezeRoutine()
    {
        isFreezing = true;
        freezeEndTime = Time.time + freezeDuration;
        TimeFreeze.Freeze(transform);

        // Time.timeScale is untouched, so plain WaitForSeconds is real time here.
        yield return new WaitForSeconds(freezeDuration);

        TimeFreeze.Unfreeze();
        nextFreezeTime = Time.time + freezeCooldown;
        isFreezing = false;
    }

    // ------------------------------------------------------------- shared bits

    private void MoveTo(Vector3 destination)
    {
        body.position = destination;
        transform.position = destination; // avoids a one-frame lag behind the physics step
        body.linearVelocity = Vector3.zero;
    }

    private void OnDisable()
    {
        // Destroyed or disabled mid-ability (e.g. the scene reloading after Game Over):
        // nothing else would ever clean up, and the scene would stay frozen.
        if (isFreezing)
        {
            TimeFreeze.Unfreeze();
            isFreezing = false;
        }
        if (isTeleporting)
        {
            if (activeMarker != null) Destroy(activeMarker);
            if (controls != null) controls.enabled = true;
            isTeleporting = false;
        }
    }

    private void OnDestroy()
    {
        if (markerMaterial != null) Destroy(markerMaterial);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.3f, 0.8f, 1f, 0.7f);
        Gizmos.DrawWireSphere(transform.position, teleportMaxRange);
    }

    private void OnGUI()
    {
        if (!showHud) return;

        if (isFreezing)
        {
            // Cold tint over the whole screen while time is stopped.
            GUI.color = new Color(0.45f, 0.7f, 1f, 0.15f);
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        float teleportRemaining = GetTeleportCooldownRemaining();
        string teleportStatus = isTeleporting ? "Charging"
            : teleportRemaining > 0f ? $"{teleportRemaining:0.0}s"
            : "Ready";

        float freezeRemaining = GetFreezeCooldownRemaining();
        string freezeStatus = isFreezing ? $"Active {Mathf.Max(0f, freezeEndTime - Time.time):0.0}s"
            : freezeRemaining > 0f ? $"{freezeRemaining:0.0}s"
            : "Ready";

        GUI.Label(new Rect(10f, Screen.height - 50f, 400f, 22f), $"[1] Teleport: {teleportStatus}");
        GUI.Label(new Rect(10f, Screen.height - 28f, 400f, 22f), $"[2] Time Freeze: {freezeStatus}");
    }
}
