#!/usr/bin/env python3
"""Download OSM data and prepare Unity-friendly map JSON."""

from __future__ import annotations

import argparse
import csv
import json
import math
import time
import urllib.parse
import urllib.request
from pathlib import Path
from typing import Any


DEFAULT_ADDRESS = "3420 W. Slauson Avenue, Los Angeles, CA 90043"
DEFAULT_RADIUS_METERS = 900
USER_AGENT = "gangland-osm-data-prep/0.1"
LANE_WIDTH_METERS = 3.4

HIGHWAY_DEFAULT_LANES = {
    "motorway": 4,
    "trunk": 4,
    "primary": 4,
    "secondary": 4,
    "tertiary": 2,
    "residential": 2,
    "unclassified": 2,
    "service": 1,
    "living_street": 1,
    "pedestrian": 1,
    "footway": 1,
    "path": 1,
    "cycleway": 1,
    "steps": 1,
    "track": 1,
}


def fetch_json(url: str, *, data: bytes | None = None, timeout: int = 90) -> Any:
    request = urllib.request.Request(
        url,
        data=data,
        headers={
            "User-Agent": USER_AGENT,
            "Accept": "application/json",
        },
        method="POST" if data else "GET",
    )
    with urllib.request.urlopen(request, timeout=timeout) as response:
        return json.loads(response.read().decode("utf-8"))


def geocode(address: str) -> dict[str, Any]:
    params = urllib.parse.urlencode({"q": address, "format": "jsonv2", "limit": 1})
    results = fetch_json(f"https://nominatim.openstreetmap.org/search?{params}", timeout=30)
    if not results:
        raise RuntimeError(f"Nominatim could not geocode address: {address}")
    result = results[0]
    return {
        "address": address,
        "display_name": result.get("display_name", address),
        "lat": float(result["lat"]),
        "lon": float(result["lon"]),
        "osm_type": result.get("osm_type"),
        "osm_id": result.get("osm_id"),
    }


def bbox_from_radius(lat: float, lon: float, radius_m: int) -> tuple[float, float, float, float]:
    lat_delta = radius_m / 111_320
    lon_delta = radius_m / (111_320 * math.cos(math.radians(lat)))
    return (lat - lat_delta, lon - lon_delta, lat + lat_delta, lon + lon_delta)


def overpass_query(bbox: tuple[float, float, float, float]) -> str:
    south, west, north, east = bbox
    box = f"{south},{west},{north},{east}"
    return f"""
[out:json][timeout:90];
(
  way["highway"]({box});
  relation["highway"]({box});
  way["building"]({box});
  relation["building"]({box});
  way["amenity"]({box});
  node["amenity"]({box});
  way["shop"]({box});
  node["shop"]({box});
  way["leisure"]({box});
  node["leisure"]({box});
  way["landuse"]({box});
  relation["landuse"]({box});
);
out body geom;
""".strip()


def download_overpass(bbox: tuple[float, float, float, float]) -> dict[str, Any]:
    query = overpass_query(bbox)
    data = urllib.parse.urlencode({"data": query}).encode("utf-8")
    return fetch_json("https://overpass-api.de/api/interpreter", data=data, timeout=120)


def parse_number(value: str | None) -> float | None:
    if not value:
        return None
    cleaned = value.lower().replace("mph", "").replace("m", "").strip()
    try:
        return float(cleaned)
    except ValueError:
        return None


def parse_lanes(tags: dict[str, str]) -> int:
    lanes = parse_number(tags.get("lanes"))
    if lanes:
        return max(1, int(round(lanes)))

    forward = parse_number(tags.get("lanes:forward")) or 0
    backward = parse_number(tags.get("lanes:backward")) or 0
    if forward or backward:
        return max(1, int(round(forward + backward)))

    highway = tags.get("highway", "")
    return HIGHWAY_DEFAULT_LANES.get(highway, 2)


def local_projector(origin_lat: float, origin_lon: float):
    meters_per_lat = 111_320
    meters_per_lon = 111_320 * math.cos(math.radians(origin_lat))

    def project(lat: float, lon: float) -> dict[str, float]:
        return {
            "x": round((lon - origin_lon) * meters_per_lon, 3),
            "z": round((lat - origin_lat) * meters_per_lat, 3),
        }

    return project


def geometry_points(element: dict[str, Any], project) -> list[dict[str, float]]:
    geometry = element.get("geometry") or []
    return [project(float(point["lat"]), float(point["lon"])) for point in geometry]


