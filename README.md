# SmartCity Location Intelligence

Akıllı Şehir Acil Hizmet Lokasyon Analiz Sistemi. Phase 3, mevcut Phase 1–2
mimarisi üzerine Leaflet tabanlı interaktif bir GIS harita arayüzü ekler.

## Mimari

- `SmartCity.Domain`: Entity'ler ve GIS veri tipleri. HTTP veya Overpass bilmez.
- `SmartCity.Application`: Import use-case'i, port interface'leri ve API DTO'ları.
- `SmartCity.Infrastructure`: Overpass istemcisi, JSON mapping ve EF Core persistence.
- `SmartCity.Api`: Configuration, dependency injection ve ince HTTP controller'ları.
- `frontend`: HTML, CSS, Vanilla JavaScript ve Leaflet tabanlı harita arayüzü.
- `SmartCity.Infrastructure.Tests`: Kritik OSM → NetTopologySuite mapping testleri.

Generic Repository eklenmedi. Import için yalnızca amaca özel `ISpatialDataStore`
ve okuma için `ISpatialDataQueryService` kullanılır.

## Veri akışı

```text
OpenStreetMap
    ↓
Overpass API
    ↓
OverpassClient
    ↓
OverpassResponseMapper
    ↓
ImportOpenStreetMapDataService
    ↓
OpenStreetMapSpatialDataStore / SmartCityDbContext
    ↓
PostgreSQL / PostGIS
```

## Pilot alan ve bounding box

Pilot alan `backend/SmartCity.Api/appsettings.json` içinden değiştirilir:

```json
"PilotArea": {
  "Name": "Istanbul Besiktas Demo",
  "South": 41.035,
  "West": 28.99,
  "North": 41.055,
  "East": 29.03
}
```

Bounding box sırası `south, west, north, east` şeklindedir. South/North enlem
(`-90..90`), West/East boylam (`-180..180`) değeridir. Ayrıca `South < North`
ve `West < East` olmak zorundadır. Başlangıç alanı özellikle küçüktür; public
Overpass servisine büyük şehir alanını tek sorguda göndermeyin.

Overpass endpoint'i, timeout ve en büyük response boyutu da configuration'dadır:

```json
"OpenStreetMap": {
  "Primary": {
    "Url": "https://overpass.kumi.systems/api/interpreter",
    "TimeoutSeconds": 60
  },
  "Fallback": {
    "Url": "https://overpass-api.de/api/interpreter",
    "TimeoutSeconds": 60
  },
  "MaxResponseBytes": 26214400
}
```

Development ortamında `appsettings.Development.json`, her iki endpoint timeout'unu
90 saniyeye yükseltir. Endpoint'ler kod içinde sabit değildir ve gerektiğinde
environment variable ile değiştirilebilir:

```powershell
$env:OpenStreetMap__Primary__Url = "https://overpass.kumi.systems/api/interpreter"
$env:OpenStreetMap__Primary__TimeoutSeconds = "90"
$env:OpenStreetMap__Fallback__Url = "https://overpass-api.de/api/interpreter"
$env:OpenStreetMap__Fallback__TimeoutSeconds = "90"
```

`HttpClientFactory` ve request `CancellationToken` davranışı korunur. Primary yalnızca
timeout, network hatası veya HTTP 5xx sonrasında bırakılır ve fallback en fazla bir kez
denenir. HTTP 4xx, geçersiz JSON ve response-size hatalarında fallback yapılmaz.
Deneme, timeout, fallback geçişi, HTTP/network hatası ve başarılı endpoint structured
log olarak kaydedilir.

## Alınan OSM verileri

- Hastane: `amenity=hospital`; node, way ve relation desteklenir.
- İtfaiye: `amenity=fire_station`; node, way ve relation desteklenir.
- Yol: `motorway`, `trunk`, `primary`, `secondary`, `tertiary` ve bunların
  `_link` türleri. Bunlar şehirler arası ve şehir içi ana erişim omurgasını temsil
  eder; residential/service gibi küçük yollar ilk MVP sorgusunu şişirmemek için alınmaz.

Node doğrudan `Point` olur. Way/relation hastane veya itfaiye için Overpass'in
`center` alanı temsil noktası olarak kullanılır. Bu değer nesnenin bounding box
merkezidir ve polygon içinde olma garantisi yoktur; MVP için bilinçli bir
yaklaşımdır. Yollar, art arda yinelenen koordinatları temizlenmiş geçerli
`LineString` olarak kaydedilir.

