# 🎯 KARAR AĞACI (Decision Tree) YÖNETİM SİSTEMİ - DETAYLI AÇIKLAMA

## **1. SİSTEMİN AMACI NEDİR?**

Bu sistem **veri işleme ve karar verme süreçlerini yönetmek** için tasarlanmıştır. Örneğin:

### **Gerçek Dünya Örneği: İş Başvurusu Değerlendirmesi**
```
Aday → Başvuru Bilgileri (Ad, Deneyim, Eğitim) → SİSTEM → Karar (Uygun/Uygun Değil)
       + Pozisyon Bilgileri (Gerekli deneyim, eğitim)
       + Kriterler
```

### **Diğer Kullanım Alanları**
- 🏥 Hastabakıcı seçimi (hastaneler)
- 🏦 Kredi başvurusu değerlendirmesi (bankalar)
- 🎓 Öğrenci seçimi (okullar)
- 📦 Ürün sınıflandırması (e-ticaret)

---

## **2. SİSTEM MİMARİSİ (Backend - C#)**

### **2.1 Veritabanı Yapısı**
```
decision_tree (Karar Ağaçları)
│
├── decision_tree_table (Giriş/Çıkış Tabloları)
│   │
│   └── decision_tree_column (Kolon Tanımları - metadata)
│       └── decision_tree_data (Gerçek Veriler - JSON)
```

**Örnek Veri:**
```
Decision Tree: JOB_APPLICATION_EVAL (İş Başvurusu Sistemi)
│
├── BasvuruBilgileri (Input Tablosu)
│   ├── Columns: AdayId, AdayAdi, Email, DeneyimYili, EgitimSeviyesi...
│   └── Data (JSON): 
│       {
│         "AdayId": 1,
│         "AdayAdi": "Mehmet",
│         "DeneyimYili": 8,
│         "EgitimSeviyesi": 3,
│         ...
│       }
│
├── PozisyonBilgileri (Input Tablosu)
│   └── Data: Pozisyon adı, maaş, gerekli yetenekler...
│
└── DegerlendirmeSonucu (Output Tablosu)
    └── Data: Karar, skor, açıklama...
```

---

## **3. BACKEND AKIŞI (C# .NET 8.0)**

### **3.1 API Endpoint'leri**

```
┌─────────────────────────────────────────────────┐
│ FRONTEND (Angular)                              │
└────────────────────┬────────────────────────────┘
                     │
        ┌────────────▼────────────┐
        │ HTTP İstekleri          │
        └────────────┬────────────┘
                     │
        ┌────────────▼────────────────────────────┐
        │ Backend API (C#)                        │
        │                                         │
        │ /api/decision-trees (CRUD)              │
        │ /api/decision-trees/{id}/tables         │
        │ /api/decision-trees/{id}/tables/{id}/   │
        │        columns                          │
        │ /api/decision-trees/{id}/data (Veri)    │
        │ /api/decision-trees/{id}/data/          │
        │        export-json                      │
        │ /api/decision-trees/{id}/data/          │
        │        import-excel                     │
        └────────────┬────────────────────────────┘
                     │
        ┌────────────▼────────────┐
        │ Entity Framework Core    │
        │ (ORM - Object Mapping)   │
        └────────────┬────────────┘
                     │
        ┌────────────▼────────────┐
        │ MySQL Database          │
        │ (Türkçe veriler)        │
        └─────────────────────────┘
```

### **3.2 Veri Ekleme Süreci (Seed Service)**

**Dosya:** `backend/DecisionTree.Api/Services/JobApplicationSeedService.cs`

