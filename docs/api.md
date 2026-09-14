# SmartCity API Reference

The development base URL is `http://localhost:5113`. The API uses JSON with
camel-case property names and serializes enums as strings. Query coordinates are
supplied as `latitude` and `longitude`; stored PostGIS points use X = longitude and
Y = latitude with SRID 4326.

Response values below are illustrative. Names, identifiers, distances, counts, and
timestamps depend on the data currently stored in PostgreSQL.

## Endpoint summary

| Method | Path | Purpose |
| --- | --- | --- |
| `GET` | `/health/live` | Verify that the API process can respond. |
| `GET` | `/health/ready` | Verify that the API and PostgreSQL are ready. |
| `POST` | `/api/import/openstreetmap` | Import pilot-area emergency services and roads from Overpass. |
| `GET` | `/api/map/config` | Return the configured pilot-area center and bounds. |
| `GET` | `/api/hospitals` | List imported hospitals. |
| `GET` | `/api/fire-stations` | List imported fire stations. |
| `GET` | `/api/roads` | List imported roads and their coordinates. |
| `GET` | `/api/location-analysis/nearest` | Find the nearest hospital and fire station. |
| `GET` | `/api/location-analysis/coverage` | Classify emergency-service coverage at a point. |
| `GET` | `/api/location-analysis/accessibility` | Score access to the nearest emergency services. |
| `GET` | `/api/location-analysis/incident-priority` | Preview an explainable incident-priority score. |
| `POST` | `/api/incidents` | Analyze and persist an incident. |
| `GET` | `/api/incidents` | List persisted incidents, newest first. |
| `GET` | `/api/dashboard/summary` | Return aggregate and recent-incident dashboard data. |

## Health

### `GET /health/live`

Checks only whether the API process can answer. It does not query the database and
has no parameters.

```text
Healthy
```

### `GET /health/ready`

Checks whether PostgreSQL/PostGIS is reachable through the configured EF Core
context. It has no parameters. A healthy dependency returns `200 OK`; an unhealthy
readiness check returns `503 Service Unavailable`.

```text
Healthy
```

## OpenStreetMap import

### `POST /api/import/openstreetmap`

Fetches hospitals, fire stations, and roads for the configured pilot-area bounding
box from the Overpass API, maps them to spatial entities, and stores previously
unseen features. No request body or query parameters are required. Repeated imports
are idempotent for the same OpenStreetMap source and external identifier.

```json
{
  "pilotArea": "Istanbul Besiktas Demo",
  "hospitalsFound": 12,
  "hospitalsAdded": 10,
  "fireStationsFound": 5,
  "fireStationsAdded": 4,
  "roadsFound": 84,
  "roadsAdded": 80
}
```

The endpoint returns `200 OK` on success, `502 Bad Gateway` when Overpass cannot
supply data, and `503 Service Unavailable` when persistence fails.

## Map configuration and spatial data

### `GET /api/map/config`

Returns the configured pilot-area name, center, and bounds. It has no parameters.
The frontend uses this response to initialize the map.

```json
{
  "pilotArea": "Istanbul Besiktas Demo",
  "centerLatitude": 41.045,
  "centerLongitude": 29.01,
  "bounds": { "south": 41.035, "west": 28.99, "north": 41.055, "east": 29.03 }
}
```

### `GET /api/hospitals`

Lists imported hospitals in name order. It has no parameters.

```json
[
  {
    "id": 1,
    "name": "Example Hospital",
    "source": "OpenStreetMap",
    "externalId": "node/123",
    "longitude": 29.012,
    "latitude": 41.043
  }
]
```

### `GET /api/fire-stations`

Lists imported fire stations in name order. It has no parameters and uses the same
point-feature shape as the hospital endpoint.

```json
[
  {
    "id": 4,
    "name": "Example Fire Station",
    "source": "OpenStreetMap",
    "externalId": "way/456",
    "longitude": 29.025,
    "latitude": 41.047
  }
]
```

