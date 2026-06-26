#!/usr/bin/env python3
"""Download OSM data and prepare Unity-friendly map JSON."""

from __future__ import annotations

import argparse
import csv
import json
import math
import shutil
import time
import urllib.parse
import urllib.request
from copy import deepcopy
from urllib.error import HTTPError
from pathlib import Path
from typing import Any


DEFAULT_ADDRESS = "3420 W. Slauson Avenue, Los Angeles, CA 90043"
DEFAULT_RADIUS_METERS = 900
DEFAULT_CHUNK_SIZE_METERS = 180
DEFAULT_TERRAIN_SAMPLE_SPACING_METERS = 90
ROAD_TILE_OVERLAP_METERS = 5.0
MIN_ROAD_TILE_LENGTH_METERS = 2.5
USER_AGENT = "gangland-osm-data-prep/0.1"
LANE_WIDTH_METERS = 3.4
OPEN_METEO_ELEVATION_URL = "https://api.open-meteo.com/v1/elevation"
OPEN_TOPO_DATA_ELEVATION_URL = "https://api.opentopodata.org/v1/srtm90m"
OPEN_ELEVATION_URL = "https://api.open-elevation.com/api/v1/lookup"

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
  node["highway"~"^(bus_stop|traffic_signals|crossing)$"]({box});
  node["public_transport"]({box});
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


def local_unprojector(origin_lat: float, origin_lon: float):
    meters_per_lat = 111_320
    meters_per_lon = 111_320 * math.cos(math.radians(origin_lat))

    def unproject(x: float, z: float) -> tuple[float, float]:
        return (
            origin_lat + (z / meters_per_lat),
            origin_lon + (x / meters_per_lon),
        )

    return unproject


def geometry_points(element: dict[str, Any], project) -> list[dict[str, float]]:
    geometry = element.get("geometry") or []
    return [project(float(point["lat"]), float(point["lon"])) for point in geometry]