```csharp
public async Task SeedDataAsync()
{
    // 1. Decision Tree Oluştur
    var decisionTree = new Entities.DecisionTree
    {
        Code = "JOB_APPLICATION_EVAL",
        Name = "İş Başvurusu Değerlendirme Sistemi"
    };
    _db.DecisionTrees.Add(decisionTree);
    await _db.SaveChangesAsync(); // ← Veritabanına kaydet
    
    // 2. Input Tabloları Oluştur
    var basvuruTable = new DecisionTreeTable
    {
        TableName = "BasvuruBilgileri",
        Direction = TableDirection.Input // ← Giriş tablosu
    };
    _db.DecisionTreeTables.Add(basvuruTable);
    
    // 3. Kolonları Tanımla (Metadata)
    var columns = new List<TableColumn>
    {
        new() { ColumnName = "AdayId", DataType = ColumnDataType.Int },
        new() { ColumnName = "AdayAdi", DataType = ColumnDataType.String },
        // ... 10 kolon
    };
    _db.TableColumns.AddRange(columns);
    
    // 4. Gerçek Verileri Ekle (10 aday)
    var data = new List<DecisionTreeData>
    {
        new() { 
            RowDataJson = "{\"AdayId\":1,\"AdayAdi\":\"Mehmet\",...}" 
        },
        // ... 10 satır
    };
    _db.DecisionTreeData.AddRange(data);
    
    await _db.SaveChangesAsync(); // ← Tüm verileri kaydet
}
```

**Program.cs'de Çağrı:**
```csharp
// Uygulama başlangıcında otomatik çalışır
using (var scope = app.Services.CreateScope())
{
    var seedService = scope.ServiceProvider.GetRequiredService<JobApplicationSeedService>();
    await seedService.SeedDataAsync(); // ← Bu metodu çalıştır
}
```

---

## **4. FRONTEND AKIŞI (Angular)**

### **4.1 4 Ekran (Sayfa)**

```
┌──────────────────────────────────────────────┐
│ EKRAN 1: Karar Ağaçları Listesi             │
│ (/decision-trees)                           │
│                                             │
│ [JOB_APPLICATION_EVAL]  [MUSTERI_ANALIZI]  │
│ İş Başvurusu             Müşteri Analizi    │
│ [TABLOLAR] [DÜZENLE] [SİL]                 │
└──────────────┬──────────────────────────────┘
               │ Tıkla: "TABLOLAR"
               │
┌──────────────▼──────────────────────────────┐
│ EKRAN 2: Tablo Yönetimi                     │
│ (/decision-trees/1/tables)                  │
│                                             │
│ [BasvuruBilgileri] [PozisyonBilgileri]    │
│   (Input)           (Input)                 │
│ [DegerlendirmeSonucu] (Output)              │
│ [KOLONLAR] [DÜZENLE] [SİL]                │
└──────────────┬──────────────────────────────┘
               │ Tıkla: "KOLONLAR"
               │
┌──────────────▼──────────────────────────────┐
│ EKRAN 3: Kolon Yönetimi                     │
│ (/decision-trees/1/tables/1/columns)        │
│                                             │
│ AdayId (Int, Zorunlu)                       │
│ AdayAdi (String, Zorunlu)                   │
│ Email (String, Zorunlu)                     │
│ DeneyimYili (Int)                           │
│ [SIRAYI DEĞIŞTIR] [DÜZENLE] [SİL]          │
└──────────────┬──────────────────────────────┘
               │ Tıkla: "VERİ GİRİŞİ"
               │
┌──────────────▼──────────────────────────────┐
│ EKRAN 4: Veri Girişi                        │
│ (/decision-trees/1/data)                    │
│                                             │
│ Tablo Seçimi:                               │
│ [BasvuruBilgileri] [PozisyonBilgileri]    │
│                                             │
│ Veriler (Tablo):                            │
│ ┌──────────────────────────────────────┐   │
│ │ # │ AdayId │ AdayAdi  │ Email │ ...  │   │
│ ├──────────────────────────────────────┤   │
│ │ 1 │   1    │ Mehmet   │ m@... │ ... │   │
│ │ 2 │   2    │ Ayşe     │ a@... │ ... │   │
│ │ 3 │   3    │ Mustafa  │ m@... │ ... │   │
│ └──────────────────────────────────────┘   │
│ [+ YENİ SATIR EKLE]                        │
│ [📥 EXCEL DIŞA AKTAR] [📥 JSON DIŞA AKTAR]│
└──────────────────────────────────────────────┘
```

