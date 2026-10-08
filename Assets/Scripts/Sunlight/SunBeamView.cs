using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace SunlightSurvivor.Sunlight
{
    /// <summary>
    /// Visuals for a moving sunlight band: a freeform Light2D fitted to the lit area,
    /// and a pulsing unlit overlay over the warning strip ahead of it.
    /// </summary>
    public class SunBeamView : MonoBehaviour
    {
        [Header("Warning")]
        [Tooltip("1x1 world-unit square sprite, ideally with an unlit material so it reads in the dark.")]
        [SerializeField] SpriteRenderer warningOverlay;
        [SerializeField] Color warningColor = new Color(1f, 0.55f, 0.1f, 0.35f);
        [SerializeField, Min(0f)] float warningPulseSpeed = 8f;

        [Header("Sunlight")]
        [Tooltip("Freeform Light2D; its shape is rebuilt whenever the lit area changes size.")]
        [SerializeField] Light2D sunLight;
        [SerializeField] Color sunColor = new Color(1f, 0.92f, 0.65f);
        [SerializeField, Min(0f)] float sunIntensity = 1.6f;

        Vector2 lightSize;

        void Awake()
        {
            sunLight.lightType = Light2D.LightType.Freeform;
            Hide();
        }

        /// <summary>Shows sunlight over <paramref name="sunArea"/> and a warning over <paramref name="warningArea"/>; zero-size rects hide that part.</summary>
        public void Show(Rect sunArea, Rect warningArea)
        {
            sunLight.enabled = HasArea(sunArea);
            if (sunLight.enabled)
            {
                sunLight.color = sunColor;
                sunLight.intensity = sunIntensity;
                sunLight.transform.position = WithZ(sunArea.center, sunLight.transform.position.z);
                if (sunArea.size != lightSize) SetLightSize(sunArea.size);
            }

            warningOverlay.enabled = HasArea(warningArea);
            if (warningOverlay.enabled)
            {
                warningOverlay.transform.position = WithZ(warningArea.center, warningOverlay.transform.position.z);
                warningOverlay.transform.localScale = new Vector3(warningArea.width, warningArea.height, 1f);
            }
        }

        public void Hide()
        {
            sunLight.enabled = false;
            warningOverlay.enabled = false;
        }

        void Update()
        {
            if (!warningOverlay.enabled) return;

            float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * warningPulseSpeed);
            Color c = warningColor;
            c.a *= Mathf.Lerp(0.4f, 1f, pulse);
            warningOverlay.color = c;
        }

        void SetLightSize(Vector2 size)
        {
            lightSize = size;
            Vector2 half = size * 0.5f;
            sunLight.SetShapePath(new[]
            {
                new Vector3(-half.x, -half.y),
                new Vector3(half.x, -half.y),
                new Vector3(half.x, half.y),
                new Vector3(-half.x, half.y)
            });
        }

        static bool HasArea(Rect r) => r.width > 0f && r.height > 0f;

        static Vector3 WithZ(Vector2 v, float z) => new Vector3(v.x, v.y, z);
    }
}
