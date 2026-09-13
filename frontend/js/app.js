import {
  createIncident,
  getAccessibilityAnalysis,
  getCoverageAnalysis,
  getDashboardSummary,
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
  accessibility: { error: null, isLoading: false, result: null },
  analysis: { error: null, isLoading: false, result: null },
  coverage: { error: null, isLoading: false, result: null },
  dashboard: { error: null, isLoading: false, result: null },
  incident: { error: null, isLoading: false, result: null },
  mapState: null,
  startupErrors: []
};

let dashboardRequestVersion = 0;

document.addEventListener("DOMContentLoaded", startApplication);

async function startApplication() {
  applyTranslations();
  configureLanguageSelector();
  void loadDashboardSummary();
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
      uiState.accessibility = { error: null, isLoading: false, result: null };
      uiState.analysis = { error: null, isLoading: false, result: null };
      uiState.coverage = { error: null, isLoading: false, result: null };
      uiState.incident = { error: null, isLoading: false, result: null };
      showSelectedLocation(location, incidentRequestPending);
      resetAnalysisPanel();
      resetCoveragePanel();
      resetAccessibilityPanel();
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
    configureAccessibilityButton(
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

        void loadDashboardSummary();
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
  refreshDashboardUi();
  refreshAnalysisUi();
  refreshCoverageUi();
  refreshAccessibilityUi();
  refreshIncidentUi();
  showErrors(uiState.startupErrors);
}

async function loadDashboardSummary() {
  const requestVersion = ++dashboardRequestVersion;
  setDashboardLoading(true);

  try {
    const result = await getDashboardSummary();
    if (requestVersion === dashboardRequestVersion) {
      showDashboardSummary(result);
    }
  } catch (error) {
    if (requestVersion === dashboardRequestVersion) {
      showDashboardFailure(error);
    }
  } finally {
    if (requestVersion === dashboardRequestVersion) {
      uiState.dashboard.isLoading = false;
    }
  }
}

function refreshDashboardUi() {
  if (uiState.dashboard.result) {
    renderDashboardSummary(uiState.dashboard.result);
  }

  if (uiState.dashboard.isLoading) {
    showStatus("#dashboard-status", "loadingDashboard", "dashboard-status--error");
  } else if (uiState.dashboard.error) {
    showDashboardFailure(uiState.dashboard.error);
  }
}

function setDashboardLoading(isLoading) {
  uiState.dashboard.isLoading = isLoading;
  if (isLoading) {
    uiState.dashboard.error = null;
    showStatus("#dashboard-status", "loadingDashboard", "dashboard-status--error");
  }
}

function showDashboardSummary(result) {
  uiState.dashboard.error = null;
  uiState.dashboard.result = result;
  renderDashboardSummary(result);
  document.querySelector("#dashboard-status").hidden = true;
}

function renderDashboardSummary(result) {
  const counts = {
    fire: normalizeCount(result?.incidentCounts?.fire),
    medical: normalizeCount(result?.incidentCounts?.medical),
    accident: normalizeCount(result?.incidentCounts?.accident),
    other: normalizeCount(result?.incidentCounts?.other)
  };
  const totalIncidents = normalizeCount(result?.totalIncidents);

  setMetricCount("#dashboard-total-incidents", totalIncidents);
  setMetricCount("#dashboard-fire-incidents", counts.fire);
  setMetricCount("#dashboard-medical-incidents", counts.medical);
  setMetricCount("#dashboard-accident-incidents", counts.accident);
  setMetricCount("#dashboard-other-incidents", counts.other);
  renderLatestIncidents(result?.latestIncidents);
  renderIncidentDistribution(counts, totalIncidents);
}

function renderLatestIncidents(latestIncidents) {
  const list = document.querySelector("#latest-incidents-list");
  const emptyMessage = document.querySelector("#no-incidents-message");
  const incidents = Array.isArray(latestIncidents)
    ? latestIncidents.slice(0, 5)
    : [];

  list.replaceChildren();
  list.hidden = incidents.length === 0;
  emptyMessage.hidden = incidents.length !== 0;

  const fragment = document.createDocumentFragment();
  incidents.forEach((incident) => fragment.append(createLatestIncidentItem(incident)));
  list.append(fragment);
}

function createLatestIncidentItem(incident) {
  const incidentType = normalizeIncidentType(incident?.type);
  const item = document.createElement("li");
  item.className =
    `latest-incidents__item latest-incidents__item--${incidentType}`;

  const headline = document.createElement("div");
  headline.className = "latest-incidents__headline";
  const identity = document.createElement("strong");
  const id = Number(incident?.id);
  identity.textContent = `#${Number.isInteger(id) && id > 0 ? id : "—"} ` +
    translateIncidentType(incident?.type);
  const time = document.createElement("time");
  time.dateTime = String(incident?.createdAtUtc || "");
  time.textContent = formatDashboardDate(incident?.createdAtUtc);
  headline.append(identity, time);
  item.append(headline);

  const description = typeof incident?.description === "string"
    ? incident.description.trim()
    : "";
  if (description) {
    const descriptionElement = document.createElement("p");
    descriptionElement.className = "latest-incidents__description";
    descriptionElement.textContent = shortenDescription(description);
    descriptionElement.title = description;
    item.append(descriptionElement);
  }

  return item;
}

function renderIncidentDistribution(counts, totalIncidents) {
  const entries = [
    ["fire", counts.fire],
    ["medical", counts.medical],
    ["accident", counts.accident],
    ["other", counts.other]
  ];

  entries.forEach(([incidentType, count]) => {
    setMetricCount(`#distribution-${incidentType}-count`, count);
    const percentage = totalIncidents === 0
      ? 0
      : Math.min(100, (count / totalIncidents) * 100);
    document.querySelector(`#distribution-${incidentType}-bar`).style.width =
      `${percentage}%`;
  });
}

function showDashboardFailure(error) {
  uiState.dashboard.error = error;
  const status = document.querySelector("#dashboard-status");
  status.textContent = getErrorMessage(error);
  status.classList.add("dashboard-status--error");
  status.hidden = false;
}

function normalizeCount(value) {
  const count = Number(value);
  return Number.isInteger(count) && count >= 0 ? count : 0;
}

function normalizeIncidentType(value) {
  const normalized = String(value || "Other").toLowerCase();
  return ["fire", "medical", "accident", "other"].includes(normalized)
    ? normalized
    : "other";
}

function formatDashboardDate(value) {
  const date = new Date(value);
  return Number.isNaN(date.getTime())
    ? t("unknown")
    : date.toLocaleString(getLocale(), {
      dateStyle: "short",
      timeStyle: "short"
    });
}

function shortenDescription(value) {
  const maximumLength = 72;
  return value.length <= maximumLength
    ? value
    : `${value.slice(0, maximumLength - 1).trimEnd()}…`;
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

function refreshAccessibilityUi() {
  const button = document.querySelector("#analyze-accessibility");
  button.textContent = t(uiState.accessibility.isLoading
    ? "analyzingAccessibility"
    : "analyzeAccessibility");

  if (uiState.accessibility.isLoading) {
    showStatus(
      "#accessibility-status",
      "calculatingAccessibilityScore",
      "accessibility-status--error");
  } else if (uiState.accessibility.error) {
    showAccessibilityFailure(uiState.accessibility.error);
  } else if (uiState.accessibility.result) {
    showAccessibilityResults(uiState.accessibility.result);
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

function configureAccessibilityButton(getSelectedLocation, getSelectionVersion) {
  const button = document.querySelector("#analyze-accessibility");

  button.addEventListener("click", async () => {
    const location = getSelectedLocation();
    if (!location) {
      showAccessibilityFailure(createUiError("selectPointFirst"));
      return;
    }

    const requestVersion = getSelectionVersion();
    setAccessibilityLoading(true);

    try {
      const result = await getAccessibilityAnalysis(
        location.latitude,
        location.longitude);

      if (requestVersion !== getSelectionVersion()) {
        return;
      }

      showAccessibilityResults(result);
    } catch (error) {
      if (requestVersion === getSelectionVersion()) {
        showAccessibilityFailure(error);
      }
    } finally {
      if (requestVersion === getSelectionVersion()) {
        setAccessibilityLoading(false);
      }
    }
  });
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
  const accessibilityButton = document.querySelector("#analyze-accessibility");
  accessibilityButton.disabled = false;
  accessibilityButton.textContent = t("analyzeAccessibility");
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

function resetAccessibilityPanel() {
  document.querySelector("#accessibility-results").hidden = true;
  const status = document.querySelector("#accessibility-status");
  status.hidden = true;
  status.textContent = "";
  status.classList.remove("accessibility-status--error");
}

function setAccessibilityLoading(isLoading) {
  uiState.accessibility.isLoading = isLoading;
  if (isLoading) {
    uiState.accessibility.error = null;
    uiState.accessibility.result = null;
  }

  const button = document.querySelector("#analyze-accessibility");
  button.disabled = isLoading;
  button.textContent = t(isLoading
    ? "analyzingAccessibility"
    : "analyzeAccessibility");

  if (isLoading) {
    showStatus(
      "#accessibility-status",
      "calculatingAccessibilityScore",
      "accessibility-status--error");
    document.querySelector("#accessibility-results").hidden = true;
  }
}

function showAccessibilityResults(result) {
  uiState.accessibility.error = null;
  uiState.accessibility.result = result;

  const totalScore = normalizeScore(result.totalScore, 100);
  document.querySelector("#accessibility-total-score").textContent =
    new Intl.NumberFormat(getLocale()).format(totalScore);
  updateAccessibilityContribution(
    "#hospital-accessibility-score",
    "#hospital-accessibility-distance",
    result.hospital);
  updateAccessibilityContribution(
    "#fire-accessibility-score",
    "#fire-accessibility-distance",
    result.fireStation);

  const level = setAccessibilityLevel(
    document.querySelector("#accessibility-level"),
    result.accessibilityLevel);
  const progress = document.querySelector("#accessibility-score-progress");
  const fill = document.querySelector("#accessibility-score-fill");
  progress.setAttribute("aria-valuenow", String(totalScore));
  progress.setAttribute(
    "aria-valuetext",
    `${totalScore} / 100, ${level === "unknown" ? t("unknown") : t(level)}`);
  fill.className = `accessibility-score-fill accessibility-score-fill--${level}`;
  fill.style.width = `${totalScore}%`;

  document.querySelector("#accessibility-status").hidden = true;
  document.querySelector("#accessibility-results").hidden = false;
}

function updateAccessibilityContribution(
  scoreSelector,
  distanceSelector,
  serviceScore) {
  const score = normalizeScore(serviceScore?.score, 50);
  document.querySelector(scoreSelector).textContent =
    new Intl.NumberFormat(getLocale()).format(score);
  document.querySelector(distanceSelector).textContent =
    serviceScore?.distanceMeters === null ||
    serviceScore?.distanceMeters === undefined
      ? t("distanceUnavailable")
      : formatDistance(serviceScore.distanceMeters);
}

function setAccessibilityLevel(element, accessibilityLevel) {
  const normalized = String(accessibilityLevel || "").toLowerCase();
  const supportedLevels = ["excellent", "good", "moderate", "poor", "critical"];
  const level = supportedLevels.includes(normalized) ? normalized : "unknown";
  element.className =
    `coverage-badge accessibility-badge coverage-badge--${level}`;
  element.textContent = level === "unknown" ? t("unknown") : t(level);
  return level;
}

function normalizeScore(value, maximum) {
  const score = Number(value);
  return Number.isInteger(score) && score >= 0 && score <= maximum ? score : 0;
}

function showAccessibilityFailure(error) {
  uiState.accessibility.error = error;
  uiState.accessibility.result = null;
  const status = document.querySelector("#accessibility-status");
  status.textContent = getErrorMessage(error);
  status.classList.add("accessibility-status--error");
  status.hidden = false;
  document.querySelector("#accessibility-results").hidden = true;
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
