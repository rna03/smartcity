import { toLeafletLatLng, toLeafletLine } from "./geo.js";
import { getLocale, t } from "./i18n.js";

const hospitalStyle = {
  bubblingMouseEvents: false,
  color: "#ffffff",
  fillColor: "#1778f2",
  fillOpacity: 0.92,
  radius: 7,
  weight: 2
};

const fireStationStyle = {
  bubblingMouseEvents: false,
  color: "#ffffff",
  fillColor: "#e45b2b",
  fillOpacity: 0.92,
  radius: 7,
  weight: 2
};

const roadStyle = {
  bubblingMouseEvents: false,
  color: "#566573",
  opacity: 0.74,
  weight: 3
};

const selectedLocationStyle = {
  bubblingMouseEvents: false,
  color: "#071521",
  fillColor: "#ffffff",
  fillOpacity: 1,
  radius: 7,
  weight: 3
};

const nearestHospitalStyle = {
  ...hospitalStyle,
  color: "#071521",
  radius: 11,
  weight: 4
};

const nearestFireStationStyle = {
  ...fireStationStyle,
  color: "#071521",
  radius: 11,
  weight: 4
};

const incidentStyles = Object.freeze({
  fire: { fillColor: "#c9343e", color: "#ffffff" },
  medical: { fillColor: "#17875f", color: "#ffffff" },
  accident: { fillColor: "#d88716", color: "#ffffff" },
  other: { fillColor: "#7654b4", color: "#ffffff" }
});

export function initializeMap(configuration, onLocationSelected) {
  if (!window.L) {
    throw new Error(t("leafletLoadError"));
  }

  const map = window.L.map("map", {
    preferCanvas: true,
    zoomControl: false
  });

  window.L.tileLayer("https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png", {
    attribution: "&copy; OpenStreetMap contributors",
    maxZoom: 19
  }).addTo(map);

  const layers = {
    pilotArea: window.L.layerGroup().addTo(map),
    hospitals: window.L.layerGroup().addTo(map),
    fireStations: window.L.layerGroup().addTo(map),
    roads: window.L.layerGroup().addTo(map),
    incidents: window.L.layerGroup().addTo(map),
    analysis: window.L.layerGroup().addTo(map)
  };

  const state = {
    dataBounds: window.L.latLngBounds([]),
    layerControl: null,
    layers,
    map,
    zoomControl: null
  };
  state.layerControl = createLayerControl(state);
  state.zoomControl = createZoomControl(state.map);

  renderPilotArea(layers.pilotArea, configuration);
  setInitialView(map, configuration);

  map.on("click", (event) => {
    const latitude = event.latlng.lat;
    const longitude = event.latlng.lng;
    const location = { latitude, longitude };

    clearAnalysis(state);
    renderSelectedLocation(state, location);

    window.L.popup()
      .setLatLng(event.latlng)
      .setContent(createSelectedLocationPopup(latitude, longitude))
      .openOn(map);

    onLocationSelected(location);
  });

  return state;
}

export function refreshMapTranslations(state) {
  if (!state?.map || !state.layers) {
    return;
  }

  if (state.layerControl) {
    state.map.removeControl(state.layerControl);
  }

  if (state.zoomControl) {
    state.map.removeControl(state.zoomControl);
  }

  state.layerControl = createLayerControl(state);
  state.zoomControl = createZoomControl(state.map);
  state.map.closePopup();
}

function createZoomControl(map) {
  return window.L.control.zoom({
    position: "topleft",
    zoomInTitle: t("zoomIn"),
    zoomOutTitle: t("zoomOut")
  }).addTo(map);
}

function createLayerControl(state) {
  return window.L.control.layers(null, {
    [t("pilotAreaLayer")]: state.layers.pilotArea,
    [t("hospitals")]: state.layers.hospitals,
    [t("fireStations")]: state.layers.fireStations,
    [t("mainRoads")]: state.layers.roads,
    [t("incidents")]: state.layers.incidents,
    [t("analysis")]: state.layers.analysis
  }, {
    collapsed: window.matchMedia("(max-width: 780px)").matches,
    position: "topright"
  }).addTo(state.map);
}

export function renderHospitals(state, hospitals) {
  return renderPointFeatures(
    state,
    hospitals,
    state.layers.hospitals,
    hospitalStyle,
    "hospital");
}

export function renderFireStations(state, fireStations) {
  return renderPointFeatures(
    state,
    fireStations,
    state.layers.fireStations,
    fireStationStyle,
    "fireStation");
}

export function renderRoads(state, roads) {
  let renderedCount = 0;
  let skippedCount = 0;

  for (const road of roads) {
    const latLngs = toLeafletLine(road?.coordinates);
    if (latLngs.length < 2) {
      skippedCount += 1;
      continue;
    }

    const line = window.L.polyline(latLngs, roadStyle)
      .bindPopup(() => createRoadPopup(road));
    line.addTo(state.layers.roads);
    state.dataBounds.extend(line.getBounds());
    renderedCount += 1;
  }

  warnAboutSkippedFeatures("roads", skippedCount);
  return renderedCount;
}

