# 📊 PROJE ÖZETİ - HIZLI REFERANS

**Proje Adı:** Karar Ağacı (Decision Tree) Yönetim Sistemi  
**Tarih:** 5 Şubat 2026  
**Durum:** Backend %95 ✅ | Frontend %25 ⏳

---

## 🎯 PROJE AMACI

Veri işleme ve karar verme süreçlerini yönetmek için veri-bağımsız bir sistem.

**Gerçek Dünya Örneği:**
- 📝 İş başvurularını değerlendirme (Aday bilgileri + Pozisyon kriteri = Karar)
- 🏥 Hastabakıcı seçimi
- 🏦 Kredi başvurusu değerlendirmesi
- 🎓 Öğrenci seçimi

---

## 🏗️ TEKNOLOJİ STACK'İ

| Katman | Teknoloji | Durum |
|--------|-----------|-------|
| **Frontend** | Angular 18+ | ⏳ Yapım (1/4 ekran) |
| **Backend** | C# .NET 8.0 | ✅ Hazır (18 API endpoint) |
| **Database** | MySQL 8.0+ | ✅ Hazır (7 tablo) |
| **ORM** | Entity Framework Core | ✅ Hazır |
| **Excel** | EPPlus | ✅ Hazır |
| **API Stil** | RESTful | ✅ Hazır |

---

## 📁 DOSYA YAPISI

```
decision-tree/
├── backend/
│   └── DecisionTree.Api/
│       ├── Controllers/          ✅ 4 controller
│       │   ├── DecisionTreesController
│       │   ├── DecisionTreeTablesController
│       │   ├── TableColumnsController
│       │   └── DataEntryController
│       │
│       ├── Services/             ✅ 4 servis
│       │   ├── ExcelService
│       │   ├── JsonBuilderService
│       │   ├── ValidationService
│       │   └── JobApplicationSeedService
│       │
│       ├── Entities/             ✅ 6 entity
│       │   ├── DecisionTree
│       │   ├── DecisionTreeTable
│       │   ├── TableColumn
│       │   ├── DecisionTreeData
│       │   ├── ValidationLog
│       │   └── ColumnValueMapping
│       │
│       ├── Data/                 ✅ DbContext
│       │   └── AppDbContext.cs
│       │
│       ├── Migrations/           ✅ 7 migration
│       ├── Contracts/            ✅ DTOs
│       └── Program.cs            ✅ Konfigürasyon
│
├── frontend/
│   └── src/app/
│       ├── pages/
│       │   ├── decision-tree-list/           ✅ Ekran 1
│       │   ├── table-management/             ⏳ Ekran 2
│       │   ├── column-management/            ⏳ Ekran 3
│       │   └── data-entry/                   ⏳ Ekran 4
│       │
│       ├── services/
│       │   ├── decision-tree.service.ts      ✅ Hazır
│       │   ├── table.service.ts              ⏳ Yapılacak
│       │   ├── column.service.ts             ⏳ Yapılacak
│       │   └── data-entry.service.ts         ⏳ Yapılacak
│       │
│       ├── app.config.ts          ✅ HTTP + Router
│       ├── app.routes.ts           ✅ 6 rota
│       └── app.ts
│
├── SISTEM_ACIKLADIRILMASI.md      📝 Temel açıklama
├── PROJE_DETAYLI_ANALIZ.md        📝 Detaylı teknik analiz
└── VERI_AKISI_DIYAGRAMLARI.md    📊 Visual diyagramlar
```

---

## 📊 VERİTABANI TABLO RAPORU

### Tablo İstatistikleri

| Tablo | Sütun Sayısı | İlişki | Amaç |
|-------|------------|--------|------|
| `decision_tree` | 6 | 1:N (Tables) | Ana karar ağacı |
| `decision_tree_table` | 7 | 1:N (Columns) | Input/Output tabloları |
| `decision_tree_column` | 12 | 1:N (Data) | Kolon metadata |
| `decision_tree_data` | 6 | - | Gerçek veriler (JSON) |
| `validation_log` | 6 | - | Hata kayıtları |
| `column_value_mapping` | 5 | - | Kolon reorder geçmişi |

### Veri Depolaması
- **Metadata:** decision_tree, decision_tree_table, decision_tree_column
- **Gerçek Veriler:** decision_tree_data (RowDataJson - JSON format)
- **Hata Takibi:** validation_log

---

## 🔌 API ENDPOİNT ÖZETI

### Ekran 1: Karar Ağaçları (✅ Tamamlandı)
```
GET    /api/decision-trees                      → Listele (filtreleme)
GET    /api/decision-trees/{id}                 → Getir (tablolar + kolonlar)
GET    /api/decision-trees/exists?code=xxx      → Duplicate check
POST   /api/decision-trees                      → Oluştur
PUT    /api/decision-trees/{id}                 → Güncelle
DELETE /api/decision-trees/{id}                 → Sil (CASCADE)
```

