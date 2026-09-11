import {
  createIncident,
  getCoverageAnalysis,
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
  refreshMapTranslations,
  renderIncident,
  renderIncidents,
  renderNearestEmergencyServices,
  renderFireStations,
  renderHospitals,
  renderRoads
} from "./map.js";
import {
  applyTranslations,
  getLanguage,
  getLocale,
  onLanguageChanged,
  setLanguage,
  t
} from "./i18n.js";

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

const uiState = {
  analysis: { error: null, isLoading: false, result: null },
  coverage: { error: null, isLoading: false, result: null },
  incident: { error: null, isLoading: false, result: null },
  mapState: null,
  startupErrors: []
};

document.addEventListener("DOMContentLoaded", startApplication);

async function startApplication() {
  applyTranslations();
  configureLanguageSelector();
  const loadingIndicator = document.querySelector("#loading-indicator");
  const errors = uiState.startupErrors;
  let selectedLocation = null;
  let selectionVersion = 0;
  let incidentRequestPending = false;
  let incidentCount = 0;

  try {
    const configuration = await loadMapConfiguration(errors);
    const pilotArea = document.querySelector("#pilot-area");
    pilotArea.removeAttribute("data-i18n");
    pilotArea.textContent = configuration.pilotArea;

    const mapState = initializeMap(configuration, (location) => {
      selectedLocation = location;
      selectionVersion += 1;
      uiState.analysis = { error: null, isLoading: false, result: null };
      uiState.coverage = { error: null, isLoading: false, result: null };
      uiState.incident = { error: null, isLoading: false, result: null };
      showSelectedLocation(location, incidentRequestPending);
      resetAnalysisPanel();
      resetCoveragePanel();
      resetIncidentPanel();
    });
    uiState.mapState = mapState;
    configureAnalysisButton(
      mapState,
      () => selectedLocation,
      () => selectionVersion);
    configureCoverageButton(
      () => selectedLocation,
      () => selectionVersion);
    const dataRequests = [
      createLayerRequest("hospitals", getHospitals, renderHospitals, "#hospital-count"),
      createLayerRequest(
        "fireStations",
        getFireStations,
        renderFireStations,
        "#fire-station-count"),
      createLayerRequest("roads", getRoads, renderRoads, "#road-count"),
      createLayerRequest(
        "incidents",
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
        errors.push({ error: result.reason, labelKey: request.labelKey });
        return;
      }

      if (!Array.isArray(result.value)) {
        errors.push({ labelKey: request.labelKey, messageKey: "apiResponseNotList" });
        return;
      }

      const renderedCount = request.render(mapState, result.value);
      setMetricCount(request.countSelector, renderedCount);
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
          setMetricCount("#incident-count", incidentCount);
        }
      });
  } catch (error) {
    errors.push({ error });
  } finally {
    loadingIndicator.hidden = true;
    showErrors(errors);
  }
}

function configureLanguageSelector() {
  const selector = document.querySelector("#language-selector");
  selector.value = getLanguage();
  selector.addEventListener("change", () => setLanguage(selector.value));
  onLanguageChanged(refreshLocalizedUi);
}

function refreshLocalizedUi() {
  refreshMapTranslations(uiState.mapState);
  refreshMetricCounts();
  refreshAnalysisUi();
  refreshCoverageUi();
  refreshIncidentUi();
  showErrors(uiState.startupErrors);
}

function refreshAnalysisUi() {
  const button = document.querySelector("#find-nearest-services");
  button.textContent = t(uiState.analysis.isLoading
    ? "findingNearestServices"
    : "findNearestServices");

  if (uiState.analysis.isLoading) {
    showStatus("#analysis-status", "calculatingDistances", "analysis-status--error");
  } else if (uiState.analysis.error) {
    showAnalysisFailure(uiState.analysis.error);
  } else if (uiState.analysis.result) {
    showAnalysisResults(uiState.analysis.result);
  }
}

function refreshCoverageUi() {
  const button = document.querySelector("#analyze-coverage");
  button.textContent = t(uiState.coverage.isLoading
    ? "analyzingCoverage"
    : "analyzeCoverage");

  if (uiState.coverage.isLoading) {
    showStatus("#coverage-status", "classifyingCoverage", "coverage-status--error");
  } else if (uiState.coverage.error) {
    showCoverageFailure(uiState.coverage.error);
  } else if (uiState.coverage.result) {
    showCoverageResults(uiState.coverage.result);
  }
}

function refreshIncidentUi() {
  const button = document.querySelector("#create-incident");
  button.textContent = t(uiState.incident.isLoading
    ? "creatingIncident"
    : "createIncident");

  if (uiState.incident.isLoading) {
    showStatus("#incident-status", "savingIncident", "incident-status--error");
  } else if (uiState.incident.error) {
    showIncidentFailure(uiState.incident.error);
  } else if (uiState.incident.result) {
    showIncidentResult(uiState.incident.result);
  }
}

function showStatus(selector, messageKey, errorClass) {
  const status = document.querySelector(selector);
  status.textContent = t(messageKey);
  status.classList.remove(errorClass);
  status.hidden = false;
}

