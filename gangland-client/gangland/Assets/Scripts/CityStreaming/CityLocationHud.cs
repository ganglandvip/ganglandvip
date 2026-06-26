using System.Collections.Generic;
using UnityEngine;

namespace Gangland.CityStreaming
{
    public sealed class CityLocationHud : MonoBehaviour
    {
        [Header("Map")]
        [SerializeField] string resourcesMapRoot = "Maps/slauson_3420";
        [SerializeField] Transform trackingTarget;
        [SerializeField] string fallbackAddress = "3420 W. Slauson Avenue, Los Angeles, CA 90043";
        [SerializeField] float mapWorldPaddingMeters = 120f;

        [Header("Layout")]
        [SerializeField] Vector2 panelSize = new Vector2(360f, 330f);
        [SerializeField] Vector2 panelMargin = new Vector2(24f, 24f);
        [SerializeField] float mapHeight = 220f;

        readonly List<StreetSegment> streetSegments = new List<StreetSegment>();
        Texture2D pixel;
        GUIStyle labelStyle;
        GUIStyle addressStyle;
        GUIStyle smallStyle;
        Rect worldBounds;
        string addressText;
        bool mapReady;

        void Awake()
        {
            pixel = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            pixel.SetPixel(0, 0, Color.white);
            pixel.Apply();
            addressText = fallbackAddress;
            LoadMapData();
        }

        void OnGUI()
        {
            EnsureStyles();

            float width = Mathf.Min(panelSize.x, Screen.width - panelMargin.x * 2f);
            float height = panelSize.y;
            Rect panel = new Rect(Screen.width - width - panelMargin.x, panelMargin.y, width, height);

            DrawRect(panel, new Color(0.02f, 0.025f, 0.035f, 0.82f));
            DrawBorder(panel, new Color(0.18f, 0.95f, 0.82f, 0.42f), 1f);

            Rect titleRect = new Rect(panel.x + 16f, panel.y + 14f, panel.width - 32f, 18f);
            GUI.Label(titleRect, "CURRENT ADDRESS", labelStyle);

            Rect addressRect = new Rect(panel.x + 16f, panel.y + 34f, panel.width - 32f, 44f);
            GUI.Label(addressRect, addressText, addressStyle);

            Rect mapRect = new Rect(panel.x + 16f, panel.y + 88f, panel.width - 32f, mapHeight);
            DrawMinimap(mapRect);

            Rect scaleRect = new Rect(mapRect.xMax - 86f, mapRect.yMax - 24f, 70f, 14f);
            GUI.Label(scaleRect, "0.5 MI", smallStyle);
        }

        void LoadMapData()
        {
            TextAsset manifestAsset = Resources.Load<TextAsset>($"{resourcesMapRoot}/manifest");
            if (manifestAsset == null)
            {
                Debug.LogWarning($"Location HUD could not find Resources/{resourcesMapRoot}/manifest.json.", this);
                return;
            }

            CityMapManifest manifest = JsonUtility.FromJson<CityMapManifest>(manifestAsset.text);
            if (manifest == null || manifest.chunks == null)
            {
                Debug.LogWarning("Location HUD loaded a map manifest without chunks.", this);
                return;
            }

            if (manifest.source != null && !string.IsNullOrWhiteSpace(manifest.source.address))
            {
                addressText = manifest.source.address;
            }

            BoundsAccumulator bounds = new BoundsAccumulator(mapWorldPaddingMeters);
            for (int i = 0; i < manifest.chunks.Length; i++)
            {
                CityChunkManifestEntry entry = manifest.chunks[i];
                if (entry == null || entry.counts == null || entry.counts.streets <= 0)
                {
                    continue;
                }

                TextAsset chunkAsset = Resources.Load<TextAsset>($"{resourcesMapRoot}/{entry.resource}");
                if (chunkAsset == null)
                {
                    continue;
                }

                CityChunk chunk = JsonUtility.FromJson<CityChunk>(chunkAsset.text);
                if (chunk == null || chunk.streets == null)
                {
                    continue;
                }

                for (int streetIndex = 0; streetIndex < chunk.streets.Length; streetIndex++)
                {
                    CityStreet street = chunk.streets[streetIndex];
                    if (street == null || street.centerline == null || street.centerline.Length < 2)
                    {
                        continue;
                    }

                    Color color = StreetColor(street);
                    float width = StreetWidth(street);
                    for (int pointIndex = 1; pointIndex < street.centerline.Length; pointIndex++)
                    {
                        CityPoint start = street.centerline[pointIndex - 1];
                        CityPoint end = street.centerline[pointIndex];
                        streetSegments.Add(new StreetSegment(
                            new Vector2(start.x, start.z),
                            new Vector2(end.x, end.z),
                            color,
                            width));
                        bounds.Include(start.x, start.z);
                        bounds.Include(end.x, end.z);
                    }
                }
            }

            if (bounds.HasValue && streetSegments.Count > 0)
            {
                worldBounds = bounds.ToRect();
                mapReady = true;
            }
        }

