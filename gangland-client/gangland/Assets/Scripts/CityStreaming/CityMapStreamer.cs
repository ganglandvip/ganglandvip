using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Gangland.CityStreaming
{
    public sealed class CityMapStreamer : MonoBehaviour
    {
        const string RendererVersion = "no-ground-grid-v16";
        const string StreetLampPath = "Assets/Night Modular City Pack/Prefabs/PropsWithColliders/NC_Street_Lamp.prefab";
        const string TrafficSignalPath = "Assets/Night Modular City Pack/Prefabs/PropsWithColliders/NC_Traffic_Signal.prefab";
        const string BusStopPath = "Assets/Night Modular City Pack/Prefabs/PropsWithColliders/NC_Bus_Stop.prefab";
        const string TrashCanPath = "Assets/Night Modular City Pack/Prefabs/PropsWithColliders/NC_Trash_Can.prefab";
        const string BenchPath = "Assets/Night Modular City Pack/Prefabs/PropsWithColliders/NC_Bench.prefab";
        const string HydrantPath = "Assets/Night Modular City Pack/Prefabs/PropsWithColliders/NC_Hydrant.prefab";
        const string CharacterPreviewPath = "Assets/Street Gangs - Miami Beach/Prefabs/Gangster_1.prefab";
        const string RoadMaterialPath = "Assets/Night Modular City Pack/Materials/NC_Asphalt_Road Texture_M_01.mat";
        const string IntersectionMaterialPath = "Assets/Night Modular City Pack/Materials/NC_Asphalt_Cross and Corner.mat";
        const string SidewalkMaterialPath = "Assets/Night Modular City Pack/Materials/NC_Footpath.mat";
        const string BuildingMaterialPath = "Assets/Night Modular City Pack/Materials/NC_Building_B_diffuse.mat";
        static readonly string[] BuildingPaths =
        {
            "Assets/Night Modular City Pack/Prefabs/Buildings/SmallBuildings/NC_SmallBuilding_A.prefab",
            "Assets/Night Modular City Pack/Prefabs/Buildings/SmallBuildings/NC_SmallBuilding_B.prefab",
            "Assets/Night Modular City Pack/Prefabs/Buildings/SmallBuildings/NC_SmallBuilding_C.prefab",
            "Assets/Night Modular City Pack/Prefabs/Buildings/SmallBuildings/NC_SmallBuilding_D.prefab",
            "Assets/Night Modular City Pack/Prefabs/Buildings/SmallBuildings/NC_SmallBuilding_E.prefab",
            "Assets/Night Modular City Pack/Prefabs/Buildings/SmallBuildings/NC_SmallBuilding_F.prefab",
            "Assets/Night Modular City Pack/Prefabs/Buildings/SmallBuildings/NC_SmallBuilding_G.prefab",
            "Assets/Night Modular City Pack/Prefabs/Buildings/Apartments/NC_Apartment_A.prefab",
            "Assets/Night Modular City Pack/Prefabs/Buildings/Apartments/NC_Apartment_B.prefab",
            "Assets/Night Modular City Pack/Prefabs/Buildings/Apartments/NC_Apartment_C.prefab",
            "Assets/Night Modular City Pack/Prefabs/Buildings/Apartments/NC_Apartment_D.prefab",
            "Assets/Night Modular City Pack/Prefabs/Buildings/Apartments/NC_Apartment_E.prefab",
            "Assets/Night Modular City Pack/Prefabs/Buildings/Apartments/NC_Apartment_F.prefab",
            "Assets/Night Modular City Pack/Prefabs/Buildings/Apartments/NC_Apartment_G.prefab",
            "Assets/Night Modular City Pack/Prefabs/Buildings/Apartments/NC_Apartment_H.prefab",
            "Assets/Night Modular City Pack/Prefabs/Buildings/Apartments/NC_Apartment_I.prefab",
            "Assets/Night Modular City Pack/Prefabs/Buildings/LargeBuildings/NC_LargeBuilding_A.prefab",
            "Assets/Night Modular City Pack/Prefabs/Buildings/LargeBuildings/NC_LargeBuilding_B.prefab",
            "Assets/Night Modular City Pack/Prefabs/Buildings/LargeBuildings/NC_LargeBuilding_C.prefab",
            "Assets/Night Modular City Pack/Prefabs/Buildings/LargeBuildings/NC_LargeBuilding_D.prefab",
            "Assets/Night Modular City Pack/Prefabs/Buildings/LargeBuildings/NC_LargeBuilding_E.prefab",
            "Assets/Night Modular City Pack/Prefabs/Buildings/LargeBuildings/NC_LargeBuilding_F.prefab",
            "Assets/Night Modular City Pack/Prefabs/Buildings/LargeBuildings/NC_LargeBuilding_G.prefab",
            "Assets/Night Modular City Pack/Prefabs/Buildings/LargeBuildings/NC_LargeBuilding_I.prefab",
            "Assets/Night Modular City Pack/Prefabs/Buildings/LargeBuildings/NC_LargeBuilding_J.prefab",
            "Assets/Night Modular City Pack/Prefabs/Buildings/LargeBuildings/NC_LargeBuilding_K.prefab",
            "Assets/Night Modular City Pack/Prefabs/Buildings/LargeBuildings/NC_LargeBuilding_L.prefab",
            "Assets/Night Modular City Pack/Prefabs/Buildings/Skyscrapers/NC_Skyscraper_A.prefab",
            "Assets/Night Modular City Pack/Prefabs/Buildings/Skyscrapers/NC_Skyscraper_B.prefab",
            "Assets/Night Modular City Pack/Prefabs/Buildings/Skyscrapers/NC_Skyscraper_C.prefab",
            "Assets/Night Modular City Pack/Prefabs/Buildings/Skyscrapers/NC_Skyscraper_D.prefab",
            "Assets/Night Modular City Pack/Prefabs/Buildings/Skyscrapers/NC_Skyscraper_E.prefab",
            "Assets/Night Modular City Pack/Prefabs/Buildings/Skyscrapers/NC_Skyscraper_F.prefab",
            "Assets/Night Modular City Pack/Prefabs/Buildings/Skyscrapers/NC_Skyscraper_G.prefab",
            "Assets/Night Modular City Pack/Prefabs/Buildings/Skyscrapers/NC_Skyscraper_H.prefab",
            "Assets/Night Modular City Pack/Prefabs/Buildings/Skyscrapers/NC_Skyscraper_I.prefab",
            "Assets/Night Modular City Pack/Prefabs/Buildings/Skyscrapers/NC_Skyscraper_J.prefab",
        };

        [Header("Map")]
        [SerializeField] string resourcesMapRoot = "Maps/slauson_3420";
        [SerializeField] Transform streamingTarget;
        [SerializeField] bool loadOnStart = true;

        [Header("Streaming")]
        [SerializeField] float loadRadiusMeters = 420f;
        [SerializeField] float unloadRadiusMeters = 560f;
        [SerializeField] float updateIntervalSeconds = 0.35f;

        [Header("Rendering")]
        [SerializeField] bool renderTerrain = false;
        [SerializeField] Material terrainMaterial;
        [SerializeField] Material urbanGroundMaterial;
        [SerializeField] Material roadMaterial;
        [SerializeField] Material sidewalkMaterial;
        [SerializeField] Material walkwayMaterial;
        [SerializeField] Material laneMarkingMaterial;
        [SerializeField] Material roadEdgeMarkingMaterial;
        [SerializeField] Material buildingMaterial;
        [SerializeField] Material windowMaterial;
        [SerializeField] Material areaMaterial;
        [SerializeField] Material poiMaterial;
        [SerializeField] bool spawnPois = true;
        [SerializeField] bool configureEnvironment = true;
        [SerializeField] bool renderProceduralBuildings = false;
        [SerializeField] bool useAssetPackMaterials = false;

        [Header("Asset Pack Dressing")]
        [SerializeField] bool useAssetPackProps = true;
        [SerializeField] bool useAssetPackBuildings = true;
        [SerializeField] bool spawnAssetPackPreview = false;
        [SerializeField] int maxPropsPerChunk = 18;
        [SerializeField] int maxAssetBuildingsPerChunk = 96;
        [SerializeField] GameObject streetLampPrefab;
        [SerializeField] GameObject trafficSignalPrefab;
        [SerializeField] GameObject busStopPrefab;
        [SerializeField] GameObject trashCanPrefab;
        [SerializeField] GameObject benchPrefab;
        [SerializeField] GameObject hydrantPrefab;
        [SerializeField] GameObject characterPreviewPrefab;

        CityMapManifest manifest;
        readonly Dictionary<string, CityChunkManifestEntry> manifestById = new Dictionary<string, CityChunkManifestEntry>();
        readonly Dictionary<string, GameObject> loadedChunks = new Dictionary<string, GameObject>();
        readonly List<GameObject> buildingPrefabs = new List<GameObject>();
        GameObject characterPreviewInstance;
        GameObject assetPackPreviewRoot;
        float nextUpdateTime;

        void Start()
        {
            ResolveAssetPackPrefabs();
            EnsureMaterials();
            if (configureEnvironment)
            {
                ConfigureEnvironment();
            }

            if (loadOnStart)
            {
                LoadManifest();
                SpawnAssetPackPreview();
                RefreshLoadedChunks(true);
            }
        }

        void ResolveAssetPackPrefabs()
        {
#if UNITY_EDITOR
            streetLampPrefab = LoadEditorPrefab(StreetLampPath, streetLampPrefab);
            trafficSignalPrefab = LoadEditorPrefab(TrafficSignalPath, trafficSignalPrefab);
            busStopPrefab = LoadEditorPrefab(BusStopPath, busStopPrefab);
            trashCanPrefab = LoadEditorPrefab(TrashCanPath, trashCanPrefab);
            benchPrefab = LoadEditorPrefab(BenchPath, benchPrefab);
            hydrantPrefab = LoadEditorPrefab(HydrantPath, hydrantPrefab);
            characterPreviewPrefab = LoadEditorPrefab(CharacterPreviewPath, characterPreviewPrefab);
            if (useAssetPackMaterials)
            {
                roadMaterial = LoadEditorMaterial(RoadMaterialPath, roadMaterial);
                roadEdgeMarkingMaterial = LoadEditorMaterial(IntersectionMaterialPath, roadEdgeMarkingMaterial);
                sidewalkMaterial = LoadEditorMaterial(SidewalkMaterialPath, sidewalkMaterial);
                walkwayMaterial = LoadEditorMaterial(SidewalkMaterialPath, walkwayMaterial);
                buildingMaterial = LoadEditorMaterial(BuildingMaterialPath, buildingMaterial);
            }

            buildingPrefabs.Clear();
            for (int i = 0; i < BuildingPaths.Length; i++)
            {
                GameObject prefab = LoadEditorPrefab(BuildingPaths[i], null);
                if (prefab != null)
                {
                    buildingPrefabs.Add(prefab);
                }
            }
#endif
        }

#if UNITY_EDITOR
        static GameObject LoadEditorPrefab(string path, GameObject currentValue)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            return prefab != null ? prefab : currentValue;
        }

        static Material LoadEditorMaterial(string path, Material currentValue)
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            return material != null ? material : currentValue;
        }
