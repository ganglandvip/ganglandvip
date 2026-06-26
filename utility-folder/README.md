# Gangland OSM Utility

This utility downloads OpenStreetMap data around `3420 W. Slauson Avenue, Los Angeles, CA 90043` and prepares Unity map files.

## Run

```bash
python3 utility-folder/osm_data_pipeline.py
```

Default output:

- `utility-folder/data/slauson_3420/geocode.json`: address lookup result and map origin.
- `utility-folder/data/slauson_3420/osm_raw.json`: raw Overpass API response.
- `utility-folder/data/slauson_3420/unity_map.json`: game-ready streets, buildings, areas, POIs, and metadata.
- `utility-folder/data/slauson_3420/unity_chunks/manifest.json`: chunk index for runtime streaming.
- `utility-folder/data/slauson_3420/unity_chunks/chunk_*.json`: per-chunk city data.
- `utility-folder/data/slauson_3420/street_metadata.csv`: quick review table for road class, lane count, widths, and speed tags.

Each chunk can include a `terrain` grid with sampled elevation points. The exporter tries:

1. Open-Meteo Elevation API, Copernicus DEM GLO-90.
2. Open Topo Data `srtm90m`.
3. Open-Elevation public API.

By default the chunk files are also copied into:

- `gangland-client/gangland/Assets/Resources/Maps/slauson_3420`

The Unity client loads that Resources folder at runtime.

## Convert Existing Map Data

Use this when `unity_map.json` already exists and you only need to rebuild the Unity streaming chunks:

```bash
python3 utility-folder/osm_data_pipeline.py \
  --from-unity-map utility-folder/data/slauson_3420/unity_map.json
```

## Unity Runtime

Open `gangland-client/gangland` in Unity. In any scene, choose:

```text
Gangland > City > Add City Map Streamer
```

The `CityMapStreamer` component loads `Resources/Maps/slauson_3420/manifest.json`, then loads and unloads nearby chunk JSON files as the player or camera moves. Assign the player rig to `Streaming Target`; if left empty, the streamer follows `Camera.main`.

Street data is split into tile-sized segment records during export. That keeps roads, sidewalks, and lane markings local to the chunks that stream near the player instead of attaching one long OSM road to only the chunk containing its overall center.

`SampleScene` includes a ground-level camera controller:

- click the Game view to lock the mouse.
- move with `WASD` or arrow keys.
- hold `Shift` to sprint.
- press `Esc` to unlock the mouse.

The generated scene objects are procedural meshes:

- terrain from sampled elevation grids.
- roads from OSM centerlines and lane/width metadata.
- buildings from OSM footprints and available height/level metadata.
- areas as flat footprint meshes.
- POIs as small markers.

Runtime rendering uses separate terrain, road, building, area, and POI materials plus ambient light, sun, sky color, and fog so the city does not render as a black/white silhouette.

Terrain data remains in the chunks, but visual terrain rendering is disabled in `SampleScene` by default. The terrain grid streams as a separate topography mesh; until roads and building bases are conformed to sampled elevation, it can look like a brown layer moving below the roads.

The Unity streamer also adds procedural street-level detail from the same OSM geometry:

- sidewalks generated along both sides of streets.
- dashed lane markings generated from road centerlines.
- low-rise building height variation when OSM height is missing.
- facade window meshes generated from building footprints.

Imported asset packs are layered on top of the streamed OSM city:

- `Night Modular City Pack`: selected street props are spawned near streamed street segments.
- `Street Gangs - Miami Beach`: `Gangster_1` is spawned as a character preview near the player on scene start.

## Unity Coordinate Shape

`unity_map.json` uses local meters from the geocoded address:

- `x`: east/west meters from origin.
- `z`: north/south meters from origin.
- `y`: reserved for height.

Street records include OSM lane tags when present and an inferred `lane_count` fallback when OSM does not provide lane data.

## Adjust Area Size

```bash
python3 utility-folder/osm_data_pipeline.py --radius-meters 1200
```

## Adjust Streaming Chunk Size

```bash
python3 utility-folder/osm_data_pipeline.py --chunk-size-meters 220
```

## Adjust Topography Sampling

```bash
python3 utility-folder/osm_data_pipeline.py --terrain-sample-spacing-meters 60
```

If public elevation providers are rate-limiting and you only need to rebuild non-terrain map chunks:

```bash
python3 utility-folder/osm_data_pipeline.py --skip-elevation
```
