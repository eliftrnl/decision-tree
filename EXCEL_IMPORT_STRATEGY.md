# 📊 Excel Import Strategy Detayları

## 🔑 Temel Konsept: Unique Identifier Column

Excel import'ta satır eşleştirmesi **IsUniqueIdentifier** alanına göre yapılır.

### **Adım 1: Unique Identifier Ayarla**
Kolon yönetiminde **MusteriNo** sütununu şöyle ayarla:
```
Kolon Adı: MusteriNo
DataType: String
IsRequired: true
IsUniqueIdentifier: ✅ (işaretli)  ← BU ÖNEMLİ!
```

---

## 📋 Import Akışı

### **A) IMPORT DIALOG**
```
Modal çıkıyor:
✅ EVET  → "REPLACE MODE"   (bütün veriyi değiştir)
❌ HAYIR → "MERGE MODE"     (akıllı birleştir)
```

---

### **B) REPLACE MODE (`replaceExisting=true`)**

**Backend kodu:**
```csharp
var existingRows = _db.DecisionTreeData.Where(d => d.TableId == table.Id);
_db.DecisionTreeData.RemoveRange(existingRows);  // TÜMÜ SİL
// Sonra yeni satırları insert et
```

**Sonuç:**
```
BEFORE:
├─ MusteriNo: 1001, KimlikNo: 12345678901, Tip: Bireysel
└─ MusteriNo: 1002, KimlikNo: 98765432109, Tip: Kurumsal

AFTER (Excel'den import ettiğinde):
├─ MusteriNo: 1001, KimlikNo: 12345678901, Tip: VIP Müşteri (EXCEL'DEN AYNISI)
└─ MusteriNo: 1003, KimlikNo: 55555555555, Tip: Bireysel     (EXCEL'DEN AYNISI)

⚠️ RESULT: MusteriNo 1002 silinir! Sadece Excel'deki satırlar kalır.
```

---

### **C) MERGE MODE (`replaceExisting=false` + Unique Identifier TİPLENDİ)**

**Backend kodu:**
```csharp
var uniqueIdColumn = "MusteriNo";  // IsUniqueIdentifier=true

foreach (var newRowDict in tableData.Rows)
{
    var newUidValue = newRowDict["MusteriNo"];  // Excel'den okunan value
    
    // Veritabanında bu MusteriNo'ya sahip satır var mı?
    var matchingExistingRow = existingDataRows.FirstOrDefault(row =>
    {
        var existingData = JsonSerializer.Deserialize<Dictionary<string, object?>>(row.RowDataJson);
        return existingData["MusteriNo"]?.ToString() == newUidValue?.ToString();
    });
    
    if (matchingExistingRow != null)
    {
        // ✅ BULUNDU → UPDATE (satırı güncelleşti)
        matchingExistingRow.RowDataJson = JsonSerializer.Serialize(newRowDict);
        matchingExistingRow.UpdatedAtUtc = DateTime.UtcNow;
    }
    else
    {
        // ✅ BULUNAMADI → INSERT (yeni satır ekle)
        var newDataRow = new DecisionTreeData { ... };
        _db.DecisionTreeData.Add(newDataRow);
    }
}
```

**Sonuç:**
```
BEFORE (Veritabanı):
├─ MusteriNo: 1001, KimlikNo: 12345678901, Tip: Bireysel
└─ MusteriNo: 1002, KimlikNo: 98765432109, Tip: Kurumsal

EXCEL'DEN IMPORT:
├─ MusteriNo: 1001, KimlikNo: 12345678901, Tip: VIP Müşteri
└─ MusteriNo: 1003, KimlikNo: 55555555555, Tip: Bireysel

AFTER (Smart merge):
├─ MusteriNo: 1001, KimlikNo: 12345678901, Tip: VIP Müşteri  ← UPDATE (MusteriNo match!)
├─ MusteriNo: 1002, KimlikNo: 98765432109, Tip: Kurumsal    ← KALIR (Excel'de yok, silinmez)
└─ MusteriNo: 1003, KimlikNo: 55555555555, Tip: Bireysel     ← INSERT (yeni)

✅ RESULT: 
  - MusteriNo 1001: Güncellenmiş (VIP Müşteri oldu)
  - MusteriNo 1002: Saklı kaldı
  - MusteriNo 1003: Yeni eklendi
```