function configureCoverageButton(getSelectedLocation, getSelectionVersion) {
  const button = document.querySelector("#analyze-coverage");

  button.addEventListener("click", async () => {
    const location = getSelectedLocation();
    if (!location) {
      showCoverageFailure(createUiError("selectPointFirst"));
      return;
    }

    const requestVersion = getSelectionVersion();
    setCoverageLoading(true);

    try {
      const result = await getCoverageAnalysis(
        location.latitude,
        location.longitude);

      if (requestVersion !== getSelectionVersion()) {
        return;
      }

      showCoverageResults(result);
    } catch (error) {
      if (requestVersion === getSelectionVersion()) {
        showCoverageFailure(error);
      }
    } finally {
      if (requestVersion === getSelectionVersion()) {
        setCoverageLoading(false);
      }
    }
  });
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
      showIncidentFailure(createUiError("selectPointFirst"));
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
      showIncidentFailure(error);
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
      showAnalysisFailure(createUiError("selectPointFirst"));
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
        showAnalysisFailure(error);
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
    errors.push({ error, labelKey: "mapConfiguration" });
    return fallbackMapConfiguration;
  }
}

function createLayerRequest(labelKey, load, render, countSelector) {
  return { countSelector, labelKey, load, render };
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
  button.textContent = t("findNearestServices");
  const coverageButton = document.querySelector("#analyze-coverage");
  coverageButton.disabled = false;
  coverageButton.textContent = t("analyzeCoverage");
  document.querySelector("#create-incident").disabled = incidentRequestPending;
}

function resetAnalysisPanel() {
  document.querySelector("#analysis-results").hidden = true;
  document.querySelector("#analysis-status").hidden = true;
  document.querySelector("#analysis-status").textContent = "";
}

function resetCoveragePanel() {
  document.querySelector("#coverage-results").hidden = true;
  const status = document.querySelector("#coverage-status");
  status.hidden = true;
  status.textContent = "";
  status.classList.remove("coverage-status--error");
}

function setCoverageLoading(isLoading) {
  uiState.coverage.isLoading = isLoading;
  if (isLoading) {
    uiState.coverage.error = null;
    uiState.coverage.result = null;
  }

  const button = document.querySelector("#analyze-coverage");
  button.disabled = isLoading;
  button.textContent = t(isLoading ? "analyzingCoverage" : "analyzeCoverage");

  if (isLoading) {
    showStatus("#coverage-status", "classifyingCoverage", "coverage-status--error");
    document.querySelector("#coverage-results").hidden = true;
  }
}

function showCoverageResults(result) {
  uiState.coverage.error = null;
  uiState.coverage.result = result;
  updateCoverageResult(
    "#hospital-coverage-level",
    "#hospital-coverage-distance",
    result.hospital);
  updateCoverageResult(
    "#fire-coverage-level",
    "#fire-coverage-distance",
    result.fireStation);
  setCoverageBadge(
    document.querySelector("#overall-coverage-level"),
    result.overallCoverageLevel);

  document.querySelector("#coverage-status").hidden = true;
  document.querySelector("#coverage-results").hidden = false;
}

function updateCoverageResult(levelSelector, distanceSelector, coverage) {
  setCoverageBadge(document.querySelector(levelSelector), coverage.coverageLevel);
  document.querySelector(distanceSelector).textContent =
    coverage.distanceMeters === null
      ? t("distanceUnavailable")
      : formatDistance(coverage.distanceMeters);
}

function setCoverageBadge(element, coverageLevel) {
  const normalized = String(coverageLevel || "Unavailable").toLowerCase();
  const supportedLevels = ["good", "moderate", "poor", "unavailable"];
  const level = supportedLevels.includes(normalized) ? normalized : "unavailable";
  element.className = `coverage-badge coverage-badge--${level}`;
  element.textContent = t(level);
}

function showCoverageFailure(error) {
  uiState.coverage.error = error;
  uiState.coverage.result = null;
  const status = document.querySelector("#coverage-status");
  status.textContent = getErrorMessage(error);
  status.classList.add("coverage-status--error");
  status.hidden = false;
  document.querySelector("#coverage-results").hidden = true;
}

function resetIncidentPanel() {
  document.querySelector("#incident-result").hidden = true;
  const status = document.querySelector("#incident-status");
  status.hidden = true;
  status.textContent = "";
  status.classList.remove("incident-status--error");
}

function setIncidentLoading(isLoading, hasSelectedLocation = true) {
  uiState.incident.isLoading = isLoading;
  if (isLoading) {
    uiState.incident.error = null;
    uiState.incident.result = null;
  }

  const button = document.querySelector("#create-incident");
  button.disabled = isLoading || !hasSelectedLocation;
  button.textContent = t(isLoading ? "creatingIncident" : "createIncident");

  if (isLoading) {
    showStatus("#incident-status", "savingIncident", "incident-status--error");
    document.querySelector("#incident-result").hidden = true;
  }
}

