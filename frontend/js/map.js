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
    roads: window.L.layerGroup().addTo(map)
  };

  window.L.control.layers(null, {
    "Pilot Area": layers.pilotArea,
    Hospitals: layers.hospitals,
    "Fire Stations": layers.fireStations,
    "Main Roads": layers.roads
  }, {
    collapsed: window.matchMedia("(max-width: 780px)").matches,
    position: "topright"
  }).addTo(map);

  renderPilotArea(layers.pilotArea, configuration);
  setInitialView(map, configuration);
  map.on("click", (event) => {
    const latitude = event.latlng.lat;
    const longitude = event.latlng.lng;

    window.L.popup()
      .setLatLng(event.latlng)
      .setContent(
        `<div class="feature-popup"><h3>Selected Location</h3>` +
        `<dl><dt>Latitude</dt><dd>${latitude.toFixed(6)}</dd>` +
        `<dt>Longitude</dt><dd>${longitude.toFixed(6)}</dd></dl></div>`)
      .openOn(map);

    onLocationSelected({ latitude, longitude });
  });

  return {
    dataBounds: window.L.latLngBounds([]),
    layers,
    map
  };
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

export function fitMapToData(state) {
  if (!state.dataBounds.isValid()) {
    return;
  }

  state.map.fitBounds(state.dataBounds, {
    maxZoom: 16,
    padding: [28, 28]
  });
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
