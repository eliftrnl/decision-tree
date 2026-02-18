# 🎯 Excel İçe Aktarma - Pratik Örnek

## 📌 Senaryo: TOEI_MUSTERI Tablosu

### **Step 1: Tablo Yapısını Ayarla**

#### Column Management (Ekran 3):
```
Tablo: TOEI_MUSTERI
├─ Kolon 1: MusteriNo
│    ├─ DataType: String (1)
│    ├─ IsRequired: ✅ true
│    ├─ IsUniqueIdentifier: ✅ TRUE  ← KRITIK!
│    └─ ExcelHeaderName: "Musteri No"
│
├─ Kolon 2: KimlikNo
│    ├─ DataType: String (1)
│    ├─ IsRequired: ✅ true
│    ├─ IsUniqueIdentifier: ❌ false
│    └─ ExcelHeaderName: "TC Kimlik No"
│
└─ Kolon 3: MusteriTipi
     ├─ DataType: String (1)
     ├─ IsRequired: ❌ false
     ├─ IsUniqueIdentifier: ❌ false
     └─ ExcelHeaderName: "Müşteri Tipi"
```

---

### **Step 2: Veri Gir (Ekran 4 - Veri Girişi)**

**Data Entry UI'dan 3 satır ekle:**

#### Satır 1:
```json
{
  "MusteriNo": "1001",
  "KimlikNo": "12345678901",
  "MusteriTipi": "Bireysel"
}
```

#### Satır 2:
```json
{
  "MusteriNo": "1002",
  "KimlikNo": "98765432109",
  "MusteriTipi": "Kurumsal"
}
```

#### Satır 3:
```json
{
  "MusteriNo": "1003",
  "KimlikNo": "55555555555",
  "MusteriTipi": "Bireysel"
}
```

**Veritabanında şu an:**
```
decision_tree_data tablosu:
┌─────┬──────────────────────────────────────────────┐
│ ID  │ RowDataJson                                  │
├─────┼──────────────────────────────────────────────┤
│ 101 │ {MusteriNo:1001, KimlikNo:123..., Tipi:B}  │
│ 102 │ {MusteriNo:1002, KimlikNo:987..., Tipi:K}  │
│ 103 │ {MusteriNo:1003, KimlikNo:555..., Tipi:B}  │
└─────┴──────────────────────────────────────────────┘
```

---

### **Step 3: Excel'e Dışa Aktar (Export)**

**Frontend: 📥 Excel Dışa Aktar butonu tıkla**

```
GET /api/decision-trees/1/data/export-excel
↓
TOEI_MUSTERI_20260213_121530.xlsx indirilir
```

**Excel dosyasının içeriği:**

| Musteri No | TC Kimlik No | Müşteri Tipi |
|------------|--------------|--------------|
| 1001       | 12345678901  | Bireysel     |
| 1002       | 98765432109  | Kurumsal     |
| 1003       | 55555555555  | Bireysel     |

---

### **Step 4: Excel'de Değişiklik Yap**

**Excel dosyasını açıp şu değişiklikleri yap:**

| Musteri No | TC Kimlik No | Müşteri Tipi |
|------------|--------------|--------------|
| **1001**   | 12345678901  | **VIP**      | ← Tipi "Bireysel" → "VIP" değişti |
| (silinmiş) | (silinmiş)   | (silinmiş)   | ← Satır 1002 sildik               |
| 1003       | 55555555555  | Bireysel     | ← Değiştirilmedi                   |
| **1004**   | **77777777777** | **Kurumsal** | ← Yeni satır ekledik             |

**Değişiklik Özeti:**
- ❌ Silinmiş: MusteriNo=1002
- ✏️ Güncellenmiş: MusteriNo=1001 (Tipi değişti)
- ➕ Yeni: MusteriNo=1004
- ➡️ Değişmedi: MusteriNo=1003

---

### **Step 5: Excel'i Geri İçe Aktar (Import)**

**Frontend: 📤 Excel İçe Aktar butonu tıkla**

```
File picker → TOEI_MUSTERI_20260213_121530.xlsx
↓
Dialog çıkıyor:
┌──────────────────────────────────────────────┐
│ Mevcut verileri yeni Excel dosyasının       │
│ verileriyle değiştirilsin mi?                 │
│                                              │
│ [EVET] - Eski veriler silinip yeni... │
│ [HAYIR] - Yeni veriler mevcut verilere...│
└──────────────────────────────────────────────┘
```

---

## 🔄 SENARYOYA GÖRE SONUÇ

