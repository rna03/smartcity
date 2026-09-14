const requestTimeoutMs = 12_000;

async function requestJson(path, options = {}) {
  const controller = new AbortController();
  const timeoutId = window.setTimeout(() => controller.abort(), requestTimeoutMs);

  try {
    const response = await fetch(path, {
      ...options,
      headers: {
        Accept: "application/json",
        ...options.headers
      },
      signal: controller.signal
    });

    if (!response.ok) {
      const detail = await readProblemDetail(response);
      throw createRequestError("requestFailed", {
        detail,
        status: response.status
      });
    }

    return await response.json();
  } catch (error) {
    if (error instanceof DOMException && error.name === "AbortError") {
      throw createRequestError("requestTimedOut", { path });
    }

    throw error;
  } finally {
    window.clearTimeout(timeoutId);
  }
}

function createRequestError(code, details) {
  return Object.assign(new Error(code), { code, ...details });
}

function getJson(path) {
  return requestJson(path);
}

async function readProblemDetail(response) {
  try {
    const problem = await response.json();
    return problem.detail || problem.title;
  } catch {
    return null;
  }
}

export function getMapConfiguration() {
  return getJson("/api/map/config");
}

export function getHospitals() {
  return getJson("/api/hospitals");
}

export function getFireStations() {
  return getJson("/api/fire-stations");
}

export function getRoads() {
  return getJson("/api/roads");
}

export function getNearestEmergencyServices(latitude, longitude) {
  const query = new URLSearchParams({
    latitude: String(latitude),
    longitude: String(longitude)
  });

  return getJson(`/api/location-analysis/nearest?${query}`);
}

export function getCoverageAnalysis(latitude, longitude) {
  const query = new URLSearchParams({
    latitude: String(latitude),
    longitude: String(longitude)
  });

  return getJson(`/api/location-analysis/coverage?${query}`);
}

export function getAccessibilityAnalysis(latitude, longitude) {
  const query = new URLSearchParams({
    latitude: String(latitude),
    longitude: String(longitude)
  });

  return getJson(`/api/location-analysis/accessibility?${query}`);
}

export function getIncidentPriorityPreview(latitude, longitude, incidentType) {
  const query = new URLSearchParams({
    latitude: String(latitude),
    longitude: String(longitude),
    incidentType: String(incidentType)
  });

  return getJson(`/api/location-analysis/incident-priority?${query}`);
}

export function getDashboardSummary() {
  return getJson("/api/dashboard/summary");
}

export function getIncidents() {
  return getJson("/api/incidents");
}

export function createIncident(incident) {
  return requestJson("/api/incidents", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(incident)
  });
}
