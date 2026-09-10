# SmartCity Location Intelligence

Akıllı Şehir Acil Hizmet Lokasyon Analiz Sistemi. Phase 2, mevcut Phase 1
mimarisi üzerine OpenStreetMap/Overpass veri içe aktarma hattını ekler.

## Mimari

- `SmartCity.Domain`: Entity'ler ve GIS veri tipleri. HTTP veya Overpass bilmez.
- `SmartCity.Application`: Import use-case'i, port interface'leri ve API DTO'ları.
- `SmartCity.Infrastructure`: Overpass istemcisi, JSON mapping ve EF Core persistence.
- `SmartCity.Api`: Configuration, dependency injection ve ince HTTP controller'ları.
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
  "OverpassUrl": "https://overpass-api.de/api/interpreter",
  "TimeoutSeconds": 60,
  "MaxResponseBytes": 26214400
}
```

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

GeoJSON serialization paketi eklenmedi. Phase 2 DTO'ları koordinatları açıkça
taşır; gerçek GeoJSON `FeatureCollection` sözleşmesi Leaflet entegrasyonuyla
birlikte Phase 3'te seçilecektir.

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

## Phase sınırı

Phase 2; import, doğrulama amaçlı read endpoint'leri ve testlerle tamamlanır.
Frontend, Leaflet, risk skoru, GeoPandas ve lokasyon önerisi bu Phase'e dahil değildir.
