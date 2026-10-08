using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace SunlightSurvivor.Sunlight
{
    public enum SunlightOrientation
    {
        Horizontal,  // row-shaped band moving vertically (default: top → bottom)
        Vertical,    // column-shaped band moving horizontally (default: left → right)
        Alternate    // switch between the two after every sweep
    }

    /// <summary>
    /// Continuously sweeps a band of sunlight across the arena tilemap, with a telegraph strip
    /// running just ahead of it. Other systems query <see cref="IsInSunlight"/> or listen to
    /// the sweep events to react (damage, buffs, enemy AI...).
    /// </summary>
    public class SunlightManager : MonoBehaviour
    {
        [Header("Arena")]
        [Tooltip("Tilemap whose painted cells define the area sunlight can cover.")]
        [SerializeField] Tilemap arena;

        [Header("Sweep")]
        [SerializeField] SunlightOrientation orientation = SunlightOrientation.Horizontal;
        [Tooltip("Off: top → bottom / left → right. On: bottom → top / right → left.")]
        [SerializeField] bool reverseDirection;
        [Tooltip("Flip the direction after every sweep.")]
        [SerializeField] bool pingPong;
        [Tooltip("Thickness of the sunlight band, in cells.")]
        [SerializeField, Min(0.1f)] float bandWidth = 2f;
        [Tooltip("Band speed, in cells per second.")]
        [SerializeField, Min(0.1f)] float sweepSpeed = 1.5f;
        [Tooltip("Length of the warning strip ahead of the band, in cells (0 = no warning).")]
        [SerializeField, Min(0f)] float warningLead = 2f;
        [Tooltip("Seconds of shade between the band leaving the arena and the next sweep.")]
        [SerializeField, Min(0f)] float pauseBetweenSweeps = 1f;

        [Header("Visuals")]
        [SerializeField] SunBeamView beamView;

        [Header("Debug")]
        [SerializeField] bool showDebugHud = true;

        public event Action SweepStarted;
        public event Action SweepEnded;

        public bool IsSweeping { get; private set; }
        /// <summary>World-space area currently in sunlight, clipped to the arena (zero-size when none).</summary>
        public Rect SunlightArea { get; private set; }
        /// <summary>World-space telegraph strip ahead of the band, clipped to the arena.</summary>
        public Rect WarningArea { get; private set; }
        public Tilemap Arena => arena;

        Rect arenaRect;
        bool horizontal;
        bool descending;   // band moves towards decreasing world coordinates
        float travelled;   // distance of the band's leading edge from the starting edge, in world units
        int sweepCount;

        IEnumerator Start()
        {
            if (arena == null)
            {
                Debug.LogError($"{nameof(SunlightManager)} needs an arena Tilemap.", this);
                enabled = false;
                yield break;
            }

            arena.CompressBounds();
            BoundsInt bounds = arena.cellBounds;
            if (bounds.size.x == 0 || bounds.size.y == 0)
            {
                Debug.LogError($"{nameof(SunlightManager)}: arena Tilemap '{arena.name}' has no tiles.", this);
                enabled = false;
                yield break;
            }

            Vector3 min = arena.CellToWorld(bounds.min);
            Vector3 max = arena.CellToWorld(new Vector3Int(bounds.xMax, bounds.yMax, 0));
            arenaRect = Rect.MinMaxRect(min.x, min.y, max.x, max.y);

            while (true)
            {
                BeginSweep();
                SweepStarted?.Invoke();
                // TODO: damage — once a Player exists, poll IsInSunlight(player.position) while sweeping.

                while (travelled < ArenaLength + BandWorldWidth)
                {
                    travelled += sweepSpeed * CellSize * Time.deltaTime;
                    UpdateAreas();
                    yield return null;
                }

                EndSweep();
                SweepEnded?.Invoke();
                yield return new WaitForSeconds(pauseBetweenSweeps);
            }
        }

        public bool IsInSunlight(Vector2 worldPosition)
        {
            return IsSweeping && SunlightArea.Contains(worldPosition);
        }

        float CellSize => horizontal ? arena.layoutGrid.cellSize.y : arena.layoutGrid.cellSize.x;
        float ArenaLength => horizontal ? arenaRect.height : arenaRect.width;
        float BandWorldWidth => bandWidth * CellSize;

        void BeginSweep()
        {
            horizontal = orientation switch
            {
                SunlightOrientation.Horizontal => true,
                SunlightOrientation.Vertical => false,
                _ => sweepCount % 2 == 0
            };

            bool reversed = reverseDirection ^ (pingPong && sweepCount % 2 == 1);
            // Default directions: horizontal bands fall from the top, vertical bands go left → right.
            descending = horizontal ? !reversed : reversed;

            travelled = 0f;
            IsSweeping = true;
            UpdateAreas();
        }

        void EndSweep()
        {
            IsSweeping = false;
            SunlightArea = Rect.zero;
            WarningArea = Rect.zero;
            sweepCount++;
            if (beamView != null) beamView.Hide();
        }

        void UpdateAreas()
        {
            // Band trails the leading edge; the warning strip runs ahead of it.
            SunlightArea = SpanToRect(travelled - BandWorldWidth, travelled);
            WarningArea = SpanToRect(travelled, travelled + warningLead * CellSize);
            if (beamView != null) beamView.Show(SunlightArea, WarningArea);
        }

        // Converts a distance span along the sweep axis (0 = starting edge) into a world rect clipped to the arena.
        Rect SpanToRect(float from, float to)
        {
            float length = ArenaLength;
            from = Mathf.Clamp(from, 0f, length);
            to = Mathf.Clamp(to, 0f, length);
            if (to <= from) return Rect.zero;

            float lo = descending ? length - to : from;
            float hi = descending ? length - from : to;
            return horizontal
                ? Rect.MinMaxRect(arenaRect.xMin, arenaRect.yMin + lo, arenaRect.xMax, arenaRect.yMin + hi)
                : Rect.MinMaxRect(arenaRect.xMin + lo, arenaRect.yMin, arenaRect.xMin + hi, arenaRect.yMax);
        }

        void OnDrawGizmos()
        {
            if (!Application.isPlaying || !IsSweeping) return;

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(SunlightArea.center, SunlightArea.size);
            Gizmos.color = new Color(1f, 0.5f, 0f);
            Gizmos.DrawWireCube(WarningArea.center, WarningArea.size);
        }

        void OnGUI()
        {
            if (!showDebugHud) return;

            string state = IsSweeping
                ? $"sweeping {travelled / (ArenaLength + BandWorldWidth):P0}"
                : "shade";
            GUI.Label(new Rect(10, 10, 400, 25), $"Sunlight [{orientation}]  {state}");
        }
    }
}