## GIS kararları

- **SRID 4326:** WGS 84 coğrafi koordinat sisteminin kimliğidir; OSM koordinatları
  dünya üzerindeki longitude/latitude değerleri olarak bu sistemde tutulur.
- **Longitude = X:** NetTopologySuite geometrilerinde yatay eksen X'tir ve boylama
  karşılık gelir.
- **Latitude = Y:** Dikey eksen Y'dir ve enleme karşılık gelir.
- **Point:** Hastane/itfaiye gibi tek konumu temsil eder.
- **LineString:** Sıralı koordinatlarla yol güzergâhını temsil eder.
- **Spatial index:** GiST index, yakınlık/kesişim gibi PostGIS sorgularında tüm
  tabloyu taramak yerine aday geometrileri hızla bulmaya yardım eder.
- **OSM kimliği:** `node/123`, `way/123`, `relation/123` biçimi sayısal ID
  çakışmasını önler. `(Source, ExternalId)` unique index'i tekrar importta duplicate
  kaydı engeller.

## Paketler

- NetTopologySuite 2.6.0
- EF Core ve EF Core Design 10.0.4
- Npgsql EF Core/PostGIS 10.0.3
- Microsoft.Extensions.Http 10.0.12 (`IHttpClientFactory`)
- Microsoft.Extensions Options/Logging/DI abstractions 10.0.12
- xUnit 2.9.3 ve Microsoft.NET.Test.Sdk 17.14.1

GeoJSON serialization paketi eklenmedi. Mevcut DTO'lar point verileri için açık
`longitude`/`latitude`, yollar için ise açık koordinat listeleri taşıdığı için
Phase 3 bu sözleşmeyi korur. Böylece yalnızca harita göstermek amacıyla çalışan
API yeniden tasarlanmaz veya yeni bir serialization bağımlılığı eklenmez.

## Phase 4A: En yakın acil servis analizi

Haritada bir nokta seçip **Find Nearest Emergency Services** düğmesine basıldığında
frontend şu salt-okunur endpoint'i çağırır:

```http
GET /api/location-analysis/nearest?latitude=41.04&longitude=29.01
```

En yakın hastane ve itfaiye istasyonu, tablolar uygulama belleğine alınmadan
PostgreSQL tarafında seçilir. PostGIS `ST_Distance(geometry::geography,
point::geography)` hesabı WGS 84 koordinatlarından yaklaşık gerçek dünya mesafesini
metre cinsinden döndürür. Response, seçilen koordinatı ve her iki tesis için kimlik,
konum ve `distanceMeters` değerini içerir; ilgili tablo boşsa o sonuç `null` olur.

Harita seçilen noktayı ve en yakın tesisleri vurgular. Aradaki kesik çizgiler yol
rotası değildir; yalnızca straight-line/geodesic mesafe görselleştirmesidir.
Latitude/longitude aralık dışı, eksik veya sonlu olmayan değerler HTTP 400 döndürür.

## Phase 4B: Incident oluşturma ve servis önerisi

Incident, kullanıcının haritada seçtiği WGS 84 noktada oluşturduğu acil olay
kaydıdır. Desteklenen türler `Fire`, `Medical`, `Accident` ve `Other` değerleridir.
Seçimden sonra sol panelde tür ve en fazla 500 karakterlik opsiyonel açıklama
girilerek incident oluşturulur:

```http
POST /api/incidents
Content-Type: application/json

{
  "type": "Fire",
  "latitude": 41.04,
  "longitude": 29.01,
  "description": "Building fire"
}
```

Başarılı istek HTTP 201 döndürür. `GET /api/incidents` kayıtlı incident'ları
oluşturulma zamanı azalan sırada listeler ve frontend bunları ayrı, kalıcı bir
Leaflet katmanında gösterir.

Öneri eşlemesi şöyledir: `Fire` → en yakın itfaiye, `Medical` ve `Accident` →
en yakın hastane, `Other` → mevcut hastane/itfaiye sonuçlarından mesafesi daha
kısa olan servis. Uygun türde servis yoksa `recommendedService` kontrollü biçimde
`null` olur. En yakın servis bilgisi Phase 4A'nın PostGIS geography analizinden
yeniden kullanılır. Bu öneri ve gösterilen mesafe gerçek yol rotası, yolculuk
süresi veya dispatch kararı değildir.

## Çalıştırma