### **4.2 Ekran 4'de Veriler Nasıl Görüntüleniyor?**

**Dosya:** `frontend/src/app/pages/data-entry/data-entry.component.ts`

```typescript
export class DataEntryComponent implements OnInit {
  tables = signal<DecisionTreeTable[]>([]); // Tablolar
  columns = signal<TableColumn[]>([]); // Seçilen tablonun kolonları
  dataRows = signal<DataRow[]>([]); // Veri satırları

  ngOnInit() {
    // 1. Tüm tabloları yükle
    this.tableService.getTables(this.dtId()).subscribe(tables => {
      this.tables.set(tables); // BasvuruBilgileri, PozisyonBilgileri...
    });
  }

  selectTable(table: DecisionTreeTable) {
    // 2. Seçilen tablonun kolonlarını yükle
    this.columnService.getColumns(this.dtId(), table.id).subscribe(cols => {
      this.columns.set(cols); // AdayId, AdayAdi, Email...
    });

    // 3. Veri satırlarını yükle (JSON'dan parse ediliyor)
    this.dataEntryService.getTableRows(this.dtId(), table.id).subscribe(rows => {
      this.dataRows.set(rows); // 10 aday verisi
    });
  }
}
```

**HTML'de Gösterim:**
```html
<!-- Tablo Seçim Butonları -->
<button *ngFor="let table of tables()"
        (click)="selectTable(table)">
  {{ table.tableName }}
</button>

<!-- Veri Tablosu -->
<table *ngIf="selectedTable()">
  <thead>
    <tr>
      <th *ngFor="let col of columns()">
        {{ col.columnName }}
      </th>
    </tr>
  </thead>
  <tbody>
    <tr *ngFor="let row of dataRows()">
      <td *ngFor="let col of columns()">
        {{ getColumnValue(row, col.columnName) }}
      </td>
    </tr>
  </tbody>
</table>
```

---

## **5. JSON VE EXCEL DÖNÜŞÜMÜ**

### **5.1 JSON'a Dönüştürme (Export)**

**Akış:**
```
VERİ TABLOSUNDA SAT SEÇER
        ↓
[📥 JSON DIŞA AKTAR] Butonunu Tıkla
        ↓
API çağrısı: GET /api/decision-trees/1/data/export-json
        ↓
JsonBuilderService: Metadata + Veri birleştirir
        ↓
JSON Dosyası İndir:
```

**JSON Çıktı Örneği:**
```json
{
  "decisionTreeCode": "JOB_APPLICATION_EVAL",
  "decisionTreeName": "İş Başvurusu Değerlendirme Sistemi",
  "schemaVersion": 1,
  "tables": [
    {
      "tableName": "BasvuruBilgileri",
      "direction": "Input",
      "metadata": [
        { "columnName": "AdayId", "dataType": "Int" },
        { "columnName": "AdayAdi", "dataType": "String" }
      ],
      "rows": [
        { "AdayId": 1, "AdayAdi": "Mehmet", "Email": "mehmet@email.com" },
        { "AdayId": 2, "AdayAdi": "Ayşe", "Email": "ayse@email.com" },
        // ... 10 satır
      ]
    }
  ]
}
```

**Backend Kodu:**
```csharp
[HttpGet("export-json")]
public async Task<ActionResult<JsonExportResponse>> ExportJson(int dtId)
{
    var export = await _jsonBuilder.BuildJsonExportAsync(dtId);
    return Ok(export); // ← JSON olarak gönder
}
```

### **5.2 Excel'e Dönüştürme (Export)**