def prepared_map(raw: dict[str, Any], origin: dict[str, Any], bbox: tuple[float, float, float, float]) -> dict[str, Any]:
    project = local_projector(origin["lat"], origin["lon"])
    streets: list[dict[str, Any]] = []
    buildings: list[dict[str, Any]] = []
    areas: list[dict[str, Any]] = []
    pois: list[dict[str, Any]] = []

    for element in raw.get("elements", []):
        tags = element.get("tags") or {}
        element_id = f"{element.get('type')}/{element.get('id')}"

        if "highway" in tags and element.get("geometry"):
            lane_count = parse_lanes(tags)
            streets.append(
                {
                    "id": element_id,
                    "name": tags.get("name"),
                    "highway": tags.get("highway"),
                    "oneway": tags.get("oneway") in {"yes", "true", "1"},
                    "lane_count": lane_count,
                    "lane_source": "osm" if tags.get("lanes") else "inferred",
                    "lane_width_m": LANE_WIDTH_METERS,
                    "estimated_road_width_m": round(lane_count * LANE_WIDTH_METERS, 2),
                    "maxspeed": tags.get("maxspeed"),
                    "surface": tags.get("surface"),
                    "turn_lanes": tags.get("turn:lanes"),
                    "osm_tags": {
                        key: tags[key]
                        for key in (
                            "lanes",
                            "lanes:forward",
                            "lanes:backward",
                            "parking:lane:both",
                            "parking:lane:left",
                            "parking:lane:right",
                            "sidewalk",
                            "cycleway",
                        )
                        if key in tags
                    },
                    "centerline": geometry_points(element, project),
                }
            )
            continue

        if "building" in tags and element.get("geometry"):
            levels = parse_number(tags.get("building:levels"))
            height = parse_number(tags.get("height"))
            buildings.append(
                {
                    "id": element_id,
                    "name": tags.get("name"),
                    "building": tags.get("building"),
                    "levels": levels,
                    "height_m": height or (round(levels * 3.2, 2) if levels else None),
                    "footprint": geometry_points(element, project),
                    "osm_tags": {
                        key: tags[key]
                        for key in ("addr:housenumber", "addr:street", "addr:city", "amenity", "shop")
                        if key in tags
                    },
                }
            )
            continue

        if element.get("type") == "node" and ("amenity" in tags or "shop" in tags):
            pois.append(
                {
                    "id": element_id,
                    "name": tags.get("name"),
                    "amenity": tags.get("amenity"),
                    "shop": tags.get("shop"),
                    "position": project(float(element["lat"]), float(element["lon"])),
                }
            )
            continue

        if element.get("geometry") and ("landuse" in tags or "leisure" in tags or "amenity" in tags or "shop" in tags):
            areas.append(
                {
                    "id": element_id,
                    "name": tags.get("name"),
                    "landuse": tags.get("landuse"),
                    "leisure": tags.get("leisure"),
                    "amenity": tags.get("amenity"),
                    "shop": tags.get("shop"),
                    "footprint": geometry_points(element, project),
                }
            )

    return {
        "schema": "gangland.unity_osm_map.v1",
        "generated_at_unix": int(time.time()),
        "source": {
            "provider": "OpenStreetMap via Nominatim and Overpass API",
            "license": "ODbL-1.0",
            "address": origin["address"],
            "display_name": origin["display_name"],
        },
        "origin": {
            "lat": origin["lat"],
            "lon": origin["lon"],
            "unity_axes": "x=east/west meters, z=north/south meters, y=height meters",
        },
        "bbox": {
            "south": bbox[0],
            "west": bbox[1],
            "north": bbox[2],
            "east": bbox[3],
        },
        "counts": {
            "streets": len(streets),
            "buildings": len(buildings),
            "areas": len(areas),
            "pois": len(pois),
        },
        "streets": streets,
        "buildings": buildings,
        "areas": areas,
        "pois": pois,
    }


def write_json(path: Path, payload: Any) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(payload, indent=2, sort_keys=True) + "\n", encoding="utf-8")


def write_street_csv(path: Path, streets: list[dict[str, Any]]) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    fields = [
        "id",
        "name",
        "highway",
        "oneway",
        "lane_count",
        "lane_source",
        "estimated_road_width_m",
        "maxspeed",
        "surface",
        "turn_lanes",
    ]
    with path.open("w", newline="", encoding="utf-8") as handle:
        writer = csv.DictWriter(handle, fieldnames=fields)
        writer.writeheader()
        for street in streets:
            writer.writerow({field: street.get(field) for field in fields})


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--address", default=DEFAULT_ADDRESS)
    parser.add_argument("--radius-meters", type=int, default=DEFAULT_RADIUS_METERS)
    parser.add_argument("--output-dir", default="utility-folder/data/slauson_3420")
    args = parser.parse_args()

    output_dir = Path(args.output_dir)
    origin = geocode(args.address)
    bbox = bbox_from_radius(origin["lat"], origin["lon"], args.radius_meters)
    raw = download_overpass(bbox)
    prepared = prepared_map(raw, origin, bbox)

    write_json(output_dir / "geocode.json", origin)
    write_json(output_dir / "osm_raw.json", raw)
    write_json(output_dir / "unity_map.json", prepared)
    write_street_csv(output_dir / "street_metadata.csv", prepared["streets"])

    print(f"origin: {origin['lat']}, {origin['lon']}")
    print(f"bbox: {bbox[0]}, {bbox[1]}, {bbox[2]}, {bbox[3]}")
    print(
        "prepared: "
        f"{prepared['counts']['streets']} streets, "
        f"{prepared['counts']['buildings']} buildings, "
        f"{prepared['counts']['areas']} areas, "
        f"{prepared['counts']['pois']} POIs"
    )
    print(f"wrote: {output_dir / 'unity_map.json'}")


if __name__ == "__main__":
    main()