Gereksinimler: .NET 10 SDK ve Docker Desktop (Compose v2).

```powershell
Copy-Item .env.example .env
docker compose config
docker compose up -d database
docker compose ps
dotnet tool restore
dotnet restore
dotnet tool run dotnet-ef database update --project backend/SmartCity.Infrastructure --startup-project backend/SmartCity.Api
dotnet run --project backend/SmartCity.Api --launch-profile http
```

Migration zaten `InitialCreate` adıyla repodadır. Yeni bir migration gerektiğinde:

```powershell
dotnet tool run dotnet-ef migrations add MigrationName --project backend/SmartCity.Infrastructure --startup-project backend/SmartCity.Api --output-dir Persistence/Migrations
```

Production ortamında parolayı dosyada tutmayın;
`ConnectionStrings__SmartCityDatabase` environment variable'ı gibi bir secret
mekanizmasıyla override edin.

## Test ve API kullanımı

```powershell
dotnet build SmartCity.slnx
dotnet test SmartCity.slnx

Invoke-RestMethod -Method Post http://localhost:5113/api/import/openstreetmap
Invoke-RestMethod http://localhost:5113/api/hospitals
Invoke-RestMethod http://localhost:5113/api/fire-stations
Invoke-RestMethod http://localhost:5113/api/roads
Invoke-RestMethod "http://localhost:5113/api/location-analysis/nearest?latitude=41.04&longitude=29.01"
Invoke-RestMethod http://localhost:5113/api/incidents
Invoke-RestMethod -Method Post http://localhost:5113/api/incidents `
  -ContentType "application/json" `
  -Body '{"type":"Fire","latitude":41.04,"longitude":29.01,"description":"Building fire"}'
Invoke-WebRequest http://localhost:5113/health/live
Invoke-WebRequest http://localhost:5113/health/ready
```

Örnek import cevabı:

```json
{
  "pilotArea": "Istanbul Besiktas Demo",
  "hospitalsFound": 12,
  "hospitalsAdded": 10,
  "fireStationsFound": 5,
  "fireStationsAdded": 4,
  "roadsFound": 142,
  "roadsAdded": 130
}
```

Aynı import yeniden çalıştırıldığında veriler değişmediyse `found` değerleri aynı,
`added` değerleri sıfır olmalıdır. Overpass erişilemezse HTTP 502, veritabanı
erişilemezse HTTP 503 Problem Details cevabı döner.

Örnek hastane cevabı:

```json
[
  {
    "id": 1,
    "name": "Demo Hospital",
    "source": "OpenStreetMap",
    "externalId": "node/123",
    "longitude": 29.01,
    "latitude": 41.05
  }
]
```

## Veri politikası

Bu Phase yalnızca OpenStreetMap kaynaklı gerçek veriyi import eder. Her kayıtta
`Source=OpenStreetMap` tutulur. İleride eklenecek synthetic nüfus/olay verileri
ayrı dizin ve metadata ile açıkça işaretlenecektir.

## Mülakat notları

1. **OpenStreetMap nedir?** Topluluk tarafından üretilen, düzenlenebilir ve açık
   lisanslı dünya harita veritabanıdır.
2. **Overpass API nedir?** OSM verisini tag, nesne tipi ve coğrafi alanla sorgulayan
   salt-okuma servisidir.
3. **PostGIS neden kullanılır?** PostgreSQL'e geometry tipleri, spatial index'ler
   ve mesafe/kesişim fonksiyonları ekler.
4. **Geometry nedir?** Nokta, çizgi veya polygon gibi konumsal şeklin koordinatlı
   veri temsilidir.
5. **SRID nedir?** Koordinatların hangi referans sistemine göre yorumlanacağını
   belirleyen sayısal kimliktir.
6. **Spatial index nedir?** Geometrilerin konumsal kapsamına göre aday kayıtları
   hızlı bulan özel index'tir.
7. **HttpClientFactory neden kullanılır?** Connection pooling, handler ömrü,
   merkezi timeout/configuration ve test edilebilir istemci üretimi sağlar.
8. **ExternalId neden tutulur?** Kaydın dış sistemdeki kalıcı kimliğini izleyerek
   tekrar importta duplicate oluşmasını engeller.
9. **Idempotent import nedir?** Aynı girdiyi tekrar işlediğinizde yeni duplicate
   üretmeden aynı kalıcı sonuca ulaşan import işlemidir.
