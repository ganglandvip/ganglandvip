using System;

namespace Gangland.CityStreaming
{
    [Serializable]
    public sealed class CityMapManifest
    {
        public string schema;
        public long generated_at_unix;
        public float chunk_size_m;
        public CityMapSource source;
        public CityMapOrigin origin;
        public CityMapBoundsLatLon bbox;
        public CityMapCounts counts;
        public CityChunkManifestEntry[] chunks;
    }

    [Serializable]
    public sealed class CityChunkManifestEntry
    {
        public string id;
        public int coord_x;
        public int coord_z;
        public CityChunkBounds bounds;
        public string resource;
        public CityMapCounts counts;
    }

    [Serializable]
    public sealed class CityChunk
    {
        public string schema;
        public string id;
        public int coord_x;
        public int coord_z;
        public CityChunkBounds bounds;
        public CityMapCounts counts;
        public CityStreet[] streets;
        public CityJunction[] junctions;
        public CityBuilding[] buildings;
        public CityArea[] areas;
        public CityPoi[] pois;
        public CityTerrain terrain;
    }

    [Serializable]
    public sealed class CityMapSource
    {
        public string provider;
        public string license;
        public string address;
        public string display_name;
    }

    [Serializable]
    public sealed class CityMapOrigin
    {
        public double lat;
        public double lon;
        public string unity_axes;
    }

    [Serializable]
    public sealed class CityMapBoundsLatLon
    {
        public double south;
        public double west;
        public double north;
        public double east;
    }

    [Serializable]
    public sealed class CityChunkBounds
    {
        public float min_x;
        public float min_z;
        public float max_x;
        public float max_z;
    }

    [Serializable]
    public sealed class CityMapCounts
    {
        public int streets;
        public int junctions;
        public int buildings;
        public int areas;
        public int pois;
    }

    [Serializable]
    public sealed class CityStreet
    {
        public string id;
        public string parent_id;
        public string name;
        public string highway;
        public bool oneway;
        public int lane_count;
        public string lane_source;
        public float lane_width_m;
        public float estimated_road_width_m;
        public string maxspeed;
        public string surface;
        public string turn_lanes;
        public string crossing;
        public int section_index;
        public string start_node_id;
        public string end_node_id;
        public float road_width_m;
        public float trim_start_m;
        public float trim_end_m;
        public string render_kind;
        public CityPoint[] centerline;
    }

    [Serializable]
    public sealed class CityJunction
    {
        public string id;
        public CityPoint position;
        public float radius_m;
        public int connected_road_count;
        public string[] highways;
    }

    [Serializable]
    public sealed class CityBuilding
    {
        public string id;
        public string name;
        public string building;
        public float levels;
        public float height_m;
        public CityPoint[] footprint;
    }

    [Serializable]
    public sealed class CityArea
    {
        public string id;
        public string name;
        public string landuse;
        public string leisure;
        public string amenity;
        public string shop;
        public CityPoint[] footprint;
    }

    [Serializable]
    public sealed class CityPoi
    {
        public string id;
        public string name;
        public string amenity;
        public string shop;
        public string highway;
        public string public_transport;
        public string crossing;
        public string bus;
        public CityPoint position;
    }

    [Serializable]
    public sealed class CityTerrain
    {
        public string schema;
        public string source;
        public int columns;
        public int rows;
        public float spacing_m;
        public float origin_elevation_m;
        public CityTerrainPoint[] points;
    }

    [Serializable]
    public sealed class CityTerrainPoint
    {
        public float x;
        public float z;
        public float elevation_m;
        public float relative_y_m;
    }

    [Serializable]
    public sealed class CityPoint
    {
        public float x;
        public float z;
    }
}
