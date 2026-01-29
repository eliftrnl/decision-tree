# Decision Tree Management System

Veri bağımsız karar ağacı yönetim sistemi. Excel ve JSON formatları arasında çift yönlü dönüşüm desteği.

## 📋 Özellikler

### Backend (C# .NET 8.0)
- ✅ **4 Ekran Desteği:**
  - Ekran 1: Karar Ağaçları Listesi (CRUD)
  - Ekran 2: Tablo Yönetimi (Input/Output tabloları)
  - Ekran 3: Kolon Yönetimi (metadata + reorder)
  - Ekran 4: Veri Girişi + JSON/Excel Dönüşümü
- ✅ RESTful API (Swagger UI)
- ✅ Entity Framework Core 8.0.6
- ✅ MySQL database
- ✅ Metadata + Data birleşik JSON export
- ✅ JSON parse ve import

### Frontend (Angular)
- ✅ Ekran 1: Karar Ağaçları Yönetimi
  - Filtreleme (kod, ad, durum)
  - CRUD modal'ları
  - Loading/Error states
- ⏳ Ekran 2: Tablo Yönetimi (yapım aşamasında)
- ⏳ Ekran 3: Kolon Yönetimi (yapım aşamasında)
- ⏳ Ekran 4: Veri Girişi (yapım aşamasında)

## 🚀 Kurulum

### Gereksinimler
- .NET 8.0 SDK
- Node.js 18+ ve npm
- MySQL 8.0+
- Angular CLI

### Backend Kurulumu

```bash
cd backend/DecisionTree.Api

# Veritabanı bağlantı ayarları
# appsettings.Development.json dosyasını düzenleyin

# Migration'ları uygula
dotnet ef database update

# Başlat
dotnet run
```

Backend: http://localhost:5135
Swagger: http://localhost:5135/swagger

### Frontend Kurulumu

```bash
cd frontend

# Paketleri yükle
npm install

# Başlat
ng serve
```

Frontend: http://localhost:4200

## 🗄️ Veritabanı

```sql
CREATE DATABASE decision_tree_db CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
```

**Tablolar:**
- `decision_tree` - Karar ağaçları
- `decision_tree_table` - Input/Output tabloları
- `table_column` - Kolon metadata
- `decision_tree_data` - Satır verileri (JSON)

## 📡 API Endpoints

### DecisionTrees
- `GET /api/DecisionTrees` - Liste (filtreleme)
- `GET /api/DecisionTrees/{id}` - Detay
- `POST /api/DecisionTrees` - Oluştur
- `PUT /api/DecisionTrees/{id}` - Güncelle
- `DELETE /api/DecisionTrees/{id}` - Sil

### Tables
- `GET /api/decision-trees/{dtId}/tables` - Tablo listesi
- `POST /api/decision-trees/{dtId}/tables` - Tablo ekle
- `PUT /api/decision-trees/{dtId}/tables/{id}` - Tablo güncelle
- `DELETE /api/decision-trees/{dtId}/tables/{id}` - Tablo sil

### Columns
- `GET /api/decision-trees/{dtId}/tables/{tableId}/columns` - Kolon listesi
- `POST /api/decision-trees/{dtId}/tables/{tableId}/columns` - Kolon ekle
- `PUT /api/decision-trees/{dtId}/tables/{tableId}/columns/{id}` - Kolon güncelle
- `PATCH /api/decision-trees/{dtId}/tables/{tableId}/columns/reorder` - Sıralama
- `DELETE /api/decision-trees/{dtId}/tables/{tableId}/columns/{id}` - Kolon sil

### Data Entry
- `GET /api/decision-trees/{dtId}/data/tables/{tableId}/rows` - Satırlar
- `POST /api/decision-trees/{dtId}/data/tables/{tableId}/rows` - Satır ekle
- `PUT /api/decision-trees/{dtId}/data/tables/{tableId}/rows/{rowId}` - Güncelle
- `DELETE /api/decision-trees/{dtId}/data/tables/{tableId}/rows/{rowId}` - Sil
- `POST /api/decision-trees/{dtId}/data/generate-json` - JSON export
- `POST /api/decision-trees/{dtId}/data/parse-json` - JSON import

## 🏗️ Mimari

### Backend
```
DecisionTree.Api/
├── Controllers/       # API endpoints
├── Entities/         # Domain models
├── Data/             # DbContext
├── Contracts/        # DTOs
└── Migrations/       # EF Core migrations
```

### Frontend
```
frontend/src/app/
├── pages/            # Ekran component'leri
├── services/         # HTTP services
└── app.routes.ts     # Routing
```

## 📝 Önemli Notlar

- **Veri Bağımsız:** Tablo ve kolon yapısı dinamik
- **JSON Formatı:** Metadata + Data birleşik
- **Excel Dönüşüm:** Header-based mapping (sıra bağımsız)
- **Versiyonlama:** SchemaVersion desteği
- **Boş Tablolar:** JSON output'ta gösterilmez
- **Direction:** Input/Output ayrımı tablo seviyesinde

## 👤 Geliştirici

Elif Turanlı (@eliftrni)

## 📅 Tarih

29 Ocak 2026
