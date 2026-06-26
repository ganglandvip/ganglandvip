# Gangland OSM Utility

This utility downloads OpenStreetMap data around `3420 W. Slauson Avenue, Los Angeles, CA 90043` and prepares a Unity-friendly map file.

## Run

```bash
python3 utility-folder/osm_data_pipeline.py
```

Default output:

- `utility-folder/data/slauson_3420/geocode.json`: address lookup result and map origin.
- `utility-folder/data/slauson_3420/osm_raw.json`: raw Overpass API response.
- `utility-folder/data/slauson_3420/unity_map.json`: game-ready streets, buildings, areas, POIs, and metadata.
- `utility-folder/data/slauson_3420/street_metadata.csv`: quick review table for road class, lane count, widths, and speed tags.

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
