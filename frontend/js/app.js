import {
  createIncident,
  getFireStations,
  getHospitals,
  getIncidents,
  getMapConfiguration,
  getNearestEmergencyServices,
  getRoads
} from "./api.js";
import {
  fitMapToData,
  initializeMap,
  renderIncident,
  renderIncidents,
  renderNearestEmergencyServices,
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
  let selectedLocation = null;
  let selectionVersion = 0;
  let incidentRequestPending = false;
  let incidentCount = 0;

  try {
    const configuration = await loadMapConfiguration(errors);
    document.querySelector("#pilot-area").textContent = configuration.pilotArea;

    const mapState = initializeMap(configuration, (location) => {
      selectedLocation = location;
      selectionVersion += 1;
      showSelectedLocation(location, incidentRequestPending);
      resetAnalysisPanel();
      resetIncidentPanel();
    });
    configureAnalysisButton(
      mapState,
      () => selectedLocation,
      () => selectionVersion);
    const dataRequests = [
      createLayerRequest("Hospitals", getHospitals, renderHospitals, "#hospital-count"),
      createLayerRequest(
        "Fire Stations",
        getFireStations,
        renderFireStations,
        "#fire-station-count"),
      createLayerRequest("Roads", getRoads, renderRoads, "#road-count"),
      createLayerRequest(
        "Incidents",
        getIncidents,
        (state, incidents) => {
          incidentCount = renderIncidents(state, incidents);
          return incidentCount;
        },
        "#incident-count")
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
    configureIncidentForm(
      () => selectedLocation,
      (isPending) => {
        incidentRequestPending = isPending;
      },
      (result) => {
        if (renderIncident(mapState, result.incident, result.recommendedService)) {
          incidentCount += 1;
          document.querySelector("#incident-count").textContent =
            incidentCount.toLocaleString();
        }
      });
  } catch (error) {
    errors.push(getErrorMessage(error));
  } finally {
    loadingIndicator.hidden = true;
    showErrors(errors);
  }
}

function configureIncidentForm(
  getSelectedLocation,
  onPendingChanged,
  onIncidentCreated) {
  const form = document.querySelector("#incident-form");

  form.addEventListener("submit", async (event) => {
    event.preventDefault();
    const location = getSelectedLocation();
    if (!location) {
      return;
    }

    onPendingChanged(true);
    setIncidentLoading(true);

    try {
      const result = await createIncident({
        type: document.querySelector("#incident-type").value,
        latitude: location.latitude,
        longitude: location.longitude,
        description: document.querySelector("#incident-description").value || null
      });

      onIncidentCreated(result);
      showIncidentResult(result);
      document.querySelector("#incident-description").value = "";
    } catch (error) {
      showIncidentFailure(getErrorMessage(error));
    } finally {
      onPendingChanged(false);
      setIncidentLoading(false, Boolean(getSelectedLocation()));
    }
  });
}

function configureAnalysisButton(mapState, getSelectedLocation, getSelectionVersion) {
  const button = document.querySelector("#find-nearest-services");

  button.addEventListener("click", async () => {
    const location = getSelectedLocation();
    if (!location) {
      return;
    }

    const requestVersion = getSelectionVersion();
    setAnalysisLoading(true);

    try {
      const result = await getNearestEmergencyServices(
        location.latitude,
        location.longitude);

      if (requestVersion !== getSelectionVersion()) {
        return;
      }

      renderNearestEmergencyServices(mapState, result);
      showAnalysisResults(result);
    } catch (error) {
      if (requestVersion === getSelectionVersion()) {
        showAnalysisFailure(getErrorMessage(error));
      }
    } finally {
      if (requestVersion === getSelectionVersion()) {
        setAnalysisLoading(false);
      }
    }
  });
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

function showSelectedLocation(location, incidentRequestPending) {
  document.querySelector("#selected-location-help").hidden = true;
  document.querySelector("#selected-coordinates").hidden = false;
  document.querySelector("#selected-latitude").textContent =
    location.latitude.toFixed(6);
  document.querySelector("#selected-longitude").textContent =
    location.longitude.toFixed(6);
  const button = document.querySelector("#find-nearest-services");
  button.disabled = false;
  button.textContent = "Find Nearest Emergency Services";
  document.querySelector("#create-incident").disabled = incidentRequestPending;
}

function resetAnalysisPanel() {
  document.querySelector("#analysis-results").hidden = true;
  document.querySelector("#analysis-status").hidden = true;
  document.querySelector("#analysis-status").textContent = "";
}

function resetIncidentPanel() {
  document.querySelector("#incident-result").hidden = true;
  const status = document.querySelector("#incident-status");
  status.hidden = true;
  status.textContent = "";
  status.classList.remove("incident-status--error");
}

function setIncidentLoading(isLoading, hasSelectedLocation = true) {
  const button = document.querySelector("#create-incident");
  button.disabled = isLoading || !hasSelectedLocation;
  button.textContent = isLoading ? "Creating incident..." : "Create Incident";

  if (isLoading) {
    const status = document.querySelector("#incident-status");
    status.textContent = "Saving incident and finding the recommended service...";
    status.classList.remove("incident-status--error");
    status.hidden = false;
    document.querySelector("#incident-result").hidden = true;
  }
}

function showIncidentResult(result) {
  const recommendation = result.recommendedService;
  document.querySelector("#incident-status").hidden = true;
  document.querySelector("#created-incident-summary").textContent =
    `${result.incident.type} incident #${result.incident.id} was created.`;
  document.querySelector("#incident-recommendation").textContent = recommendation
    ? `${recommendation.name} (${formatDistance(recommendation.distanceMeters)})`
    : "No matching emergency service is currently available.";
  document.querySelector("#incident-result").hidden = false;
}

function showIncidentFailure(message) {
  const status = document.querySelector("#incident-status");
  status.textContent = message;
  status.classList.add("incident-status--error");
  status.hidden = false;
  document.querySelector("#incident-result").hidden = true;
}

function setAnalysisLoading(isLoading) {
  const button = document.querySelector("#find-nearest-services");
  button.disabled = isLoading;
  button.textContent = isLoading
    ? "Finding nearest services..."
    : "Find Nearest Emergency Services";

  if (isLoading) {
    const status = document.querySelector("#analysis-status");
    status.textContent = "Calculating PostGIS straight-line distances...";
    status.classList.remove("analysis-status--error");
    status.hidden = false;
    document.querySelector("#analysis-results").hidden = true;
  }
}

function showAnalysisResults(result) {
  updateServiceResult(
    "#nearest-hospital-name",
    "#nearest-hospital-distance",
    result.nearestHospital,
    "No hospital records are available.");
  updateServiceResult(
    "#nearest-fire-station-name",
    "#nearest-fire-station-distance",
    result.nearestFireStation,
    "No fire station records are available.");

  document.querySelector("#analysis-status").hidden = true;
  document.querySelector("#analysis-results").hidden = false;
}

function updateServiceResult(nameSelector, distanceSelector, service, emptyMessage) {
  const name = document.querySelector(nameSelector);
  const distance = document.querySelector(distanceSelector);

  if (!service) {
    name.textContent = emptyMessage;
    distance.textContent = "—";
    return;
  }

  name.textContent = service.name || "Unnamed service";
  distance.textContent = formatDistance(service.distanceMeters);
}

function showAnalysisFailure(message) {
  const status = document.querySelector("#analysis-status");
  status.textContent = message;
  status.classList.add("analysis-status--error");
  status.hidden = false;
  document.querySelector("#analysis-results").hidden = true;
}

function formatDistance(distanceMeters) {
  const distance = Number(distanceMeters);
  if (!Number.isFinite(distance) || distance < 0) {
    return "Distance unavailable";
  }

  return distance < 1000
    ? `${Math.round(distance)} m`
    : `${(distance / 1000).toFixed(2)} km`;
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