### Ekran 2: Tablo Yönetimi (✅ Backend Hazır)
```
GET    /api/decision-trees/{dtId}/tables
POST   /api/decision-trees/{dtId}/tables
PUT    /api/decision-trees/{dtId}/tables/{tableId}
DELETE /api/decision-trees/{dtId}/tables/{tableId}
```

### Ekran 3: Kolon Yönetimi (✅ Backend Hazır)
```
GET    /api/decision-trees/{dtId}/tables/{tableId}/columns
POST   /api/decision-trees/{dtId}/tables/{tableId}/columns
PUT    /api/decision-trees/{dtId}/tables/{tableId}/columns/{columnId}
DELETE /api/decision-trees/{dtId}/tables/{tableId}/columns/{columnId}
PUT    /api/decision-trees/{dtId}/tables/{tableId}/reorder-columns
```

### Ekran 4: Veri Girişi (✅ Backend Hazır)
```
GET    /api/decision-trees/{dtId}/data/tables/{tableId}/rows
POST   /api/decision-trees/{dtId}/data/tables/{tableId}/rows
PUT    /api/decision-trees/{dtId}/data/tables/{tableId}/rows/{rowId}
DELETE /api/decision-trees/{dtId}/data/tables/{tableId}/rows/{rowId}

POST   /api/decision-trees/{dtId}/data/import-excel
GET    /api/decision-trees/{dtId}/data/export-excel

POST   /api/decision-trees/{dtId}/data/import-json
GET    /api/decision-trees/{dtId}/data/export-json
GET    /api/decision-trees/{dtId}/data/export-json-string
```

---

## 💾 VERİ AKIŞI ÖZET

### Excel → Database
```
Excel (.xlsx)
  ↓ EPPlus ile oku
  ↓ Başlık eşleştir (ColumnName/ExcelHeaderName)
  ↓ Veri tipi dönüştür (String → Int/Date/Decimal/etc.)
  ↓ ValidationService ile doğrula
  ↓ INSERT decision_tree_data (RowDataJson: JSON)
✅ Database'de kaydedildi
```

### Database → JSON
```
decision_tree_data (RowDataJson okuduk)
  ↓ Metadata topla (decision_tree, tables, columns)
  ↓ Kolon sırasına göre düzenle (OrderIndex)
  ↓ JSON yapısında formatla
  ↓ Response gönder veya dosya indir
✅ JSON'da dışa aktarıldı
```

---

## 🔑 ÖNEMLİ KONSEPTLER

### 1. **Metadata vs Veri**
- **Metadata:** decision_tree_column tablosunda (kolon tanımları)
- **Veri:** decision_tree_data.RowDataJson'da (gerçek veriler - JSON)

```csharp
// Metadata Örneği
var column = new TableColumn {
    ColumnName = "AdayId",
    ExcelHeaderName = "Aday ID",
    DataType = ColumnDataType.Int,
    IsRequired = true,
    OrderIndex = 1
};

// Veri Örneği (JSON olarak saklanır)
var rowDataJson = "{\"AdayId\": 1001, \"AdayAdi\": \"Mehmet\", ...}";
```

### 2. **Tablo Yönü (Direction)**
- **Input (1):** Dış kaynaktan veri alır (Excel/JSON import)
- **Output (2):** İşleme sonucunda veri üretir

### 3. **Durum (StatusCode)**
- **Active (1):** Kullanılan veri
- **Passive (2):** Arşivlenmiş veri

### 4. **Veri Tipler (ColumnDataType)**
- **String (1):** Metin
- **Int (2):** Tam sayı
- **Decimal (3):** Ondalıklı sayı
- **Date (4):** Tarih (çoklu format: dd/MM/yyyy, yyyy-MM-dd, etc.)
- **Boolean (5):** true/false, 1/0, evet/hayır, e/h

---

## 🚀 BAŞLATMA KOMUTU

```bash
# Terminal 1: Backend (:5135)
cd backend/DecisionTree.Api
dotnet run

# Terminal 2: Frontend (:4200)
cd frontend
ng serve
# veya npm start

# Browser'de açın
http://localhost:4200
```

---

## 📊 MIGRASYON TARİHÇESİ