def geometry_points_with_node_ids(element: dict[str, Any], project) -> tuple[list[dict[str, float]], list[str]]:
    geometry = element.get("geometry") or []
    nodes = element.get("nodes") or []
    points = [project(float(point["lat"]), float(point["lon"])) for point in geometry]
    node_ids = [str(node_id) for node_id in nodes]

    if len(node_ids) != len(points):
        node_ids = [f"{element.get('type')}/{element.get('id')}/vertex/{index}" for index in range(len(points))]

    return points, node_ids


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
            centerline, node_ids = geometry_points_with_node_ids(element, project)
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
                    "crossing": tags.get("crossing"),
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
                            "crossing",
                        )
                        if key in tags
                    },
                    "centerline": centerline,
                    "node_ids": node_ids,
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

        if element.get("type") == "node" and (
            "amenity" in tags
            or "shop" in tags
            or tags.get("highway") in {"bus_stop", "traffic_signals", "crossing"}
            or "public_transport" in tags
        ):
            pois.append(
                {
                    "id": element_id,
                    "name": tags.get("name"),
                    "amenity": tags.get("amenity"),
                    "shop": tags.get("shop"),
                    "highway": tags.get("highway"),
                    "public_transport": tags.get("public_transport"),
                    "crossing": tags.get("crossing"),
                    "bus": tags.get("bus"),
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


def feature_points(feature: dict[str, Any]) -> list[dict[str, float]]:
    for key in ("centerline", "footprint"):
        points = feature.get(key)
        if points:
            return points
    position = feature.get("position")
    return [position] if position else []


def feature_center(feature: dict[str, Any]) -> dict[str, float]:
    points = feature_points(feature)
    if not points:
        return {"x": 0.0, "z": 0.0}
    return {
        "x": round(sum(point["x"] for point in points) / len(points), 3),
        "z": round(sum(point["z"] for point in points) / len(points), 3),
    }


def street_segment_features(street: dict[str, Any]) -> list[dict[str, Any]]:
    centerline = street.get("centerline") or []
    if len(centerline) < 2:
        return []

    segments: list[dict[str, Any]] = []
    for index in range(len(centerline) - 1):
        start = centerline[index]
        end = centerline[index + 1]
        if start == end:
            continue
        segment = deepcopy(street)
        segment["id"] = f"{street.get('id')}#segment/{index}"
        segment["parent_id"] = street.get("id")
        segment["segment_index"] = index
        segment["centerline"] = [start, end]
        segments.append(segment)
    return segments


DRIVABLE_HIGHWAYS = {
    "motorway",
    "trunk",
    "primary",
    "secondary",
    "tertiary",
    "residential",
    "unclassified",
    "service",
    "living_street",
}

WALKWAY_HIGHWAYS = {"footway", "path", "pedestrian", "cycleway", "steps"}


def is_drivable_street(street: dict[str, Any]) -> bool:
    return street.get("highway") in DRIVABLE_HIGHWAYS


def is_walkway_street(street: dict[str, Any]) -> bool:
    return street.get("highway") in WALKWAY_HIGHWAYS


def road_width_m(street: dict[str, Any]) -> float:
    estimated = float(street.get("estimated_road_width_m") or LANE_WIDTH_METERS)
    highway = street.get("highway")
    if highway in {"motorway", "trunk", "primary"}:
        return max(8.5, estimated)
    if highway in {"secondary", "tertiary"}:
        return max(6.6, estimated)
    if highway in {"residential", "unclassified"}:
        return min(max(estimated, 5.2), 7.4)
    if highway == "service":
        return min(max(estimated, 3.2), 4.8)
    return max(2.5, estimated)


def point_distance(a: dict[str, float], b: dict[str, float]) -> float:
    return math.hypot(a["x"] - b["x"], a["z"] - b["z"])


def interpolate_point(a: dict[str, float], b: dict[str, float], t: float) -> dict[str, float]:
    return {
        "x": round(a["x"] + (b["x"] - a["x"]) * t, 3),
        "z": round(a["z"] + (b["z"] - a["z"]) * t, 3),
    }


def polyline_length(points: list[dict[str, float]]) -> float:
    return sum(point_distance(points[index], points[index + 1]) for index in range(len(points) - 1))


def trim_polyline(points: list[dict[str, float]], start_trim_m: float, end_trim_m: float) -> list[dict[str, float]]:
    trimmed = [dict(point) for point in points]
    trim_polyline_start(trimmed, start_trim_m)
    trim_polyline_end(trimmed, end_trim_m)
    return trimmed


def trim_polyline_start(points: list[dict[str, float]], trim_m: float) -> None:
    remaining = max(0.0, trim_m)
    while remaining > 0.001 and len(points) >= 2:
        distance = point_distance(points[0], points[1])
        if distance <= 0.001:
            points.pop(0)
            continue
        if remaining < distance:
            points[0] = interpolate_point(points[0], points[1], remaining / distance)
            return
        points.pop(0)
        remaining -= distance


def trim_polyline_end(points: list[dict[str, float]], trim_m: float) -> None:
    remaining = max(0.0, trim_m)
    while remaining > 0.001 and len(points) >= 2:
        distance = point_distance(points[-1], points[-2])
        if distance <= 0.001:
            points.pop()
            continue
        if remaining < distance:
            points[-1] = interpolate_point(points[-1], points[-2], remaining / distance)
            return
        points.pop()
        remaining -= distance


def junction_radius(width_m: float) -> float:
    return max(3.2, width_m * 0.38 + 1.1)


def build_street_topology(prepared: dict[str, Any]) -> tuple[list[dict[str, Any]], list[dict[str, Any]]]:
    node_usage: dict[str, int] = {}
    node_positions: dict[str, dict[str, float]] = {}
    node_max_width: dict[str, float] = {}
    node_highways: dict[str, set[str]] = {}

    for street in prepared.get("streets", []):
        if not is_drivable_street(street):
            continue
        centerline = street.get("centerline") or []
        node_ids = street.get("node_ids") or []
        if len(centerline) < 2 or len(node_ids) != len(centerline):
            continue

        width = road_width_m(street)
        for index, node_id in enumerate(node_ids):
            node_usage[node_id] = node_usage.get(node_id, 0) + 1
            node_positions[node_id] = centerline[index]
            node_max_width[node_id] = max(node_max_width.get(node_id, 0), width)
            node_highways.setdefault(node_id, set()).add(street.get("highway") or "")

    junction_ids: set[str] = {node_id for node_id, count in node_usage.items() if count > 1}
    sections: list[dict[str, Any]] = []

    for street in prepared.get("streets", []):
        centerline = street.get("centerline") or []
        node_ids = street.get("node_ids") or []
        if len(centerline) < 2:
            continue

        if is_walkway_street(street) or len(node_ids) != len(centerline):
            for segment in street_segment_features(street):
                segment["render_kind"] = "walkway" if is_walkway_street(street) else "road"
                segment["road_width_m"] = road_width_m(street)
                segment["trim_start_m"] = 0
                segment["trim_end_m"] = 0
                sections.append(segment)
            continue

        if not is_drivable_street(street):
            continue

        split_indices = {0, len(centerline) - 1}
        for index, node_id in enumerate(node_ids):
            if node_id in junction_ids:
                split_indices.add(index)

        ordered_indices = sorted(split_indices)
        section_index = 0
        for start_index, end_index in zip(ordered_indices, ordered_indices[1:]):
            if end_index <= start_index:
                continue
            section_points = centerline[start_index : end_index + 1]
            section_length = polyline_length(section_points)
            if section_length < 0.5:
                continue

            start_node_id = node_ids[start_index]
            end_node_id = node_ids[end_index]
            width = road_width_m(street)
            start_trim = junction_radius(node_max_width.get(start_node_id, width)) if start_node_id in junction_ids else 0
            end_trim = junction_radius(node_max_width.get(end_node_id, width)) if end_node_id in junction_ids else 0
            max_total_trim = max(0, section_length - 1.0)
            total_trim = start_trim + end_trim
            if total_trim > max_total_trim and total_trim > 0:
                scale = max_total_trim / total_trim
                start_trim *= scale
                end_trim *= scale

            section = deepcopy(street)
            section["id"] = f"{street.get('id')}#section/{section_index}"
            section["parent_id"] = street.get("id")
            section["section_index"] = section_index
            section["start_node_id"] = start_node_id
            section["end_node_id"] = end_node_id
            section["centerline"] = section_points
            section["road_width_m"] = round(width, 3)
            section["trim_start_m"] = round(start_trim, 3)
            section["trim_end_m"] = round(end_trim, 3)
            section["render_kind"] = "road"
            sections.append(section)
            section_index += 1

    junctions = []
    for node_id in sorted(junction_ids):
        position = node_positions.get(node_id)
        if not position:
            continue
        junctions.append(
            {
                "id": node_id,
                "position": position,
                "radius_m": round(junction_radius(node_max_width.get(node_id, LANE_WIDTH_METERS)), 3),
                "connected_road_count": node_usage.get(node_id, 0),
                "highways": sorted(value for value in node_highways.get(node_id, set()) if value),
            }
        )

    return sections, junctions


def tiled_street_sections(sections: list[dict[str, Any]], chunk_size_m: int) -> list[dict[str, Any]]:
    tiled: list[dict[str, Any]] = []
    for section in sections:
        centerline = section.get("centerline") or []
        if len(centerline) < 2:
            continue

        points = [dict(point) for point in centerline]
        if len(points) < 2:
            continue

        tile_index = 0
        for index in range(len(points) - 1):
            a = points[index]
            b = points[index + 1]
            for segment_start, segment_end in split_segment_at_chunk_boundaries(a, b, chunk_size_m):
                if point_distance(segment_start, segment_end) < MIN_ROAD_TILE_LENGTH_METERS:
                    continue
                segment_start, segment_end = extend_split_segment(a, b, segment_start, segment_end, ROAD_TILE_OVERLAP_METERS)
                tile = deepcopy(section)
                tile["id"] = f"{section.get('id')}#tile/{tile_index}"
                tile["centerline"] = [segment_start, segment_end]
                tile["trim_start_m"] = 0
                tile["trim_end_m"] = 0
                tile["tile_index"] = tile_index
                tiled.append(tile)
                tile_index += 1

    return tiled


def extend_split_segment(
    source_start: dict[str, float],
    source_end: dict[str, float],
    segment_start: dict[str, float],
    segment_end: dict[str, float],
    overlap_m: float,
) -> tuple[dict[str, float], dict[str, float]]:
    length = point_distance(source_start, source_end)
    if length <= 0.001:
        return segment_start, segment_end

    dx = source_end["x"] - source_start["x"]
    dz = source_end["z"] - source_start["z"]

    def t_for(point: dict[str, float]) -> float:
        if abs(dx) >= abs(dz) and abs(dx) > 0.001:
            return (point["x"] - source_start["x"]) / dx
        if abs(dz) > 0.001:
            return (point["z"] - source_start["z"]) / dz
        return 0.0

    start_t = max(0.0, min(1.0, t_for(segment_start) - overlap_m / length))
    end_t = max(0.0, min(1.0, t_for(segment_end) + overlap_m / length))
    return interpolate_point(source_start, source_end, start_t), interpolate_point(source_start, source_end, end_t)


def split_segment_at_chunk_boundaries(
    a: dict[str, float],
    b: dict[str, float],
    chunk_size_m: int,
) -> list[tuple[dict[str, float], dict[str, float]]]:
    t_values = {0.0, 1.0}
    dx = b["x"] - a["x"]
    dz = b["z"] - a["z"]

    if abs(dx) > 0.001:
        min_x = min(a["x"], b["x"])
        max_x = max(a["x"], b["x"])
        first = math.floor(min_x / chunk_size_m) + 1
        last = math.floor(max_x / chunk_size_m)
        for grid in range(first, last + 1):
            boundary = grid * chunk_size_m
            if min_x < boundary < max_x:
                t_values.add((boundary - a["x"]) / dx)

    if abs(dz) > 0.001:
        min_z = min(a["z"], b["z"])
        max_z = max(a["z"], b["z"])
        first = math.floor(min_z / chunk_size_m) + 1
        last = math.floor(max_z / chunk_size_m)
        for grid in range(first, last + 1):
            boundary = grid * chunk_size_m
            if min_z < boundary < max_z:
                t_values.add((boundary - a["z"]) / dz)

    ordered = sorted(t for t in t_values if -0.0001 <= t <= 1.0001)
    segments: list[tuple[dict[str, float], dict[str, float]]] = []
    for start_t, end_t in zip(ordered, ordered[1:]):
        if end_t - start_t < 0.00001:
            continue
        segment_start = interpolate_point(a, b, max(0.0, min(1.0, start_t)))
        segment_end = interpolate_point(a, b, max(0.0, min(1.0, end_t)))
        segments.append((segment_start, segment_end))
    return segments


def chunk_key_for_feature(feature: dict[str, Any], chunk_size_m: int) -> tuple[int, int]:
    center = feature_center(feature)
    return (
        math.floor(center["x"] / chunk_size_m),
        math.floor(center["z"] / chunk_size_m),
    )


def chunk_bounds(cx: int, cz: int, chunk_size_m: int) -> dict[str, float]:
    min_x = cx * chunk_size_m
    min_z = cz * chunk_size_m
    return {
        "min_x": round(min_x, 3),
        "min_z": round(min_z, 3),
        "max_x": round(min_x + chunk_size_m, 3),
        "max_z": round(min_z + chunk_size_m, 3),
    }


def fetch_open_meteo_elevations(lat_lon_pairs: list[tuple[float, float]]) -> list[float]:
    elevations: list[float] = []
    for start in range(0, len(lat_lon_pairs), 100):
        batch = lat_lon_pairs[start : start + 100]
        params = urllib.parse.urlencode(
            {
                "latitude": ",".join(f"{lat:.7f}" for lat, _lon in batch),
                "longitude": ",".join(f"{lon:.7f}" for _lat, lon in batch),
            }
        )
        url = f"{OPEN_METEO_ELEVATION_URL}?{params}"
        for attempt in range(4):
            try:
                payload = fetch_json(url, timeout=45)
                break
            except HTTPError as error:
                if error.code != 429 or attempt == 3:
                    raise
                time.sleep(2 * (attempt + 1))
        values = payload.get("elevation")
        if not isinstance(values, list) or len(values) != len(batch):
            raise RuntimeError(f"Open-Meteo elevation response did not match request size: {payload}")
        elevations.extend(float(value) for value in values)
    return elevations


def fetch_open_topo_data_elevations(lat_lon_pairs: list[tuple[float, float]]) -> list[float]:
    elevations: list[float] = []
    for start in range(0, len(lat_lon_pairs), 100):
        batch = lat_lon_pairs[start : start + 100]
        locations = "|".join(f"{lat:.7f},{lon:.7f}" for lat, lon in batch)
        params = urllib.parse.urlencode({"locations": locations, "interpolation": "bilinear"})
        payload = fetch_json(f"{OPEN_TOPO_DATA_ELEVATION_URL}?{params}", timeout=45)
        results = payload.get("results")
        if payload.get("status") != "OK" or not isinstance(results, list) or len(results) != len(batch):
            raise RuntimeError(f"Open Topo Data elevation response did not match request size: {payload}")
        elevations.extend(float(result["elevation"] or 0) for result in results)
    return elevations


def fetch_open_elevation_elevations(lat_lon_pairs: list[tuple[float, float]]) -> list[float]:
    elevations: list[float] = []
    for start in range(0, len(lat_lon_pairs), 100):
        batch = lat_lon_pairs[start : start + 100]
        locations = "|".join(f"{lat:.7f},{lon:.7f}" for lat, lon in batch)
        params = urllib.parse.urlencode({"locations": locations})
        payload = fetch_json(f"{OPEN_ELEVATION_URL}?{params}", timeout=45)
        results = payload.get("results")
        if not isinstance(results, list) or len(results) != len(batch):
            raise RuntimeError(f"Open-Elevation response did not match request size: {payload}")
        elevations.extend(float(result["elevation"] or 0) for result in results)
    return elevations


def fetch_elevations(lat_lon_pairs: list[tuple[float, float]]) -> tuple[list[float], str]:
    try:
        return fetch_open_meteo_elevations(lat_lon_pairs), "Open-Meteo Elevation API, Copernicus DEM GLO-90"
    except HTTPError as error:
        if error.code != 429:
            raise
        print("Open-Meteo elevation API returned 429; falling back to Open Topo Data srtm90m.")
    try:
        return fetch_open_topo_data_elevations(lat_lon_pairs), "Open Topo Data API, SRTM 90m"
    except HTTPError as error:
        if error.code != 429:
            raise
        print("Open Topo Data returned 429; falling back to Open-Elevation.")
        return fetch_open_elevation_elevations(lat_lon_pairs), "Open-Elevation public API"


def build_terrain_for_chunks(
    chunks: dict[str, dict[str, Any]],
    prepared: dict[str, Any],
    sample_spacing_m: int,
    *,
    include_elevation: bool,
) -> str | None:
    if not include_elevation:
        return None

    origin = prepared["origin"]
    unproject = local_unprojector(origin["lat"], origin["lon"])
    point_order: list[tuple[str, int, int, float, float]] = []
    sample_positions: list[tuple[float, float]] = []

    for chunk_id, chunk in sorted(chunks.items()):
        bounds = chunk["bounds"]
        columns = max(2, int(round((bounds["max_x"] - bounds["min_x"]) / sample_spacing_m)) + 1)
        rows = max(2, int(round((bounds["max_z"] - bounds["min_z"]) / sample_spacing_m)) + 1)
        terrain = {
            "schema": "gangland.unity_city_terrain.v1",
            "source": "",
            "columns": columns,
            "rows": rows,
            "spacing_m": sample_spacing_m,
            "points": [],
        }
        chunk["terrain"] = terrain

        for row in range(rows):
            z = bounds["min_z"] if rows == 1 else bounds["min_z"] + (bounds["max_z"] - bounds["min_z"]) * row / (rows - 1)
            for column in range(columns):
                x = bounds["min_x"] if columns == 1 else bounds["min_x"] + (bounds["max_x"] - bounds["min_x"]) * column / (columns - 1)
                point_order.append((chunk_id, row, column, round(x, 3), round(z, 3)))
                sample_positions.append((round(x, 3), round(z, 3)))

    unique_positions = sorted(set(sample_positions))
    unique_lat_lon_pairs = [unproject(x, z) for x, z in unique_positions]
    unique_elevations, terrain_source = fetch_elevations(unique_lat_lon_pairs)
    elevation_by_position = {
        position: elevation
        for position, elevation in zip(unique_positions, unique_elevations)
    }
    origin_elevations, _origin_source = fetch_elevations([(origin["lat"], origin["lon"])])
    origin_elevation = origin_elevations[0]

    for chunk_id, _row, _column, x, z in point_order:
        elevation = elevation_by_position[(x, z)]
        chunks[chunk_id]["terrain"]["points"].append(
            {
                "x": x,
                "z": z,
                "elevation_m": round(elevation, 3),
                "relative_y_m": round(elevation - origin_elevation, 3),
            }
        )

    for chunk in chunks.values():
        chunk["terrain"]["origin_elevation_m"] = round(origin_elevation, 3)
        chunk["terrain"]["source"] = terrain_source

    return terrain_source


def build_chunked_map(
    prepared: dict[str, Any],
    chunk_size_m: int,
    terrain_sample_spacing_m: int,
    *,
    include_elevation: bool,
) -> tuple[dict[str, Any], dict[str, dict[str, Any]]]:
    chunks: dict[str, dict[str, Any]] = {}
    street_sections, junctions = build_street_topology(prepared)
    tiled_streets = tiled_street_sections(street_sections, chunk_size_m)

    for collection_name in ("streets", "junctions", "buildings", "areas", "pois"):
        source_features: list[dict[str, Any]] = []
        if collection_name == "streets":
            source_features = street_sections
        elif collection_name == "junctions":
            source_features = junctions
        else:
            source_features = prepared[collection_name]

        for feature in source_features:
            feature = unity_chunk_feature(feature, collection_name)
            cx, cz = chunk_key_for_feature(feature, chunk_size_m)
            chunk_id = f"{cx}_{cz}"
            if chunk_id not in chunks:
                bounds = chunk_bounds(cx, cz, chunk_size_m)
                chunks[chunk_id] = {
                    "schema": "gangland.unity_city_chunk.v1",
                    "id": chunk_id,
                    "coord_x": cx,
                    "coord_z": cz,
                    "bounds": bounds,
                    "streets": [],
                    "junctions": [],
                    "buildings": [],
                    "areas": [],
                    "pois": [],
                    "terrain": None,
                }
            chunks[chunk_id][collection_name].append(feature)

    terrain_source = build_terrain_for_chunks(chunks, prepared, terrain_sample_spacing_m, include_elevation=include_elevation)

    manifest_chunks = []
    for chunk_id, chunk in sorted(chunks.items()):
        counts = {
            "streets": len(chunk["streets"]),
            "junctions": len(chunk["junctions"]),
            "buildings": len(chunk["buildings"]),
            "areas": len(chunk["areas"]),
            "pois": len(chunk["pois"]),
        }
        chunk["counts"] = counts
        manifest_chunks.append(
            {
                "id": chunk_id,
                "coord_x": chunk["coord_x"],
                "coord_z": chunk["coord_z"],
                "bounds": chunk["bounds"],
                "resource": f"chunk_{chunk_id}",
                "counts": counts,
            }
        )

    manifest = {
        "schema": "gangland.unity_city_manifest.v1",
        "generated_at_unix": prepared["generated_at_unix"],
        "chunk_size_m": chunk_size_m,
        "source": prepared["source"],
        "origin": prepared["origin"],
        "bbox": prepared["bbox"],
        "counts": prepared["counts"],
        "topology_counts": {
            "street_sections": len(street_sections),
            "tiled_streets": len(tiled_streets),
            "junctions": len(junctions),
        },
        "terrain": {
            "source": terrain_source,
            "sample_spacing_m": terrain_sample_spacing_m if include_elevation else 0,
        },
        "chunks": manifest_chunks,
    }
    return manifest, chunks


def unity_chunk_feature(feature: dict[str, Any], collection_name: str) -> dict[str, Any]:
    unity_feature = deepcopy(feature)

    if collection_name == "buildings":
        unity_feature["levels"] = unity_feature.get("levels") or 0
        unity_feature["height_m"] = unity_feature.get("height_m") or 0

    if collection_name == "streets":
        unity_feature["lane_count"] = unity_feature.get("lane_count") or 1
        unity_feature["lane_width_m"] = unity_feature.get("lane_width_m") or LANE_WIDTH_METERS
        unity_feature["estimated_road_width_m"] = unity_feature.get("estimated_road_width_m") or LANE_WIDTH_METERS
        unity_feature["road_width_m"] = unity_feature.get("road_width_m") or road_width_m(unity_feature)
        unity_feature["trim_start_m"] = 0
        unity_feature["trim_end_m"] = 0

    unity_feature.pop("osm_tags", None)
    unity_feature.pop("node_ids", None)
    return unity_feature


def write_chunked_map(
    output_dir: Path,
    prepared: dict[str, Any],
    chunk_size_m: int,
    terrain_sample_spacing_m: int,
    *,
    include_elevation: bool,
) -> dict[str, Any]:
    manifest, chunks = build_chunked_map(
        prepared,
        chunk_size_m,
        terrain_sample_spacing_m,
        include_elevation=include_elevation,
    )
    chunks_dir = output_dir / "unity_chunks"
    if chunks_dir.exists():
        shutil.rmtree(chunks_dir)
    chunks_dir.mkdir(parents=True, exist_ok=True)

    write_json(chunks_dir / "manifest.json", manifest)
    for chunk_id, chunk in chunks.items():
        write_json(chunks_dir / f"chunk_{chunk_id}.json", chunk)

    return manifest


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--address", default=DEFAULT_ADDRESS)
    parser.add_argument("--radius-meters", type=int, default=DEFAULT_RADIUS_METERS)
    parser.add_argument("--chunk-size-meters", type=int, default=DEFAULT_CHUNK_SIZE_METERS)
    parser.add_argument("--terrain-sample-spacing-meters", type=int, default=DEFAULT_TERRAIN_SAMPLE_SPACING_METERS)
    parser.add_argument("--skip-elevation", action="store_true")
    parser.add_argument("--output-dir", default="utility-folder/data/slauson_3420")
    parser.add_argument(
        "--from-unity-map",
        help="Skip download and build chunked Unity Resources from an existing unity_map.json.",
    )
    parser.add_argument(
        "--from-osm-raw",
        help="Skip Overpass download and rebuild unity_map.json from an existing osm_raw.json with OSM node topology.",
    )
    parser.add_argument(
        "--from-geocode",
        help="Geocode JSON to use with --from-osm-raw. Defaults to geocode.json next to the raw file.",
    )
    parser.add_argument(
        "--unity-resources-dir",
        default="gangland-client/gangland/Assets/Resources/Maps/slauson_3420",
        help="Where Unity should load chunked JSON Resources from.",
    )
    args = parser.parse_args()

    output_dir = Path(args.output_dir)
    if args.from_unity_map:
        prepared = json.loads(Path(args.from_unity_map).read_text(encoding="utf-8"))
        origin = {
            "lat": prepared["origin"]["lat"],
            "lon": prepared["origin"]["lon"],
        }
        bbox = (
            prepared["bbox"]["south"],
            prepared["bbox"]["west"],
            prepared["bbox"]["north"],
            prepared["bbox"]["east"],
        )
        output_dir.mkdir(parents=True, exist_ok=True)
        write_street_csv(output_dir / "street_metadata.csv", prepared["streets"])
    elif args.from_osm_raw:
        raw_path = Path(args.from_osm_raw)
        raw = json.loads(raw_path.read_text(encoding="utf-8"))
        geocode_path = Path(args.from_geocode) if args.from_geocode else raw_path.with_name("geocode.json")
        origin = json.loads(geocode_path.read_text(encoding="utf-8"))
        bbox = bbox_from_radius(origin["lat"], origin["lon"], args.radius_meters)
        prepared = prepared_map(raw, origin, bbox)

        output_dir.mkdir(parents=True, exist_ok=True)
        write_json(output_dir / "unity_map.json", prepared)
        write_street_csv(output_dir / "street_metadata.csv", prepared["streets"])
    else:
        origin = geocode(args.address)
        bbox = bbox_from_radius(origin["lat"], origin["lon"], args.radius_meters)
        raw = download_overpass(bbox)
        prepared = prepared_map(raw, origin, bbox)

        write_json(output_dir / "geocode.json", origin)
        write_json(output_dir / "osm_raw.json", raw)
        write_json(output_dir / "unity_map.json", prepared)
        write_street_csv(output_dir / "street_metadata.csv", prepared["streets"])
    manifest = write_chunked_map(
        output_dir,
        prepared,
        args.chunk_size_meters,
        args.terrain_sample_spacing_meters,
        include_elevation=not args.skip_elevation,
    )

    unity_resources_dir = Path(args.unity_resources_dir)
    if unity_resources_dir:
        if unity_resources_dir.exists():
            shutil.rmtree(unity_resources_dir)
        shutil.copytree(output_dir / "unity_chunks", unity_resources_dir)

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
    print(f"wrote: {len(manifest['chunks'])} Unity chunks to {output_dir / 'unity_chunks'}")
    if unity_resources_dir:
        print(f"copied Unity Resources map to: {unity_resources_dir}")


if __name__ == "__main__":
    main()
