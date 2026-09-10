const requestTimeoutMs = 12_000;

async function getJson(path) {
  const controller = new AbortController();
  const timeoutId = window.setTimeout(() => controller.abort(), requestTimeoutMs);

  try {
    const response = await fetch(path, {
      headers: { Accept: "application/json" },
      signal: controller.signal
    });

    if (!response.ok) {
      const detail = await readProblemDetail(response);
      throw new Error(detail || `Request failed with HTTP ${response.status}.`);
    }

    return await response.json();
  } catch (error) {
    if (error instanceof DOMException && error.name === "AbortError") {
      throw new Error(`The request to ${path} timed out.`);
    }

    throw error;
  } finally {
    window.clearTimeout(timeoutId);
  }
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