10. **Bounding box nedir?** Bir coğrafi alanı güney, batı, kuzey ve doğu sınırlarıyla
    tanımlayan dikdörtgendir.

## Phase 3: interaktif GIS haritası

Frontend dosyaları `frontend/` altında backend'den ayrı tutulur. API build sırasında
bu statik dosyaları çıktısına alır ve `/` adresinden sunar. Ayrı bir Node.js veya
Python static server gerekmez.

```text
Browser
  ↓
HTML / CSS / Vanilla JavaScript
  ↓
Leaflet + OpenStreetMap tile layer
  ↓
ASP.NET Core API
  ↓
PostgreSQL / PostGIS
```

Harita şu endpoint'leri kullanır:

- `GET /api/map/config`: `PilotArea` adını, merkezini ve bounds değerlerini verir.
- `GET /api/hospitals`: Hastane marker'larını besler.
- `GET /api/fire-stations`: İtfaiye marker'larını besler.
- `GET /api/roads`: Ana yol polyline'larını besler.

Frontend, `POST /api/import/openstreetmap` endpoint'ini otomatik çağırmaz. Import,
harita açılmadan önce kullanıcının bilinçli olarak çalıştırdığı ayrı bir veri hazırlama
adımıdır.

### Leaflet, OpenStreetMap ve koordinatlar

Leaflet, tarayıcıda interaktif harita, katman, marker, polyline ve popup işlemlerini
yöneten hafif bir JavaScript kütüphanesidir. OpenStreetMap tile layer ise haritanın
görsel tabanını oluşturan döşeme görselleridir; tile ve Leaflet CDN kaynakları için
internet bağlantısı gerekir. OSM attribution harita üzerinde korunur.

PostGIS/API koordinatları GIS standardına uygun biçimde `longitude, latitude`
(X, Y) sırasıyla taşır. Leaflet `latitude, longitude` sırasını bekler. Bu dönüşüm
yalnızca `frontend/js/geo.js` içindeki `toLeafletLatLng()` fonksiyonunda yapılır.

GeoJSON; geometri ve özellikleri `Feature`/`FeatureCollection` yapısında taşıyan
standart bir JSON formatıdır. Leaflet ile doğal uyumlu olsa da mevcut DTO'lar Phase 3
ihtiyacını açık biçimde karşıladığı için sırf frontend adına API GeoJSON'a çevrilmedi.

Katman kontrolü Pilot Area, Hospitals, Fire Stations ve Main Roads katmanlarını bağımsız
açıp kapatır. PilotArea bounds tıklanabilir bir rectangle/polygon olarak gösterilir.
Veri yüklendikten sonra `fitBounds`, geçerli tüm marker ve yolları görünür
alana sığdırır; boş veri varsa mevcut pilot alan görünümünü korur. Haritada boş bir
noktaya tıklandığında latitude/longitude hem popup'ta hem bilgi panelinde gösterilir;
henüz backend'e analiz isteği gönderilmez.

### Çalıştırma ve doğrulama

PowerShell üzerinden repository kökünde:

```powershell
Copy-Item .env.example .env
docker compose up -d database
dotnet tool restore
dotnet restore SmartCity.slnx
dotnet tool run dotnet-ef database update --project backend/SmartCity.Infrastructure --startup-project backend/SmartCity.Api
dotnet run --project backend/SmartCity.Api --launch-profile http
```

İlk veri hazırlama işlemini ayrı bir PowerShell penceresinde çalıştırın:

```powershell
Invoke-RestMethod -Method Post http://localhost:5113/api/import/openstreetmap
Start-Process http://localhost:5113
```

Ardından marker/line popup'larını, layer control seçeneklerini, harita tıklamasını
ve sayaçları kontrol edin. Backend/frontend aynı `http://localhost:5113` origin'ini
kullandığı için CORS policy eklenmedi. Frontend ileride farklı origin'e ayrılırsa
yalnızca bilinen development origin'leri açılmalıdır; production ortamında
`AllowAnyOrigin` kullanılmamalıdır.

```powershell
dotnet build SmartCity.slnx
dotnet test SmartCity.slnx
```

## Phase sınırı

Phase 3; harita görselleştirmesi, mevcut spatial read endpoint'leri, katman yönetimi
ve kullanıcı koordinat seçimiyle tamamlanır. Distance analysis, en yakın acil hizmet
sorguları, risk skoru, GeoPandas ve lokasyon önerisi Phase 4 ve sonrasına aittir.
