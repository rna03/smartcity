# SmartCity Location Intelligence

A GIS-based emergency-service location-intelligence and decision-support system
built with ASP.NET Core, PostgreSQL/PostGIS, OpenStreetMap/Overpass, and Leaflet.

## 1. Project Overview

SmartCity imports real emergency-service and road data for a configurable pilot
area, stores spatial features in PostGIS, and exposes an interactive bilingual
map. A user can inspect nearby services, compare coverage and accessibility,
preview an explainable incident-priority score, create an incident, and see the
dashboard update without reloading the page.

The current pilot area is a small Beşiktaş, İstanbul bounding box. Its size is
deliberately limited to keep public Overpass API usage responsible and the demo
quick to reproduce.

## 2. Problem Statement

Emergency-service data is inherently spatial: a service's usefulness depends not
only on whether it exists, but also on its distance from an incident. A normal
CRUD application does not answer those questions well. This project demonstrates
how a clean .NET application can import open geospatial data, keep distance work
inside a spatial database, and turn the results into transparent decision support.

## 3. Key Features

- Idempotent OpenStreetMap import for hospitals, fire stations, and main roads.
- Configurable primary and fallback Overpass endpoints with bounded timeouts.
- PostGIS nearest-hospital and nearest-fire-station analysis in meters.
- Rule-based coverage, accessibility, and incident-priority analysis.
- Incident persistence with recommended service, score, and priority level.
- Database-side dashboard aggregation and latest-incident queries.
- Leaflet map with independent layers, analysis panels, incident markers, and
  English/Turkish UI text.
- Liveness/readiness health endpoints and consistent API error responses.
- Automated unit, API-controller, configuration, mapping, and persistence tests.

## 4. Architecture

The solution follows a pragmatic clean-architecture dependency direction:

**Domain ← Application ← Infrastructure / API**

- **SmartCity.Domain** owns entities, enums, and geospatial domain state.
- **SmartCity.Application** owns use cases, abstractions, DTOs, validation, and
  deterministic analysis rules.
- **SmartCity.Infrastructure** implements database and Overpass boundaries.
- **SmartCity.Api** composes dependencies, hosts controllers and the static UI.
- **frontend** is a same-origin Vanilla JavaScript and Leaflet client.
- **tests** exercises the rules and integration boundaries without requiring a
  running production database for every test.

See [Architecture](docs/architecture.md) and
[Request flows](docs/request-flows.md) for diagrams and end-to-end examples.

## 5. Technology Stack

| Area | Technology |
| --- | --- |
| API/runtime | .NET 10, ASP.NET Core |
| Data access | EF Core 10, Npgsql |
| Spatial model | PostGIS, NetTopologySuite, SRID 4326 |
| Database | PostgreSQL 17 + PostGIS 3.5 container |
| Source data | OpenStreetMap through Overpass QL |
| Frontend | HTML, CSS, Vanilla JavaScript, Leaflet 1.9.4 |
| Tests | xUnit, EF Core InMemory, coverlet |
| Local orchestration | Docker Compose |

## 6. GIS / Spatial Capabilities

- Hospitals and fire stations are stored as **Point** geometries.
- Roads are stored as **LineString** geometries.
- OSM longitude/latitude is represented as X/Y with SRID 4326.
- GiST spatial indexes support scalable spatial access paths.
- Nearest-service distance uses PostGIS **ST_Distance** over **geography**, so the
  returned unit is meters rather than coordinate-system degrees.
- The nearest rows are selected by PostgreSQL; the application does not load all
  facilities and calculate distances in C#.

Distances shown by this project are straight-line/geodesic distances. Displayed
connector lines are not road routes.

## 7. Emergency Analysis Flow

After a map click, the selected latitude/longitude is validated and sent to the
API. The Infrastructure layer asks PostGIS for the nearest hospital and fire
station. Application services reuse that result to produce:

