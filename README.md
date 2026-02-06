# Decision Tree Management System

Veri bağımsız karar ağacı yönetim sistemi. Excel ve JSON formatları arasında çift yönlü dönüşüm desteği.

## � Proje Durumu

### Tamamlanan ✅
- **Backend API:** 100% (Tüm CRUD endpoint'leri, seeding, export/import)
- **Ekran 1 - Decision Tree Listesi:** 100% (CRUD, filtreleme, modal)
- **Ekran 2 - Tablo Yönetimi:** 100% (Input/Output tabloları, standardize modal)
- **Ekran 3 - Kolon Yönetimi:** 100% (Ekleme/silme/sıralama, standardize modal)
- **Ekran 4 - Veri Girişi:** 100% (Tablo seçimi, dinamik kolon binding, tab navigation)
- **Export/Import Sistemleri:** 100% (JSON export, Excel export, Excel import)
- **Modal UI Standardizasyonu:** 100% (Cancel buton kaldırıldı, tutarlı button text)
- **Demo Veriler:** 100% (Otomatik seeding - 10 aday, 3 pozisyon, 3 kriter)
- **JSON Depolama:** 100% (MySQL native JSON type, esnek şema)

## �📋 Özellikler

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

### Frontend (Angular 18+)
- ✅ Ekran 1: Karar Ağaçları Yönetimi
  - Filtreleme (kod, ad, durum)
  - CRUD modal'ları
  - Loading/Error states
  - Standardize modal UI
- ✅ Ekran 2: Tablo Yönetimi
  - Input/Output tabloları CRUD
  - Modal'lar (Cancel buton kaldırıldı)
- ✅ Ekran 3: Kolon Yönetimi
  - Kolon ekleme/silme/güncelleme
  - Sıralama (drag-drop)
  - Standardize modal UI
- ✅ Ekran 4: Veri Girişi
  - Tablo seçimi ve veri görüntüleme
  - Dinamik kolon binding
  - Tab-based navigation
  - Signal-based state management
- ✅ Veri Export/Import
  - JSON export (metadata + data)
  - Excel export
  - Excel import

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
├── Services/         # Business logic
├── Migrations/       # EF Core migrations
└── Scripts/          # Seed scripts
```

### Frontend
```
frontend/src/app/
├── pages/            # Ekran component'leri
│   ├── decision-tree-list/
│   ├── table-management/
│   ├── column-management/
│   └── data-entry/
├── services/         # HTTP & data services
└── app.routes.ts     # Routing
```

### Veritabanı Şeması - JSON Depolaması

**`decision_tree_data` Tablosu:**
```sql
CREATE TABLE decision_tree_data (
  id INT PRIMARY KEY AUTO_INCREMENT,
  decision_tree_id INT NOT NULL,
  table_id INT NOT NULL,
  row_index INT NOT NULL,
  row_data_json JSON NOT NULL,  -- ← MySQL native JSON type
  created_at_utc DATETIME(6),
  updated_at_utc DATETIME(6),
  FOREIGN KEY (decision_tree_id) REFERENCES decision_tree(id),
  FOREIGN KEY (table_id) REFERENCES decision_tree_table(id)
);
```

**JSON Depolama Örneği:**
```json
{
  "AdayId": 1,
  "AdayAdi": "Mehmet",
  "AdaySoyadi": "Yılmaz",
  "Email": "mehmet@email.com",
  "DeneyimYili": 8,
  "EgitimSeviyesi": 3,
  "ProgramlamaDilleri": "C#,Java,Python",
  "YabancıDilSeviyesi": 3,
  "BasvuruTarihi": "2024-01-15"
}
```

**Entity Framework Core Konfigürasyonu:**
```csharp
modelBuilder.Entity<DecisionTreeData>(e =>
{
    e.Property(x => x.RowDataJson)
        .HasColumnType("json")      // MySQL JSON type
        .IsRequired();
});
```

**Avantajlar:**
- ✅ Esnek şema (yeni alanlar migration gerektirmez)
- ✅ Dinamik veri yapısı (her satır farklı alanlar olabilir)
- ✅ Tek tablo (normalizasyon gerekmez)
- ✅ Native MySQL JSON sorguları destekli

## 📝 Önemli Notlar

- **Veri Bağımsız:** Tablo ve kolon yapısı dinamik
- **JSON Formatı:** Metadata + Data birleşik
- **Excel Dönüşüm:** Header-based mapping (sıra bağımsız)
- **Versiyonlama:** SchemaVersion desteği
- **Boş Tablolar:** JSON output'ta gösterilmez
- **Direction:** Input/Output ayrımı tablo seviyesinde
- **JSON Depolama:** MySQL native JSON type kullanılır (esnek şema)
- **Demo Veri:** Development ortamında otomatik seeding
- **Modal UI:** Standardize edilmiş modal component'leri (Cancel buton kaldırıldı)
- **State Management:** Angular Signal'ları ile reactive data binding

## 🌱 Demo Veriler

Uygulama başladığında `Development` ortamında otomatik olarak yüklenen örnek veriler:

**Decision Tree:** İş Başvurusu Değerlendirme Sistemi (`JOB_APPLICATION_EVAL`)

**5 Tablo:**
1. **BasvuruBilgileri** (INPUT) - 10 aday, 10 kolon
2. **PozisyonBilgileri** (INPUT) - 3 pozisyon, 8 kolon
3. **PozisyonKriterleri** (INPUT) - 3 kriter seti, 6 kolon
4. **DegerlendirmeSonucu** (OUTPUT) - 10 kolon
5. **AdayUyumluluk** (OUTPUT) - 10 kolon

**Seeding Yöntemi:** `JobApplicationSeedService` (C#) veya `SeedJobApplicationData.sql` (SQL)

## 🔄 Veri Akışı

```
Program.cs (app startup)
    ↓
JobApplicationSeedService.SeedDataAsync()
    ↓
MySQL Database (decision_tree_data with JSON)
    ↓
Backend API: GET /api/decision-trees/{id}/tables
    ↓
Frontend: tableService.getTables() → Signal update
    ↓
HTML: *ngFor loop renders data in table
```

## 👤 Geliştirici

Elif Turanlı (@eliftrnl)

## 📅 Geliştirme Tarihi ve Aşamaları

### 📌 Session 1-3 (29 Ocak 2026)
**Backend & Temel Altyapı**
- ✅ C# .NET 8.0 projesi oluşturuldu
- ✅ MySQL veritabanı ve EF Core migrations
- ✅ 5 entity ve table yapısı (Decision Tree, Table, Column, Data, Validation Log)
- ✅ RESTful API endpoints (Swagger UI desteği)
- ✅ CRUD operasyonları (Decision Tree, Table, Column)
- ✅ Decision Tree List screen (filtreleme)
- ✅ Table Management screen (input/output tabloları)
- ✅ Column Management screen (metadata + reorder)
- ✅ JSON export (metadata + data)
- ✅ Excel export 
- ✅ Excel import
- ✅ Data Entry screen (tab-based navigation, dinamik kolon binding)

### 📌 Session 4 (6 Şubat 2026) - ✨ FINALIZES & POLISH
**Modal UI Standardizasyonu**
- ✅ Table Management modal: Cancel buton kaldırıldı, "Oluştur" → "Kaydet"
- ✅ Column Management modal: Cancel buton kaldırıldı
- ✅ Decision Tree List modal: Önceki session'da standardize edilmiş

**Dokumentasyon & Anlayış**
- ✅ JSON depolama mekanizması tam olarak belgelendirildi
- ✅ Veri akışı (backend → database → frontend) açıklandı
- ✅ Demo veri seeding süreci (3 yöntem: C#, SQL, otomatik) dokumente edildi
- ✅ MySQL native JSON type konfigürasyonu açıklandı
- ✅ Frontend Angular Signal-based architecture açıklandı

**Demo Veriler (Otomatik Seeding)**
- 10 aday (BasvuruBilgileri tablosu)
- 3 pozisyon (PozisyonBilgileri tablosu)
- 3 kriter seti (PozisyonKriterleri tablosu)
- Output tablolarına placeholder yapısı

### Git Commit Tarihi
- **ef4dc85** (6 Şubat 2026): "UI: Modal güncellemeleri - iptal butonları kaldırıldı"
- Son push: Güncellenmiş README (JSON depolama + demo veriler + veri akışı belgelendirildi)