### `GET /api/roads`

Lists imported roads in name order. Each road contains its OSM classification and
an ordered polyline coordinate list. It has no parameters.

```json
[
  {
    "id": 9,
    "name": "Example Avenue",
    "roadType": "primary",
    "source": "OpenStreetMap",
    "externalId": "way/789",
    "coordinates": [
      { "longitude": 29.008, "latitude": 41.041 },
      { "longitude": 29.011, "latitude": 41.044 }
    ]
  }
]
```

## Location analysis

All four endpoints in this section require finite coordinates. Latitude must be
between -90 and 90; longitude must be between -180 and 180. Invalid or missing
values return `400 Bad Request`. Distance values are straight-line geodesic meters
calculated by PostGIS, not driving routes or travel-time estimates.

### `GET /api/location-analysis/nearest`

Finds the closest hospital and fire station to the selected point using database-side
spatial distance queries.

| Parameter | Required | Description |
| --- | --- | --- |
| `latitude` | Yes | Selected latitude in decimal degrees. |
| `longitude` | Yes | Selected longitude in decimal degrees. |

```http
GET /api/location-analysis/nearest?latitude=41.04&longitude=29.01
```

```json
{
  "selectedLocation": { "latitude": 41.04, "longitude": 29.01 },
  "nearestHospital": {
    "id": 1,
    "name": "Example Hospital",
    "externalId": "node/123",
    "latitude": 41.043,
    "longitude": 29.012,
    "distanceMeters": 870.2
  },
  "nearestFireStation": {
    "id": 4,
    "name": "Example Fire Station",
    "externalId": "way/456",
    "latitude": 41.047,
    "longitude": 29.025,
    "distanceMeters": 1460.4
  }
}
```

Either nearest-service property is `null` when its table has no records.

### `GET /api/location-analysis/coverage`

Classifies hospital and fire-station coverage from their nearest distances, then
reports the least favorable classification as the overall result.

| Parameter | Required | Description |
| --- | --- | --- |
| `latitude` | Yes | Selected latitude in decimal degrees. |
| `longitude` | Yes | Selected longitude in decimal degrees. |

```http
GET /api/location-analysis/coverage?latitude=41.04&longitude=29.01
```

```json
{
  "selectedLocation": { "latitude": 41.04, "longitude": 29.01 },
  "hospital": { "distanceMeters": 870.2, "coverageLevel": "Good" },
  "fireStation": { "distanceMeters": 1460.4, "coverageLevel": "Good" },
  "overallCoverageLevel": "Good"
}
```

Coverage is `Good` through 2,000 m, `Moderate` through 5,000 m, and `Poor`
beyond 5,000 m. A missing service produces `Unavailable`.

### `GET /api/location-analysis/accessibility`

Converts the two nearest-service distances into contributions of 0–50, then sums
them into a 0–100 accessibility score and level.

| Parameter | Required | Description |
| --- | --- | --- |
| `latitude` | Yes | Selected latitude in decimal degrees. |
| `longitude` | Yes | Selected longitude in decimal degrees. |

```http
GET /api/location-analysis/accessibility?latitude=41.04&longitude=29.01
```

```json
{
  "selectedLocation": { "latitude": 41.04, "longitude": 29.01 },
  "hospital": { "distanceMeters": 870.2, "score": 50 },
  "fireStation": { "distanceMeters": 1460.4, "score": 45 },
  "totalScore": 95,
  "accessibilityLevel": "Excellent"
}
```

### `GET /api/location-analysis/incident-priority`

Previews the same explainable priority calculation used when an incident is created,
without writing to the database.

| Parameter | Required | Description |
| --- | --- | --- |
| `latitude` | Yes | Selected latitude in decimal degrees. |
| `longitude` | Yes | Selected longitude in decimal degrees. |
| `incidentType` | Yes | `Fire`, `Medical`, `Accident`, or `Other`. |