1. nearest-service details and distances;
2. service coverage: **Good**, **Moderate**, **Poor**, or **Unavailable**;
3. an accessibility score from 0 to 100;
4. an explainable incident-priority preview for a selected incident type.

The same priority workflow runs during incident creation before the score and
level are persisted. See [Request flows](docs/request-flows.md).

## 8. Incident Priority Decision Support

Phase 5A uses a deterministic sum:

**priority = incident base + relevant-service distance + accessibility penalty**

The result is clamped to 0–100.

| Incident type | Base |
| --- | ---: |
| Fire | 40 |
| Medical | 35 |
| Accident | 30 |
| Other | 20 |

| Relevant-service distance | Contribution |
| --- | ---: |
| ≤ 1,000 m | 0 |
| > 1,000 and ≤ 2,000 m | 10 |
| > 2,000 and ≤ 3,000 m | 20 |
| > 3,000 and ≤ 5,000 m | 30 |
| > 5,000 m | 40 |
| Service unavailable | 50 |

| Accessibility | Penalty |
| --- | ---: |
| Excellent | 0 |
| Good | 5 |
| Moderate | 10 |
| Poor | 15 |
| Critical | 20 |

| Final score | Priority level |
| --- | --- |
| 0–29 | Low |
| 30–49 | Medium |
| 50–69 | High |
| 70–100 | Critical |

The API also returns the three score components, relevant service, distance, and
accessibility level so the decision is auditable. Full details are in
[Technical decisions](docs/technical-decisions.md).

## 9. API Endpoints

| Method | Path | Purpose |
| --- | --- | --- |
| GET | /health/live | Process liveness |
| GET | /health/ready | PostgreSQL readiness |
| GET | /api/map/config | Pilot-area map configuration |
| POST | /api/import/openstreetmap | Import OSM spatial data |
| GET | /api/hospitals | List hospitals |
| GET | /api/fire-stations | List fire stations |
| GET | /api/roads | List main roads |
| GET | /api/location-analysis/nearest | Nearest hospital and fire station |
| GET | /api/location-analysis/coverage | Emergency-service coverage |
| GET | /api/location-analysis/accessibility | Accessibility score |
| GET | /api/location-analysis/incident-priority | Priority preview |
| POST | /api/incidents | Create and prioritize an incident |
| GET | /api/incidents | List incidents newest first |
| GET | /api/dashboard/summary | Counts and latest incidents |

Spatial analysis uses **latitude** and **longitude** query parameters. Priority
preview additionally requires **incidentType**: **Fire**, **Medical**,
**Accident**, or **Other**. See [API reference](docs/api.md) and the executable
[HTTP examples](backend/SmartCity.Api/SmartCity.Api.http).

## 10. Project Structure

~~~text
.
├── backend/
│   ├── SmartCity.Api/
│   ├── SmartCity.Application/
│   ├── SmartCity.Domain/
│   └── SmartCity.Infrastructure/
├── frontend/
│   ├── css/
│   └── js/
├── tests/SmartCity.Infrastructure.Tests/
├── docs/
├── docker-compose.yml
└── SmartCity.slnx
~~~

## 11. Database Model

| Entity/table | Spatial/data role |
| --- | --- |
| Hospital / hospitals | OSM facility point and source identity |
| FireStation / fire_stations | OSM facility point and source identity |
| Road / roads | OSM main-road line and road class |
| Incident / incidents | Selected point, type, description, time, priority |
| Region / regions | Polygon-ready regional model |
| LocationAnalysis / location_analyses | Region-linked analysis model |

OSM-backed tables use a unique **(Source, ExternalId)** key to make repeated
imports idempotent. Spatial columns use explicit geometry types and GiST indexes.
EF Core migrations preserve existing incident rows; the priority migration
backfills them as score **0**, level **Low**.

## 12. Running the Project

Prerequisites: .NET 10 SDK, Docker Desktop with Compose v2, and internet access
for Overpass, Leaflet CDN assets, and OSM map tiles.

~~~powershell
Copy-Item .env.example .env
docker compose up -d database
docker compose ps