**Akış:**
```
[📥 EXCEL DIŞA AKTAR] Butonunu Tıkla
        ↓
API çağrısı: GET /api/decision-trees/1/data/export-excel
        ↓
ExcelService: Veriler → Excel dosyasına dönüştür
        ↓
.xlsx Dosyası İndir (Microsoft Excel formatı)
```

**Excel Yapısı:**
```
Sheet 1: BasvuruBilgileri
┌────────┬────────────┬────────────┬──────────────┬─────────────┐
│ AdayId │ AdayAdi    │ AdaySoyadi │ Email        │ DeneyimYili │
├────────┼────────────┼────────────┼──────────────┼─────────────┤
│ 1      │ Mehmet     │ Yılmaz     │ mehmet@...   │ 8           │
│ 2      │ Ayşe       │ Demir      │ ayse@...     │ 2           │
│ 3      │ Mustafa    │ Kara       │ mustafa@...  │ 5           │
│ ...    │ ...        │ ...        │ ...          │ ...         │
└────────┴────────────┴────────────┴──────────────┴─────────────┘

Sheet 2: PozisyonBilgileri
┌────────────┬────────────────────────┬───────────┐
│ PozisyonId │ PozisyonAdi            │ Maaş Min  │
├────────────┼────────────────────────┼───────────┤
│ 1          │ Senior Yazılım Geliştir│ 45000     │
│ 2          │ Junior Yazılım Geliştir│ 25000     │
└────────────┴────────────────────────┴───────────┘
```

### **5.3 JSON'dan İçeri Aktar (Import)**

**Akış:**
```
[📤 JSON İÇERİ AKTAR] (Modal)
        ↓
JSON metin yapıştır ve "AL" butonunu tıkla
        ↓
API çağrısı: POST /api/decision-trees/1/data/parse-json
        ↓
Veriler validate edilir (hata kontrolü)
        ↓
Veritabanına kaydedilir
        ↓
Ekran yenilenir
```

---

## **6. VERİ AKIŞI ÖZET (Bütün Sistem)**

```
┌─────────────────────────────────────────────────────────────┐
│                    FONTENDİ AÇAR                            │
│                 localhost:4200                              │
└─────────────────────────────────────────────────────────────┘
                          ↓
┌─────────────────────────────────────────────────────────────┐
│        EKRAN 1: Karar Ağaçları Listesi                      │
│  JOB_APPLICATION_EVAL  ← Seed Service ile eklendi           │
│  (MySQL'den getiriliyor)                                    │
└─────────────────────────────────────────────────────────────┘
                          ↓
┌─────────────────────────────────────────────────────────────┐
│        EKRAN 2: Tablo Yönetimi                              │
│  BasvuruBilgileri (10 kolon)                                │
│  PozisyonBilgileri (8 kolon)                                │
│  PozisyonKriterleri (6 kolon)                               │
│  DegerlendirmeSonucu (10 kolon)                             │
│  AdayUyumluluk (10 kolon)                                   │
└─────────────────────────────────────────────────────────────┘
                          ↓
┌─────────────────────────────────────────────────────────────┐
│        EKRAN 3: Kolon Yönetimi                              │
│  Kolon bilgilerini düzenleme                                │
│  Sırayı değiştirme                                          │
└─────────────────────────────────────────────────────────────┘
                          ↓
┌─────────────────────────────────────────────────────────────┐
│        EKRAN 4: Veri Girişi                                 │
│  BasvuruBilgileri seçilir                                   │
│          ↓                                                   │
│  10 aday verisi tabloda gösterilir                          │
│  (Seed Service tarafından eklenen veriler)                  │
│          ↓                                                   │
│  ┌──────────────────────────────────┐                      │
│  │ Mehmet Yılmaz   8 yıl  C#,Java   │                      │
│  │ Ayşe Demir      2 yıl  JS,React  │                      │
│  │ Mustafa Kara    5 yıl  C#,ASP    │                      │
│  │ ... (7 satır daha)               │                      │
│  └──────────────────────────────────┘                      │
│          ↓                                                   │
│  [JSON DIŞA AKTAR] → metadata + veri (bir dosyada)         │
│  [EXCEL DIŞA AKTAR] → .xlsx dosyası                        │
│  [JSON İÇERİ AKTAR] → Yeni veriler ekle                    │
│  [+ YENİ SATIR EKLE] → Manuel veri ekleme                  │
└─────────────────────────────────────────────────────────────┘
```