```http
GET /api/location-analysis/incident-priority?latitude=41.04&longitude=29.01&incidentType=Fire
```

```json
{
  "selectedLocation": { "latitude": 41.04, "longitude": 29.01 },
  "incidentType": "Fire",
  "priorityScore": 50,
  "priorityLevel": "High",
  "relevantService": { "serviceType": "FireStation", "distanceMeters": 1460.4 },
  "accessibilityLevel": "Excellent",
  "breakdown": {
    "incidentTypeBaseScore": 40,
    "serviceDistanceScore": 10,
    "accessibilityPenalty": 0
  }
}
```

`Fire` uses the nearest fire station; `Medical` and `Accident` use the nearest
hospital; `Other` uses the closest available service. This is project-level decision
support, not an emergency dispatch or medical-triage protocol.

## Incidents

### `POST /api/incidents`

Runs service recommendation, accessibility analysis, and priority scoring, then
persists the incident and returns `201 Created`.

| Body property | Required | Description |
| --- | --- | --- |
| `type` | Yes | `Fire`, `Medical`, `Accident`, or `Other`. |
| `latitude` | Yes | Latitude from -90 through 90. |
| `longitude` | Yes | Longitude from -180 through 180. |
| `description` | No | Free text with a maximum length of 500 characters; blank text is stored as `null`. |

```http
POST /api/incidents
Content-Type: application/json

{
  "type": "Fire",
  "latitude": 41.04,
  "longitude": 29.01,
  "description": "Building fire reported"
}
```

```json
{
  "incident": {
    "id": 17,
    "type": "Fire",
    "latitude": 41.04,
    "longitude": 29.01,
    "description": "Building fire reported",
    "priorityScore": 50,
    "priorityLevel": "High",
    "createdAtUtc": "2026-09-14T09:30:00+00:00"
  },
  "recommendedService": {
    "serviceType": "FireStation",
    "id": 4,
    "name": "Example Fire Station",
    "externalId": "way/456",
    "latitude": 41.047,
    "longitude": 29.025,
    "distanceMeters": 1460.4
  },
  "priority": {
    "score": 50,
    "level": "High",
    "incidentTypeBaseScore": 40,
    "serviceDistanceScore": 10,
    "accessibilityPenalty": 0,
    "relevantServiceType": "FireStation",
    "relevantServiceDistanceMeters": 1460.4,
    "accessibilityLevel": "Excellent"
  }
}
```

Invalid types, coordinates, or descriptions return `400 Bad Request`. A persistence
failure returns `503 Service Unavailable`.

### `GET /api/incidents`

Lists all persisted incidents in descending creation-time order. It has no
parameters.

```json
[
  {
    "id": 17,
    "type": "Fire",
    "latitude": 41.04,
    "longitude": 29.01,
    "description": "Building fire reported",
    "priorityScore": 50,
    "priorityLevel": "High",
    "createdAtUtc": "2026-09-14T09:30:00+00:00"
  }
]
```

## Dashboard

### `GET /api/dashboard/summary`

Returns the total number of incidents, counts grouped by incident type and priority
level, and the five newest incidents. It has no parameters.

```json
{
  "totalIncidents": 7,
  "incidentCounts": { "fire": 3, "medical": 2, "accident": 1, "other": 1 },
  "priorityCounts": { "critical": 1, "high": 4, "medium": 2, "low": 0 },
  "latestIncidents": [
    {
      "id": 17,
      "type": "Fire",
      "latitude": 41.04,
      "longitude": 29.01,
      "description": "Building fire reported",
      "priorityScore": 50,
      "priorityLevel": "High",
      "createdAtUtc": "2026-09-14T09:30:00+00:00"
    }
  ]
}
```

Database-backed endpoint failures are returned as RFC 7807-style problem details
with `503 Service Unavailable`.