export function renderIncidents(state, incidents) {
  state.layers.incidents.clearLayers();
  let renderedCount = 0;
  let skippedCount = 0;

  for (const incident of incidents) {
    if (renderIncident(state, incident)) {
      renderedCount += 1;
    } else {
      skippedCount += 1;
    }
  }

  warnAboutSkippedFeatures("incidents", skippedCount);
  return renderedCount;
}

export function renderIncident(state, incident, recommendation = null) {
  const latLng = toLeafletLatLng(incident);
  if (!latLng) {
    return false;
  }

  const incidentType = String(incident.type || "other").toLowerCase();
  const typeStyle = incidentStyles[incidentType] || incidentStyles.other;
  window.L.circleMarker(latLng, {
    bubblingMouseEvents: false,
    ...typeStyle,
    fillOpacity: 0.95,
    radius: 9,
    weight: 3
  })
    .bindPopup(() => createIncidentPopup(incident, recommendation))
    .bindTooltip(() => t("incidentPopupTitle", {
      type: formatIncidentType(incident.type)
    }))
    .addTo(state.layers.incidents);

  state.dataBounds.extend(latLng);
  return true;
}

export function fitMapToData(state) {
  if (!state.dataBounds.isValid()) {
    return;
  }

  state.map.fitBounds(state.dataBounds, {
    maxZoom: 16,
    padding: [28, 28]
  });
}

export function clearAnalysis(state) {
  state.layers.analysis.clearLayers();
}

export function renderNearestEmergencyServices(state, result) {
  clearAnalysis(state);

  const selectedLatLng = renderSelectedLocation(state, result?.selectedLocation);
  if (!selectedLatLng) {
    throw new Error(t("invalidAnalysisLocation"));
  }

  renderNearestService(
    state,
    selectedLatLng,
    result.nearestHospital,
    nearestHospitalStyle,
    hospitalStyle.fillColor,
    "nearestHospital");
  renderNearestService(
    state,
    selectedLatLng,
    result.nearestFireStation,
    nearestFireStationStyle,
    fireStationStyle.fillColor,
    "nearestFireStation");
}

function renderSelectedLocation(state, location) {
  const latLng = toLeafletLatLng(location);
  if (!latLng) {
    return null;
  }

  window.L.circleMarker(latLng, selectedLocationStyle)
    .bindTooltip(() => t("selectedLocationTooltip"))
    .addTo(state.layers.analysis);
  return latLng;
}

function renderNearestService(
  state,
  selectedLatLng,
  service,
  markerStyle,
  lineColor,
  labelKey) {
  if (!service) {
    return;
  }

  const serviceLatLng = toLeafletLatLng(service);
  if (!serviceLatLng) {
    console.warn(`${labelKey} has invalid coordinates and was not rendered.`);
    return;
  }

  window.L.polyline([selectedLatLng, serviceLatLng], {
    bubblingMouseEvents: false,
    color: lineColor,
    dashArray: "7 7",
    opacity: 0.88,
    weight: 3
  })
    .bindTooltip(() => t("straightLineDistance", { service: t(labelKey) }))
    .addTo(state.layers.analysis);

  window.L.circleMarker(serviceLatLng, markerStyle)
    .bindPopup(() => createAnalysisPopup(service, labelKey))
    .bindTooltip(() => t(labelKey))
    .addTo(state.layers.analysis);
}

function renderPointFeatures(state, features, layer, style, featureType) {
  let renderedCount = 0;
  let skippedCount = 0;

  for (const feature of features) {
    const latLng = toLeafletLatLng(feature);
    if (!latLng) {
      skippedCount += 1;
      continue;
    }

    window.L.circleMarker(latLng, style)
      .bindPopup(() => createPointPopup(feature, featureType))
      .addTo(layer);
    state.dataBounds.extend(latLng);
    renderedCount += 1;
  }

  warnAboutSkippedFeatures(`${featureType} features`, skippedCount);
  return renderedCount;
}

function setInitialView(map, configuration) {
  const bounds = getPilotAreaBounds(configuration);
  if (bounds) {
    map.fitBounds(bounds);
    return;
  }

  map.setView([
    Number(configuration.centerLatitude),
    Number(configuration.centerLongitude)
  ], 13);
}

function renderPilotArea(layer, configuration) {
  const bounds = getPilotAreaBounds(configuration);
  if (!bounds) {
    console.warn("Pilot area bounds are invalid; boundary was not rendered.");
    return;
  }

  const rectangle = window.L.rectangle(bounds, {
    bubblingMouseEvents: false,
    color: "#16849b",
    dashArray: "7 6",
    fillColor: "#16849b",
    fillOpacity: 0.06,
    weight: 2
  });

  rectangle
    .bindPopup(() =>
      `<div class="feature-popup"><h3>${escapeHtml(configuration.pilotArea)}</h3>` +
      `<dl><dt>${t("southWest")}</dt><dd>${bounds[0][0].toFixed(5)}, ${bounds[0][1].toFixed(5)}</dd>` +
      `<dt>${t("northEast")}</dt><dd>${bounds[1][0].toFixed(5)}, ${bounds[1][1].toFixed(5)}</dd></dl></div>`)
    .addTo(layer);
}