---

## **7. SEED SERVICE İLE NEDİR? NEDEN KULLANDI?**

### **Seed Service Nedir?**
Uygulama ilk çalıştığında otomatik olarak örnek/test verilerini veritabanına ekleyen bir servis.

### **Neden Kullanıldı?**
```
✅ Frontend'de test etmek için veri gerekiyor
✅ Gösterişli bir örnek veri seti hazırlamak
✅ Sistem işleyişini göstermek
✅ Sabit, güvenilir test verisi sağlamak
✅ Her uygulama başlatmada aynı veriler
```

### **Seed Service Dosya Yolu:**
```
backend/DecisionTree.Api/Services/JobApplicationSeedService.cs
```

---

## **8. TEKNIK STACK**

### **Backend**
```
┌─────────────────────────────────┐
│ C# .NET 8.0                     │
│ ↓                               │
│ ASP.NET Core Web API            │
│ (REST Endpoints)                │
│ ↓                               │
│ Entity Framework Core 8.0.6      │
│ (ORM - Database bağlantısı)     │
│ ↓                               │
│ MySQL 8.0                       │
│ (Veritabanı)                    │
└─────────────────────────────────┘
```

### **Frontend**
```
┌─────────────────────────────────┐
│ Angular 17                      │
│ (Standalone Components)         │
│ ↓                               │
│ TypeScript                      │
│ ↓                               │
│ RxJS (Observable)               │
│ ↓                               │
│ HttpClient (API çağrıları)      │
│ ↓                               │
│ Signals (Reactive State)        │
└─────────────────────────────────┘
```

---

## **9. ÖZET - BİR CÜMLEDE**

**"Karar ağacı sistemi, Excel/JSON formatında veri alıp işleme tabi tutarak sonuç veren, web tabanlı bir veri yönetim platformudur. Backend seed service ile örnek veriler otomatik yüklenir, frontend angular ile bunları tablosal formatta gösterir ve kullanıcı yeni veri ekleyebilir, Excel/JSON'a çevirebilir."**

---

## **10. KULLANICI AKIŞI (Step by Step)**

### **Senaryo: İş Başvurusunu Değerlendirmek**

1️⃣ **Kullanıcı sistemi açar**
   - localhost:4200 → Karar Ağaçları Listesi
   
2️⃣ **"JOB_APPLICATION_EVAL" satırına tıklar**
   - "VERİ GİRİŞİ" butonunu tıklar
   
3️⃣ **"BasvuruBilgileri" tablosunu seçer**
   - 10 aday otomatik görünür (Seed tarafından)
   
4️⃣ **Yeni aday eklemek isterse**
   - "+ YENİ SATIR EKLE" → Modal açılır
   - Bilgileri doldurur → Kaydet
   
5️⃣ **Verileri dışa aktarmak isterse**
   - "📥 EXCEL DIŞA AKTAR" → .xlsx dosyası indirilir
   - "📥 JSON DIŞA AKTAR" → JSON dosyası indirilir
   
6️⃣ **Dışarıdan veri almak isterse**
   - JSON içeriğini kopyala
   - "📤 JSON İÇERİ AKTAR" → Yapıştır → AL
   - Veriler veritabanına kaydedilir

---

Umarım şimdi proje tamamıyla açık! Başka sorun varsa sor! 🚀