#endif

        void Update()
        {
            if (manifest == null || Time.time < nextUpdateTime)
            {
                return;
            }

            nextUpdateTime = Time.time + updateIntervalSeconds;
            RefreshLoadedChunks(false);
        }

        public void LoadManifest()
        {
            TextAsset manifestAsset = Resources.Load<TextAsset>($"{resourcesMapRoot}/manifest");
            if (manifestAsset == null)
            {
                Debug.LogError($"City map manifest not found at Resources/{resourcesMapRoot}/manifest.json", this);
                return;
            }

            manifest = JsonUtility.FromJson<CityMapManifest>(manifestAsset.text);
            manifestById.Clear();

            if (manifest.chunks == null)
            {
                Debug.LogError("City map manifest loaded, but it does not contain chunks.", this);
                return;
            }

            foreach (var chunk in manifest.chunks)
            {
                manifestById[chunk.id] = chunk;
            }

            Debug.Log($"Loaded city map manifest from Resources/{resourcesMapRoot}: {manifest.chunks.Length} chunks. Renderer {RendererVersion}.", this);
        }

        public void RefreshLoadedChunks(bool force)
        {
            Vector3 targetPosition = GetTargetPosition();
            var needed = new HashSet<string>();

            foreach (var chunk in manifest.chunks)
            {
                float distance = DistanceToBounds2D(targetPosition, chunk.bounds);
                if (distance <= loadRadiusMeters)
                {
                    needed.Add(chunk.id);
                    if (!loadedChunks.ContainsKey(chunk.id))
                    {
                        LoadChunk(chunk);
                    }
                }
            }

            var unloadIds = new List<string>();
            foreach (var loaded in loadedChunks)
            {
                if (!manifestById.TryGetValue(loaded.Key, out var chunk))
                {
                    unloadIds.Add(loaded.Key);
                    continue;
                }

                float distance = DistanceToBounds2D(targetPosition, chunk.bounds);
                if (!needed.Contains(loaded.Key) && (force || distance > unloadRadiusMeters))
                {
                    unloadIds.Add(loaded.Key);
                }
            }

            foreach (string chunkId in unloadIds)
            {
                UnloadChunk(chunkId);
            }
        }

        void LoadChunk(CityChunkManifestEntry manifestEntry)
        {
            TextAsset chunkAsset = Resources.Load<TextAsset>($"{resourcesMapRoot}/{manifestEntry.resource}");
            if (chunkAsset == null)
            {
                Debug.LogWarning($"City map chunk not found at Resources/{resourcesMapRoot}/{manifestEntry.resource}.json", this);
                return;
            }

            CityChunk chunk = JsonUtility.FromJson<CityChunk>(chunkAsset.text);
            var chunkRoot = new GameObject($"CityChunk_{chunk.id}");
            chunkRoot.transform.SetParent(transform, false);

            if (renderTerrain)
            {
                AddMeshObject(chunkRoot.transform, "Terrain", CityChunkMeshBuilder.BuildTerrainMesh(chunk.terrain), terrainMaterial);
            }
            AddMeshObject(chunkRoot.transform, "Urban Ground", CityChunkMeshBuilder.BuildChunkGroundMesh(chunk.bounds), urbanGroundMaterial);
            AddMeshObject(chunkRoot.transform, "Areas", CityChunkMeshBuilder.BuildAreaMesh(chunk.areas), areaMaterial);
            AddMeshObject(chunkRoot.transform, "Sidewalks", CityChunkMeshBuilder.BuildSidewalkMesh(chunk.streets), sidewalkMaterial);
            AddMeshObject(chunkRoot.transform, "Curbs", CityChunkMeshBuilder.BuildCurbMesh(chunk.streets), sidewalkMaterial);
            AddMeshObject(chunkRoot.transform, "Walkways", CityChunkMeshBuilder.BuildWalkwayMesh(chunk.streets), walkwayMaterial);
            AddMeshObject(chunkRoot.transform, "Roads", CityChunkMeshBuilder.BuildRoadMesh(chunk.streets), roadMaterial);
            AddMeshObject(chunkRoot.transform, "Junctions", CityChunkMeshBuilder.BuildJunctionMesh(chunk.junctions, chunk.streets), roadMaterial);
            AddMeshObject(chunkRoot.transform, "Road Edge Markings", CityChunkMeshBuilder.BuildRoadEdgeMarkingMesh(chunk.streets), roadEdgeMarkingMaterial);
            AddMeshObject(chunkRoot.transform, "Lane Markings", CityChunkMeshBuilder.BuildLaneMarkingMesh(chunk.streets), laneMarkingMaterial);
            AddMeshObject(chunkRoot.transform, "Intersection Markings", CityChunkMeshBuilder.BuildIntersectionMarkingMesh(chunk.streets), roadEdgeMarkingMaterial);
            if (renderProceduralBuildings || !useAssetPackBuildings)
            {
                AddMeshObject(chunkRoot.transform, "OSM Buildings", CityChunkMeshBuilder.BuildBuildingMesh(chunk.buildings), buildingMaterial);
                AddMeshObject(chunkRoot.transform, "OSM Building Windows", CityChunkMeshBuilder.BuildBuildingWindowMesh(chunk.buildings), windowMaterial);
            }

            if (useAssetPackBuildings)
            {
                AddAssetPackBuildingAccents(chunkRoot.transform, chunk.buildings, chunk.id);
            }

            if (spawnPois && chunk.pois != null)
            {
                AddPoiMarkers(chunkRoot.transform, chunk.pois);
            }

            if (useAssetPackProps && chunk.streets != null)
            {
                AddStreetProps(chunkRoot.transform, chunk.streets, chunk.junctions, chunk.pois, chunk.id);
            }

            loadedChunks[chunk.id] = chunkRoot;
            Debug.Log($"Loaded city chunk {chunk.id}: {chunk.counts.streets} streets, {chunk.counts.buildings} buildings, {chunk.counts.areas} areas, {chunk.counts.pois} POIs.", this);
        }

        void AddAssetPackBuildingAccents(Transform parent, CityBuilding[] buildings, string chunkId)
        {
            if (buildings == null)
            {
                return;
            }

            if (buildingPrefabs.Count == 0)
            {
                Debug.LogWarning("Asset-pack buildings are enabled, but no Night Modular City Pack building prefabs resolved. Procedural building fallback is disabled; check prefab paths/imports.", this);
                return;
            }

            int placed = 0;
            for (int i = 0; i < buildings.Length && placed < maxAssetBuildingsPerChunk; i++)
            {
                CityBuilding building = buildings[i];
                if (building.footprint == null || building.footprint.Length < 3)
                {
                    continue;
                }

                Vector3 center = BuildingCenter(building);
                Vector2 size = BuildingFootprintSize(building);
                if (size.x < 4f || size.y < 4f)
                {
                    continue;
                }

                int hash = StableHash($"{chunkId}:{building.id}");
                GameObject prefab = buildingPrefabs[hash % buildingPrefabs.Count];
                Quaternion rotation = BuildingRotation(building, hash);
                GameObject instance = PlacePrefab(parent, prefab, center, rotation, 1f);
                if (instance == null)
                {
                    continue;
                }

                FitToFootprint(instance, size);
                placed++;
            }
        }

        void UnloadChunk(string chunkId)
        {
            if (!loadedChunks.TryGetValue(chunkId, out GameObject chunkRoot))
            {
                return;
            }

            loadedChunks.Remove(chunkId);
            Destroy(chunkRoot);
        }

        void AddMeshObject(Transform parent, string objectName, Mesh mesh, Material material)
        {
            if (mesh == null || mesh.vertexCount == 0)
            {
                return;
            }

            var gameObject = new GameObject(objectName);
            gameObject.transform.SetParent(parent, false);
            gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
            gameObject.AddComponent<MeshRenderer>().sharedMaterial = material;
        }

        void AddPoiMarkers(Transform parent, CityPoi[] pois)
        {
            foreach (var poi in pois)
            {
                if (poi.position == null || IsBusStopPoi(poi) || IsTrafficSignalPoi(poi) || poi.highway == "crossing" || !string.IsNullOrEmpty(poi.public_transport))
                {
                    continue;
                }

                GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                marker.name = string.IsNullOrEmpty(poi.name) ? $"POI_{poi.id}" : $"POI_{poi.name}";
                marker.transform.SetParent(parent, false);
                marker.transform.position = new Vector3(poi.position.x, 2.2f, poi.position.z);
                marker.transform.localScale = Vector3.one * 0.8f;
                marker.GetComponent<Renderer>().sharedMaterial = poiMaterial;
            }
        }

        void AddStreetProps(Transform parent, CityStreet[] streets, CityJunction[] junctions, CityPoi[] pois, string chunkId)
        {
            int placed = 0;
            AddIntersectionProps(parent, junctions, chunkId, ref placed);
            AddSemanticPoiProps(parent, pois, streets, chunkId, ref placed);

            for (int i = 0; i < streets.Length && placed < maxPropsPerChunk; i++)
            {
                CityStreet street = streets[i];
                if (!IsPropStreet(street) || street.centerline == null || street.centerline.Length < 2)
                {
                    continue;
                }

                CityPoint startPoint = street.centerline[0];
                CityPoint endPoint = street.centerline[1];
                Vector3 start = new Vector3(startPoint.x, 0f, startPoint.z);
                Vector3 end = new Vector3(endPoint.x, 0f, endPoint.z);
                Vector3 direction = end - start;
                float length = direction.magnitude;
                if (length < 12f)
                {
                    continue;
                }

                direction /= length;
                Vector3 right = Vector3.Cross(Vector3.up, direction).normalized;
                int hash = StableHash($"{chunkId}:{street.id}");
                float side = hash % 2 == 0 ? 1f : -1f;
                Vector3 sideVector = right * side;
                Vector3 basePosition = Vector3.Lerp(start, end, 0.5f);
                float roadOffset = RoadWidth(street) * 0.5f + 2.9f;
                Quaternion rotation = Quaternion.LookRotation(direction, Vector3.up);

                if (streetLampPrefab != null && hash % 4 == 0)
                {
                    PlacePrefab(parent, streetLampPrefab, basePosition + sideVector * roadOffset, rotation, 1f);
                    placed++;
                }

                if (benchPrefab != null && hash % 13 == 0 && placed < maxPropsPerChunk)
                {
                    PlacePrefab(parent, benchPrefab, Vector3.Lerp(start, end, 0.65f) + sideVector * (roadOffset + 0.4f), rotation, 1f);
                    placed++;
                }

                if (trashCanPrefab != null && hash % 17 == 0 && placed < maxPropsPerChunk)
                {
                    PlacePrefab(parent, trashCanPrefab, Vector3.Lerp(start, end, 0.25f) + sideVector * (roadOffset + 0.2f), rotation, 1f);
                    placed++;
                }

                if (hydrantPrefab != null && hash % 19 == 0 && placed < maxPropsPerChunk)
                {
                    PlacePrefab(parent, hydrantPrefab, Vector3.Lerp(start, end, 0.75f) - sideVector * (roadOffset + 0.2f), rotation, 1f);
                    placed++;
                }
            }
        }

        void AddIntersectionProps(Transform parent, CityJunction[] junctions, string chunkId, ref int placed)
        {
            if (junctions == null)
            {
                return;
            }

            for (int i = 0; i < junctions.Length && placed < maxPropsPerChunk; i++)
            {
                CityJunction junction = junctions[i];
                if (junction == null || junction.position == null)
                {
                    continue;
                }

                int hash = StableHash($"{chunkId}:junction:{junction.id}");
                bool major = HasMajorHighway(junction);
                Vector3 center = new Vector3(junction.position.x, 0f, junction.position.z);
                Vector3 diagonal = new Vector3(hash % 2 == 0 ? 1f : -1f, 0f, hash % 3 == 0 ? 1f : -1f).normalized;
                float offset = Mathf.Max(6.8f, junction.radius_m + 4.2f);

                if (streetLampPrefab != null)
                {
                    PlacePrefab(parent, streetLampPrefab, center + diagonal * offset, Quaternion.identity, 1f);
                    placed++;
                }

                if (major && trafficSignalPrefab != null && placed < maxPropsPerChunk)
                {
                    PlacePrefab(parent, trafficSignalPrefab, center - diagonal * offset, Quaternion.identity, 1f);
                    placed++;
                }
            }
        }

        void AddSemanticPoiProps(Transform parent, CityPoi[] pois, CityStreet[] streets, string chunkId, ref int placed)
        {
            if (pois == null)
            {
                return;
            }

            for (int i = 0; i < pois.Length && placed < maxPropsPerChunk; i++)
            {
                CityPoi poi = pois[i];
                if (poi == null || poi.position == null)
                {
                    continue;
                }

                Vector3 position = new Vector3(poi.position.x, 0f, poi.position.z);
                Quaternion rotation = Quaternion.identity;
                Vector3 adjusted = OffsetPoiToNearestSidewalk(position, streets, out rotation);

                if (IsBusStopPoi(poi) && busStopPrefab != null)
                {
                    PlacePrefab(parent, busStopPrefab, adjusted, rotation, 1f);
                    placed++;
                }
                else if (IsTrafficSignalPoi(poi) && trafficSignalPrefab != null)
                {
                    PlacePrefab(parent, trafficSignalPrefab, adjusted, rotation, 1f);
                    placed++;
                }
            }
        }

        GameObject PlacePrefab(Transform parent, GameObject prefab, Vector3 position, Quaternion rotation, float scale)
        {
            if (prefab == null)
            {
                return null;
            }

            GameObject instance;
            try
            {
                instance = Instantiate(prefab, parent);
            }
            catch (System.InvalidCastException error)
            {
                Debug.LogWarning($"Could not instantiate asset-pack prefab {prefab.name}: {error.Message}", this);
                return null;
            }

            instance.transform.position = position;
            instance.transform.rotation = rotation;
            instance.transform.localScale = Vector3.one * scale;
            return instance;
        }

        static Vector3 OffsetPoiToNearestSidewalk(Vector3 position, CityStreet[] streets, out Quaternion rotation)
        {
            rotation = Quaternion.identity;
            if (streets == null)
            {
                return position;
            }

            float bestDistance = float.MaxValue;
            Vector3 bestPosition = position;
            Vector3 bestDirection = Vector3.forward;

            for (int i = 0; i < streets.Length; i++)
            {
                CityStreet street = streets[i];
                if (!IsPropStreet(street) || street.centerline == null || street.centerline.Length < 2)
                {
                    continue;
                }

                for (int p = 0; p < street.centerline.Length - 1; p++)
                {
                    Vector3 a = new Vector3(street.centerline[p].x, 0f, street.centerline[p].z);
                    Vector3 b = new Vector3(street.centerline[p + 1].x, 0f, street.centerline[p + 1].z);
                    Vector3 closest = ClosestPointOnSegment(position, a, b);
                    float distance = Vector3.Distance(position, closest);
                    if (distance >= bestDistance)
                    {
                        continue;
                    }

                    Vector3 direction = b - a;
                    direction.y = 0f;
                    if (direction.sqrMagnitude < 0.01f)
                    {
                        continue;
                    }

                    direction.Normalize();
                    Vector3 right = Vector3.Cross(Vector3.up, direction).normalized;
                    float side = Vector3.Dot(position - closest, right) >= 0f ? 1f : -1f;
                    float offset = RoadWidth(street) * 0.5f + 2.8f;
                    bestDistance = distance;
                    bestPosition = closest + right * side * offset;
                    bestDirection = direction;
                }
            }

            rotation = Quaternion.LookRotation(bestDirection, Vector3.up);
            return bestPosition;
        }

        static Vector3 ClosestPointOnSegment(Vector3 point, Vector3 a, Vector3 b)
        {
            Vector3 segment = b - a;
            float lengthSquared = segment.sqrMagnitude;
            if (lengthSquared < 0.001f)
            {
                return a;
            }

            float t = Mathf.Clamp01(Vector3.Dot(point - a, segment) / lengthSquared);
            return a + segment * t;
        }

        void SpawnAssetPackPreview()
        {
            if (!spawnAssetPackPreview || assetPackPreviewRoot != null)
            {
                return;
            }

            Transform target = streamingTarget != null ? streamingTarget : (Camera.main != null ? Camera.main.transform : null);
            Vector3 origin = target != null ? target.position : Vector3.zero;
            Vector3 forward = target != null ? target.forward : Vector3.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.01f)
            {
                forward = Vector3.forward;
            }
            forward.Normalize();
            Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
            Vector3 basePosition = origin + forward * 12f;
            basePosition.y = 0f;

            assetPackPreviewRoot = new GameObject("Asset Pack Preview");
            assetPackPreviewRoot.transform.position = basePosition;

            if (characterPreviewPrefab != null)
            {
                characterPreviewInstance = PlacePrefab(
                    assetPackPreviewRoot.transform,
                    characterPreviewPrefab,
                    basePosition + right * 2f,
                    Quaternion.LookRotation(-forward, Vector3.up),
                    1.25f
                );
                if (characterPreviewInstance != null)
                {
                    characterPreviewInstance.name = "Street Gangs Character Preview";
                }
            }

            PlacePrefab(assetPackPreviewRoot.transform, streetLampPrefab, basePosition - right * 4f, Quaternion.identity, 1f);
            PlacePrefab(assetPackPreviewRoot.transform, trafficSignalPrefab, basePosition - right * 2f, Quaternion.identity, 1f);
            PlacePrefab(assetPackPreviewRoot.transform, busStopPrefab, basePosition + right * 4f, Quaternion.LookRotation(forward, Vector3.up), 1f);
            PlacePrefab(assetPackPreviewRoot.transform, benchPrefab, basePosition + right * 6f, Quaternion.LookRotation(forward, Vector3.up), 1f);

            if (buildingPrefabs.Count > 0)
            {
                GameObject building = PlacePrefab(
                    assetPackPreviewRoot.transform,
                    buildingPrefabs[0],
                    basePosition + forward * 14f,
                    Quaternion.LookRotation(forward, Vector3.up),
                    1f
                );
                if (building != null)
                {
                    FitToFootprint(building, new Vector2(18f, 18f));
                    building.name = "Night City Building Preview";
                }
            }
        }

        Vector3 GetTargetPosition()
        {
            if (streamingTarget != null)
            {
                return streamingTarget.position;
            }

            Camera mainCamera = Camera.main;
            return mainCamera != null ? mainCamera.transform.position : Vector3.zero;
        }

        static float DistanceToBounds2D(Vector3 position, CityChunkBounds bounds)
        {
            float dx = Mathf.Max(bounds.min_x - position.x, 0f, position.x - bounds.max_x);
            float dz = Mathf.Max(bounds.min_z - position.z, 0f, position.z - bounds.max_z);
            return Mathf.Sqrt(dx * dx + dz * dz);
        }

        static Vector3 BuildingCenter(CityBuilding building)
        {
            Vector3 sum = Vector3.zero;
            int count = 0;
            for (int i = 0; i < building.footprint.Length; i++)
            {
                CityPoint point = building.footprint[i];
                sum += new Vector3(point.x, 0f, point.z);
                count++;
            }

            return count > 0 ? sum / count : Vector3.zero;
        }

        static Vector2 BuildingFootprintSize(CityBuilding building)
        {
            float minX = float.MaxValue;
            float maxX = float.MinValue;
            float minZ = float.MaxValue;
            float maxZ = float.MinValue;
            for (int i = 0; i < building.footprint.Length; i++)
            {
                CityPoint point = building.footprint[i];
                minX = Mathf.Min(minX, point.x);
                maxX = Mathf.Max(maxX, point.x);
                minZ = Mathf.Min(minZ, point.z);
                maxZ = Mathf.Max(maxZ, point.z);
            }

            return new Vector2(maxX - minX, maxZ - minZ);
        }

        static Quaternion BuildingRotation(CityBuilding building, int hash)
        {
            Vector3 bestDirection = Vector3.zero;
            float bestLength = 0f;
            int count = building.footprint != null ? building.footprint.Length : 0;
            for (int i = 0; i < count - 1; i++)
            {
                CityPoint current = building.footprint[i];
                CityPoint next = building.footprint[i + 1];
                Vector3 direction = new Vector3(next.x - current.x, 0f, next.z - current.z);
                float length = direction.sqrMagnitude;
                if (length > bestLength)
                {
                    bestLength = length;
                    bestDirection = direction;
                }
            }

            if (bestDirection.sqrMagnitude < 0.01f)
            {
                return Quaternion.Euler(0f, (hash % 4) * 90f, 0f);
            }

            bestDirection.Normalize();
            return Quaternion.LookRotation(bestDirection, Vector3.up);
        }

        static void FitToFootprint(GameObject instance, Vector2 footprintSize)
        {
            Renderer[] renderers = instance.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
            {
                return;
            }

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }

            if (bounds.size.x <= 0.01f || bounds.size.z <= 0.01f)
            {
                return;
            }

            float scale = Mathf.Min(footprintSize.x / bounds.size.x, footprintSize.y / bounds.size.z);
            scale = Mathf.Clamp(scale, 0.12f, 3.5f);
            instance.transform.localScale *= scale;

            renderers = instance.GetComponentsInChildren<Renderer>();
            bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }

            Vector3 delta = instance.transform.position - bounds.center;
            delta.y = -bounds.min.y;
            instance.transform.position += delta;
        }

        static bool IsPropStreet(CityStreet street)
        {
            switch (street.highway)
            {
                case "primary":
                case "secondary":
                case "tertiary":
                case "residential":
                case "unclassified":
                    return true;
                default:
                    return false;
            }
        }

        static bool IsMajorRoad(CityStreet street)
        {
            return street.highway == "primary" || street.highway == "secondary" || street.highway == "tertiary";
        }

        static bool HasMajorHighway(CityJunction junction)
        {
            if (junction.highways == null)
            {
                return false;
            }

            for (int i = 0; i < junction.highways.Length; i++)
            {
                string highway = junction.highways[i];
                if (highway == "primary" || highway == "secondary" || highway == "tertiary")
                {
                    return true;
                }
            }

            return false;
        }

        static bool IsBusStopPoi(CityPoi poi)
        {
            return poi.highway == "bus_stop" || poi.public_transport == "platform" || poi.public_transport == "stop_position" || poi.bus == "yes";
        }

        static bool IsTrafficSignalPoi(CityPoi poi)
        {
            return poi.highway == "traffic_signals";
        }

        static float RoadWidth(CityStreet street)
        {
            switch (street.highway)
            {
                case "primary":
                    return Mathf.Max(8.5f, street.estimated_road_width_m);
                case "secondary":
                case "tertiary":
                    return Mathf.Max(6.6f, street.estimated_road_width_m);
                case "residential":
                case "unclassified":
                    return Mathf.Clamp(street.estimated_road_width_m, 5.2f, 7.4f);
                default:
                    return Mathf.Max(3.5f, street.estimated_road_width_m);
            }
        }

        static int StableHash(string value)
        {
            unchecked
            {
                int hash = 17;
                if (value == null)
                {
                    return hash;
                }

                for (int i = 0; i < value.Length; i++)
                {
                    hash = hash * 31 + value[i];
                }

                return Mathf.Abs(hash);
            }
        }

        void EnsureMaterials()
        {
            if (terrainMaterial == null)
            {
                terrainMaterial = NewMaterial("Gangland Terrain", new Color(0.42f, 0.34f, 0.23f), 0.25f, 0.08f);
            }

            if (urbanGroundMaterial == null)
            {
                urbanGroundMaterial = NewMaterial("Gangland Urban Ground", new Color(0.40f, 0.39f, 0.36f), 0.35f, 0.06f);
            }

            if (roadMaterial == null)
            {
                roadMaterial = NewMaterial("Gangland Roads", new Color(0.16f, 0.16f, 0.15f), 0.24f, 0.035f);
            }

            if (sidewalkMaterial == null)
            {
                sidewalkMaterial = NewMaterial("Gangland Sidewalks", new Color(0.58f, 0.56f, 0.50f), 0.4f, 0.05f);
            }

            if (walkwayMaterial == null)
            {
                walkwayMaterial = NewMaterial("Gangland Walkways", new Color(0.46f, 0.45f, 0.4f), 0.45f, 0.05f);
            }

            if (laneMarkingMaterial == null)
            {
                laneMarkingMaterial = NewMaterial("Gangland Lane Markings", new Color(0.92f, 0.78f, 0.28f), 0.2f, 0.12f);
            }

            if (roadEdgeMarkingMaterial == null)
            {
                roadEdgeMarkingMaterial = NewMaterial("Gangland White Road Markings", new Color(0.82f, 0.8f, 0.72f), 0.24f, 0.08f);
            }

            if (buildingMaterial == null)
            {
                buildingMaterial = NewMaterial("Gangland Buildings", new Color(0.56f, 0.52f, 0.45f), 0.45f, 0.12f);
            }

            if (windowMaterial == null)
            {
                windowMaterial = NewMaterial("Gangland Windows", new Color(0.055f, 0.08f, 0.095f), 0.65f, 0.38f);
            }

            if (areaMaterial == null)
            {
                areaMaterial = NewMaterial("Gangland Areas", new Color(0.28f, 0.43f, 0.22f), 0.3f, 0.05f);
            }

            if (poiMaterial == null)
            {
                poiMaterial = NewMaterial("Gangland POIs", new Color(0.95f, 0.72f, 0.22f), 0.25f, 0.35f);
            }
        }

        static void ConfigureEnvironment()
        {
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.42f, 0.49f, 0.56f);
            RenderSettings.ambientEquatorColor = new Color(0.34f, 0.34f, 0.31f);
            RenderSettings.ambientGroundColor = new Color(0.18f, 0.17f, 0.15f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(0.43f, 0.48f, 0.52f);
            RenderSettings.fogStartDistance = 240f;
            RenderSettings.fogEndDistance = 1100f;

            Camera mainCamera = Camera.main;
            if (mainCamera != null)
            {
                mainCamera.clearFlags = CameraClearFlags.SolidColor;
                mainCamera.backgroundColor = new Color(0.43f, 0.52f, 0.60f);
                mainCamera.farClipPlane = Mathf.Max(mainCamera.farClipPlane, 1400f);
            }

            Light sun = Object.FindAnyObjectByType<Light>();
            if (sun != null && sun.type == LightType.Directional)
            {
                sun.color = new Color(1f, 0.94f, 0.84f);
                sun.intensity = 0.85f;
                sun.transform.rotation = Quaternion.Euler(42f, -35f, 0f);
            }
        }

        static Material NewMaterial(string name, Color color, float roughness, float metallic)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Simple Lit");
            if (shader == null)
            {
                shader = Shader.Find("Universal Render Pipeline/Lit");
            }
            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }

            var material = new Material(shader) { name = name };
            material.color = color;
            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }
            if (material.HasProperty("_Smoothness"))
            {
                material.SetFloat("_Smoothness", Mathf.Clamp01(1f - roughness));
            }
            if (material.HasProperty("_Metallic"))
            {
                material.SetFloat("_Metallic", Mathf.Clamp01(metallic));
            }
            return material;
        }
    }
}
