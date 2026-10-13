using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public class SunlightSweep3D : MonoBehaviour
{
    [Header("Timing")]
    [SerializeField, Min(0f)] private float initialDelay = 5f;
    [SerializeField, Min(0f)] private float timeBetweenShots = 6f;
    [SerializeField, Min(0.1f)] private float warningDuration = 2f;
    [SerializeField, Min(0.1f)] private float laserDuration = 1.25f;

    [Header("Laser")]
    [SerializeField, Min(0.1f)] private float laserWidth = 1.5f;

    private readonly Color warningColor = new Color(1f, 0.35f, 0.05f, 0.35f);
    private readonly Color sunlightColor = new Color(1f, 0.85f, 0.15f, 0.55f);

    private PlayerControls player;
    private Bounds arenaBounds;
    private GameObject warningBand;
    private GameObject sunlightBand;
    private Material warningMaterial;
    private Material sunlightMaterial;
    private bool warningIsVisible;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CreateForGameScene()
    {
        if (FindFirstObjectByType<PlayerControls>() == null ||
            FindFirstObjectByType<SunlightSweep3D>() != null)
        {
            return;
        }

        new GameObject("Sunlight System").AddComponent<SunlightSweep3D>();
    }

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void Start()
    {
        StartForCurrentScene();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        StartForCurrentScene();
    }

    private void StartForCurrentScene()
    {
        StopAllCoroutines();
        warningIsVisible = false;

        if (warningBand != null)
        {
            warningBand.SetActive(false);
        }

        if (sunlightBand != null)
        {
            sunlightBand.SetActive(false);
        }

        player = FindFirstObjectByType<PlayerControls>();
        if (player == null)
        {
            return;
        }

        GameObject level = GameObject.Find("Level");
        Collider levelCollider = level != null ? level.GetComponent<Collider>() : null;
        if (levelCollider == null)
        {
            Debug.LogError("Sunlight needs a Level object with a Collider.", this);
            return;
        }

        arenaBounds = levelCollider.bounds;
        if (warningBand == null)
        {
            warningBand = CreateBand("Sunlight Warning", warningColor, out warningMaterial);
        }

        if (sunlightBand == null)
        {
            sunlightBand = CreateBand("Sunlight Danger", sunlightColor, out sunlightMaterial);
        }

        StartCoroutine(LaserLoop());
    }

    private IEnumerator LaserLoop()
    {
        yield return new WaitForSeconds(initialDelay);

        while (true)
        {
            yield return FireLaser();
            yield return new WaitForSeconds(timeBetweenShots);
        }
    }

    private IEnumerator FireLaser()
    {
        float floorY = arenaBounds.max.y + 0.06f;
        ChooseLaserPath(floorY, out Vector3 start, out Vector3 end);

        float warningWidth = Mathf.Max(0.2f, laserWidth * 0.35f);
        ConfigureLaser(warningBand, start, end, warningWidth);
        warningBand.SetActive(true);
        warningIsVisible = true;

        float warningElapsed = 0f;
        while (warningElapsed < warningDuration)
        {
            warningElapsed += Time.deltaTime;
            float pulse = 0.5f + 0.5f * Mathf.Sin(warningElapsed * 10f);
            SetMaterialColor(warningMaterial, new Color(
                warningColor.r,
                warningColor.g,
                warningColor.b,
                Mathf.Lerp(0.15f, 0.5f, pulse)));
            yield return null;
        }

        warningBand.SetActive(false);
        warningIsVisible = false;

        ConfigureLaser(sunlightBand, start, end, laserWidth);
        sunlightBand.SetActive(true);

        float laserElapsed = 0f;
        while (laserElapsed < laserDuration)
        {
            laserElapsed += Time.deltaTime;

            if (PlayerIsInLaser(start, end))
            {
                player.Kill();
                yield break;
            }

            yield return null;
        }

        sunlightBand.SetActive(false);
    }

    private void ChooseLaserPath(float floorY, out Vector3 start, out Vector3 end)
    {
        float padding = laserWidth;

        switch (Random.Range(0, 4))
        {
            case 0:
                start = new Vector3(
                    arenaBounds.min.x - padding,
                    floorY,
                    Random.Range(arenaBounds.min.z, arenaBounds.max.z));
                end = new Vector3(
                    arenaBounds.max.x + padding,
                    floorY,
                    Random.Range(arenaBounds.min.z, arenaBounds.max.z));
                break;
            case 1:
                start = new Vector3(
                    arenaBounds.max.x + padding,
                    floorY,
                    Random.Range(arenaBounds.min.z, arenaBounds.max.z));
                end = new Vector3(
                    arenaBounds.min.x - padding,
                    floorY,
                    Random.Range(arenaBounds.min.z, arenaBounds.max.z));
                break;
            case 2:
                start = new Vector3(
                    Random.Range(arenaBounds.min.x, arenaBounds.max.x),
                    floorY,
                    arenaBounds.min.z - padding);
                end = new Vector3(
                    Random.Range(arenaBounds.min.x, arenaBounds.max.x),
                    floorY,
                    arenaBounds.max.z + padding);
                break;
            default:
                start = new Vector3(
                    Random.Range(arenaBounds.min.x, arenaBounds.max.x),
                    floorY,
                    arenaBounds.max.z + padding);
                end = new Vector3(
                    Random.Range(arenaBounds.min.x, arenaBounds.max.x),
                    floorY,
                    arenaBounds.min.z - padding);
                break;
        }
    }

    private static void ConfigureLaser(GameObject laser, Vector3 start, Vector3 end, float width)
    {
        Vector3 direction = end - start;
        laser.transform.position = (start + end) * 0.5f;
        laser.transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        laser.transform.localScale = new Vector3(width, 0.08f, direction.magnitude);
    }

    private bool PlayerIsInLaser(Vector3 start, Vector3 end)
    {
        if (player == null)
        {
            return false;
        }

        Vector3 playerPosition = player.transform.position;
        bool insideArena =
            playerPosition.x >= arenaBounds.min.x && playerPosition.x <= arenaBounds.max.x &&
            playerPosition.z >= arenaBounds.min.z && playerPosition.z <= arenaBounds.max.z;

        if (!insideArena)
        {
            return false;
        }

        Vector2 playerPoint = new Vector2(playerPosition.x, playerPosition.z);
        Vector2 laserStart = new Vector2(start.x, start.z);
        Vector2 laserEnd = new Vector2(end.x, end.z);
        Vector2 laserDirection = laserEnd - laserStart;

        float lengthSquared = laserDirection.sqrMagnitude;
        if (lengthSquared <= Mathf.Epsilon)
        {
            return false;
        }

        float positionOnLaser = Mathf.Clamp01(
            Vector2.Dot(playerPoint - laserStart, laserDirection) / lengthSquared);
        Vector2 closestPoint = laserStart + laserDirection * positionOnLaser;

        return Vector2.Distance(playerPoint, closestPoint) <= laserWidth * 0.5f;
    }

    private GameObject CreateBand(string objectName, Color color, out Material material)
    {
        GameObject band = GameObject.CreatePrimitive(PrimitiveType.Cube);
        band.name = objectName;
        band.transform.SetParent(transform);
        band.SetActive(false);

        Collider bandCollider = band.GetComponent<Collider>();
        if (bandCollider != null)
        {
            Destroy(bandCollider);
        }

        Renderer bandRenderer = band.GetComponent<Renderer>();
        bandRenderer.shadowCastingMode = ShadowCastingMode.Off;
        bandRenderer.receiveShadows = false;
        material = CreateTransparentMaterial(color);
        bandRenderer.material = material;

        return band;
    }

    private static Material CreateTransparentMaterial(Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null)
        {
            shader = Shader.Find("Unlit/Color");
        }

        Material material = new Material(shader);
        material.SetFloat("_Surface", 1f);
        material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
        material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
        material.SetFloat("_ZWrite", 0f);
        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.renderQueue = (int)RenderQueue.Transparent;
        SetMaterialColor(material, color);
        return material;
    }

    private static void SetMaterialColor(Material material, Color color)
    {
        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", color);
        }

        if (material.HasProperty("_Color"))
        {
            material.SetColor("_Color", color);
        }
    }

    private void OnGUI()
    {
        if (!warningIsVisible)
        {
            return;
        }

        GUIStyle warningStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = Mathf.RoundToInt(Mathf.Clamp(Screen.height * 0.045f, 26f, 46f)),
            fontStyle = FontStyle.Bold
        };
        warningStyle.normal.textColor = new Color(1f, 0.45f, 0.1f);

        GUI.Label(
            new Rect(0f, Screen.height * 0.08f, Screen.width, 70f),
            "SUNLIGHT INCOMING!",
            warningStyle);
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;

        if (warningMaterial != null)
        {
            Destroy(warningMaterial);
        }

        if (sunlightMaterial != null)
        {
            Destroy(sunlightMaterial);
        }
    }
}