        void DrawMinimap(Rect mapRect)
        {
            DrawRect(mapRect, new Color(0.015f, 0.018f, 0.024f, 0.95f));
            DrawGrid(mapRect, 32f, new Color(1f, 0.96f, 0.82f, 0.07f));
            DrawBorder(mapRect, new Color(0.18f, 0.95f, 0.82f, 0.32f), 1f);

            if (!mapReady)
            {
                GUI.Label(new Rect(mapRect.x + 12f, mapRect.center.y - 10f, mapRect.width - 24f, 22f), "MAP DATA OFFLINE", smallStyle);
                return;
            }

            for (int i = 0; i < streetSegments.Count; i++)
            {
                StreetSegment segment = streetSegments[i];
                Vector2 start = WorldToMap(segment.Start, mapRect);
                Vector2 end = WorldToMap(segment.End, mapRect);
                DrawLine(start, end, segment.Color, segment.Width);
            }

            Vector3 targetPosition = GetTargetPosition();
            Vector2 target = WorldToMap(new Vector2(targetPosition.x, targetPosition.z), mapRect);
            DrawMarker(target);

            GUI.Label(new Rect(mapRect.x + 12f, mapRect.y + 10f, mapRect.width - 24f, 18f), "SLAUSON AREA STREET GRID", smallStyle);
        }

        Vector3 GetTargetPosition()
        {
            if (trackingTarget != null)
            {
                return trackingTarget.position;
            }

            Camera mainCamera = Camera.main;
            return mainCamera != null ? mainCamera.transform.position : Vector3.zero;
        }

        Vector2 WorldToMap(Vector2 world, Rect mapRect)
        {
            float x = Mathf.InverseLerp(worldBounds.xMin, worldBounds.xMax, world.x);
            float z = Mathf.InverseLerp(worldBounds.yMin, worldBounds.yMax, world.y);
            return new Vector2(
                Mathf.Lerp(mapRect.xMin, mapRect.xMax, x),
                Mathf.Lerp(mapRect.yMax, mapRect.yMin, z));
        }

