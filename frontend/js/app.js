import {
  getFireStations,
  getHospitals,
  getMapConfiguration,
  getRoads
} from "./api.js";
import {
  fitMapToData,
  initializeMap,
  renderFireStations,
  renderHospitals,
  renderRoads
} from "./map.js";

const fallbackMapConfiguration = Object.freeze({
  pilotArea: "Istanbul Besiktas Demo",
  centerLatitude: 41.045,
  centerLongitude: 29.01,
  bounds: {
    south: 41.035,
    west: 28.99,
    north: 41.055,
    east: 29.03
  }
});

document.addEventListener("DOMContentLoaded", startApplication);

async function startApplication() {
  const loadingIndicator = document.querySelector("#loading-indicator");
  const errors = [];

  try {
    const configuration = await loadMapConfiguration(errors);
    document.querySelector("#pilot-area").textContent = configuration.pilotArea;

    const mapState = initializeMap(configuration, showSelectedLocation);
    const dataRequests = [
      createLayerRequest("Hospitals", getHospitals, renderHospitals, "#hospital-count"),
      createLayerRequest(
        "Fire Stations",
        getFireStations,
        renderFireStations,
        "#fire-station-count"),
      createLayerRequest("Roads", getRoads, renderRoads, "#road-count")
    ];

    const results = await Promise.allSettled(
      dataRequests.map((request) => request.load()));

    results.forEach((result, index) => {
      const request = dataRequests[index];

      if (result.status === "rejected") {
        errors.push(`${request.label}: ${getErrorMessage(result.reason)}`);
        return;
      }

      if (!Array.isArray(result.value)) {
        errors.push(`${request.label}: API response was not a list.`);
        return;
      }

      const renderedCount = request.render(mapState, result.value);
      document.querySelector(request.countSelector).textContent =
        renderedCount.toLocaleString();
    });

    fitMapToData(mapState);
  } catch (error) {
    errors.push(getErrorMessage(error));
  } finally {
    loadingIndicator.hidden = true;
    showErrors(errors);
  }
}

async function loadMapConfiguration(errors) {
  try {
    return await getMapConfiguration();
  } catch (error) {
    errors.push(`Map configuration: ${getErrorMessage(error)}`);
    return fallbackMapConfiguration;
  }
}

function createLayerRequest(label, load, render, countSelector) {
  return { countSelector, label, load, render };
}

function showSelectedLocation(location) {
  document.querySelector("#selected-location-help").hidden = true;
  document.querySelector("#selected-coordinates").hidden = false;
  document.querySelector("#selected-latitude").textContent =
    location.latitude.toFixed(6);
  document.querySelector("#selected-longitude").textContent =
    location.longitude.toFixed(6);
}

function showErrors(errors) {
  if (errors.length === 0) {
    return;
  }

  const panel = document.querySelector("#error-panel");
  document.querySelector("#error-message").textContent = errors.join(" ");
  panel.hidden = false;
}

function getErrorMessage(error) {
  return error instanceof Error ? error.message : "An unexpected error occurred.";
}
