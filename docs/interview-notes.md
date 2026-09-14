# Interview Notes

## What problem does this project solve?

It combines emergency-service locations, roads, incidents, and spatial analysis in one GIS dashboard. A user can inspect a selected location, find nearby services, assess coverage and accessibility, preview an explainable incident priority, and persist the incident for dashboard reporting.

## How does a map click reach PostgreSQL?

A click first gives Leaflet a latitude and longitude and updates browser state; the click alone does not query the database. When the user starts an analysis or creates an incident, `api.js` sends those coordinates to an ASP.NET Core endpoint. The controller validates them, calls an Application abstraction, and an Infrastructure implementation runs parameterized PostGIS SQL or persists an EF Core entity. The JSON result then returns through the same layers for Leaflet to render.

## Why PostGIS?

PostGIS gives PostgreSQL native geometry types, coordinate-system metadata, spatial functions, and spatial indexes. It lets the project calculate WGS 84 geography distance in the database while keeping spatial and relational data consistent. Plain latitude and longitude columns would require more custom, error-prone spatial logic.

## Why not calculate distance in C#?

Calculating in C# would require fetching all hospitals and fire stations, allocating objects for them, calculating every distance, and sorting in the API. The current SQL asks PostgreSQL for only the closest row from each table, reducing data transfer and keeping distance semantics centralized. At larger scale, the query could be tuned further with an indexed candidate search.

## What is SRID 4326?

SRID 4326 identifies WGS 84, the longitude/latitude coordinate reference system commonly used by GPS and OpenStreetMap. NetTopologySuite stores longitude as X and latitude as Y. Its geometry units are degrees, so the query casts to PostGIS `geography` when it needs distance in metres.

## What does NetTopologySuite do?

NetTopologySuite provides the .NET geometry objects used by the domain, such as `Point` and `LineString`. Npgsql's integration maps those objects to typed PostGIS columns while preserving SRID information. The current nearest-distance calculation itself runs in PostGIS rather than iterating over NetTopologySuite objects in memory.

## How do you prevent duplicate OSM imports?

Features receive a stable external key from the OpenStreetMap element type and ID. The mapper de-duplicates a response, the persistence adapter checks existing IDs for the OpenStreetMap source, and unique `(Source, ExternalId)` indexes enforce database integrity. The import adds new rows only; synchronizing later OSM edits or deletions is a future improvement.

## What happens if Overpass fails?

The typed client tries the configured primary endpoint first. A timeout, network
failure, or HTTP 5xx response triggers one fallback attempt; uncontrolled retries
are avoided. Rate limits, other client errors, invalid JSON, or an oversized
response fail without a fallback retry. If fetching or mapping fails, the import
never reaches `SaveChangesAsync`. External-source failures become HTTP 502, while
database failures become 503; detailed exceptions are logged and production
responses remain generic.

## How is priority calculated?

The score is `incident base + relevant-service distance + accessibility penalty`, clamped to 0–100.

- Base: Fire 40, Medical 35, Accident 30, Other 20.
- Distance: up to 1 km adds 0; up to 2 km 10; up to 3 km 20; up to 5 km 30; farther 40; unavailable 50.
- Accessibility: Excellent adds 0, Good 5, Moderate 10, Poor 15, Critical 20.
- Level: 0–29 Low, 30–49 Medium, 50–69 High, and 70–100 Critical.

Fire uses the nearest fire station, Medical and Accident use the nearest hospital, and Other uses the closest available service. Accessibility reflects both hospital and fire-station proximity.

## Why is priority explainable?

It is a deterministic rule set rather than an opaque model. The API returns the
base score, distance contribution, accessibility penalty, relevant service and
distance, final score, and level. Preview and creation share the same server-side
service, so an interviewer can trace every output to code and boundary tests. This
is an explainable project-level decision-support score, not an official emergency
dispatch protocol, medical triage system, or real-world response-time model.

## What would you add in production?

Priorities would include authentication and authorization, managed secrets, audit
and privacy controls, structured observability, rate limiting, caching, and stronger
external-service resilience. Spatial work would add governed scoring policies,
scheduled incremental OSM synchronization, query-plan monitoring, and a dedicated
road-network routing source for traffic-aware travel time when the use case requires
it. Deployment would also need backups, high availability, security testing, and
broader PostGIS integration and load tests.