        void EnsureStyles()
        {
            if (labelStyle != null)
            {
                return;
            }

            labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.96f, 0.78f, 0.33f) }
            };
            addressStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 15,
                fontStyle = FontStyle.Bold,
                wordWrap = true,
                normal = { textColor = new Color(1f, 0.96f, 0.86f) }
            };
            smallStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 10,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = new Color(0.72f, 0.95f, 0.88f, 0.86f) }
            };
        }

        void DrawGrid(Rect rect, float spacing, Color color)
        {
            for (float x = rect.xMin + spacing; x < rect.xMax; x += spacing)
            {
                DrawLine(new Vector2(x, rect.yMin), new Vector2(x, rect.yMax), color, 1f);
            }

            for (float y = rect.yMin + spacing; y < rect.yMax; y += spacing)
            {
                DrawLine(new Vector2(rect.xMin, y), new Vector2(rect.xMax, y), color, 1f);
            }
        }

        void DrawMarker(Vector2 center)
        {
            const float radius = 7f;
            Rect outer = new Rect(center.x - radius, center.y - radius, radius * 2f, radius * 2f);
            DrawRect(outer, new Color(1f, 0.12f, 0.22f, 0.94f));
            DrawBorder(outer, new Color(1f, 0.78f, 0.22f, 1f), 2f);
        }

        void DrawRect(Rect rect, Color color)
        {
            Color previousColor = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, pixel);
            GUI.color = previousColor;
        }

        void DrawBorder(Rect rect, Color color, float thickness)
        {
            DrawRect(new Rect(rect.xMin, rect.yMin, rect.width, thickness), color);
            DrawRect(new Rect(rect.xMin, rect.yMax - thickness, rect.width, thickness), color);
            DrawRect(new Rect(rect.xMin, rect.yMin, thickness, rect.height), color);
            DrawRect(new Rect(rect.xMax - thickness, rect.yMin, thickness, rect.height), color);
        }

        void DrawLine(Vector2 start, Vector2 end, Color color, float width)
        {
            float length = Vector2.Distance(start, end);
            if (length <= 0.01f)
            {
                return;
            }

            Matrix4x4 previousMatrix = GUI.matrix;
            Color previousColor = GUI.color;
            float angle = Vector2.SignedAngle(Vector2.right, end - start);

            GUI.color = color;
            GUIUtility.RotateAroundPivot(angle, start);
            GUI.DrawTexture(new Rect(start.x, start.y - width * 0.5f, length, width), pixel);
            GUI.matrix = previousMatrix;
            GUI.color = previousColor;
        }

        static Color StreetColor(CityStreet street)
        {
            if (street.highway == "primary" || street.highway == "secondary")
            {
                return new Color(1f, 0.92f, 0.72f, 0.94f);
            }

            if (street.render_kind == "walkway" || street.highway == "footway" || street.highway == "path")
            {
                return new Color(0.18f, 0.95f, 0.82f, 0.55f);
            }

            return new Color(0.82f, 0.84f, 0.78f, 0.72f);
        }

        static float StreetWidth(CityStreet street)
        {
            if (street.highway == "primary")
            {
                return 3f;
            }

            if (street.highway == "secondary")
            {
                return 2.5f;
            }

            if (street.render_kind == "walkway")
            {
                return 1f;
            }

            return 1.8f;
        }

        readonly struct StreetSegment
        {
            public StreetSegment(Vector2 start, Vector2 end, Color color, float width)
            {
                Start = start;
                End = end;
                Color = color;
                Width = width;
            }

            public Vector2 Start { get; }
            public Vector2 End { get; }
            public Color Color { get; }
            public float Width { get; }
        }

        struct BoundsAccumulator
        {
            readonly float padding;
            float minX;
            float minZ;
            float maxX;
            float maxZ;

            public BoundsAccumulator(float paddingMeters)
            {
                padding = paddingMeters;
                minX = float.PositiveInfinity;
                minZ = float.PositiveInfinity;
                maxX = float.NegativeInfinity;
                maxZ = float.NegativeInfinity;
                HasValue = false;
            }

            public bool HasValue { get; private set; }

            public void Include(float x, float z)
            {
                minX = Mathf.Min(minX, x);
                minZ = Mathf.Min(minZ, z);
                maxX = Mathf.Max(maxX, x);
                maxZ = Mathf.Max(maxZ, z);
                HasValue = true;
            }

            public Rect ToRect()
            {
                return Rect.MinMaxRect(minX - padding, minZ - padding, maxX + padding, maxZ + padding);
            }
        }
    }
}