dotnet tool restore
dotnet restore SmartCity.slnx
dotnet tool run dotnet-ef database update --project backend/SmartCity.Infrastructure --startup-project backend/SmartCity.Api
dotnet run --project backend/SmartCity.Api --launch-profile http
~~~

Open [http://localhost:5113](http://localhost:5113). Import is intentionally not
automatic; run it once when the database is ready:

~~~powershell
Invoke-RestMethod -Method Post http://localhost:5113/api/import/openstreetmap
~~~

The checked-in password **smartcity_dev_password** is a non-secret,
development-only default. Do not reuse it outside the local demo. Production
deployments must override the connection string through an environment variable
or secret manager, for example:

~~~powershell
$env:ConnectionStrings__SmartCityDatabase = "Host=db;Database=smartcity;Username=app;Password=<secret>"
~~~

Overpass URL/timeout settings and the pilot bounds are in
**backend/SmartCity.Api/appsettings.json**. Environment variables can override
nested keys, such as **OpenStreetMap__Primary__Url**. Development raises both
endpoint timeouts to 90 seconds.

## 13. Testing

From the repository root:

~~~powershell
dotnet tool restore
dotnet restore SmartCity.slnx
dotnet build SmartCity.slnx
dotnet test SmartCity.slnx
git diff --check
~~~

The suite covers scoring boundaries, service selection, invalid coordinates and
incident types, Overpass fallback rules, OSM mapping, EF model configuration,
incident persistence, and dashboard aggregation.

## 14. Demo Scenario

A concise interview demo is:

1. start PostGIS and the API;
2. import and display OSM hospitals, fire stations, and roads;
3. select a point and run nearest, coverage, and accessibility analyses;
4. choose **Fire** and preview the explainable priority;
5. create the incident and show its marker and refreshed dashboard metrics.

The narrated 3–5 minute version is in [Demo script](docs/demo.md).

## 15. Technical Decisions

- PostGIS performs distance/order/limit work close to indexed spatial data.
- NetTopologySuite keeps domain geometry strongly typed and EF-compatible.
- HttpClientFactory manages the Overpass client; only network, timeout, and 5xx
  failures trigger one fallback attempt. 4xx responses are not retried.
- OSM source identifiers and database uniqueness protect import idempotency.
- Rule-based scoring favors transparency and testable boundaries over opaque ML.
- Vanilla JavaScript and Leaflet keep the demo lightweight; the API hosts the UI
  at the same origin, so no broad CORS policy is needed.

Trade-offs and alternatives are documented in
[Technical decisions](docs/technical-decisions.md).

## 16. Current Limitations

- The pilot is a small demo area, not a production citywide deployment.
- Public Overpass availability and rate limits affect imports.
- Distance is geodesic, not road-network travel time or traffic-aware ETA.
- Coverage and accessibility use fixed project-level thresholds.
- Priority is not an official dispatch protocol, medical triage system, fire
  department procedure, or real-world response-time model.
- The project has no authentication, authorization, dispatch workflow, ML,
  routing engine, heatmap, or production observability stack.
- Region and historical-analysis entities are schema foundations, not a complete
  operational planning module.

## 17. Future Improvements

- Introduce authenticated roles and audited operational workflows.
- Add a routing provider for travel-time estimates while retaining geodesic
  distance as a transparent baseline.
- Version configurable scoring policies and preserve decision audit history.
- Add controlled scheduled imports, metrics, tracing, and resilience monitoring.
- Expand integration tests against disposable PostgreSQL/PostGIS containers.
- Add larger-area ingestion strategies that respect provider limits.
- Evaluate visualization such as time series or heatmaps only when supported by a
  defined operational question and appropriate data.

Additional review material:

- [Architecture](docs/architecture.md)
- [API reference](docs/api.md)
- [Request flows](docs/request-flows.md)
- [Demo script](docs/demo.md)
- [Technical decisions](docs/technical-decisions.md)
- [Interview notes](docs/interview-notes.md)
