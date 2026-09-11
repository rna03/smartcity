import { toLeafletLatLng, toLeafletLine } from "./geo.js";

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
    throw new Error("Leaflet could not be loaded. Check the browser network connection.");
  }

  const map = window.L.map("map", {
    preferCanvas: true,
    zoomControl: true
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

  window.L.control.layers(null, {
    "Pilot Area": layers.pilotArea,
    Hospitals: layers.hospitals,
    "Fire Stations": layers.fireStations,
    "Main Roads": layers.roads,
    Incidents: layers.incidents,
    Analysis: layers.analysis
  }, {
    collapsed: window.matchMedia("(max-width: 780px)").matches,
    position: "topright"
  }).addTo(map);

  renderPilotArea(layers.pilotArea, configuration);
  setInitialView(map, configuration);
  const state = {
    dataBounds: window.L.latLngBounds([]),
    layers,
    map
  };

  map.on("click", (event) => {
    const latitude = event.latlng.lat;
    const longitude = event.latlng.lng;
    const location = { latitude, longitude };

    clearAnalysis(state);
    renderSelectedLocation(state, location);

    window.L.popup()
      .setLatLng(event.latlng)
      .setContent(
        `<div class="feature-popup"><h3>Selected Location</h3>` +
        `<dl><dt>Latitude</dt><dd>${latitude.toFixed(6)}</dd>` +
        `<dt>Longitude</dt><dd>${longitude.toFixed(6)}</dd></dl></div>`)
      .openOn(map);

    onLocationSelected(location);
  });

  return state;
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
    "fire station");
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
      .bindPopup(createRoadPopup(road));
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
    .bindPopup(createIncidentPopup(incident, recommendation))
    .bindTooltip(`${formatIncidentType(incident.type)} incident`)
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
    throw new Error("The analysis response contains an invalid selected location.");
  }

  renderNearestService(
    state,
    selectedLatLng,
    result.nearestHospital,
    nearestHospitalStyle,
    hospitalStyle.fillColor,
    "Nearest Hospital");
  renderNearestService(
    state,
    selectedLatLng,
    result.nearestFireStation,
    nearestFireStationStyle,
    fireStationStyle.fillColor,
    "Nearest Fire Station");
}

function renderSelectedLocation(state, location) {
  const latLng = toLeafletLatLng(location);
  if (!latLng) {
    return null;
  }

  window.L.circleMarker(latLng, selectedLocationStyle)
    .bindTooltip("Selected location")
    .addTo(state.layers.analysis);
  return latLng;
}

function renderNearestService(
  state,
  selectedLatLng,
  service,
  markerStyle,
  lineColor,
  label) {
  if (!service) {
    return;
  }

  const serviceLatLng = toLeafletLatLng(service);
  if (!serviceLatLng) {
    console.warn(`${label} has invalid coordinates and was not rendered.`);
    return;
  }

  window.L.polyline([selectedLatLng, serviceLatLng], {
    bubblingMouseEvents: false,
    color: lineColor,
    dashArray: "7 7",
    opacity: 0.88,
    weight: 3
  })
    .bindTooltip(`${label} straight-line distance`)
    .addTo(state.layers.analysis);

  window.L.circleMarker(serviceLatLng, markerStyle)
    .bindPopup(createAnalysisPopup(service, label))
    .bindTooltip(label)
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
      .bindPopup(createPointPopup(feature, featureType))
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
    .bindPopup(
      `<div class="feature-popup"><h3>${escapeHtml(configuration.pilotArea)}</h3>` +
      `<dl><dt>South / West</dt><dd>${bounds[0][0].toFixed(5)}, ${bounds[0][1].toFixed(5)}</dd>` +
      `<dt>North / East</dt><dd>${bounds[1][0].toFixed(5)}, ${bounds[1][1].toFixed(5)}</dd></dl></div>`)
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
  return `<div class="feature-popup">` +
    `<h3>${escapeHtml(feature.name || `Unnamed ${featureType}`)}</h3>` +
    `<dl><dt>Source</dt><dd>${escapeHtml(feature.source || "Unknown")}</dd>` +
    `<dt>External ID</dt><dd>${escapeHtml(feature.externalId || "—")}</dd>` +
    `<dt>Latitude</dt><dd>${Number(feature.latitude).toFixed(6)}</dd>` +
    `<dt>Longitude</dt><dd>${Number(feature.longitude).toFixed(6)}</dd></dl>` +
    `</div>`;
}

function createRoadPopup(road) {
  return `<div class="feature-popup">` +
    `<h3>${escapeHtml(road.name || "Unnamed road")}</h3>` +
    `<dl><dt>Road type</dt><dd>${escapeHtml(road.roadType || "Unknown")}</dd>` +
    `<dt>Source</dt><dd>${escapeHtml(road.source || "Unknown")}</dd>` +
    `<dt>External ID</dt><dd>${escapeHtml(road.externalId || "—")}</dd></dl>` +
    `</div>`;
}

function createAnalysisPopup(service, label) {
  return `<div class="feature-popup">` +
    `<h3>${escapeHtml(label)}</h3>` +
    `<dl><dt>Name</dt><dd>${escapeHtml(service.name || "Unnamed")}</dd>` +
    `<dt>External ID</dt><dd>${escapeHtml(service.externalId || "—")}</dd>` +
    `<dt>Distance</dt><dd>${formatDistance(service.distanceMeters)}</dd></dl>` +
    `</div>`;
}

function createIncidentPopup(incident, recommendation) {
  const description = incident.description
    ? `<dt>Description</dt><dd>${escapeHtml(incident.description)}</dd>`
    : "";
  const recommendedService = recommendation
    ? `<dt>Recommended</dt><dd>${escapeHtml(recommendation.name || "Unnamed service")}</dd>` +
      `<dt>Distance</dt><dd>${formatDistance(recommendation.distanceMeters)}</dd>`
    : `<dt>Recommended</dt><dd>No matching service available</dd>`;

  return `<div class="feature-popup">` +
    `<h3>${escapeHtml(formatIncidentType(incident.type))} Incident</h3>` +
    `<dl><dt>Created</dt><dd>${escapeHtml(formatDate(incident.createdAtUtc))}</dd>` +
    description +
    recommendedService +
    `</dl></div>`;
}

function formatIncidentType(value) {
  const type = String(value || "Other");
  return type.charAt(0).toUpperCase() + type.slice(1).toLowerCase();
}

function formatDate(value) {
  const date = new Date(value);
  return Number.isNaN(date.getTime()) ? "Unknown" : date.toLocaleString();
}

function formatDistance(distanceMeters) {
  const distance = Number(distanceMeters);
  if (!Number.isFinite(distance) || distance < 0) {
    return "Unknown";
  }

  return distance < 1000
    ? `${Math.round(distance)} m`
    : `${(distance / 1000).toFixed(2)} km`;
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
