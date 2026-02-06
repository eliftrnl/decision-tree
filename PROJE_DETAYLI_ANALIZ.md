# 🎯 KARAR AĞACI (Decision Tree) YÖNETİM SİSTEMİ - KAPSAMLI TEKNIK ANALIZ

**Proje Durumu:** ✅ Backend %95 hazır | ⏳ Frontend ekranları yapım aşamasında

---

## 📑 İÇİNDEKİLER
1. [Sistem Mimarisi](#sistem-mimarisi)
2. [Veritabanı Yapısı](#veritabanı-yapısı)
3. [Backend Katmanı (C# .NET 8.0)](#backend-katmanı)
4. [Frontend Katmanı (Angular)](#frontend-katmanı)
5. [Veri Akışı](#veri-akışı)
6. [Excel/JSON Dönüşümleri](#exceljson-dönüşümleri)

---

## 🏗️ SİSTEM MİMARİSİ

### Genel Mimari Diyagram
```
┌─────────────────────────────────────────────────────────────────┐
│                   FRONTEND (Angular 18+)                        │
│  Bileşenler: Decision-Tree-List, Table-Mgmt, Column-Mgmt,      │
│              Data-Entry (Excel/JSON Import-Export)             │
│                       :4200 Port                                │
└──────────────────────────┬──────────────────────────────────────┘
                           │ HTTP (REST API)
                           │ CORS Enabled
┌──────────────────────────▼──────────────────────────────────────┐
│              BACKEND (C# .NET 8.0 / ASP.NET Core)              │
│                       :5135 Port                                │
│                                                                  │
│  Controllers:                                                    │
│  ├── DecisionTreesController (CRUD işlemleri)                  │
│  ├── DecisionTreeTablesController (Tablo yönetimi)             │
│  ├── TableColumnsController (Kolon yönetimi + metadata)        │
│  └── DataEntryController (Veri girişi + Excel/JSON)            │
│                                                                  │
│  Services:                                                       │
│  ├── ExcelService (EPPlus → Excel okuma/yazma)                │
│  ├── JsonBuilderService (JSON export + metadata)               │
│  ├── ValidationService (Veri doğrulama)                        │
│  ├── JobApplicationSeedService (Demo veri yükleme)             │
│  └── AppDbContext (Entity Framework Core)                      │
│                                                                  │
│  Entities (Veri Modelleri):                                     │
│  ├── DecisionTree (Karar ağacı ana yapısı)                     │
│  ├── DecisionTreeTable (Input/Output tabloları)                │
│  ├── TableColumn (Kolon metadata tanımları)                    │
│  ├── DecisionTreeData (Gerçek veri satırları - JSON)           │
│  ├── ValidationLog (Hata kayıtları)                            │
│  └── ColumnValueMapping (Kolon sıra değişiklikleri)            │
└──────────────────────────┬──────────────────────────────────────┘
                           │ SQL Sorgular (EF Core)
                           │ Connection String
┌──────────────────────────▼──────────────────────────────────────┐
│            MySQL DATABASE (Version 8.0+)                        │
│            Database: decision_tree_db                           │
│            User: root                                           │
└──────────────────────────────────────────────────────────────────┘
```

---

## 🗄️ VERİTABANI YAPISI

### Tablo İlişkileri
```
decision_tree
│   Id (PK)
│   Code (Unique) - "JOB_APPLICATION_EVAL"
│   Name - "İş Başvurusu Değerlendirme Sistemi"
│   StatusCode - Active(1) / Passive(2)
│   SchemaVersion - 1 (Şema değişiklikleri takibi)
│   CreatedAtUtc
│   UpdatedAtUtc
│
├─── decision_tree_table (1:N ilişkisi)
│    │  Id (PK)
│    │  DecisionTreeId (FK) → decision_tree.Id
│    │  TableName - "BasvuruBilgileri", "PozisyonBilgileri", vb.
│    │  Direction - Input(1) / Output(2)
│    │  StatusCode - Active/Passive
│    │
│    └─── decision_tree_column (1:N ilişkisi)
│         │  Id (PK)
│         │  TableId (FK) → decision_tree_table.Id
│         │  ColumnName - "AdayId", "AdayAdi", "Email", vb.
│         │  ExcelHeaderName - Excel'deki başlık (ColumnName'den farklıysa)
│         │  DataType - String(1), Int(2), Decimal(3), Date(4), Boolean(5)
│         │  IsRequired - true/false
│         │  Format - "yyyy-MM-dd" (Date için), etc.
│         │  MaxLength, Precision, Scale
│         │  ValidFrom, ValidTo (Temporal columns)
│         │  OrderIndex - UI'da gösterilme sırası
│         │  StatusCode - Active/Passive
│         │
│         └─── decision_tree_data (1:N ilişkisi)
│              │  Id (PK)
│              │  DecisionTreeId (FK)
│              │  TableId (FK) → decision_tree_table.Id
│              │  RowIndex - Sıra numarası
│              │  RowDataJson - Gerçek veri (JSON formatında)
│              │  CreatedAtUtc
│              │  UpdatedAtUtc
│              │
│              │  Örnek RowDataJson:
│              │  {
│              │    "AdayId": 1001,
│              │    "AdayAdi": "Mehmet Yılmaz",
│              │    "Email": "mehmet@example.com",
│              │    "DeneyimYili": 8,
│              │    "EgitimSeviyesi": 3,
│              │    "BasvuruTarihi": "2025-02-05"
│              │  }
│
└─── validation_log (Hata kayıtları)
     │  Id (PK)
     │  DecisionTreeId (FK)
     │  TableId (FK) - Optional
     │  ColumnName - Hata yapan kolon
     │  ErrorType - Tip bilgisi
     │  ErrorMessage - Hata mesajı
     │  LoggedAtUtc

column_value_mapping (Kolon reorder geçmişi)
     │  Id (PK)
     │  TableColumnId (FK)
     │  OldPosition - Eski sıra
     │  NewPosition - Yeni sıra
     │  ChangedAtUtc
```

### Enum Tanımları
```csharp
public enum StatusCode
{
    Active = 1,
    Passive = 2
}

public enum TableDirection
{
    Input = 1,      // Giriş tablosu (veri alır)
    Output = 2      // Çıkış tablosu (sonuç üretir)
}

public enum ColumnDataType
{
    String = 1,
    Int = 2,
    Decimal = 3,
    Date = 4,
    Boolean = 5
}
```

---

## 🔧 BACKEND KATMANI (C# .NET 8.0)

### Program.cs - Başlangıç Yapılandırması
```csharp
// Dosya: backend/DecisionTree.Api/Program.cs

builder.Services.AddScoped<ExcelService>();
builder.Services.AddScoped<JsonBuilderService>();
builder.Services.AddScoped<ValidationService>();
builder.Services.AddScoped<JobApplicationSeedService>();

// CORS: Angular (localhost:4200) ile haberleşme
builder.Services.AddCors(opt => {
    opt.AddPolicy("dev", p =>
        p.WithOrigins(
            "http://localhost:4200",
            "https://localhost:4200",
            "http://127.0.0.1:4200"
        )
        .AllowAnyHeader()
        .AllowAnyMethod()
    );
});

// MySQL Bağlantısı
var cs = builder.Configuration.GetConnectionString("Default");
// "Server=localhost;Port=3306;Database=decision_tree_db;User=root;Password=..."
builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseMySql(cs, ServerVersion.AutoDetect(cs)));

// Development'ta seed veri yükleme
if (app.Environment.IsDevelopment())
{
    var seedService = scope.ServiceProvider.GetRequiredService<JobApplicationSeedService>();
    await seedService.SeedDataAsync();
}
```

### API Endpoints - Detaylı Açıklama

#### 1️⃣ EKRAN 1: Karar Ağaçları (DecisionTreesController)
```
🔹 GET /api/decision-trees
   Parametreler: code, name, status
   Görev: Karar ağaçlarını listele (filtreleme desteği)
   Dönüş: 
   [
     {
       "id": 1,
       "code": "JOB_APPLICATION_EVAL",
       "name": "İş Başvurusu Değerlendirme",
       "statusCode": 1,
       "lastOperationDateUtc": "2025-02-05T10:30:00Z"
     }
   ]

🔹 GET /api/decision-trees/exists?code=xxx
   Görev: Code'un zaten var olup olmadığını kontrol et (duplicate check)
   Dönüş: { "exists": true/false }

🔹 GET /api/decision-trees/{id}
   Görev: Belirli karar ağacını getir (tablolar + kolonlar dahil)

🔹 POST /api/decision-trees
   Body: { "code": "...", "name": "..." }
   Görev: Yeni karar ağacı oluştur
   Dönüş: Oluşturulan entity ID'si

🔹 PUT /api/decision-trees/{id}
   Body: { "code": "...", "name": "...", "statusCode": 1 }
   Görev: Karar ağacını güncelle

🔹 DELETE /api/decision-trees/{id}
   Görev: Karar ağacını ve tüm ilişkili verileri sil (CASCADE)
```

#### 2️⃣ EKRAN 2: Tablo Yönetimi (DecisionTreeTablesController)
```
🔹 GET /api/decision-trees/{dtId}/tables
   Görev: Karar ağacına ait tüm tabloları listele
   
🔹 POST /api/decision-trees/{dtId}/tables
   Body: { 
     "tableName": "BasvuruBilgileri",
     "direction": 1,  // Input=1, Output=2
     "statusCode": 1
   }
   Görev: Yeni tablo ekle

🔹 PUT /api/decision-trees/{dtId}/tables/{tableId}
   Görev: Tablo metadata'sını güncelle

🔹 DELETE /api/decision-trees/{dtId}/tables/{tableId}
   Görev: Tabloyu ve tüm kolonlarını + verilerini sil
```

#### 3️⃣ EKRAN 3: Kolon Yönetimi (TableColumnsController)
```
🔹 GET /api/decision-trees/{dtId}/tables/{tableId}/columns
   Görev: Tablonun tüm kolonlarını listele (OrderIndex'e göre sıralı)

🔹 POST /api/decision-trees/{dtId}/tables/{tableId}/columns
   Body: {
     "columnName": "AdayId",
     "excelHeaderName": "Aday ID",  // Optional
     "dataType": 2,                 // Int
     "isRequired": true,
     "format": null,
     "maxLength": null,
     "orderIndex": 1,
     "statusCode": 1
   }
   Görev: Tabloya kolon ekle

🔹 PUT /api/decision-trees/{dtId}/tables/{tableId}/columns/{columnId}
   Görev: Kolon metadata'sını güncelle

🔹 DELETE /api/decision-trees/{dtId}/tables/{tableId}/columns/{columnId}
   Görev: Kolonun tüm verisini sil

🔹 PUT /api/decision-trees/{dtId}/tables/{tableId}/reorder-columns
   Body: [{ "columnId": 1, "newIndex": 2 }, ...]
   Görev: Kolonları yeniden sırala (UI drag-drop sonrası)
```

#### 4️⃣ EKRAN 4: Veri Girişi (DataEntryController)

##### A) Veri Okuma
```
🔹 GET /api/decision-trees/{dtId}/data/tables/{tableId}/rows
   Görev: Tablonun tüm veri satırlarını getir
   Dönüş: [
     {
       "id": 1,
       "tableId": 1,
       "rowIndex": 1,
       "rowDataJson": "{\"AdayId\": 1001, \"AdayAdi\": \"Mehmet\", ...}",
       "createdAtUtc": "2025-02-05T10:00:00Z",
       "updatedAtUtc": "2025-02-05T11:00:00Z"
     }
   ]

🔹 GET /api/decision-trees/{dtId}/data/tables/{tableId}/rows/{rowId}
   Görev: Belirli bir veri satırını getir
```

##### B) Veri Yazma
```
🔹 POST /api/decision-trees/{dtId}/data/tables/{tableId}/rows
   Body: {
     "rowIndex": 1,
     "rowDataJson": "{\"AdayId\": 1001, \"AdayAdi\": \"Mehmet\", ...}"
   }
   Görev: Yeni veri satırı ekle
   Validasyon: 
     - RowDataJson geçerli JSON olmalı
     - Kolon veri tipleri doğru olmalı
     - Required alanlar boş olmamalı

🔹 PUT /api/decision-trees/{dtId}/data/tables/{tableId}/rows/{rowId}
   Görev: Veri satırını güncelle

🔹 DELETE /api/decision-trees/{dtId}/data/tables/{tableId}/rows/{rowId}
   Görev: Veri satırını sil
```

##### C) Excel İçe/Dışa Aktarma
```
🔹 POST /api/decision-trees/{dtId}/data/import-excel
   FormData: { file: <Excel File> }
   Görev: 
     1. Excel dosyasını EPPlus ile oku
     2. Her worksheet = DecisionTreeTable eşleştir
     3. Kolon başlıkları (ColumnName veya ExcelHeaderName) ile eşleştir
     4. Veri tipini dönüştür (String→Int, String→Date, vb.)
     5. Validasyon hatalarını kaydet
     6. Başarılı satırları DB'ye yaz
   Dönüş: {
     "success": true,
     "rowsInserted": 150,
     "errors": ["Row 5: Geçersiz tarih..."],
     "warnings": ["Unknown column..."]
   }

🔹 GET /api/decision-trees/{dtId}/data/export-excel
   Görev: Tüm tabloları ayrı Excel worksheet'lerine dışa aktar
   Dönüş: .xlsx file

🔹 POST /api/decision-trees/{dtId}/data/import-json
   Body: { 
     "jsonContent": "{...}",
     "replaceExistingData": false
   }
   Görev: JSON verilerini parse et ve DB'ye yazıl
```

##### D) JSON Export (Metadata + Veriler)
```
🔹 GET /api/decision-trees/{dtId}/data/export-json
   Parametreler: 
     - includeInactiveTables (default: false)
     - includeInactiveColumns (default: false)
   Görev: 
     1. Decision tree'nin tüm metadata'sını topla
     2. Tüm Input tabloları ve verilerini al
     3. Tüm Output tabloları ve verilerini al
     4. JSON formatında birleştir
   Dönüş: {
     "metadata": {
       "decisionTreeId": 1,
       "decisionTreeCode": "JOB_APPLICATION_EVAL",
       "decisionTreeName": "İş Başvurusu Değerlendirme",
       "schemaVersion": 1,
       "exportedAtUtc": "2025-02-05T12:00:00Z"
     },
     "tables": [
       {
         "tableId": 1,
         "tableName": "BasvuruBilgileri",
         "direction": "Input",
         "columns": [
           {
             "columnId": 1,
             "columnName": "AdayId",
             "dataType": "Int",
             "isRequired": true,
             "orderIndex": 1
           }
         ],
         "rows": [
           {
             "AdayId": 1001,
             "AdayAdi": "Mehmet",
             "Email": "mehmet@example.com"
           }
         ]
       }
     ]
   }

🔹 GET /api/decision-trees/{dtId}/data/export-json-string
   Görev: JSON'ı formatlı string olarak dönür
```

### Servisler - Detaylı Fonksiyonalite

#### 📄 ExcelService (EPPlus Kullanılıyor)
```csharp
public class ExcelService
{
    // 1. Excel Okuma
    public async Task<ExcelReadResult> ReadExcelAsync(
        Stream excelStream,
        List<DecisionTreeTable> tables)
    {
        // Her worksheet = bir DecisionTreeTable
        // Başlık satırı = ColumnName veya ExcelHeaderName
        // Veri satırları = Database'e yazılacak veriler
        
        // Örnek:
        // Excel Sheet: "BasvuruBilgileri"
        // Başlıklar: [Aday ID, Aday Adı, Email]
        // Column metadata: 
        //   - ColumnName="AdayId", ExcelHeaderName="Aday ID"
        //   - ColumnName="AdayAdi", ExcelHeaderName="Aday Adı"
        //   - ColumnName="Email", ExcelHeaderName="Email"
        
        // Fonksiyon Excel'deki başlıkları metadata ile eşleştir
        // Veri tipini dönüştür (String → Int, String → Date, etc.)
    }
    
    // 2. Veri Tip Dönüştürme
    private (object?, string?) ConvertCellValue(
        string cellValue, 
        TableColumn column, 
        int rowNumber)
    {
        // String → Int: TryParse
        // String → Decimal: TryParse (CultureInfo.InvariantCulture)
        // String → Date: TryParseExact (çoklu format desteği)
        //   - "dd/MM/yyyy", "dd.MM.yyyy", "yyyy-MM-dd"
        //   - "dd/MM/yyyy HH:mm:ss" (Tarih+Saat)
        // String → Boolean: "true"/"false", "1"/"0", "evet"/"hayır", "e"/"h"
        
        // Başarısızlık → (null, "Row 5: Geçersiz integer...")
    }
}
```

#### 🔍 ValidationService
```csharp
public class ValidationService
{
    public async Task<ValidationResult> ValidateRowAsync(
        int tableId,
        Dictionary<string, object?> rowData,
        int rowIndex)
    {
        // Yapılan Kontroller:
        // 1. Required alanlar boş mu?
        // 2. Bilinmeyen kolonlar var mı?
        // 3. Veri tipleri doğru mu?
        // 4. Format uyumlu mu?
        // 5. MaxLength/Precision/Scale kontrolleri
        
        // Hataları döner, exception atmaz (graceful)
        return new ValidationResult { 
            IsValid = true/false,
            Errors = [...],
            Warnings = [...]
        };
    }
}
```

#### 📊 JsonBuilderService
```csharp
public class JsonBuilderService
{
    public async Task<JsonExportResponse> BuildJsonExportAsync(
        int decisionTreeId,
        bool includeInactiveTables = false,
        bool includeInactiveColumns = false)
    {
        // 1. Decision tree'yi ve tüm ilişkili veriyi yükle
        //    (Include: Tables → Columns)
        
        // 2. Metadata oluştur:
        //    - Decision tree ID, Code, Name
        //    - Schema version
        //    - Export timestamp
        
        // 3. Her tablo için:
        //    - Kolon metadata'sını (OrderIndex sırasında) topla
        //    - decision_tree_data'dan verileri oku
        //    - RowDataJson'ı parse et (Dictionary<string, object?>)
        //    - Boş tabloları atla (no data rows)
        
        // 4. JSON yapısında döner:
        {
          "metadata": {...},
          "tables": [
            {
              "tableId": 1,
              "tableName": "...",
              "direction": "Input"/"Output",
              "columns": [...],
              "rows": [
                {"col1": value1, "col2": value2, ...},
                ...
              ]
            }
          ]
        }
    }
}
```

#### 🚀 JobApplicationSeedService (Demo Veri)
```csharp
public class JobApplicationSeedService
{
    public async Task SeedDataAsync()
    {
        // 1. Decision tree oluştur: JOB_APPLICATION_EVAL
        // 2. İnput tablolarını ekle:
        //    - BasvuruBilgileri (Aday info)
        //    - PozisyonBilgileri (Pozisyon gerekçeleri)
        // 3. Output tablosu ekle:
        //    - DegerlendirmeSonucu (Karar)
        // 4. Kolonları metadata ile tanımla
        // 5. Demo veriler ekle
        // 
        // Program.cs'te şu kod otomatik çalışır:
        // if (app.Environment.IsDevelopment()) {
        //     var seedService = scope.GetService<JobApplicationSeedService>();
        //     await seedService.SeedDataAsync();
        // }
    }
}
```

---

## 🎨 FRONTEND KATMANI (Angular 18+)

### Dosya Yapısı
```
frontend/src/app/
├── pages/
│   ├── decision-tree-list/
│   │   ├── decision-tree-list.component.ts
│   │   ├── decision-tree-list.component.html
│   │   ├── decision-tree-list.component.css
│   │   └── decision-tree-list.component.spec.ts
│   │
│   ├── table-management/         (⏳ Yapım aşamasında)
│   ├── column-management/         (⏳ Yapım aşamasında)
│   └── data-entry/               (⏳ Yapım aşamasında)
│
├── services/
│   ├── decision-tree.service.ts   (✅ Tamamlandı)
│   ├── table.service.ts           (⏳ Yapım aşamasında)
│   ├── column.service.ts          (⏳ Yapım aşamasında)
│   └── data-entry.service.ts      (⏳ Yapım aşamasında)
│
├── app.config.ts
├── app.routes.ts
└── app.ts
```

### Konfigürasyon (app.config.ts)
```typescript
export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),  // Global error handling
    provideRouter(routes),                 // Routing
    provideHttpClient()                    // HTTP client (çok önemli!)
  ]
};
```

### Rotalar (app.routes.ts)
```typescript
export const routes: Routes = [
  { path: '', redirectTo: '/decision-trees', pathMatch: 'full' },
  
  // Ekran 1: Karar Ağaçları Listesi
  { path: 'decision-trees', component: DecisionTreeListComponent },
  
  // Ekran 2: Tablo Yönetimi
  { path: 'decision-trees/:id/tables', component: TableManagementComponent },
  
  // Ekran 3: Kolon Yönetimi
  { path: 'decision-trees/:id/tables/:tableId/columns', 
    component: ColumnManagementComponent },
  
  // Ekran 4: Veri Girişi
  { path: 'decision-trees/:id/data', component: DataEntryComponent },
  { path: 'decision-trees/:id/data/tables/:tableId', 
    component: DataEntryComponent },
];
```

### Services - HTTP İletişimi

#### DecisionTreeService (✅ Tamamlandı)
```typescript
@Injectable({ providedIn: 'root' })
export class DecisionTreeService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = 'http://localhost:5135/api/decision-trees';

  search(filter: DecisionTreeFilter): Observable<DecisionTree[]> {
    // Backend'e GET isteği gönder
    // Parametreler: code, name, statusCode
    // Dönüş: DecisionTree[] (id, code, name, statusCode, lastOperationDateUtc)
  }

  getById(id: number): Observable<DecisionTree> {
    // Belirli bir karar ağacını getir
  }

  create(data: Omit<DecisionTree, 'id'>): Observable<DecisionTree> {
    // Yeni karar ağacı oluştur
    // Body'de: code, name, statusCode (Optional)
  }

  update(id: number, data: Partial<DecisionTree>): Observable<void> {
    // Karar ağacını güncelle
  }

  delete(id: number): Observable<void> {
    // Karar ağacını sil
  }
}
```

#### TableService (⏳ Yapılacak)
```typescript
// Tablolar ile ilgili HTTP işlemleri
- getTablesByDecisionTreeId(dtId)
- createTable(dtId, table)
- updateTable(dtId, tableId, table)
- deleteTable(dtId, tableId)
```

#### ColumnService (⏳ Yapılacak)
```typescript
// Kolonlar ile ilgili HTTP işlemleri
- getColumnsByTableId(dtId, tableId)
- createColumn(dtId, tableId, column)
- updateColumn(dtId, tableId, columnId, column)
- deleteColumn(dtId, tableId, columnId)
- reorderColumns(dtId, tableId, reorderRequest)
```

#### DataEntryService (⏳ Yapılacak)
```typescript
// Veri girişi ve dönüşümleri
- getTableRows(dtId, tableId)
- createRow(dtId, tableId, row)
- updateRow(dtId, tableId, rowId, row)
- deleteRow(dtId, tableId, rowId)
- importExcel(dtId, file)
- exportExcel(dtId)
- importJson(dtId, jsonContent)
- exportJson(dtId)
```

### EKRAN 1: Karar Ağaçları Listesi (✅ Tamamlandı)

**Dosya:** `frontend/src/app/pages/decision-tree-list/decision-tree-list.component.ts`

**Özellikler:**
- Filtreleme (code, name, status)
- CRUD Modal'ları
- Loading/Error states
- Tablo yönetimine gitme butonu
- Veri girişine gitme butonu

**Çalışma Akışı:**
```typescript
1. Component yüklenir → DecisionTreeService.search() çağrılır
2. Backend'den DecisionTree[] alınır
3. Ekranda tablo gösterilir
4. Kullanıcı "Tablo Yönet" butonuna tıklar
   → Router.navigate(['/decision-trees', id, 'tables'])
   → TableManagementComponent yüklenir
5. Kullanıcı "Veri Gir" butonuna tıklar
   → Router.navigate(['/decision-trees', id, 'data'])
   → DataEntryComponent yüklenir
```

---

## 📊 VERİ AKIŞI (End-to-End)

### Senaryo: İş Başvurusu Verilerini Excel'den İçeri Aktarma

```
Step 1: Frontend'de Excel Dosyası Seçme
┌─────────────────────────────────────────┐
│ DataEntryComponent                      │
│                                         │
│ <input type="file" accept=".xlsx">      │
│ [İçe Aktar] Butonu                      │
└──────────────────┬──────────────────────┘
                   │
Step 2: Backend'e Gönderme
                   ▼
┌─────────────────────────────────────────┐
│ POST /api/decision-trees/1/data/        │
│       import-excel                      │
│ FormData: { file: <Binary .xlsx> }      │
└──────────────────┬──────────────────────┘
                   │
Step 3: Backend İşleme
                   ▼
┌─────────────────────────────────────────┐
│ DataEntryController.ImportExcel()       │
│                                         │
│ 1. ExcelService.ReadExcelAsync() →      │
│    EPPlus ile dosyayı oku               │
│    Her worksheet = Table                │
│    Başlıklar = Column metadata           │
│    Veri satırları = Rows                │
│                                         │
│ 2. ValidationService.ValidateRowAsync() │
│    Her satırı kontrol et:               │
│    - Required alanlar boş mu?           │
│    - Veri tipi uyumlu mu?               │
│    - Format doğru mu?                   │
│                                         │
│ 3. Başarılı satırları Database'e yazıl  │
│    decision_tree_data.RowDataJson:      │
│    {"AdayId": 1001, "AdayAdi": "...",}  │
│                                         │
│ 4. Hatalar ValidationLog'a kaydedilir   │
└──────────────────┬──────────────────────┘
                   │
Step 4: Sonuç Dönme
                   ▼
┌─────────────────────────────────────────┐
│ ExcelImportResponse                     │
│ {                                       │
│   "success": true,                      │
│   "rowsInserted": 150,                  │
│   "errors": ["Row 5: Invalid date..."], │
│   "warnings": ["Row 12: Unknown col..."]│
│ }                                       │
└──────────────────┬──────────────────────┘
                   │
Step 5: Frontend'de Sonuç Gösterme
                   ▼
┌─────────────────────────────────────────┐
│ DataEntryComponent                      │
│                                         │
│ ✅ 150 satır başarıyla yüklendi!        │
│ ⚠️ Bazı hatalar: [...]                  │
│                                         │
│ Tablo'da yüklenen veriler gösterilir    │
└─────────────────────────────────────────┘
```

### Senaryo: JSON Olarak Dışa Aktarma

```
Step 1: Frontend'de Export Talebi
┌──────────────────────────────────────────┐
│ DataEntryComponent                       │
│ [JSON'a Dışa Aktar] Butonu               │
│ Parametreler:                            │
│ - includeInactiveTables: false           │
│ - includeInactiveColumns: false          │
└──────────────┬───────────────────────────┘
               │
Step 2: Backend'e GET İsteği
               ▼
┌──────────────────────────────────────────┐
│ GET /api/decision-trees/1/data/          │
│     export-json?includeInactiveTables=.. │
└──────────────┬───────────────────────────┘
               │
Step 3: Backend İşleme
               ▼
┌──────────────────────────────────────────┐
│ DataEntryController.ExportJson()         │
│                                          │
│ 1. JsonBuilderService.BuildJsonExport()  │
│    - Decision tree metadata topla        │
│    - Her table ve columns yükle          │
│    - decision_tree_data'dan veri oku     │
│    - RowDataJson parse et                │
│                                          │
│ 2. JSON yapısında döner:                 │
│    {                                     │
│      "metadata": {                       │
│        "decisionTreeCode": "...",        │
│        "schemaVersion": 1,               │
│        "exportedAtUtc": "2025-02-05..."  │
│      },                                  │
│      "tables": [                         │
│        {                                 │
│          "tableName": "BasvuruBilgileri",│
│          "direction": "Input",           │
│          "columns": [                    │
│            {                             │
│              "columnName": "AdayId",     │
│              "dataType": "Int"           │
│            }                             │
│          ],                              │
│          "rows": [                       │
│            {"AdayId": 1, "Name": "..."}  │
│          ]                               │
│        }                                 │
│      ]                                   │
│    }                                     │
└──────────────┬───────────────────────────┘
               │
Step 4: Frontend'de JSON Gösterme/İndir
               ▼
┌──────────────────────────────────────────┐
│ DataEntryComponent                       │
│                                          │
│ JSON'ı formatında göster (Pretty Print)  │
│ Download Link sağla (.json dosyası)      │
│ Copy to Clipboard butonu                 │
└──────────────────────────────────────────┘
```

---

## 🔄 EXCEL / JSON DÖNÜŞÜMLERI

### Excel → Database Akışı

```
Excel Dosyası (.xlsx)
│
├─ Sheet 1: "BasvuruBilgileri"
│  ├─ Başlıklar (Row 1): [Aday ID, Aday Adı, Email, Deneyim Yılı]
│  ├─ Row 2: [1001, Mehmet Yılmaz, mehmet@ex.com, 8]
│  ├─ Row 3: [1002, Ayşe Kaya, ayse@ex.com, 5]
│  └─ Row 4: [1003, Ali Demir, ali@ex.com, 12]
│
├─ Sheet 2: "PozisyonBilgileri"
│  ├─ Başlıklar: [Pozisyon ID, Pozisyon Adı, Min Deneyim]
│  └─ Row 2: [101, Senior Developer, 10]
│
└─ Sheet 3: "DegerlendirmeSonucu"
   ├─ Başlıklar: [Değerlendirme ID, Karar, Skor]
   └─ (Boş - output tablosu)

                   │
                   ▼

ExcelService.ReadExcelAsync()
│
├─ EPPlus ile Excel'i aç
├─ Her Sheet için:
│  ├─ Sheet adı ile DecisionTreeTable eşleştir
│  ├─ Row 1 (başlıklar) oku
│  ├─ Her başlık için metadata'da ColumnName/ExcelHeaderName ara
│  ├─ Başlık & Metadata eşleştirmesi yap:
│  │  Aday ID (Excel) → AdayId (ColumnName)
│  │  Aday Adı (Excel) → AdayAdi (ColumnName)
│  │  Email (Excel) → Email (ColumnName)
│  │  Deneyim Yılı (Excel) → DeneyimYili (ColumnName)
│  │
│  └─ Her veri satırı için:
│     ├─ Değerleri oku
│     ├─ Veri tipi dönüştür:
│     │  "8" (string) → 8 (int)
│     │  "12.5" (string) → 12.5 (decimal)
│     │  "2025-02-05" (string) → DateTime (date)
│     │
│     └─ Dictionary<string, object?> yap:
│        {
│          "AdayId": 1001,
│          "AdayAdi": "Mehmet Yılmaz",
│          "Email": "mehmet@ex.com",
│          "DeneyimYili": 8
│        }

                   │
                   ▼

ValidationService.ValidateRowAsync()
│
├─ Her alan kontrol et
├─ Required alanlar boş mu?
├─ Veri tipi uyumlu mu?
├─ Format doğru mu?
└─ Hataları döner (exception atmaz)

                   │
                   ▼

Database'e Yazma (DbContext.SaveChangesAsync())
│
└─ DecisionTreeData tablosuna insert:
   {
     DecisionTreeId: 1,
     TableId: 1,
     RowIndex: 2,
     RowDataJson: '{"AdayId": 1001, "AdayAdi": "Mehmet Yılmaz", ...}',
     CreatedAtUtc: DateTime.UtcNow,
     UpdatedAtUtc: DateTime.UtcNow
   }
```

### Database → JSON Akışı

```
MySQL Database (decision_tree_data tablosu)
│
├─ Row 1: {DecisionTreeId: 1, TableId: 1, RowDataJson: '{"AdayId": 1001, ...}'}
├─ Row 2: {DecisionTreeId: 1, TableId: 1, RowDataJson: '{"AdayId": 1002, ...}'}
└─ Row 3: {DecisionTreeId: 1, TableId: 2, RowDataJson: '{"PozId": 101, ...}'}

                   │
                   ▼

JsonBuilderService.BuildJsonExportAsync()
│
├─ DecisionTree yükle (Code, Name, SchemaVersion)
├─ DecisionTreeTable'ları yükle (TableName, Direction)
├─ TableColumn'ları yükle (ColumnName, DataType, OrderIndex)
├─ DecisionTreeData'ları yükle (RowDataJson)
│
├─ Her table için:
│  ├─ Kolonları OrderIndex'e göre sırala
│  ├─ RowDataJson'ları parse et (string → Dictionary)
│  ├─ Boş tabloları atla (no data rows)
│  └─ JSON yapısı oluştur
│
└─ Sonuç JSON:
   {
     "metadata": {
       "decisionTreeId": 1,
       "decisionTreeCode": "JOB_APPLICATION_EVAL",
       "schemaVersion": 1,
       "exportedAtUtc": "2025-02-05T12:30:00Z"
     },
     "tables": [
       {
         "tableId": 1,
         "tableName": "BasvuruBilgileri",
         "direction": "Input",
         "columns": [
           {
             "columnId": 1,
             "columnName": "AdayId",
             "dataType": "Int",
             "orderIndex": 1
           }
         ],
         "rows": [
           {
             "AdayId": 1001,
             "AdayAdi": "Mehmet Yılmaz"
           }
         ]
       }
     ]
   }

                   │
                   ▼

Frontend'de JSON Gösterme
│
├─ Pretty-print formatında göster
├─ Download linkini sağla
└─ Copy to Clipboard
```

---

## 🔌 BİLEŞENLERİN BİRBİRİNE BAĞLANMASI

### HTTP İletişim Akışı

```
Frontend (Angular)                Backend (.NET)
     │                                  │
     │ HttpClient.get()                 │
     ├────────────────────────────────>│
     │  GET /api/decision-trees        │
     │                                  │ DecisionTreesController
     │                                  │ ├─ AppDbContext.DecisionTrees
     │                                  │ ├─ Include(Tables)
     │                                  │ ├─ Include(Columns)
     │                                  │ └─ ToListAsync()
     │                                  │
     │ HttpClient.post()                │
     │<─────────────────────────────────┤
     │  200 OK + JSON: [...Trees]       │
     │                                  │
     │ Signal/State güncelle            │
     │ UI'da tablo göster               │
```

### Entity Framework Core Akışı

```
DataEntryController.ImportExcel()
        │
        ▼
ExcelService.ReadExcelAsync(stream, tables)
        │
        ├─ EPPlus ile .xlsx dosyasını oku
        ├─ Worksheet adlarını DecisionTreeTable'larla eşleştir
        ├─ Kolon başlıklarını metadata'yla eşleştir
        └─ Veri satırlarını Dictionary'ye dönüştür
        │
        ▼
ValidationService.ValidateRowAsync()
        │
        ├─ Required alanlar kontrol
        ├─ Veri tipi kontrol
        └─ Hataları kaydet
        │
        ▼
DbContext.DecisionTreeData.AddRangeAsync()
        │
        ├─ Her satır için DecisionTreeData entity'si oluştur
        ├─ RowDataJson = JsonSerializer.Serialize(dictionary)
        └─ DbContext'e ekle
        │
        ▼
DbContext.SaveChangesAsync()
        │
        ├─ DbContext üzerinde override:
        │  ├─ UpdatedAtUtc otomatik set
        │  ├─ CreatedAtUtc otomatik set
        │  └─ SQL INSERT komutları generate
        │
        ▼
MySQL Database
        │
        └─ decision_tree_data tablosuna yazıl
```

### CORS Yapılandırması

```
Frontend: http://localhost:4200
Backend:  http://localhost:5135

Program.cs'te:
builder.Services.AddCors(opt => {
    opt.AddPolicy("dev", p =>
        p.WithOrigins(
            "http://localhost:4200",
            "https://localhost:4200",
            "http://127.0.0.1:4200"
        )
        .AllowAnyHeader()      // Content-Type, Authorization, vb. her başlık izin
        .AllowAnyMethod()      // GET, POST, PUT, DELETE, vb. her method izin
    );
});

app.UseCors("dev");            // Middleware'de aktifleştir

Sonuç: Frontend'den Backend'e HTTP istekleri başarılı olur
```

---

## 📝 VERITABANI MIGRASYONLARI

### Migration Süreci

```
1. Migration Oluşturma
   $ dotnet ef migrations add InitialSchema
   → Migrations/20260129093405_InitialSchema.cs oluşturulur
   → Code-first approachla DB şeması define edilir

2. Migration Uygulama
   $ dotnet ef database update
   → SQL'i generate et
   → MySQL'de tabloları oluştur

3. Migrasyonlar:
   ✅ 20260129093405_InitialSchema
      └─ decision_tree, decision_tree_table, decision_tree_column
   
   ✅ 20260129104841_AddDecisionTreeData
      └─ decision_tree_data tablosu
   
   ✅ 20260129111444_RemoveTableCodeAndColumnType
      └─ Eski alanları kaldır
   
   ✅ 20260202085907_AddDataEntryTables
      └─ validation_log, column_value_mapping tabloları
```

### Migration'un Detaylı Çalışması

```csharp
public partial class AddDataEntryTables : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // 1. decision_tree_data tablosuna kolon ekle
        migrationBuilder.AddColumn<int>(
            name: "DecisionTreeId",
            table: "decision_tree_data",
            type: "int",
            nullable: false,
            defaultValue: 0);
        
        // 2. Foreign key constraint ekle
        migrationBuilder.AddForeignKey(
            name: "fk_decision_tree_data_decision_tree",
            table: "decision_tree_data",
            column: "DecisionTreeId",
            principalTable: "decision_tree",
            principalColumn: "Id",
            onDelete: ReferentialAction.Cascade);
        
        // 3. Yeni tablo oluştur
        migrationBuilder.CreateTable(
            name: "validation_log",
            columns: table => new
            {
                Id = table.Column<int>(...),
                DecisionTreeId = table.Column<int>(...),
                ColumnName = table.Column<string>(...),
                ErrorMessage = table.Column<string>(...),
                LoggedAtUtc = table.Column<DateTime>(...)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_validation_log", x => x.Id);
                table.ForeignKey(
                    "fk_validation_log_decision_tree",
                    x => x.DecisionTreeId,
                    "decision_tree",
                    "Id",
                    onDelete: ReferentialAction.Cascade);
            });
    }
}
```

---

## 🧪 EKRAN 4: VERİ GİRİŞİ (Detaylı Akış)

### Ekran Özellikleri (⏳ Yapım Aşamasında)

```
┌──────────────────────────────────────────────────────┐
│ Veri Girişi Ekranı                                   │
│                                                      │
│ [Decision Tree Seçimi] ↓                             │
│ [Tablo Seçimi] ↓                                     │
│                                                      │
│ ┌────────────────────────────────────────────────┐  │
│ │ İçe Aktar                                      │  │
│ ├────────────────────────────────────────────────┤  │
│ │ [📁 Excel Dosyası Seç] [▶ İçe Aktar]          │  │
│ │ [📄 JSON Yapıştır] [▶ İçe Aktar]              │  │
│ └────────────────────────────────────────────────┘  │
│                                                      │
│ ┌────────────────────────────────────────────────┐  │
│ │ Dışa Aktar                                     │  │
│ ├────────────────────────────────────────────────┤  │
│ │ [⬇ Excel İndir] [⬇ JSON İndir]               │  │
│ │ [📋 JSON Kopyala]                              │  │
│ └────────────────────────────────────────────────┘  │
│                                                      │
│ ┌────────────────────────────────────────────────┐  │
│ │ Veriler (Tablo Görünümü)                       │  │
│ ├────────────────────────────────────────────────┤  │
│ │ ID  | Aday Adı      | Email            | Aksy  │  │
│ ├─────┼───────────────┼──────────────────┼──────┤  │
│ │ 1   | Mehmet Yılmaz | mehmet@ex.com    | 🗑️   │  │
│ │ 2   | Ayşe Kaya     | ayse@ex.com      | 🗑️   │  │
│ │ 3   | Ali Demir     | ali@ex.com       | 🗑️   │  │
│ └────────────────────────────────────────────────┘  │
│                                                      │
│ [+ Yeni Satır Ekle]                                │
└──────────────────────────────────────────────────────┘
```

---

## 📚 ÖZET - SİSTEM TÜM PARÇALARı

| Bileşen | Durum | Görev |
|---------|-------|-------|
| **MySQL DB** | ✅ | 7 tablo, migrasyonlar |
| **Entity Framework** | ✅ | ORM, LINQ, Code-First |
| **Backend APIs** | ✅ | 4 Controller, 18+ Endpoint |
| **Services** | ✅ | Excel, JSON, Validasyon |
| **Ekran 1 (Frontend)** | ✅ | Decision Tree listesi |
| **Ekran 2 (Frontend)** | ⏳ | Tablo yönetimi |
| **Ekran 3 (Frontend)** | ⏳ | Kolon yönetimi + reorder |
| **Ekran 4 (Frontend)** | ⏳ | Veri girişi + Excel/JSON |

---

## 🚀 BAŞLATMA KOMUTU

```bash
# Terminal 1: Backend
cd backend/DecisionTree.Api
dotnet run

# Terminal 2: Frontend
cd frontend
npm start
# veya
ng serve

# Browser'de aç
http://localhost:4200
```

---

## 📌 ÖNEMLİ NOTLAR

1. **JSON Verileri:** `decision_tree_data.RowDataJson` alanında saklanır
   - Format: `{"column1": value1, "column2": value2}`
   - Türkçe karakterler desteklenir

2. **Validasyon:** Hiçbir exception atılmaz, tüm hatalar response'a yazılır

3. **Excel Eşleştirmesi:** 
   - Başlıklar: `ColumnName` veya `ExcelHeaderName` ile eşleştir
   - Case-insensitive eşleştirme

4. **CORS:** Backend ve Frontend ön ayarları yapılandırıldı

5. **Demo Veri:** Development'ta otomatik yüklenir

---

Herhangi bir sorunuz veya geliştirme ihtiyacı için bu doküman referans alınabilir! 🎯