function showIncidentResult(result) {
  uiState.incident.error = null;
  uiState.incident.result = result;
  const recommendation = result.recommendedService;
  document.querySelector("#incident-status").hidden = true;
  document.querySelector("#created-incident-summary").textContent =
    t("incidentCreated", {
      id: result.incident.id,
      type: translateIncidentType(result.incident.type)
    });
  document.querySelector("#incident-recommendation").textContent = recommendation
    ? t("recommendedService", {
      service: `${recommendation.name || t("unnamedService")} ` +
        `(${formatDistance(recommendation.distanceMeters)})`
    })
    : t("noMatchingService");
  document.querySelector("#incident-result").hidden = false;
}

function showIncidentFailure(error) {
  uiState.incident.error = error;
  uiState.incident.result = null;
  const status = document.querySelector("#incident-status");
  status.textContent = getErrorMessage(error);
  status.classList.add("incident-status--error");
  status.hidden = false;
  document.querySelector("#incident-result").hidden = true;
}

function setAnalysisLoading(isLoading) {
  uiState.analysis.isLoading = isLoading;
  if (isLoading) {
    uiState.analysis.error = null;
    uiState.analysis.result = null;
  }

  const button = document.querySelector("#find-nearest-services");
  button.disabled = isLoading;
  button.textContent = t(isLoading
    ? "findingNearestServices"
    : "findNearestServices");

  if (isLoading) {
    showStatus("#analysis-status", "calculatingDistances", "analysis-status--error");
    document.querySelector("#analysis-results").hidden = true;
  }
}

function showAnalysisResults(result) {
  uiState.analysis.error = null;
  uiState.analysis.result = result;
  updateServiceResult(
    "#nearest-hospital-name",
    "#nearest-hospital-distance",
    result.nearestHospital,
    "noHospitalRecords");
  updateServiceResult(
    "#nearest-fire-station-name",
    "#nearest-fire-station-distance",
    result.nearestFireStation,
    "noFireStationRecords");

  document.querySelector("#analysis-status").hidden = true;
  document.querySelector("#analysis-results").hidden = false;
}

function updateServiceResult(nameSelector, distanceSelector, service, emptyMessageKey) {
  const name = document.querySelector(nameSelector);
  const distance = document.querySelector(distanceSelector);

  if (!service) {
    name.textContent = t(emptyMessageKey);
    distance.textContent = "—";
    return;
  }

  name.textContent = service.name || t("unnamedService");
  distance.textContent = formatDistance(service.distanceMeters);
}

function showAnalysisFailure(error) {
  uiState.analysis.error = error;
  uiState.analysis.result = null;
  const status = document.querySelector("#analysis-status");
  status.textContent = getErrorMessage(error);
  status.classList.add("analysis-status--error");
  status.hidden = false;
  document.querySelector("#analysis-results").hidden = true;
}

function formatDistance(distanceMeters) {
  const distance = Number(distanceMeters);
  if (!Number.isFinite(distance) || distance < 0) {
    return t("distanceUnavailable");
  }

  return distance < 1000
    ? `${new Intl.NumberFormat(getLocale(), { maximumFractionDigits: 0 }).format(distance)} m`
    : `${new Intl.NumberFormat(getLocale(), {
      maximumFractionDigits: 2,
      minimumFractionDigits: 2
    }).format(distance / 1000)} km`;
}

function showErrors(errors) {
  if (errors.length === 0) {
    return;
  }

  const panel = document.querySelector("#error-panel");
  document.querySelector("#error-message").textContent = errors
    .map(formatErrorEntry)
    .join(" ");
  panel.hidden = false;
}

function getErrorMessage(error) {
  if (error?.code === "uiMessage") {
    return t(error.messageKey);
  }

  if (error?.code === "requestTimedOut") {
    return t("requestTimedOut", { path: error.path });
  }

  if (error?.code === "requestFailed") {
    return t("requestFailed", { status: error.status });
  }

  if (error instanceof TypeError) {
    return t("networkError");
  }

  return error instanceof Error ? error.message : t("unexpectedError");
}

function translateIncidentType(value) {
  const normalized = String(value || "Other").toLowerCase();
  return ["fire", "medical", "accident", "other"].includes(normalized)
    ? t(normalized)
    : String(value);
}

function createUiError(messageKey) {
  return Object.assign(new Error(messageKey), {
    code: "uiMessage",
    messageKey
  });
}

function formatErrorEntry(entry) {
  const message = entry.messageKey
    ? t(entry.messageKey)
    : getErrorMessage(entry.error);
  return entry.labelKey ? `${t(entry.labelKey)}: ${message}` : message;
}

function setMetricCount(selector, count) {
  const element = document.querySelector(selector);
  element.dataset.count = String(count);
  element.textContent = new Intl.NumberFormat(getLocale()).format(count);
}

function refreshMetricCounts() {
  document.querySelectorAll("[data-count]").forEach((element) => {
    const count = Number(element.dataset.count);
    if (Number.isFinite(count)) {
      element.textContent = new Intl.NumberFormat(getLocale()).format(count);
    }
  });
}