---

### **D) MERGE MODE (IsUniqueIdentifier AYARLANMAYAN Tablo)**

Eğer kolon yönetiminde **IsUniqueIdentifier** işaretli YOKSA:

```csharp
// No unique identifier - warn user and insert as new rows
allErrors.Add(
    $"Table '{tableName}' has no unique identifier. " +
    $"Rows are being inserted as new (not merged with existing data). " +
    $"Set IsUniqueIdentifier=true on one column to enable merge mode.");

// Excel'den gelen butun satırlar INSERT olarak eklenir (UPDATE değil!)
foreach (var rowDict in tableData.Rows)
{
    var newDataRow = new DecisionTreeData { ... };
    _db.DecisionTreeData.Add(newDataRow);  // Yeni satır ekle
}
```

**Sonuç:**
```
BEFORE:
├─ MusteriNo: 1001, ...
└─ MusteriNo: 1002, ...

AFTER (İçe aktar):
├─ MusteriNo: 1001, ...
├─ MusteriNo: 1002, ...
├─ MusteriNo: 1001, ... (DUPLICATE! Excel'den tekrar eklenir)
└─ MusteriNo: 1003, ...

⚠️ DUPLİKAT SATIR RİSKİ!
```

---

## ✅ BEST PRACTICE

### **1. Tablo Oluştur**
```
TOEI_MUSTERI tablosunu yaratırken Kolonları ekle:
├─ MusteriNo     (String, IsRequired=true, IsUniqueIdentifier=✅)
├─ KimlikNo      (String, IsRequired=true)
└─ MusteriTipi   (String, IsRequired=false)
```

### **2. Veri Gir**
UI üzerinden veya manual insert edip 2-3 satır ekle.

### **3. Excel'e Çıkart**
```
Frontend: 📥 Excel Dışa Aktar
→ MusteriNo_20260213.xlsx indirilir
```

### **4. Excel'de Değiştir**
- MusteriNo 1001 satırında "Tip" → "VIP Müşteri" yap
- Yeni satır ekle: MusteriNo=1003

### **5. Geri İçe Aktar (MERGE MODE)**
```
Frontend: 📤 Excel İçe Aktar
→ File seç
→ Dialog: HAYIR (Merge mode)
→ Modal gösterilir: Başarılı ✅
```

---

## 🔍 Sonucu Kontrol Et

**Frontend Veri Tablosu:**
```
Sıra | MusteriNo | KimlikNo | MusteriTipi
-----|-----------|----------|------------
1    | 1001      | 12345... | VIP Müşteri  ← UPDATED
2    | 1002      | 98765... | Kurumsal     ← KALABİLİR
3    | 1003      | 55555... | Bireysel     ← NEW
```

---

## 📝 Response Example (Frontend Modal'da Gösterilir)

### **Success Response:**
```json
{
  "message": "Excel başarıyla içe aktarıldı (Veriler birleştirildi)",
  "importedRowsCount": 2,
  "tablesProcessed": 1,
  "warnings": [
    "Excel column 'ExtraColumn' does not match any database column"
  ],
  "errors": []
}
```

### **Error Response (Schema Mismatch):**
```json
{
  "message": "Failed to read Excel file",
  "code": "EXCEL_PARSE_ERROR",
  "errors": [
    "Required column 'MusteriNo' not found in Excel file"
  ],
  "warnings": []
}
```

---

## 🎯 Özet

| Mode | Komut | Davranış | Risk |
|------|-------|----------|------|
| **REPLACE** | `?replaceExisting=true` | Tüm veriyi sil + yenisini ekle | Veri kaybı ⚠️ |
| **MERGE** | `?replaceExisting=false` | Unique ID ile match + update/insert | Duplikat ⚠️ (ID yoksa) |

> **💡 Tavsiye:** Merge mode kullan + her kolonda **IsUniqueIdentifier** birini işaretle!
