using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Gangland.CityStreaming
{
    public static class CityChunkMeshBuilder
    {
        const float DefaultBuildingHeight = 6.4f;
        const float MinimumRoadWidth = 2.5f;
        const float TerrainVerticalScale = 0.08f;
        const float TerrainYBias = -5.8f;
        const float RoadY = 0.04f;
        const float JunctionY = RoadY;
        const float CurbY = 0.105f;
        const float SidewalkY = 0.075f;
        const float LaneMarkingY = 0.1f;
        const float RoadEdgeMarkingY = 0.115f;
        const float CrosswalkY = 0.13f;
        const float WalkwayY = 0.085f;
        const float AreaY = 0.01f;
        const float SidewalkWidth = 2.2f;
        const float LaneMarkingWidth = 0.18f;
        const float EdgeLineWidth = 0.12f;
        const float StopBarWidth = 0.35f;
        const float CrosswalkStripeWidth = 0.45f;
        const float LaneDashLength = 3.2f;
        const float LaneDashGap = 6.4f;
        const float WalkwayWidth = 2.4f;
        const float CurbWidth = 0.28f;
        const float GroundY = -0.02f;

        public static Mesh BuildChunkGroundMesh(CityChunkBounds bounds)
        {
            var vertices = new List<Vector3>();
            var triangles = new List<int>();

            if (bounds == null)
            {
                return CreateMesh("UrbanGround", vertices, triangles);
            }

            float padding = 1f;
            int start = vertices.Count;
            vertices.Add(new Vector3(bounds.min_x - padding, GroundY, bounds.min_z - padding));
            vertices.Add(new Vector3(bounds.max_x + padding, GroundY, bounds.min_z - padding));
            vertices.Add(new Vector3(bounds.max_x + padding, GroundY, bounds.max_z + padding));
            vertices.Add(new Vector3(bounds.min_x - padding, GroundY, bounds.max_z + padding));
            AddUpwardQuad(vertices, triangles, start, start + 1, start + 2, start + 3);
            return CreateMesh("UrbanGround", vertices, triangles);
        }

        public static Mesh BuildTerrainMesh(CityTerrain terrain)
        {
            var vertices = new List<Vector3>();
            var triangles = new List<int>();

            if (terrain == null || terrain.points == null || terrain.columns < 2 || terrain.rows < 2)
            {
                return CreateMesh("Terrain", vertices, triangles);
            }

            for (int i = 0; i < terrain.points.Length; i++)
            {
                CityTerrainPoint point = terrain.points[i];
                vertices.Add(new Vector3(point.x, TerrainYBias + point.relative_y_m * TerrainVerticalScale, point.z));
            }

            for (int row = 0; row < terrain.rows - 1; row++)
            {
                for (int column = 0; column < terrain.columns - 1; column++)
                {
                    int a = row * terrain.columns + column;
                    int b = a + 1;
                    int c = a + terrain.columns;
                    int d = c + 1;

                    if (d >= vertices.Count)
                    {
                        continue;
                    }

                    triangles.Add(a);
                    triangles.Add(c);
                    triangles.Add(b);
                    triangles.Add(b);
                    triangles.Add(c);
                    triangles.Add(d);
                }
            }

            return CreateMesh("Terrain", vertices, triangles);
        }

        public static Mesh BuildRoadMesh(CityStreet[] streets)
        {
            var vertices = new List<Vector3>();
            var triangles = new List<int>();

            if (streets == null)
            {
                return CreateMesh("Roads", vertices, triangles);
            }

            foreach (var street in streets)
            {
                if (!IsDrivableRoad(street) || street.centerline == null || street.centerline.Length < 2)
                {
                    continue;
                }

                AddPolylineRibbon(vertices, triangles, TrimmedPolyline(street, RoadY), RoadWidth(street) * 0.5f);
            }

            return CreateMesh("Roads", vertices, triangles);
        }

        public static Mesh BuildJunctionMesh(CityJunction[] junctions, CityStreet[] streets)
        {
            var vertices = new List<Vector3>();
            var triangles = new List<int>();

            if (junctions == null || streets == null)
            {
                return CreateMesh("Junctions", vertices, triangles);
            }

            foreach (var junction in junctions)
            {
                if (junction == null || junction.position == null)
                {
                    continue;
                }

                AddJunctionConnectors(vertices, triangles, junction, streets);
            }

            return CreateMesh("Junctions", vertices, triangles);
        }

        public static Mesh BuildSidewalkMesh(CityStreet[] streets)
        {
            var vertices = new List<Vector3>();
            var triangles = new List<int>();

            if (streets == null)
            {
                return CreateMesh("Sidewalks", vertices, triangles);
            }

            foreach (var street in streets)
            {
                if (!HasSidewalks(street) || street.centerline == null || street.centerline.Length < 2)
                {
                    continue;
                }

                List<Vector3> points = TrimmedPolyline(street, SidewalkY);
                float sidewalkWidth = Mathf.Max(0.25f, SidewalkWidth - 0.25f);
                float centerOffset = RoadWidth(street) * 0.5f + 0.25f + sidewalkWidth * 0.5f;
                AddOffsetPolylineRibbon(vertices, triangles, points, centerOffset, sidewalkWidth * 0.5f);
                AddOffsetPolylineRibbon(vertices, triangles, points, -centerOffset, sidewalkWidth * 0.5f);
            }

            return CreateMesh("Sidewalks", vertices, triangles);
        }

        public static Mesh BuildCurbMesh(CityStreet[] streets)
        {
            var vertices = new List<Vector3>();
            var triangles = new List<int>();

            if (streets == null)
            {
                return CreateMesh("Curbs", vertices, triangles);
            }

            foreach (var street in streets)
            {
                if (!HasSidewalks(street) || street.centerline == null || street.centerline.Length < 2)
                {
                    continue;
                }

                List<Vector3> points = TrimmedPolyline(street, CurbY);
                float curbCenterOffset = RoadWidth(street) * 0.5f;
                AddOffsetPolylineRibbon(vertices, triangles, points, curbCenterOffset, CurbWidth);
                AddOffsetPolylineRibbon(vertices, triangles, points, -curbCenterOffset, CurbWidth);
            }

            return CreateMesh("Curbs", vertices, triangles);
        }

        public static Mesh BuildWalkwayMesh(CityStreet[] streets)
        {
            var vertices = new List<Vector3>();
            var triangles = new List<int>();

            if (streets == null)
            {
                return CreateMesh("Walkways", vertices, triangles);
            }

            foreach (var street in streets)
            {
                if (!IsWalkway(street) || street.centerline == null || street.centerline.Length < 2)
                {
                    continue;
                }

                for (int i = 0; i < street.centerline.Length - 1; i++)
                {
                    Vector3 a = ToVector(street.centerline[i], WalkwayY);
                    Vector3 b = ToVector(street.centerline[i + 1], WalkwayY);
                    Vector3 direction = b - a;
                    if (direction.sqrMagnitude < 0.001f)
                    {
                        continue;
                    }

                    Vector3 right = Vector3.Cross(Vector3.up, direction.normalized) * (WalkwayWidth * 0.5f);
                    int start = vertices.Count;
                    vertices.Add(a - right);
                    vertices.Add(a + right);
                    vertices.Add(b + right);
                    vertices.Add(b - right);
                    AddUpwardQuad(vertices, triangles, start, start + 1, start + 2, start + 3);
                }
            }

            return CreateMesh("Walkways", vertices, triangles);
        }

        public static Mesh BuildLaneMarkingMesh(CityStreet[] streets)
        {
            var vertices = new List<Vector3>();
            var triangles = new List<int>();

            if (streets == null)
            {
                return CreateMesh("LaneMarkings", vertices, triangles);
            }

            foreach (var street in streets)
            {
                if (!HasLaneMarkings(street) || street.centerline == null || street.centerline.Length < 2)
                {
                    continue;
                }

                List<Vector3> points = TrimmedPolyline(street, LaneMarkingY);
                for (int i = 0; i < points.Count - 1; i++)
                {
                    AddDashedLine(vertices, triangles, points[i], points[i + 1]);
                }
            }

            return CreateMesh("LaneMarkings", vertices, triangles);
        }

        public static Mesh BuildRoadEdgeMarkingMesh(CityStreet[] streets)
        {
            var vertices = new List<Vector3>();
            var triangles = new List<int>();

            if (streets == null)
            {
                return CreateMesh("RoadEdgeMarkings", vertices, triangles);
            }

            foreach (var street in streets)
            {
                if (!IsDrivableRoad(street) || street.centerline == null || street.centerline.Length < 2)
                {
                    continue;
                }

                List<Vector3> points = TrimmedPolyline(street, RoadEdgeMarkingY);
                float edgeCenterOffset = RoadWidth(street) * 0.5f - EdgeLineWidth * 0.5f;
                AddOffsetPolylineRibbon(vertices, triangles, points, edgeCenterOffset, EdgeLineWidth * 0.5f);
                AddOffsetPolylineRibbon(vertices, triangles, points, -edgeCenterOffset, EdgeLineWidth * 0.5f);
            }

            return CreateMesh("RoadEdgeMarkings", vertices, triangles);
        }

        public static Mesh BuildIntersectionMarkingMesh(CityStreet[] streets)
        {
            var vertices = new List<Vector3>();
            var triangles = new List<int>();

            if (streets == null)
            {
                return CreateMesh("IntersectionMarkings", vertices, triangles);
            }

            foreach (var street in streets)
            {
                if (street.centerline == null || street.centerline.Length < 2)
                {
                    continue;
                }

                if (IsCrossing(street))
                {
                    AddCrosswalk(vertices, triangles, street);
                }
                else if (IsMajorRoad(street))
                {
                    AddStopBars(vertices, triangles, street);
                }
            }

            return CreateMesh("IntersectionMarkings", vertices, triangles);
        }

        public static Mesh BuildAreaMesh(CityArea[] areas)
        {
            var vertices = new List<Vector3>();
            var triangles = new List<int>();

            if (areas == null)
            {
                return CreateMesh("Areas", vertices, triangles);
            }

            foreach (var area in areas)
            {
                AddFlatPolygon(area.footprint, AreaY, vertices, triangles);
            }

            return CreateMesh("Areas", vertices, triangles);
        }

        public static Mesh BuildBuildingMesh(CityBuilding[] buildings)
        {
            var vertices = new List<Vector3>();
            var triangles = new List<int>();

            if (buildings == null)
            {
                return CreateMesh("Buildings", vertices, triangles);
            }

            foreach (var building in buildings)
            {
                AddBuilding(building, vertices, triangles);
            }

            return CreateMesh("Buildings", vertices, triangles);
        }

        public static Mesh BuildBuildingWindowMesh(CityBuilding[] buildings)
        {
            var vertices = new List<Vector3>();
            var triangles = new List<int>();

            if (buildings == null)
            {
                return CreateMesh("BuildingWindows", vertices, triangles);
            }

            foreach (var building in buildings)
            {
                AddBuildingWindows(building, vertices, triangles);
            }

            return CreateMesh("BuildingWindows", vertices, triangles);
        }

        static List<Vector3> TrimmedPolyline(CityStreet street, float y)
        {
            var points = new List<Vector3>();
            if (street == null || street.centerline == null)
            {
                return points;
            }

            for (int i = 0; i < street.centerline.Length; i++)
            {
                points.Add(ToVector(street.centerline[i], y));
            }

            TrimPolylineStart(points, Mathf.Max(0f, street.trim_start_m));
            TrimPolylineEnd(points, Mathf.Max(0f, street.trim_end_m));
            return points;
        }

        static void TrimPolylineStart(List<Vector3> points, float trimMeters)
        {
            if (trimMeters <= 0.01f || points.Count < 2)
            {
                return;
            }

            float remaining = trimMeters;
            while (points.Count >= 2)
            {
                Vector3 a = points[0];
                Vector3 b = points[1];
                float length = Vector3.Distance(a, b);
                if (length <= 0.001f)
                {
                    points.RemoveAt(0);
                    continue;
                }

                if (remaining < length)
                {
                    points[0] = Vector3.Lerp(a, b, remaining / length);
                    return;
                }

                points.RemoveAt(0);
                remaining -= length;
            }
        }

        static void TrimPolylineEnd(List<Vector3> points, float trimMeters)
        {
            if (trimMeters <= 0.01f || points.Count < 2)
            {
                return;
            }

            float remaining = trimMeters;
            while (points.Count >= 2)
            {
                int last = points.Count - 1;
                Vector3 a = points[last];
                Vector3 b = points[last - 1];
                float length = Vector3.Distance(a, b);
                if (length <= 0.001f)
                {
                    points.RemoveAt(last);
                    continue;
                }

                if (remaining < length)
                {
                    points[last] = Vector3.Lerp(a, b, remaining / length);
                    return;
                }

                points.RemoveAt(last);
                remaining -= length;
            }
        }

        static void AddPolylineRibbon(List<Vector3> vertices, List<int> triangles, List<Vector3> points, float halfWidth)
        {
            if (points == null || points.Count < 2)
            {
                return;
            }

            int start = vertices.Count;
            for (int i = 0; i < points.Count; i++)
            {
                Vector3 offset = PolylineOffset(points, i, halfWidth);
                vertices.Add(points[i] - offset);
                vertices.Add(points[i] + offset);
            }

            for (int i = 0; i < points.Count - 1; i++)
            {
                int index = start + i * 2;
                AddUpwardQuad(vertices, triangles, index, index + 1, index + 3, index + 2);
            }
        }

        static void AddOffsetPolylineRibbon(List<Vector3> vertices, List<int> triangles, List<Vector3> points, float centerOffset, float halfWidth)
        {
            if (points == null || points.Count < 2 || halfWidth <= 0.001f)
            {
                return;
            }

            var offsetPoints = new List<Vector3>(points.Count);
            for (int i = 0; i < points.Count; i++)
            {
                offsetPoints.Add(points[i] + PolylineOffset(points, i, centerOffset));
            }

            AddPolylineRibbon(vertices, triangles, offsetPoints, halfWidth);
        }

        static void AddJunctionConnectors(List<Vector3> vertices, List<int> triangles, CityJunction junction, CityStreet[] streets)
        {
            Vector3 center = ToVector(junction.position, JunctionY);
            float maxDistance = Mathf.Max(8f, junction.radius_m + 3.5f);
            var edgePoints = new List<Vector3>();

            for (int i = 0; i < streets.Length; i++)
            {
                CityStreet street = streets[i];
                if (!IsDrivableRoad(street) || street.centerline == null || street.centerline.Length < 2)
                {
                    continue;
                }

                float halfWidth = RoadWidth(street) * 0.5f;
                List<Vector3> trimmedPoints = TrimmedPolyline(street, JunctionY);
                if (trimmedPoints.Count < 2)
                {
                    continue;
                }

                if (street.start_node_id == junction.id)
                {
                    AddJunctionEdgePoints(edgePoints, center, trimmedPoints[0], halfWidth, maxDistance);
                }

                if (street.end_node_id == junction.id)
                {
                    AddJunctionEdgePoints(edgePoints, center, trimmedPoints[trimmedPoints.Count - 1], halfWidth, maxDistance);
                }
            }

            AddSortedJunctionPolygon(vertices, triangles, center, edgePoints);
        }

        static void AddJunctionEdgePoints(List<Vector3> edgePoints, Vector3 center, Vector3 endpoint, float halfWidth, float maxDistance)
        {
            Vector3 direction = endpoint - center;
            direction.y = 0f;
            float distance = direction.magnitude;
            if (distance < 0.2f || distance > maxDistance)
            {
                return;
            }

            direction /= distance;
            Vector3 right = Vector3.Cross(Vector3.up, direction).normalized * halfWidth;
            AddUniqueJunctionPoint(edgePoints, endpoint - right);
            AddUniqueJunctionPoint(edgePoints, endpoint + right);
        }

        static void AddUniqueJunctionPoint(List<Vector3> points, Vector3 point)
        {
            for (int i = 0; i < points.Count; i++)
            {
                Vector3 delta = points[i] - point;
                delta.y = 0f;
                if (delta.sqrMagnitude < 0.04f)
                {
                    return;
                }
            }

            points.Add(point);
        }

        static void AddSortedJunctionPolygon(List<Vector3> vertices, List<int> triangles, Vector3 center, List<Vector3> edgePoints)
        {
            if (edgePoints.Count < 3)
            {
                return;
            }

            edgePoints.Sort((a, b) =>
            {
                float angleA = Mathf.Atan2(a.z - center.z, a.x - center.x);
                float angleB = Mathf.Atan2(b.z - center.z, b.x - center.x);
                return angleA.CompareTo(angleB);
            });

            int start = vertices.Count;
            vertices.Add(center);
            for (int i = 0; i < edgePoints.Count; i++)
            {
                vertices.Add(edgePoints[i]);
            }

            for (int i = 0; i < edgePoints.Count; i++)
            {
                int a = start;
                int b = start + 1 + i;
                int c = start + 1 + ((i + 1) % edgePoints.Count);
                AddUpwardTriangle(vertices, triangles, a, b, c);
            }
        }

        static Vector3 PolylineOffset(List<Vector3> points, int index, float halfWidth)
        {
            Vector3 previousDirection = Vector3.zero;
            Vector3 nextDirection = Vector3.zero;

            if (index > 0)
            {
                previousDirection = FlatDirection(points[index - 1], points[index]);
            }

            if (index < points.Count - 1)
            {
                nextDirection = FlatDirection(points[index], points[index + 1]);
            }

            if (previousDirection == Vector3.zero)
            {
                return Vector3.Cross(Vector3.up, nextDirection).normalized * halfWidth;
            }

            if (nextDirection == Vector3.zero)
            {
                return Vector3.Cross(Vector3.up, previousDirection).normalized * halfWidth;
            }

            Vector3 previousRight = Vector3.Cross(Vector3.up, previousDirection).normalized;
            Vector3 nextRight = Vector3.Cross(Vector3.up, nextDirection).normalized;
            Vector3 miter = previousRight + nextRight;
            if (miter.sqrMagnitude < 0.001f)
            {
                return nextRight * halfWidth;
            }

            miter.Normalize();
            float denominator = Mathf.Abs(Vector3.Dot(miter, nextRight));
            float scale = denominator > 0.2f ? halfWidth / denominator : halfWidth;
            return miter * Mathf.Min(scale, halfWidth * 2.2f);
        }

        static Vector3 FlatDirection(Vector3 a, Vector3 b)
        {
            Vector3 direction = b - a;
            direction.y = 0f;
            return direction.sqrMagnitude > 0.001f ? direction.normalized : Vector3.zero;
        }

        static void AddStrip(List<Vector3> vertices, List<int> triangles, Vector3 a, Vector3 b, Vector3 side, float innerOffset, float outerOffset)
        {
            int start = vertices.Count;
            vertices.Add(a + side * innerOffset);
            vertices.Add(a + side * outerOffset);
            vertices.Add(b + side * outerOffset);
            vertices.Add(b + side * innerOffset);
            AddUpwardQuad(vertices, triangles, start, start + 1, start + 2, start + 3);
        }

        static void TrimSegment(ref Vector3 a, ref Vector3 b, float trimMeters)
        {
            Vector3 direction = b - a;
            float length = direction.magnitude;
            if (length <= trimMeters * 2f + 0.1f)
            {
                return;
            }

            Vector3 offset = direction / length * trimMeters;
            a += offset;
            b -= offset;
        }

        static void AddDashedLine(List<Vector3> vertices, List<int> triangles, Vector3 a, Vector3 b)
        {
            Vector3 direction = b - a;
            float length = direction.magnitude;
            if (length < 4f)
            {
                return;
            }

            Vector3 forward = direction / length;
            Vector3 right = Vector3.Cross(Vector3.up, forward) * (LaneMarkingWidth * 0.5f);

            for (float distance = 1.5f; distance < length; distance += LaneDashLength + LaneDashGap)
            {
                float dashEnd = Mathf.Min(distance + LaneDashLength, length);
                Vector3 start = a + forward * distance;
                Vector3 end = a + forward * dashEnd;
                int index = vertices.Count;
                vertices.Add(start - right);
                vertices.Add(start + right);
                vertices.Add(end + right);
                vertices.Add(end - right);
                AddUpwardQuad(vertices, triangles, index, index + 1, index + 2, index + 3);
            }
        }

        static void AddStopBars(List<Vector3> vertices, List<int> triangles, CityStreet street)
        {
            List<Vector3> points = TrimmedPolyline(street, CrosswalkY);
            if (points.Count < 2)
            {
                return;
            }

            if (street.trim_start_m > 0.01f)
            {
                AddStopBar(vertices, triangles, points[0], points[1], RoadWidth(street));
            }

            if (street.trim_end_m > 0.01f)
            {
                AddStopBar(vertices, triangles, points[points.Count - 1], points[points.Count - 2], RoadWidth(street));
            }
        }

        static void AddStopBar(List<Vector3> vertices, List<int> triangles, Vector3 junctionEnd, Vector3 interiorPoint, float roadWidth)
        {
            Vector3 direction = interiorPoint - junctionEnd;
            direction.y = 0f;
            float length = direction.magnitude;
            if (length < 10f)
            {
                return;
            }

            direction /= length;
            Vector3 center = junctionEnd + direction * Mathf.Min(7f, length * 0.3f);
            Vector3 right = Vector3.Cross(Vector3.up, direction).normalized;
            AddStrip(vertices, triangles, center - direction * StopBarWidth, center + direction * StopBarWidth, right, -roadWidth * 0.45f, roadWidth * 0.45f);
        }

        static void AddCrosswalk(List<Vector3> vertices, List<int> triangles, CityStreet street)
        {
            List<Vector3> points = TrimmedPolyline(street, CrosswalkY);
            if (points.Count < 2)
            {
                return;
            }

            for (int i = 0; i < points.Count - 1; i++)
            {
                Vector3 a = points[i];
                Vector3 b = points[i + 1];
                Vector3 direction = b - a;
                direction.y = 0f;
                float length = direction.magnitude;
                if (length < 3f)
                {
                    continue;
                }

                direction /= length;
                Vector3 right = Vector3.Cross(Vector3.up, direction).normalized;
                float stripeLength = Mathf.Min(5.5f, Mathf.Max(3.0f, length * 0.45f));
                for (float offset = 0.8f; offset < length - 0.5f; offset += 1.15f)
                {
                    Vector3 center = a + direction * offset;
                    AddStrip(vertices, triangles, center - direction * (CrosswalkStripeWidth * 0.5f), center + direction * (CrosswalkStripeWidth * 0.5f), right, -stripeLength * 0.5f, stripeLength * 0.5f);
                }
            }
        }

        static void AddFlatPolygon(CityPoint[] points, float y, List<Vector3> vertices, List<int> triangles)
        {
            int count = EffectivePointCount(points);
            if (count < 3)
            {
                return;
            }

            int start = vertices.Count;
            for (int i = 0; i < count; i++)
            {
                vertices.Add(ToVector(points[i], y));
            }

            for (int i = 1; i < count - 1; i++)
            {
                AddUpwardTriangle(vertices, triangles, start, start + i, start + i + 1);
            }
        }

        static void AddBuilding(CityBuilding building, List<Vector3> vertices, List<int> triangles)
        {
            int count = EffectivePointCount(building.footprint);
            if (count < 3)
            {
                return;
            }

            float height = BuildingHeight(building);
            int bottomStart = vertices.Count;
            for (int i = 0; i < count; i++)
            {
                vertices.Add(ToVector(building.footprint[i], 0f));
            }

            int topStart = vertices.Count;
            for (int i = 0; i < count; i++)
            {
                vertices.Add(ToVector(building.footprint[i], height));
            }

            for (int i = 1; i < count - 1; i++)
            {
                AddTwoSidedTriangle(triangles, topStart, topStart + i + 1, topStart + i);
            }

            for (int i = 0; i < count; i++)
            {
                int next = (i + 1) % count;
                int b0 = bottomStart + i;
                int b1 = bottomStart + next;
                int t0 = topStart + i;
                int t1 = topStart + next;

                AddTwoSidedQuad(triangles, b0, t0, t1, b1);
            }
        }

        static void AddBuildingWindows(CityBuilding building, List<Vector3> vertices, List<int> triangles)
        {
            int count = EffectivePointCount(building.footprint);
            if (count < 3)
            {
                return;
            }

            float height = BuildingHeight(building);
            int floors = Mathf.Clamp(Mathf.FloorToInt(height / 3f), 1, 5);

            for (int i = 0; i < count; i++)
            {
                CityPoint current = building.footprint[i];
                CityPoint next = building.footprint[(i + 1) % count];
                Vector3 a = ToVector(current, 0f);
                Vector3 b = ToVector(next, 0f);
                Vector3 edge = b - a;
                float length = edge.magnitude;
                if (length < 5f)
                {
                    continue;
                }

                Vector3 forward = edge / length;
                Vector3 outward = Vector3.Cross(Vector3.up, forward).normalized * 0.045f;
                int windows = Mathf.Clamp(Mathf.FloorToInt(length / 4f), 1, 8);

                for (int floor = 0; floor < floors; floor++)
                {
                    float y = 1.45f + floor * 2.7f;
                    if (y + 0.7f > height)
                    {
                        continue;
                    }

                    for (int window = 0; window < windows; window++)
                    {
                        float t = (window + 0.5f) / windows;
                        Vector3 center = Vector3.Lerp(a, b, t) + Vector3.up * y;
                        AddFacadeQuad(vertices, triangles, center, forward, outward);
                        AddFacadeQuad(vertices, triangles, center, forward, -outward);
                    }
                }
            }
        }

        static void AddFacadeQuad(List<Vector3> vertices, List<int> triangles, Vector3 center, Vector3 horizontal, Vector3 offset)
        {
            float halfWidth = 0.55f;
            float halfHeight = 0.42f;
            Vector3 side = horizontal.normalized * halfWidth;
            Vector3 up = Vector3.up * halfHeight;
            int start = vertices.Count;
            vertices.Add(center - side - up + offset);
            vertices.Add(center + side - up + offset);
            vertices.Add(center + side + up + offset);
            vertices.Add(center - side + up + offset);
            AddTwoSidedQuad(triangles, start, start + 1, start + 2, start + 3);
        }

        static float BuildingHeight(CityBuilding building)
        {
            if (building.height_m > 0.01f)
            {
                float minimum = BuildingMinimumHeight(building);
                return Mathf.Clamp(building.height_m, minimum, 42f);
            }

            int hash = StableHash(building.id);
            int stories = 2 + hash % 4;
            return Mathf.Max(BuildingMinimumHeight(building), stories * 3.2f);
        }

        static float BuildingMinimumHeight(CityBuilding building)
        {
            switch (building.building)
            {
                case "house":
                case "residential":
                case "detached":
                case "apartments":
                    return 7.2f;
                case "commercial":
                case "retail":
                case "office":
                case "industrial":
                case "warehouse":
                    return 9.6f;
                default:
                    return DefaultBuildingHeight;
            }
        }

        static float RoadWidth(CityStreet street)
        {
            if (street.road_width_m > 0.01f)
            {
                return Mathf.Max(MinimumRoadWidth, street.road_width_m);
            }

            switch (street.highway)
            {
                case "motorway":
                case "trunk":
                case "primary":
                    return Mathf.Max(8.5f, street.estimated_road_width_m);
                case "secondary":
                case "tertiary":
                    return Mathf.Max(6.6f, street.estimated_road_width_m);
                case "residential":
                case "unclassified":
                    return Mathf.Clamp(street.estimated_road_width_m, 5.2f, 7.4f);
                case "service":
                    return Mathf.Clamp(street.estimated_road_width_m, 3.2f, 4.8f);
                default:
                    return Mathf.Max(MinimumRoadWidth, street.estimated_road_width_m);
            }
        }

        static bool IsDrivableRoad(CityStreet street)
        {
            if (street.render_kind == "road")
            {
                return true;
            }

            switch (street.highway)
            {
                case "motorway":
                case "trunk":
                case "primary":
                case "secondary":
                case "tertiary":
                case "residential":
                case "unclassified":
                case "service":
                case "living_street":
                    return true;
                default:
                    return false;
            }
        }

        static bool HasSidewalks(CityStreet street)
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

        static bool HasLaneMarkings(CityStreet street)
        {
            switch (street.highway)
            {
                case "primary":
                case "secondary":
                case "tertiary":
                case "residential":
                case "unclassified":
                    return street.estimated_road_width_m >= 6f;
                default:
                    return false;
            }
        }

        static bool IsMajorRoad(CityStreet street)
        {
            switch (street.highway)
            {
                case "primary":
                case "secondary":
                case "tertiary":
                    return true;
                default:
                    return false;
            }
        }

        static bool IsCrossing(CityStreet street)
        {
            return !string.IsNullOrEmpty(street.crossing) && IsWalkway(street);
        }

        static bool IsWalkway(CityStreet street)
        {
            if (street.render_kind == "walkway")
            {
                return true;
            }

            switch (street.highway)
            {
                case "footway":
                case "path":
                case "pedestrian":
                case "cycleway":
                case "steps":
                    return true;
                default:
                    return false;
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

        static void AddUpwardQuad(List<Vector3> vertices, List<int> triangles, int a, int b, int c, int d)
        {
            AddUpwardTriangle(vertices, triangles, a, b, c);
            AddUpwardTriangle(vertices, triangles, a, c, d);
        }

        static void AddUpwardTriangle(List<Vector3> vertices, List<int> triangles, int a, int b, int c)
        {
            Vector3 normal = Vector3.Cross(vertices[b] - vertices[a], vertices[c] - vertices[a]);
            if (normal.y >= 0f)
            {
                triangles.Add(a);
                triangles.Add(b);
                triangles.Add(c);
            }
            else
            {
                triangles.Add(a);
                triangles.Add(c);
                triangles.Add(b);
            }
        }

        static void AddTwoSidedQuad(List<int> triangles, int a, int b, int c, int d)
        {
            AddTwoSidedTriangle(triangles, a, b, c);
            AddTwoSidedTriangle(triangles, a, c, d);
        }

        static void AddTwoSidedTriangle(List<int> triangles, int a, int b, int c)
        {
            triangles.Add(a);
            triangles.Add(b);
            triangles.Add(c);
            triangles.Add(a);
            triangles.Add(c);
            triangles.Add(b);
        }

        static int EffectivePointCount(CityPoint[] points)
        {
            if (points == null || points.Length == 0)
            {
                return 0;
            }

            int count = points.Length;
            CityPoint first = points[0];
            CityPoint last = points[count - 1];
            if (count > 1 && Mathf.Approximately(first.x, last.x) && Mathf.Approximately(first.z, last.z))
            {
                count--;
            }

            return count;
        }

        static Vector3 ToVector(CityPoint point, float y)
        {
            return new Vector3(point.x, y, point.z);
        }

        static Mesh CreateMesh(string name, List<Vector3> vertices, List<int> triangles)
        {
            var mesh = new Mesh { name = name };
            if (vertices.Count > 65535)
            {
                mesh.indexFormat = IndexFormat.UInt32;
            }

            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
            mesh.RecalculateNormals();
            return mesh;
        }
    }
}