### **SCENARIO A: EVET (REPLACE MODE)**

**Dialog'ta EVET'i tıkla**

```
Backend: replaceExisting = true
```

#### Execution:
```csharp
// ADIM 1: Tüm satırları SİL
var existingRows = _db.DecisionTreeData.Where(d => d.TableId == tableId);
_db.DecisionTreeData.RemoveRange(existingRows);
// Siler: ID 101, 102, 103

// ADIM 2: Excel'deki tüm satırları INSERT et
_db.DecisionTreeData.Add(
  new DecisionTreeData {
    TableId = 1,
    RowDataJson = "{MusteriNo:1001, KimlikNo:123..., Tipi:VIP}"
  }
);
_db.DecisionTreeData.Add(
  new DecisionTreeData {
    TableId = 1,
    RowDataJson = "{MusteriNo:1003, KimlikNo:555..., Tipi:Bireysel}"
  }
);
_db.DecisionTreeData.Add(
  new DecisionTreeData {
    TableId = 1,
    RowDataJson = "{MusteriNo:1004, KimlikNo:777..., Tipi:Kurumsal}"
  }
);
```

#### **SONUÇ (Veritabanı):**
```
decision_tree_data tablosu:
┌─────┬────────────────────────────────────────────┐
│ ID  │ RowDataJson                                │
├─────┼────────────────────────────────────────────┤
│ 104 │ {MusteriNo:1001, KimlikNo:123..., T:VIP}  │ ← ID DEĞİŞTİ (ESKI SILINIP YENİ OLUŞTU)
│ 105 │ {MusteriNo:1003, KimlikNo:555..., T:B}   │ ← ID DEĞİŞTİ
│ 106 │ {MusteriNo:1004, KimlikNo:777..., T:K}   │ ← YENİ
└─────┴────────────────────────────────────────────┘

⚠️ MusteriNo=1002 SİLİNDİ!
✅ Response:
{
  "message": "Excel başarıyla içe aktarıldı (Veriler değiştirildi)",
  "importedRowsCount": 3,
  "tablesProcessed": 1,
  "warnings": [],
  "errors": []
}
```

**Frontend Modal:**
```
✅ Excel başarıyla içe aktarıldı (Veriler değiştirildi)
├─ Yüklenen Satırlar: 3
├─ İşlenen Tablolar: 1
└─ Hatalar: 0
```

---

### **SCENARIO B: HAYIR (MERGE MODE)**

**Dialog'ta HAYIR'ı tıkla**

```
Backend: replaceExisting = false
uniqueIdColumn = "MusteriNo" (IsUniqueIdentifier=true)
```

#### Execution:

**Excel Row 1: MusteriNo=1001**
```csharp
string uniqueIdColumn = "MusteriNo";
var newUidValue = "1001";

// Veritabanında MusteriNo=1001 var mı?
var matchingRow = existingDataRows.FirstOrDefault(row =>
{
    var data = JsonDeserialize(row.RowDataJson);
    return data["MusteriNo"]?.ToString() == "1001";
});

// ✅ BULUNDU (ID 101'i buldu)
if (matchingRow != null)
{
    // UPDATE: ID 101'in JSON'ını yenile
    matchingRow.RowDataJson = "{MusteriNo:1001, KimlikNo:123..., Tipi:VIP}";
    matchingRow.UpdatedAtUtc = DateTime.UtcNow;
}
```

**Excel Row 2: MusteriNo=1003**
```csharp
var newUidValue = "1003";

// Veritabanında MusteriNo=1003 var mı?
var matchingRow = existingDataRows.FirstOrDefault(...);

// ✅ BULUNDU (ID 103'ü buldu)
if (matchingRow != null)
{
    // UPDATE: Ama hiç değişmedi, aynı veri
    matchingRow.RowDataJson = "{MusteriNo:1003, KimlikNo:555..., Tipi:Bireysel}";
    matchingRow.UpdatedAtUtc = DateTime.UtcNow;
}
```

**Excel Row 3: MusteriNo=1004**
```csharp
var newUidValue = "1004";

// Veritabanında MusteriNo=1004 var mı?
var matchingRow = existingDataRows.FirstOrDefault(...);

// ❌ BULUNAMADI
if (matchingRow != null)
{
    // Not executed
}
else
{
    // INSERT: Yeni satır ekle
    var newDataRow = new DecisionTreeData
    {
        TableId = 1,
        RowDataJson = "{MusteriNo:1004, KimlikNo:777..., Tipi:Kurumsal}",
        CreatedAtUtc = DateTime.UtcNow,
        UpdatedAtUtc = DateTime.UtcNow
    };
    _db.DecisionTreeData.Add(newDataRow);
}
```