function getPilotAreaBounds(configuration) {
  const bounds = configuration?.bounds;
  const south = Number(bounds?.south);
  const west = Number(bounds?.west);
  const north = Number(bounds?.north);
  const east = Number(bounds?.east);

  if (!Number.isFinite(south) ||
      !Number.isFinite(west) ||
      !Number.isFinite(north) ||
      !Number.isFinite(east) ||
      south >= north ||
      west >= east) {
    return null;
  }

  return [[south, west], [north, east]];
}

function createPointPopup(feature, featureType) {
  const unnamedKey = featureType === "hospital"
    ? "unnamedHospital"
    : "unnamedFireStation";
  return `<div class="feature-popup">` +
    `<h3>${escapeHtml(feature.name || t(unnamedKey))}</h3>` +
    `<dl><dt>${t("source")}</dt><dd>${escapeHtml(feature.source || t("unknown"))}</dd>` +
    `<dt>${t("externalId")}</dt><dd>${escapeHtml(feature.externalId || "—")}</dd>` +
    `<dt>${t("latitude")}</dt><dd>${Number(feature.latitude).toFixed(6)}</dd>` +
    `<dt>${t("longitude")}</dt><dd>${Number(feature.longitude).toFixed(6)}</dd></dl>` +
    `</div>`;
}

function createRoadPopup(road) {
  return `<div class="feature-popup">` +
    `<h3>${escapeHtml(road.name || t("unnamedRoad"))}</h3>` +
    `<dl><dt>${t("roadType")}</dt><dd>${escapeHtml(road.roadType || t("unknown"))}</dd>` +
    `<dt>${t("source")}</dt><dd>${escapeHtml(road.source || t("unknown"))}</dd>` +
    `<dt>${t("externalId")}</dt><dd>${escapeHtml(road.externalId || "—")}</dd></dl>` +
    `</div>`;
}

function createAnalysisPopup(service, labelKey) {
  return `<div class="feature-popup">` +
    `<h3>${escapeHtml(t(labelKey))}</h3>` +
    `<dl><dt>${t("name")}</dt><dd>${escapeHtml(service.name || t("unnamed"))}</dd>` +
    `<dt>${t("externalId")}</dt><dd>${escapeHtml(service.externalId || "—")}</dd>` +
    `<dt>${t("distance")}</dt><dd>${formatDistance(service.distanceMeters)}</dd></dl>` +
    `</div>`;
}

function createIncidentPopup(incident, recommendation) {
  const description = incident.description
    ? `<dt>${t("description")}</dt><dd>${escapeHtml(incident.description)}</dd>`
    : "";
  const recommendedService = recommendation
    ? `<dt>${t("recommended")}</dt><dd>${escapeHtml(recommendation.name || t("unnamedService"))}</dd>` +
      `<dt>${t("distance")}</dt><dd>${formatDistance(recommendation.distanceMeters)}</dd>`
    : `<dt>${t("recommended")}</dt><dd>${t("noMatchingServiceShort")}</dd>`;

  return `<div class="feature-popup">` +
    `<h3>${escapeHtml(t("incidentPopupTitle", { type: formatIncidentType(incident.type) }))}</h3>` +
    `<dl><dt>${t("created")}</dt><dd>${escapeHtml(formatDate(incident.createdAtUtc))}</dd>` +
    description +
    recommendedService +
    `</dl></div>`;
}

function formatIncidentType(value) {
  const normalized = String(value || "Other").toLowerCase();
  return ["fire", "medical", "accident", "other"].includes(normalized)
    ? t(normalized)
    : String(value);
}

function formatDate(value) {
  const date = new Date(value);
  return Number.isNaN(date.getTime()) ? t("unknown") : date.toLocaleString(getLocale());
}

function formatDistance(distanceMeters) {
  const distance = Number(distanceMeters);
  if (!Number.isFinite(distance) || distance < 0) {
    return t("unknown");
  }

  return distance < 1000
    ? `${new Intl.NumberFormat(getLocale(), { maximumFractionDigits: 0 }).format(distance)} m`
    : `${new Intl.NumberFormat(getLocale(), {
      maximumFractionDigits: 2,
      minimumFractionDigits: 2
    }).format(distance / 1000)} km`;
}

function createSelectedLocationPopup(latitude, longitude) {
  return `<div class="feature-popup"><h3>${t("selectedLocation")}</h3>` +
    `<dl><dt>${t("latitude")}</dt><dd>${latitude.toFixed(6)}</dd>` +
    `<dt>${t("longitude")}</dt><dd>${longitude.toFixed(6)}</dd></dl></div>`;
}

function escapeHtml(value) {
  const element = document.createElement("span");
  element.textContent = String(value);
  return element.innerHTML;
}

function warnAboutSkippedFeatures(featureType, skippedCount) {
  if (skippedCount > 0) {
    console.warn(`Skipped ${skippedCount} invalid ${featureType}.`);
  }
}
