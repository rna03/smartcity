const storageKey = "smartcity-language";
const defaultLanguage = "en";
const supportedLanguages = new Set(["en", "tr"]);

export const translations = Object.freeze({
  en: Object.freeze({
    pageDescription: "Interactive emergency service map for SmartCity Location Intelligence.",
    appTitle: "SmartCity Location Intelligence",
    dashboardEyebrow: "Geospatial Operations Dashboard",
    appSubtitle: "Emergency Service Location Analysis",
    mapDataSource: "OpenStreetMap data",
    language: "Language",
    english: "English",
    turkish: "Türkçe",
    mapInformation: "Map information",
    pilotArea: "Pilot area",
    loadingConfiguration: "Loading configuration...",
    currentRecords: "Current records available through the SmartCity API.",
    hospitals: "Hospitals",
    fireStations: "Fire Stations",
    roads: "Roads",
    incidents: "Incidents",
    mapSelection: "Map selection",
    selectedLocation: "Selected Location",
    selectPointHelp: "Click an empty point on the map to inspect its coordinates.",
    selectPointFirst: "Select a point on the map first.",
    latitude: "Latitude",
    longitude: "Longitude",
    findNearestServices: "Find Nearest Emergency Services",
    findingNearestServices: "Finding nearest services...",
    calculatingDistances: "Calculating PostGIS straight-line distances...",
    nearestHospital: "Nearest Hospital",
    nearestFireStation: "Nearest Fire Station",
    nearestAnalysisNote: "Dashed lines visualize straight-line geodesic distance, not road routes.",
    noHospitalRecords: "No hospital records are available.",
    noFireStationRecords: "No fire station records are available.",
    analyzeCoverage: "Analyze Coverage",
    analyzingCoverage: "Analyzing coverage...",
    classifyingCoverage: "Classifying emergency-service coverage...",
    hospitalCoverage: "Hospital Coverage",
    fireStationCoverage: "Fire Station Coverage",
    overallCoverage: "Overall Coverage",
    coverageNote: "Coverage uses straight-line geodesic distance, not travel time.",
    analyzeAccessibility: "Analyze Accessibility",
    analyzingAccessibility: "Analyzing accessibility...",
    calculatingAccessibilityScore: "Calculating the accessibility score...",
    accessibilityScore: "Accessibility Score",
    accessibilityLevel: "Accessibility Level",
    hospitalContribution: "Hospital Contribution",
    fireStationContribution: "Fire Station Contribution",
    accessibilityExplanation: "This score is based on straight-line distance to the nearest hospital and fire station.",
    excellent: "Excellent",
    critical: "Critical",
    good: "Good",
    moderate: "Moderate",
    poor: "Poor",
    unavailable: "Unavailable",
    distanceUnavailable: "Distance unavailable",
    emergencyReport: "Emergency report",
    createIncident: "Create Incident",
    creatingIncident: "Creating incident...",
    selectIncidentHelp: "Select a map location, then describe the incident.",
    incidentType: "Incident Type",
    fire: "Fire",
    medical: "Medical",
    accident: "Accident",
    other: "Other",
    description: "Description",
    optional: "(optional)",
    incidentDescriptionPlaceholder: "Briefly describe the incident",
    savingIncident: "Saving incident and finding the recommended service...",
    incidentCreated: "{type} incident #{id} was created.",
    recommendedService: "Recommended service: {service}",
    noMatchingService: "No matching emergency service is currently available.",
    mapLegend: "Map legend",
    mapLayers: "Map layers",
    pilotAreaLayer: "Pilot Area",
    mainRoads: "Main Roads",
    nearestServiceAnalysis: "Nearest-service analysis",
    analysis: "Analysis",
    interactiveCityMap: "Interactive city map",
    interactiveMapDescription: "Interactive map showing hospitals, fire stations and main roads",
    dataCouldNotBeLoaded: "Data could not be loaded.",
    apiDatabaseCheck: "Check that the API and database are running.",
    mapDataLoading: "Map data is loading...",
    footerAttribution: "Map tiles © OpenStreetMap contributors · Operational data served by SmartCity API",
    mapConfiguration: "Map configuration",
    apiResponseNotList: "API response was not a list.",
    requestFailed: "Request failed with HTTP {status}.",
    requestTimedOut: "The request to {path} timed out.",
    networkError: "The API could not be reached. Check the network connection.",
    unexpectedError: "An unexpected error occurred.",
    leafletLoadError: "Leaflet could not be loaded. Check the browser network connection.",
    invalidAnalysisLocation: "The analysis response contains an invalid selected location.",
    selectedLocationTooltip: "Selected location",
    straightLineDistance: "{service} straight-line distance",
    source: "Source",
    externalId: "External ID",
    unknown: "Unknown",
    unnamed: "Unnamed",
    unnamedHospital: "Unnamed hospital",
    unnamedFireStation: "Unnamed fire station",
    unnamedRoad: "Unnamed road",
    unnamedService: "Unnamed service",
    roadType: "Road type",
    name: "Name",
    distance: "Distance",
    southWest: "South / West",
    northEast: "North / East",
    recommended: "Recommended",
    noMatchingServiceShort: "No matching service available",
    incident: "Incident",
    incidentPopupTitle: "{type} Incident",
    created: "Created",
    zoomIn: "Zoom in",
    zoomOut: "Zoom out"
  }),
  tr: Object.freeze({
    pageDescription: "SmartCity Konum Analiz Sistemi için etkileşimli acil hizmet haritası.",
    appTitle: "SmartCity Konum Analiz Sistemi",
    dashboardEyebrow: "Mekânsal Operasyon Paneli",
    appSubtitle: "Acil Hizmet Konum Analizi",
    mapDataSource: "OpenStreetMap verileri",
    language: "Dil",
    english: "English",
    turkish: "Türkçe",
    mapInformation: "Harita bilgileri",
    pilotArea: "Pilot bölge",
    loadingConfiguration: "Yapılandırma yükleniyor...",
    currentRecords: "SmartCity API üzerinden erişilebilen güncel kayıtlar.",
    hospitals: "Hastaneler",
    fireStations: "İtfaiye İstasyonları",
    roads: "Yollar",
    incidents: "Olaylar",
    mapSelection: "Harita seçimi",
    selectedLocation: "Seçilen Konum",
    selectPointHelp: "Koordinatlarını incelemek için haritada boş bir noktaya tıklayın.",
    selectPointFirst: "Önce haritada bir nokta seçin.",
    latitude: "Enlem",
    longitude: "Boylam",
    findNearestServices: "En Yakın Acil Hizmetleri Bul",
    findingNearestServices: "En yakın hizmetler bulunuyor...",
    calculatingDistances: "PostGIS düz çizgi mesafelerini hesaplıyor...",
    nearestHospital: "En Yakın Hastane",
    nearestFireStation: "En Yakın İtfaiye İstasyonu",
    nearestAnalysisNote: "Kesikli çizgiler yol rotasını değil, jeodezik düz çizgi mesafesini gösterir.",
    noHospitalRecords: "Kullanılabilir hastane kaydı yok.",
    noFireStationRecords: "Kullanılabilir itfaiye istasyonu kaydı yok.",
    analyzeCoverage: "Kapsama Analizi Yap",
    analyzingCoverage: "Kapsama analiz ediliyor...",
    classifyingCoverage: "Acil hizmet kapsaması sınıflandırılıyor...",
    hospitalCoverage: "Hastane Kapsaması",
    fireStationCoverage: "İtfaiye İstasyonu Kapsaması",
    overallCoverage: "Genel Kapsama",
    coverageNote: "Kapsama, seyahat süresine değil jeodezik düz çizgi mesafesine dayanır.",
    analyzeAccessibility: "Erişilebilirliği Analiz Et",
    analyzingAccessibility: "Erişilebilirlik analiz ediliyor...",
    calculatingAccessibilityScore: "Erişilebilirlik skoru hesaplanıyor...",
    accessibilityScore: "Erişilebilirlik Skoru",
    accessibilityLevel: "Erişilebilirlik Düzeyi",
    hospitalContribution: "Hastane Katkısı",
    fireStationContribution: "İtfaiye İstasyonu Katkısı",
    accessibilityExplanation: "Bu skor, en yakın hastane ve itfaiye istasyonuna olan kuş uçuşu mesafeye göre hesaplanır.",
    excellent: "Mükemmel",
    critical: "Kritik",
    good: "İyi",
    moderate: "Orta",
    poor: "Zayıf",
    unavailable: "Kullanılamıyor",
    distanceUnavailable: "Mesafe kullanılamıyor",
    emergencyReport: "Acil durum bildirimi",
    createIncident: "Olay Oluştur",
    creatingIncident: "Olay oluşturuluyor...",
    selectIncidentHelp: "Haritada bir konum seçin, ardından olayı açıklayın.",
    incidentType: "Olay Türü",
    fire: "Yangın",
    medical: "Sağlık",
    accident: "Kaza",
    other: "Diğer",
    description: "Açıklama",
    optional: "(isteğe bağlı)",
    incidentDescriptionPlaceholder: "Olayı kısaca açıklayın",
    savingIncident: "Olay kaydediliyor ve önerilen hizmet bulunuyor...",
    incidentCreated: "{type} olayı #{id} oluşturuldu.",
    recommendedService: "Önerilen hizmet: {service}",
    noMatchingService: "Şu anda uygun bir acil hizmet bulunmuyor.",
    mapLegend: "Harita açıklaması",
    mapLayers: "Harita katmanları",
    pilotAreaLayer: "Pilot Bölge",
    mainRoads: "Ana Yollar",
    nearestServiceAnalysis: "En yakın hizmet analizi",
    analysis: "Analiz",
    interactiveCityMap: "Etkileşimli şehir haritası",
    interactiveMapDescription: "Hastaneleri, itfaiye istasyonlarını ve ana yolları gösteren etkileşimli harita",
    dataCouldNotBeLoaded: "Veriler yüklenemedi.",
    apiDatabaseCheck: "API ve veritabanının çalıştığını kontrol edin.",
    mapDataLoading: "Harita verileri yükleniyor...",
    footerAttribution: "Harita döşemeleri © OpenStreetMap katkıda bulunanları · Operasyonel veriler SmartCity API tarafından sunulur",
    mapConfiguration: "Harita yapılandırması",
    apiResponseNotList: "API yanıtı bir liste değil.",
    requestFailed: "İstek HTTP {status} hatasıyla başarısız oldu.",
    requestTimedOut: "{path} isteği zaman aşımına uğradı.",
    networkError: "API'ye ulaşılamadı. Ağ bağlantısını kontrol edin.",
    unexpectedError: "Beklenmeyen bir hata oluştu.",
    leafletLoadError: "Leaflet yüklenemedi. Tarayıcının ağ bağlantısını kontrol edin.",
    invalidAnalysisLocation: "Analiz yanıtı geçersiz bir seçili konum içeriyor.",
    selectedLocationTooltip: "Seçilen konum",
    straightLineDistance: "{service} düz çizgi mesafesi",
    source: "Kaynak",
    externalId: "Harici Kimlik",
    unknown: "Bilinmiyor",
    unnamed: "Adsız",
    unnamedHospital: "Adsız hastane",
    unnamedFireStation: "Adsız itfaiye istasyonu",
    unnamedRoad: "Adsız yol",
    unnamedService: "Adsız hizmet",
    roadType: "Yol türü",
    name: "Ad",
    distance: "Mesafe",
    southWest: "Güney / Batı",
    northEast: "Kuzey / Doğu",
    recommended: "Önerilen",
    noMatchingServiceShort: "Uygun hizmet bulunmuyor",
    incident: "Olay",
    incidentPopupTitle: "{type} Olayı",
    created: "Oluşturulma",
    zoomIn: "Yakınlaştır",
    zoomOut: "Uzaklaştır"
  })
});

