export function isValidLongitudeLatitude(longitude, latitude) {
  const lon = Number(longitude);
  const lat = Number(latitude);

  return Number.isFinite(lon) &&
    Number.isFinite(lat) &&
    lon >= -180 &&
    lon <= 180 &&
    lat >= -90 &&
    lat <= 90;
}

export function toLeafletLatLng(coordinate) {
  if (!coordinate ||
      !isValidLongitudeLatitude(coordinate.longitude, coordinate.latitude)) {
    return null;
  }

  // API/PostGIS uses [longitude, latitude]; Leaflet expects [latitude, longitude].
  return [Number(coordinate.latitude), Number(coordinate.longitude)];
}

export function toLeafletLine(coordinates) {
  if (!Array.isArray(coordinates)) {
    return [];
  }

  return coordinates
    .map(toLeafletLatLng)
    .filter((coordinate) => coordinate !== null);
}
