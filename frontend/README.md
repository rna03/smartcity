# SmartCity Frontend

Arayüz HTML, CSS, Vanilla JavaScript ve Leaflet ile hazırlanmıştır.
Frontend ayrı bir framework veya package manager gerektirmez. Dosyalar build
sırasında `SmartCity.Api` çıktısına kopyalanır ve backend ile aynı origin'den sunulur.

## Dosya yapısı

```text
frontend/
├── index.html
├── css/
│   └── styles.css
└── js/
    ├── api.js
    ├── geo.js
    ├── i18n.js
    ├── map.js
    └── app.js
```

- `api.js`: HTTP timeout, `response.ok` kontrolü ve API çağrıları.
- `geo.js`: GIS → Leaflet koordinat sırası dönüşümü.
- `i18n.js`: İngilizce/Türkçe kullanıcı metinleri ve enum gösterim çevirileri.
- `map.js`: Leaflet kurulumu, pilot alan polygon'u, katmanlar, incident popup'ları
  ve `fitBounds`.
- `app.js`: Veri yükleme, spatial analizler, öncelik önizleme, incident oluşturma,
  dashboard yenileme ve UI durumları.

## Çalıştırma

Repository kökünde:

```powershell
dotnet run --project backend/SmartCity.Api --launch-profile http
Start-Process http://localhost:5113
```

Veri endpoint'leri PostgreSQL/PostGIS bağlantısı gerektirir. Veritabanı veya API
ulaşılamazsa harita arayüzü çökmek yerine ilgili hata mesajını gösterir. Leaflet CDN
dosyaları ve OpenStreetMap tile'ları için internet bağlantısı gerekir.
