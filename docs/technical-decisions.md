# Technical Decisions

This document records the main engineering choices in SmartCity Location
Intelligence, why they fit the current project, and the trade-offs they create.

## PostgreSQL and PostGIS

The project stores spatial data in PostgreSQL with PostGIS instead of representing coordinates as unrelated numeric columns and reproducing spatial mathematics in application code. PostGIS provides typed geometries, SRID-aware functions, geography distance calculations, and GiST spatial indexes alongside the relational data.

The trade-off is additional deployment and operational complexity: the database must have the PostGIS extension, and developers need to understand spatial types and coordinate systems.

## NetTopologySuite and SRID 4326

Domain entities use NetTopologySuite `Point`, `LineString`, and `MultiPolygon` values. The Npgsql NetTopologySuite integration maps those values to PostGIS columns. All current geometries use SRID 4326, the WGS 84 longitude/latitude coordinate system used by GPS and OpenStreetMap. In application code, X is longitude and Y is latitude.

SRID 4326 geometry coordinates are measured in degrees, not metres. The nearest-service SQL therefore casts geometries to PostGIS `geography` before calling `ST_Distance`. Assigning an SRID labels a geometry's coordinate system; it does not transform coordinates from another system.

The choice aligns directly with OSM and browser-map coordinates, but it requires
strict longitude/X and latitude/Y ordering plus an explicit geography cast (or a
projection) for metric calculations.

## Database-side spatial queries

Nearest hospitals and fire stations are selected in PostgreSQL with parameterized SQL using `ST_MakePoint`, `ST_SetSRID`, `ST_Distance`, `ORDER BY`, and `LIMIT 1`. Only the nearest row from each table is returned to the API. This keeps spatial semantics in one place and avoids loading every facility into application memory merely to calculate and sort distances.

The current query favors clarity and exact geography distance. At much larger scale, it would need query-plan measurement and could use an indexed bounding prefilter or K-nearest-neighbor candidate search before the exact distance calculation.

## OpenStreetMap and Overpass

OpenStreetMap supplies open geographic source data, while Overpass allows the
application to request only hospitals, fire stations, and selected major roads
inside the configured pilot-area bounding box. The mapper validates coordinates
and geometry, applies stable fallback names, and converts accepted features to
SRID 4326 NetTopologySuite objects. The frontend retains OpenStreetMap
attribution.

This avoids a proprietary data dependency, but Overpass is a shared external service with rate limits and no application-specific service-level guarantee. Completeness and freshness also depend on community-maintained OpenStreetMap data.

## `HttpClientFactory`

The Overpass adapter is registered as a typed `HttpClient` through `HttpClientFactory`. This centralizes client configuration, reuses managed handlers, sets a descriptive user agent, and avoids unsafe manual lifetime management. Endpoint-specific linked cancellation tokens enforce the configured timeout for each attempt.

The trade-off is that resilience behavior must still be designed explicitly. This project performs one controlled transition from the primary endpoint to the fallback for transient network, timeout, or server failures; it does not implement broad retries that could amplify Overpass load.

## Configurable Overpass endpoints

Primary and fallback URLs, per-endpoint timeouts, maximum response size, and the
pilot-area bounds are configuration values validated at startup. Operators can
change providers and limits without recompiling or binding the import workflow to
one public Overpass instance. Fail-fast validation prevents the application from
running with malformed coordinates, URLs, or limits.

Configuration adds operational responsibility: values must be maintained for each environment, and changing them normally requires an application restart.

## Idempotent OSM import

Each imported feature uses its OpenStreetMap `type/id` as `ExternalId` and records `OpenStreetMap` as its source. The mapper removes duplicates within one response, the persistence adapter excludes identifiers already stored, and unique database indexes on `(Source, ExternalId)` provide a final integrity boundary.

Repeated imports therefore do not create duplicate rows. The current import is intentionally add-only, however; it does not update changed features or remove features that have disappeared from OpenStreetMap.

## Clean architecture separation

`SmartCity.Domain` contains entities and core enums. `SmartCity.Application` references the domain, defines use-case abstractions, DTOs, validation, and scoring rules. `SmartCity.Infrastructure` implements those abstractions with Npgsql, EF Core, PostGIS, and Overpass. `SmartCity.Api` is the HTTP host and composition root. Dependencies point toward the core, with the API referencing Infrastructure only to assemble the running application.

This separation makes rules independently testable and external systems replaceable. For a small demonstration it introduces more projects, interfaces, and dependency-injection wiring than a single-layer application would require.

## Explainable accessibility and priority rules

Accessibility and incident priority use deterministic threshold rules. Each
available hospital and fire-station distance contributes an accessibility score;
the combined score maps to `Excellent`, `Good`, `Moderate`, `Poor`, or `Critical`.
Priority is then calculated as:

```text
clamp(incident-type base + relevant-service distance + accessibility penalty, 0, 100)
```

Fire uses the nearest fire station, Medical and Accident use the nearest hospital,
and Other uses the closest available service. Priority-analysis responses expose
the three score components, relevant service and distance, final score, and level.
Preview and incident creation share `IncidentPriorityService`; creation recalculates
server-side and persists the resulting score and level rather than trusting client
input. Exact thresholds are listed in [Request flows](request-flows.md#phase-5a-priority-rules).

Rule-based scoring is transparent, testable, and does not require training data,
but its thresholds are project assumptions rather than operationally or clinically
validated policy. This is an explainable project-level decision-support score. It
is **not** an official emergency dispatch protocol, medical triage system, or
real-world response-time model.

## Vanilla JavaScript and Leaflet

The frontend uses Leaflet, browser ES modules, and direct DOM APIs. The interface remains small, requires no framework-specific build pipeline, and keeps the GIS interaction easy to inspect during a technical review.

Manual state and DOM coordination becomes harder as an interface grows. A component framework could become justified for substantially more screens or complex shared state, but would add unnecessary weight today.

## Same-origin static frontend hosting

The API project copies the frontend into its output and serves it as static content. Browser API calls use relative paths, so the demonstration has one origin, one process to start, and no separate CORS or frontend deployment configuration.

This couples frontend and API release cadence and does not allow them to scale independently. A larger production system could deploy static assets through a CDN and keep the API as a separate service.

## Geodesic distance, not routing

Nearest-service results use PostGIS geography distance between coordinates. They represent point-to-point distance over the Earth model; they do not follow roads and do not account for turn restrictions, traffic, vehicle access, or travel time. The frontend deliberately visualizes the result with a direct dashed line.

This calculation is fast, deterministic, and useful for location screening, but it must not be presented as a route or emergency response-time estimate. A production routing engine would be a separate capability with different data and operational costs.