#### **SONUÇ (Veritabanı):**
```
decision_tree_data tablosu:
┌─────┬────────────────────────────────────────────┐
│ ID  │ RowDataJson                                │
├─────┼────────────────────────────────────────────┤
│ 101 │ {MusteriNo:1001, KimlikNo:123..., T:VIP}  │ ← UPDATED (Tipi=VIP)
│ 102 │ {MusteriNo:1002, KimlikNo:987..., T:K}   │ ← KALDI (silinmedi!)
│ 103 │ {MusteriNo:1003, KimlikNo:555..., T:B}   │ ← KALDI (değişmedi)
│ 104 │ {MusteriNo:1004, KimlikNo:777..., T:K}   │ ← YENİ
└─────┴────────────────────────────────────────────┘

✅ Response:
{
  "message": "Excel başarıyla içe aktarıldı (Veriler birleştirildi)",
  "importedRowsCount": 3,
  "tablesProcessed": 1,
  "warnings": [],
  "errors": []
}
```

**Frontend Modal:**
```
✅ Excel başarıyla içe aktarıldı (Veriler birleştirildi)
├─ Yüklenen Satırlar: 3
│   ├─ 1 satır güncellendi (1001)
│   ├─ 2 satır eklendi (1004)
│   └─ 1 satır saklanıyor (1002)
├─ İşlenen Tablolar: 1
└─ Hatalar: 0
```

---

## 🔍 Veritabanı Sorgusu ile Fark Görmek

### **REPLACE MODE Sonrası:**
```sql
SELECT * FROM decision_tree_data WHERE table_id = 1;
-- 3 row(s)
```

### **MERGE MODE Sonrası:**
```sql
SELECT * FROM decision_tree_data WHERE table_id = 1;
-- 4 row(s) ← 1002 kalabildi, 1004 eklendi
```

---

## 📝 Log Çıkışları (Backend)

### **REPLACE MODE:**
```
[INFO] Replacing all data in table 'TOEI_MUSTERI' (total rows to insert: 3)
[DEBUG] Inserted new row...
[DEBUG] Inserted new row...
[DEBUG] Inserted new row...
```

### **MERGE MODE (Unique ID ile):**
```
[INFO] Merging data in table 'TOEI_MUSTERI' using unique identifier 'MusteriNo'
[DEBUG] Updated row with MusteriNo=1001 in table 'TOEI_MUSTERI'
[DEBUG] Updated row with MusteriNo=1003 in table 'TOEI_MUSTERI'
[DEBUG] Inserted new row with MusteriNo=1004 in table 'TOEI_MUSTERI'
```

### **MERGE MODE (Unique ID YOKSA):**
```
[WARN] Table 'TOEI_MUSTERI' has no unique identifier column defined.
[WARN] Import will only INSERT mode (not merge). Set IsUniqueIdentifier on a column.
[DEBUG] Inserted new row...
[DEBUG] Inserted new row...
[DEBUG] Inserted new row...
```

---

## 🎯 Best Practice Tavsiye

### ✅ YAPILMASI GEREKEN:
1. **Her tablo için UN bir merkez bulunmalı** (Müşteri No, TC Kimlik, vs.)
2. Kolon yönetiminde `IsUniqueIdentifier=true` işaretle
3. **Export → Değiştir → Import** akışı için MERGE mode kullan
4. Önemli tablolarda **backup** al

### ❌ KAÇINILMASI GEREKEN:
1. REPLACE mode'u hafif alıp test'te kullan
2. Unique identifier olmadan MERGE mode
3. Aynı unique ID ile birden fazla satır
4. Excel'de formula veya hidden column

---

## 🧪 Test Senaryosu

### **Test Case 1: Basic Merge**
```
Setup: 2 satır (1001, 1002) DB'de
Action: Excel'den submit (1001 modified, 1003 new)
Mode: MERGE
Expected: 3 satır (1001 updated, 1002 kalır, 1003 eklenir)
```

### **Test Case 2: No Unique ID**
```
Setup: Table'da IsUniqueIdentifier yok
Action: Excel'den submit 2 satır
Mode: MERGE
Expected: ⚠️ Warning modal, 4 satır total (duplikat var)
```

### **Test Case 3: Replace Mode**
```
Setup: 2 satır DB'de
Action: Excel'den submit (1001 modified)
Mode: REPLACE
Expected: ✅ 1 satır total (eski silinmiş)
```

---