| Migration | Tarih | İçerik |
|-----------|-------|--------|
| InitialSchema | 2025-01-29 | Ana tablolar (decision_tree, tables, columns) |
| AddDecisionTreeData | 2025-01-29 | decision_tree_data tablosu |
| RemoveTableCodeAndColumnType | 2025-01-29 | Eski alanları temizle |
| ConvertEnumsToInt | 2025-01-29 | Enum'ları Int'e dönüştür |
| RemoveColumnCode | 2025-01-29 | Gereksiz alanı kaldır |
| RenameTableColumnToDecisionTreeColumn | 2025-01-29 | Tablo adlandırma düzelt |
| AddDataEntryTables | 2025-02-02 | validation_log + column_value_mapping |

---

## 📝 SERVİS FONKSİYONALİTELERİ

### ExcelService
- `ReadExcelAsync()` → EPPlus ile Excel dosyasını oku
- `ConvertCellValue()` → Veri tipi dönüştür
- `TryParseDate()` → Çoklu tarih formatı desteği
- `TryParseBoolean()` → Boolean çeşitleri parse et

### JsonBuilderService
- `BuildJsonExportAsync()` → Metadata + veri birleştir
- `BuildAndSerializeJsonAsync()` → JSON string olarak döner
- Boş tabloları otomatik atlar

### ValidationService
- `ValidateRowAsync()` → Her satırı kontrol et
- `ValidateValue()` → Tek değer doğrulaması
- Hiçbir exception atmaz (graceful error handling)

### JobApplicationSeedService
- Development ortamında otomatik çalışır
- Demo veri yükler (karar ağacı + tablolar + kolonlar + veriler)

---

## 🎨 FRONTEND ROTALAR

```typescript
/                              → /decision-trees (redirect)
/decision-trees                → Ekran 1: Karar Ağaçları
/decision-trees/:id/tables     → Ekran 2: Tablo Yönetimi
/decision-trees/:id/tables/:tableId/columns  → Ekran 3: Kolon Yönetimi
/decision-trees/:id/data       → Ekran 4: Veri Girişi
```

---

## 🔐 CORS AYARLARI

Backend CORS politikası (`Program.cs`):
```csharp
opt.AddPolicy("dev", p =>
    p.WithOrigins(
        "http://localhost:4200",
        "https://localhost:4200",
        "http://127.0.0.1:4200"
    )
    .AllowAnyHeader()
    .AllowAnyMethod()
);
```

Sonuç: Frontend (port 4200) ↔ Backend (port 5135) haberleşmesi başarılı ✅

---

## 💾 VERİTABANI BAĞLANTISI

**appsettings.Development.json:**
```json
{
  "ConnectionStrings": {
    "Default": "Server=localhost;Port=3306;Database=decision_tree_db;User=root;Password=SentinelX123!;"
  }
}
```

---

## ✅ KONTROL LİSTESİ

### Backend (✅ %95 Tamamlandı)
- [x] MySQL database + tablolar
- [x] Entity Framework Core
- [x] 4 Controller (18 endpoint)
- [x] 4 Service (Excel, JSON, Validasyon, Seed)
- [x] CORS konfigürasyonu
- [x] Swagger UI
- [x] Demo veri seed
- [ ] Unit tests (Optional)

### Frontend (⏳ %25 Yapıldı)
- [x] Ekran 1: Karar Ağaçları (CRUD + filtreleme)
- [ ] Ekran 2: Tablo Yönetimi
- [ ] Ekran 3: Kolon Yönetimi (+ drag-drop reorder)
- [ ] Ekran 4: Veri Girişi (Excel/JSON import-export)
- [x] HTTP Services (partial)
- [ ] Error handling
- [ ] Loading states
- [ ] Success notifications

---

## 📚 REFERANS DOSYALAR

- **SISTEM_ACIKLADIRILMASI.md** → Temel sistem açıklaması
- **PROJE_DETAYLI_ANALIZ.md** → Detaylı teknik analiz (370+ satır)
- **VERI_AKISI_DIYAGRAMLARI.md** → Visual diyagramlar ve akışlar
- **PROJE_OZETI_HIZLI_REFERANS.md** → Bu dosya (hızlı referans)

---

## 🤔 SSCAK SORULAR

**S: Excel başlıkları metadata'yla nasıl eşleştirilir?**  
C: `ColumnName` veya `ExcelHeaderName` ile case-insensitive eşleştirme yapılır.

**S: Veriler nerede saklanır?**  
C: `decision_tree_data.RowDataJson` alanında JSON formatında saklanır.

**S: Hata kayıtları nereye yazılır?**  
C: `validation_log` tablosuna yazılır (işlem sırasında veya import sonrası).

**S: Eksik kolon ne olur?**  
C: Required değilse null, required ise hata kaydedilir (row import edilmez).

**S: Excel dosyası format değişirse ne olur?**  
C: Kolon yönetiminde metadata update edilebilir (ExcelHeaderName değiştirilebilir).

---

**Son Güncelleme:** 5 Şubat 2026 🎯
