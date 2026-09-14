# Technical Interview Demo

This walkthrough is designed for a 3–5 minute interview segment. It demonstrates a
complete flow from a map selection to a persisted, explainable decision-support
result without claiming road routing, dispatch optimization, or medical triage.

## Prepare before the interview

From the repository root, start PostGIS, apply the existing migrations, and run the
API with its HTTP development profile:

```powershell
Copy-Item .env.example .env
docker compose up -d database
dotnet tool restore
dotnet restore SmartCity.slnx
dotnet tool run dotnet-ef database update --project backend/SmartCity.Infrastructure --startup-project backend/SmartCity.Api
dotnet run --project backend/SmartCity.Api --launch-profile http
```

In a second PowerShell window, verify readiness and load the pilot-area data once:

```powershell
Invoke-WebRequest http://localhost:5113/health/ready
Invoke-RestMethod -Method Post http://localhost:5113/api/import/openstreetmap
Start-Process http://localhost:5113
```

Keep this data in PostgreSQL for the demo. Re-running the import is safe: existing
OpenStreetMap features are recognized by source and external identifier, so the
`...Added` counts should be zero when nothing new is available. The live map tiles
and a fresh Overpass import require internet access, but previously imported spatial
data remains in PostgreSQL.

## Timed walkthrough

### 0:00–0:30 — Frame the problem

Show the dashboard and say:

> SmartCity Location Intelligence helps an operator evaluate emergency-service
> access at a selected location. It combines open geographic data, database-side
> spatial analysis, and transparent rule-based scoring in one small system.

Point out the Istanbul Beşiktaş pilot boundary and the English/Turkish selector.
Explain that ASP.NET Core serves this vanilla Leaflet frontend from the same origin,
so the demo does not need a separate frontend server or a permissive CORS policy.

### 0:30–1:00 — Show imported spatial data

Toggle the Hospitals, Fire Stations, and Main Roads layers and open one marker popup.
Briefly explain:

- operational features came from OpenStreetMap through a configurable Overpass
  client;
- validation and mapping convert the response to NetTopologySuite geometries;
- PostGIS persists the geometry with SRID 4326;
- import deduplication makes repeated demo setup predictable.

Avoid spending time on every marker. The point is to establish the real data source
and the persisted spatial model.

### 1:00–1:40 — Select a point and find the nearest services

Click an empty point near `41.04, 29.01`, then choose **Find Nearest Emergency
Services**. While the hospital and fire-station results appear, say:

> The click becomes latitude and longitude query parameters. The API delegates to
> the application abstraction, and the infrastructure layer asks PostGIS for the
> nearest row using `ST_Distance` on geography values. Only the nearest result comes
> back to the application; it does not load every facility and calculate distance in
> C#.

Point to the dashed lines and distances. State explicitly that they are straight-line
geodesic distances in meters, not road routes or response-time estimates.

### 1:40–2:15 — Compare coverage and accessibility

Choose **Analyze Coverage**. Explain that the result classifies each nearest service
as Good (up to 2 km), Moderate (up to 5 km), Poor (over 5 km), or Unavailable; the
overall value is the least favorable service result.

Then choose **Analyze Accessibility**. Point to the hospital and fire-station
contributions and say:

> Accessibility converts both distances into deterministic contributions of up to
> 50 points. Their sum produces a 0–100 score and an explainable level. The rules are
> intentionally visible and testable rather than hidden in a model.

### 2:15–3:10 — Preview priority for a fire

In **Create Incident**, select **Fire**, optionally enter `Building fire reported`,
and choose **Preview Priority**. Walk through the displayed breakdown:

- the Fire base score is 40;
- distance to the nearest fire station contributes a distance score;
- the accessibility level contributes a penalty;
- the sum is capped at 100 and mapped to Low, Medium, High, or Critical.

Explain that Fire uses the nearest fire station, Medical and Accident use the nearest
hospital, and Other uses the closest available service. Emphasize:

> This is an explainable project-level decision-support score. It is not an official
> emergency dispatch protocol, medical-triage system, or real-world response-time
> model.

### 3:10–3:50 — Create and persist the incident

Choose **Create Incident**. Point out the returned incident ID, recommended fire
station, priority level, and score. Explain that creation deliberately runs the same
analysis as the preview, then stores the incident point, normalized description,
UTC timestamp, priority score, and priority level in PostgreSQL.

The new marker should appear without a page reload. This demonstrates that the UI is
showing the persisted API result rather than a browser-only mock.

### 3:50–4:30 — Show the dashboard update and close

Show the updated total, Fire count, priority count, and newest-incident entry. Close
with the request path:

```text
Leaflet UI -> ASP.NET Core controller -> application service
           -> infrastructure -> EF Core / PostGIS -> PostgreSQL
```

Finish with one trade-off:

> PostGIS is the right place for filtering and distance calculations because it can
> use spatial types and indexes close to the data. A production version would add
> authentication, observability, resilient deployment configuration, and a routing
> engine before making travel-time or dispatch claims.

## Fast fallback if the network is unreliable

Do not depend on a fresh Overpass call during the timed walkthrough. Use the already
imported database records, keep the API terminal visible for request logs, and verify
`/health/ready` before sharing the screen. If map tiles are unavailable, demonstrate
the same flow with the requests in `backend/SmartCity.Api/SmartCity.Api.http`; the
analysis and persistence APIs continue to use local PostgreSQL/PostGIS data.