let currentLanguage = readSavedLanguage();

export function getLanguage() {
  return currentLanguage;
}

export function getLocale() {
  return currentLanguage === "tr" ? "tr-TR" : "en-US";
}

export function t(key, parameters = {}) {
  const template = translations[currentLanguage][key] ?? translations.en[key] ?? key;

  return template.replace(/\{(\w+)\}/g, (match, parameterName) =>
    Object.hasOwn(parameters, parameterName) ? String(parameters[parameterName]) : match);
}

export function applyTranslations(root = document) {
  root.querySelectorAll("[data-i18n]").forEach((element) => {
    element.textContent = t(element.dataset.i18n);
  });
  root.querySelectorAll("[data-i18n-placeholder]").forEach((element) => {
    element.setAttribute("placeholder", t(element.dataset.i18nPlaceholder));
  });
  root.querySelectorAll("[data-i18n-aria-label]").forEach((element) => {
    element.setAttribute("aria-label", t(element.dataset.i18nAriaLabel));
  });
  root.querySelectorAll("[data-i18n-content]").forEach((element) => {
    element.setAttribute("content", t(element.dataset.i18nContent));
  });

  document.documentElement.lang = currentLanguage;
  const selector = document.querySelector("#language-selector");
  if (selector) {
    selector.value = currentLanguage;
  }
}

export function setLanguage(language) {
  const normalizedLanguage = supportedLanguages.has(language) ? language : defaultLanguage;
  const changed = normalizedLanguage !== currentLanguage;
  currentLanguage = normalizedLanguage;
  saveLanguage(currentLanguage);
  applyTranslations();

  if (changed) {
    window.dispatchEvent(new CustomEvent("smartcity:languagechange", {
      detail: { language: currentLanguage }
    }));
  }
}

export function onLanguageChanged(listener) {
  window.addEventListener("smartcity:languagechange", listener);
  return () => window.removeEventListener("smartcity:languagechange", listener);
}

function readSavedLanguage() {
  try {
    const savedLanguage = window.localStorage.getItem(storageKey);
    return supportedLanguages.has(savedLanguage) ? savedLanguage : defaultLanguage;
  } catch {
    return defaultLanguage;
  }
}

function saveLanguage(language) {
  try {
    window.localStorage.setItem(storageKey, language);
  } catch {
    // The UI can still switch languages when browser storage is unavailable.
  }
}
