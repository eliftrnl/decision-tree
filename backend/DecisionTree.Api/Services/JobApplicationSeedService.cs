using DecisionTree.Api.Data;
using DecisionTree.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace DecisionTree.Api.Services;

/// <summary>
/// Seed service to populate the database with sample Job Application Evaluation data
/// </summary>
public sealed class JobApplicationSeedService
{
    private readonly AppDbContext _db;
    private readonly ILogger<JobApplicationSeedService> _logger;

    public JobApplicationSeedService(AppDbContext db, ILogger<JobApplicationSeedService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task SeedDataAsync()
    {
        try
        {
            var existingTree = await _db.DecisionTrees
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Code == "JOB_APPLICATION_EVAL");

            if (existingTree != null)
            {
                _logger.LogInformation("Job Application Evaluation data already exists. Skipping seed.");
                return;
            }

            _logger.LogInformation("Seeding Job Application Evaluation data...");

            var decisionTree = new Entities.DecisionTree
            {
                Code = "JOB_APPLICATION_EVAL",
                Name = "İş Başvurusu Değerlendirme Sistemi",
                StatusCode = StatusCode.Active,
                SchemaVersion = 1,
                CreatedAtUtc = DateTime.UtcNow
            };

            _db.DecisionTrees.Add(decisionTree);
            await _db.SaveChangesAsync();

            var dtId = decisionTree.Id;

            // ========================================================
            // INPUT TABLES
            // ========================================================

            var basvuruTable = new DecisionTreeTable
            {
                DecisionTreeId = dtId,
                TableName = "BasvuruBilgileri",
                Direction = TableDirection.Input,
                StatusCode = StatusCode.Active,
                CreatedAtUtc = DateTime.UtcNow
            };
            _db.DecisionTreeTables.Add(basvuruTable);
            await _db.SaveChangesAsync();

            var basvuruColumns = new List<TableColumn>
            {
                new() { TableId = basvuruTable.Id, ColumnName = "AdayId", ExcelHeaderName = "ADAY_ID", Description = "Aday kimlik numarası", DataType = ColumnDataType.Int, IsRequired = true, OrderIndex = 0 },
                new() { TableId = basvuruTable.Id, ColumnName = "AdayAdi", ExcelHeaderName = "ADAY_ADI", Description = "Adayın adı", DataType = ColumnDataType.String, IsRequired = true, OrderIndex = 1, MaxLength = 100 },
                new() { TableId = basvuruTable.Id, ColumnName = "AdaySoyadi", ExcelHeaderName = "ADAY_SOYADI", Description = "Adayın soyadı", DataType = ColumnDataType.String, IsRequired = true, OrderIndex = 2, MaxLength = 100 },
                new() { TableId = basvuruTable.Id, ColumnName = "Email", ExcelHeaderName = "EMAIL", Description = "E-posta adresi", DataType = ColumnDataType.String, IsRequired = true, OrderIndex = 3, MaxLength = 200 },
                new() { TableId = basvuruTable.Id, ColumnName = "Telefon", ExcelHeaderName = "TELEFON", Description = "Telefon numarası", DataType = ColumnDataType.String, IsRequired = false, OrderIndex = 4, MaxLength = 20 },
                new() { TableId = basvuruTable.Id, ColumnName = "DeneyimYili", ExcelHeaderName = "DENEYIM_YILI", Description = "İş deneyimi (yıl)", DataType = ColumnDataType.Int, IsRequired = true, OrderIndex = 5 },
                new() { TableId = basvuruTable.Id, ColumnName = "EgitimSeviyesi", ExcelHeaderName = "EGITIM_SEVIYESI", Description = "Eğitim seviyesi", DataType = ColumnDataType.Int, IsRequired = true, OrderIndex = 6 },
                new() { TableId = basvuruTable.Id, ColumnName = "ProgramlamaDilleri", ExcelHeaderName = "PROGRAMLAMA_DILLERI", Description = "Programlama dilleri", DataType = ColumnDataType.String, IsRequired = false, OrderIndex = 7, MaxLength = 500 },
                new() { TableId = basvuruTable.Id, ColumnName = "YabancıDilSeviyesi", ExcelHeaderName = "YABANCI_DIL_SEVIYESI", Description = "İngilizce seviyesi", DataType = ColumnDataType.Int, IsRequired = false, OrderIndex = 8 },
                new() { TableId = basvuruTable.Id, ColumnName = "BasvuruTarihi", ExcelHeaderName = "BASVURU_TARIHI", Description = "Başvuru tarihi", DataType = ColumnDataType.Date, IsRequired = true, OrderIndex = 9, Format = "yyyy-MM-dd" }
            };
            _db.TableColumns.AddRange(basvuruColumns);
            await _db.SaveChangesAsync();

            var pozisyonTable = new DecisionTreeTable
            {
                DecisionTreeId = dtId,
                TableName = "PozisyonBilgileri",
                Direction = TableDirection.Input,
                StatusCode = StatusCode.Active,
                CreatedAtUtc = DateTime.UtcNow
            };
            _db.DecisionTreeTables.Add(pozisyonTable);
            await _db.SaveChangesAsync();

            var pozisyonColumns = new List<TableColumn>
            {
                new() { TableId = pozisyonTable.Id, ColumnName = "PozisyonId", ExcelHeaderName = "POZISYON_ID", Description = "Pozisyon kimlik", DataType = ColumnDataType.Int, IsRequired = true, OrderIndex = 0 },
                new() { TableId = pozisyonTable.Id, ColumnName = "PozisyonAdi", ExcelHeaderName = "POZISYON_ADI", Description = "Pozisyon adı", DataType = ColumnDataType.String, IsRequired = true, OrderIndex = 1, MaxLength = 100 },
                new() { TableId = pozisyonTable.Id, ColumnName = "Departman", ExcelHeaderName = "DEPARTMAN", Description = "Departman", DataType = ColumnDataType.String, IsRequired = true, OrderIndex = 2, MaxLength = 100 },
                new() { TableId = pozisyonTable.Id, ColumnName = "Lokasyon", ExcelHeaderName = "LOKASYON", Description = "İş lokasyonu", DataType = ColumnDataType.String, IsRequired = true, OrderIndex = 3, MaxLength = 100 },
                new() { TableId = pozisyonTable.Id, ColumnName = "PozisyonSeviyesi", ExcelHeaderName = "POZISYON_SEVIYESI", Description = "Seviye", DataType = ColumnDataType.Int, IsRequired = true, OrderIndex = 4 },
                new() { TableId = pozisyonTable.Id, ColumnName = "MaasAraligiMin", ExcelHeaderName = "MAAS_ARALIGI_MIN", Description = "Min maaş", DataType = ColumnDataType.Decimal, IsRequired = true, OrderIndex = 5, Precision = 10, Scale = 2 },
                new() { TableId = pozisyonTable.Id, ColumnName = "MaasAraligiMax", ExcelHeaderName = "MAAS_ARALIGI_MAX", Description = "Max maaş", DataType = ColumnDataType.Decimal, IsRequired = true, OrderIndex = 6, Precision = 10, Scale = 2 },
                new() { TableId = pozisyonTable.Id, ColumnName = "GerekliYetenekler", ExcelHeaderName = "GEREKLI_YETENEKLER", Description = "Gerekli yetenekler", DataType = ColumnDataType.String, IsRequired = false, OrderIndex = 7, MaxLength = 1000 }
            };
            _db.TableColumns.AddRange(pozisyonColumns);
            await _db.SaveChangesAsync();

            var kriterTable = new DecisionTreeTable
            {
                DecisionTreeId = dtId,
                TableName = "PozisyonKriterleri",
                Direction = TableDirection.Input,
                StatusCode = StatusCode.Active,
                CreatedAtUtc = DateTime.UtcNow
            };
            _db.DecisionTreeTables.Add(kriterTable);
            await _db.SaveChangesAsync();

            var kriterColumns = new List<TableColumn>
            {
                new() { TableId = kriterTable.Id, ColumnName = "KriterId", ExcelHeaderName = "KRITER_ID", Description = "Kriter kimlik", DataType = ColumnDataType.Int, IsRequired = true, OrderIndex = 0 },
                new() { TableId = kriterTable.Id, ColumnName = "PozisyonId", ExcelHeaderName = "POZISYON_ID", Description = "Pozisyon kimlik", DataType = ColumnDataType.Int, IsRequired = true, OrderIndex = 1 },
                new() { TableId = kriterTable.Id, ColumnName = "MinDeneyimYili", ExcelHeaderName = "MIN_DENEYIM_YILI", Description = "Min deneyim", DataType = ColumnDataType.Int, IsRequired = true, OrderIndex = 2 },
                new() { TableId = kriterTable.Id, ColumnName = "MinEgitimSeviyesi", ExcelHeaderName = "MIN_EGITIM_SEVIYESI", Description = "Min eğitim", DataType = ColumnDataType.Int, IsRequired = true, OrderIndex = 3 },
                new() { TableId = kriterTable.Id, ColumnName = "GerekliDilSeviyesi", ExcelHeaderName = "GEREKLI_DIL_SEVIYESI", Description = "Min dil", DataType = ColumnDataType.Int, IsRequired = true, OrderIndex = 4 },
                new() { TableId = kriterTable.Id, ColumnName = "GerekliProgramlamaDilleri", ExcelHeaderName = "GEREKLI_PROGRAMLAMA_DILLERI", Description = "Zorunlu diller", DataType = ColumnDataType.String, IsRequired = false, OrderIndex = 5, MaxLength = 500 }
            };
            _db.TableColumns.AddRange(kriterColumns);
            await _db.SaveChangesAsync();

            // ========================================================
            // OUTPUT TABLES
            // ========================================================

            var sonucTable = new DecisionTreeTable
            {
                DecisionTreeId = dtId,
                TableName = "DegerlendirmeSonucu",
                Direction = TableDirection.Output,
                StatusCode = StatusCode.Active,
                CreatedAtUtc = DateTime.UtcNow
            };
            _db.DecisionTreeTables.Add(sonucTable);
            await _db.SaveChangesAsync();

            var sonucColumns = new List<TableColumn>
            {
                new() { TableId = sonucTable.Id, ColumnName = "SonucId", ExcelHeaderName = "SONUC_ID", Description = "Sonuç kimlik", DataType = ColumnDataType.Int, IsRequired = true, OrderIndex = 0 },
                new() { TableId = sonucTable.Id, ColumnName = "AdayId", ExcelHeaderName = "ADAY_ID", Description = "Aday kimlik", DataType = ColumnDataType.Int, IsRequired = true, OrderIndex = 1 },
                new() { TableId = sonucTable.Id, ColumnName = "PozisyonId", ExcelHeaderName = "POZISYON_ID", Description = "Pozisyon kimlik", DataType = ColumnDataType.Int, IsRequired = true, OrderIndex = 2 },
                new() { TableId = sonucTable.Id, ColumnName = "DegerlendirmeKarari", ExcelHeaderName = "DEGERLENDIRME_KARARI", Description = "Karar", DataType = ColumnDataType.Int, IsRequired = true, OrderIndex = 3 },
                new() { TableId = sonucTable.Id, ColumnName = "Skor", ExcelHeaderName = "SKOR", Description = "Puan", DataType = ColumnDataType.Int, IsRequired = false, OrderIndex = 4 },
                new() { TableId = sonucTable.Id, ColumnName = "DeneyimPuani", ExcelHeaderName = "DENEYIM_PUANI", Description = "Deneyim puanı", DataType = ColumnDataType.Int, IsRequired = false, OrderIndex = 5 },
                new() { TableId = sonucTable.Id, ColumnName = "EgitimPuani", ExcelHeaderName = "EGITIM_PUANI", Description = "Eğitim puanı", DataType = ColumnDataType.Int, IsRequired = false, OrderIndex = 6 },
                new() { TableId = sonucTable.Id, ColumnName = "DilPuani", ExcelHeaderName = "DIL_PUANI", Description = "Dil puanı", DataType = ColumnDataType.Int, IsRequired = false, OrderIndex = 7 },
                new() { TableId = sonucTable.Id, ColumnName = "DegerlendirmeNotu", ExcelHeaderName = "DEGERLENDIRME_NOTU", Description = "Not", DataType = ColumnDataType.String, IsRequired = false, OrderIndex = 8, MaxLength = 2000 },
                new() { TableId = sonucTable.Id, ColumnName = "DegerlendirmeTarihi", ExcelHeaderName = "DEGERLENDIRME_TARIHI", Description = "Tarih", DataType = ColumnDataType.Date, IsRequired = true, OrderIndex = 9, Format = "yyyy-MM-dd HH:mm:ss" }
            };
            _db.TableColumns.AddRange(sonucColumns);
            await _db.SaveChangesAsync();

            var uyumlulukTable = new DecisionTreeTable
            {
                DecisionTreeId = dtId,
                TableName = "AdayUyumluluk",
                Direction = TableDirection.Output,
                StatusCode = StatusCode.Active,
                CreatedAtUtc = DateTime.UtcNow
            };
            _db.DecisionTreeTables.Add(uyumlulukTable);
            await _db.SaveChangesAsync();

            var uyumlulukColumns = new List<TableColumn>
            {
                new() { TableId = uyumlulukTable.Id, ColumnName = "UyumlulukId", ExcelHeaderName = "UYUMLULUK_ID", Description = "Kimlik", DataType = ColumnDataType.Int, IsRequired = true, OrderIndex = 0 },
                new() { TableId = uyumlulukTable.Id, ColumnName = "AdayId", ExcelHeaderName = "ADAY_ID", Description = "Aday kimlik", DataType = ColumnDataType.Int, IsRequired = true, OrderIndex = 1 },
                new() { TableId = uyumlulukTable.Id, ColumnName = "PozisyonId", ExcelHeaderName = "POZISYON_ID", Description = "Pozisyon kimlik", DataType = ColumnDataType.Int, IsRequired = true, OrderIndex = 2 },
                new() { TableId = uyumlulukTable.Id, ColumnName = "GenelUyumlulukYuzdesi", ExcelHeaderName = "GENEL_UYUMLULUK_YUZDESI", Description = "Uyumluluk %", DataType = ColumnDataType.Int, IsRequired = true, OrderIndex = 3 },
                new() { TableId = uyumlulukTable.Id, ColumnName = "TeknikUyumluluk", ExcelHeaderName = "TEKNIK_UYUMLULUK", Description = "Teknik uyumluluk", DataType = ColumnDataType.Int, IsRequired = false, OrderIndex = 4 },
                new() { TableId = uyumlulukTable.Id, ColumnName = "DeneyimUyumlulugu", ExcelHeaderName = "DENEYIM_UYUMLULUGU", Description = "Deneyim uyumu", DataType = ColumnDataType.Int, IsRequired = false, OrderIndex = 5 },
                new() { TableId = uyumlulukTable.Id, ColumnName = "EksikBeceriler", ExcelHeaderName = "EKSIK_BECERILER", Description = "Eksik beceriler", DataType = ColumnDataType.String, IsRequired = false, OrderIndex = 6, MaxLength = 1000 },
                new() { TableId = uyumlulukTable.Id, ColumnName = "GüclüYönler", ExcelHeaderName = "GUCLU_YONLER", Description = "Güçlü yönler", DataType = ColumnDataType.String, IsRequired = false, OrderIndex = 7, MaxLength = 1000 },
                new() { TableId = uyumlulukTable.Id, ColumnName = "ÖnerilenAksiyon", ExcelHeaderName = "ONERILEN_AKSIYON", Description = "Önerilen aksiyon", DataType = ColumnDataType.Int, IsRequired = true, OrderIndex = 8 },
                new() { TableId = uyumlulukTable.Id, ColumnName = "UyumlulukAciklamasi", ExcelHeaderName = "UYUMLULUK_ACIKLAMASI", Description = "Açıklama", DataType = ColumnDataType.String, IsRequired = false, OrderIndex = 9, MaxLength = 2000 }
            };
            _db.TableColumns.AddRange(uyumlulukColumns);
            await _db.SaveChangesAsync();

            // ========================================================
            // SAMPLE DATA
            // ========================================================

            var basvuruData = new List<DecisionTreeData>
            {
                new() { DecisionTreeId = dtId, TableId = basvuruTable.Id, RowIndex = 1, RowDataJson = "{\"AdayId\":1,\"AdayAdi\":\"Mehmet\",\"AdaySoyadi\":\"Yılmaz\",\"Email\":\"mehmet@email.com\",\"DeneyimYili\":8,\"EgitimSeviyesi\":3,\"ProgramlamaDilleri\":\"C#,Java,Python\",\"YabancıDilSeviyesi\":3,\"BasvuruTarihi\":\"2024-01-15\"}" },
                new() { DecisionTreeId = dtId, TableId = basvuruTable.Id, RowIndex = 2, RowDataJson = "{\"AdayId\":2,\"AdayAdi\":\"Ayşe\",\"AdaySoyadi\":\"Demir\",\"Email\":\"ayse@email.com\",\"DeneyimYili\":2,\"EgitimSeviyesi\":3,\"ProgramlamaDilleri\":\"JavaScript,React\",\"YabancıDilSeviyesi\":2,\"BasvuruTarihi\":\"2024-01-16\"}" },
                new() { DecisionTreeId = dtId, TableId = basvuruTable.Id, RowIndex = 3, RowDataJson = "{\"AdayId\":3,\"AdayAdi\":\"Mustafa\",\"AdaySoyadi\":\"Kara\",\"Email\":\"mustafa@email.com\",\"DeneyimYili\":5,\"EgitimSeviyesi\":4,\"ProgramlamaDilleri\":\"C#,ASP.NET,SQL\",\"YabancıDilSeviyesi\":3,\"BasvuruTarihi\":\"2024-01-17\"}" },
                new() { DecisionTreeId = dtId, TableId = basvuruTable.Id, RowIndex = 4, RowDataJson = "{\"AdayId\":4,\"AdayAdi\":\"Elif\",\"AdaySoyadi\":\"Öztürk\",\"Email\":\"elif@email.com\",\"DeneyimYili\":10,\"EgitimSeviyesi\":5,\"ProgramlamaDilleri\":\"Java,Spring,Kubernetes\",\"YabancıDilSeviyesi\":3,\"BasvuruTarihi\":\"2024-01-18\"}" },
                new() { DecisionTreeId = dtId, TableId = basvuruTable.Id, RowIndex = 5, RowDataJson = "{\"AdayId\":5,\"AdayAdi\":\"Ali\",\"AdaySoyadi\":\"Şahin\",\"Email\":\"ali@email.com\",\"DeneyimYili\":1,\"EgitimSeviyesi\":3,\"ProgramlamaDilleri\":\"Python,Django\",\"YabancıDilSeviyesi\":1,\"BasvuruTarihi\":\"2024-01-19\"}" },
                new() { DecisionTreeId = dtId, TableId = basvuruTable.Id, RowIndex = 6, RowDataJson = "{\"AdayId\":6,\"AdayAdi\":\"Zeynep\",\"AdaySoyadi\":\"Arslan\",\"Email\":\"zeynep@email.com\",\"DeneyimYili\":4,\"EgitimSeviyesi\":3,\"ProgramlamaDilleri\":\"React,Node.js,TypeScript\",\"YabancıDilSeviyesi\":2,\"BasvuruTarihi\":\"2024-01-20\"}" },
                new() { DecisionTreeId = dtId, TableId = basvuruTable.Id, RowIndex = 7, RowDataJson = "{\"AdayId\":7,\"AdayAdi\":\"Emre\",\"AdaySoyadi\":\"Yıldız\",\"Email\":\"emre@email.com\",\"DeneyimYili\":7,\"EgitimSeviyesi\":4,\"ProgramlamaDilleri\":\"Python,Bash,Go\",\"YabancıDilSeviyesi\":3,\"BasvuruTarihi\":\"2024-01-21\"}" },
                new() { DecisionTreeId = dtId, TableId = basvuruTable.Id, RowIndex = 8, RowDataJson = "{\"AdayId\":8,\"AdayAdi\":\"Sema\",\"AdaySoyadi\":\"Koç\",\"Email\":\"sema@email.com\",\"DeneyimYili\":0,\"EgitimSeviyesi\":3,\"ProgramlamaDilleri\":\"Java,C++\",\"YabancıDilSeviyesi\":2,\"BasvuruTarihi\":\"2024-01-22\"}" },
                new() { DecisionTreeId = dtId, TableId = basvuruTable.Id, RowIndex = 9, RowDataJson = "{\"AdayId\":9,\"AdayAdi\":\"Hakan\",\"AdaySoyadi\":\"Polat\",\"Email\":\"hakan@email.com\",\"DeneyimYili\":6,\"EgitimSeviyesi\":3,\"ProgramlamaDilleri\":\"C#,Azure,SQL Server\",\"YabancıDilSeviyesi\":2,\"BasvuruTarihi\":\"2024-01-23\"}" },
                new() { DecisionTreeId = dtId, TableId = basvuruTable.Id, RowIndex = 10, RowDataJson = "{\"AdayId\":10,\"AdayAdi\":\"Derya\",\"AdaySoyadi\":\"Çelik\",\"Email\":\"derya@email.com\",\"DeneyimYili\":9,\"EgitimSeviyesi\":4,\"ProgramlamaDilleri\":\"Python,Java,Selenium\",\"YabancıDilSeviyesi\":3,\"BasvuruTarihi\":\"2024-01-24\"}" }
            };
            _db.DecisionTreeData.AddRange(basvuruData);

            var pozisyonData = new List<DecisionTreeData>
            {
                new() { DecisionTreeId = dtId, TableId = pozisyonTable.Id, RowIndex = 1, RowDataJson = "{\"PozisyonId\":1,\"PozisyonAdi\":\"Senior Yazılım Geliştirici\",\"Departman\":\"Yazılım\",\"Lokasyon\":\"İstanbul\",\"PozisyonSeviyesi\":3,\"MaasAraligiMin\":45000,\"MaasAraligiMax\":75000,\"GerekliYetenekler\":\"C#,ASP.NET Core,SQL\"}" },
                new() { DecisionTreeId = dtId, TableId = pozisyonTable.Id, RowIndex = 2, RowDataJson = "{\"PozisyonId\":2,\"PozisyonAdi\":\"Junior Yazılım Geliştirici\",\"Departman\":\"Yazılım\",\"Lokasyon\":\"Ankara\",\"PozisyonSeviyesi\":1,\"MaasAraligiMin\":25000,\"MaasAraligiMax\":35000,\"GerekliYetenekler\":\"JavaScript,HTML,CSS\"}" },
                new() { DecisionTreeId = dtId, TableId = pozisyonTable.Id, RowIndex = 3, RowDataJson = "{\"PozisyonId\":3,\"PozisyonAdi\":\"DevOps Mühendisi\",\"Departman\":\"Altyapı\",\"Lokasyon\":\"İzmir\",\"PozisyonSeviyesi\":3,\"MaasAraligiMin\":50000,\"MaasAraligiMax\":80000,\"GerekliYetenekler\":\"Docker,Kubernetes,Terraform\"}" }
            };
            _db.DecisionTreeData.AddRange(pozisyonData);

            var kriterData = new List<DecisionTreeData>
            {
                new() { DecisionTreeId = dtId, TableId = kriterTable.Id, RowIndex = 1, RowDataJson = "{\"KriterId\":1,\"PozisyonId\":1,\"MinDeneyimYili\":5,\"MinEgitimSeviyesi\":3,\"GerekliDilSeviyesi\":2,\"GerekliProgramlamaDilleri\":\"C#,ASP.NET Core\"}" },
                new() { DecisionTreeId = dtId, TableId = kriterTable.Id, RowIndex = 2, RowDataJson = "{\"KriterId\":2,\"PozisyonId\":2,\"MinDeneyimYili\":0,\"MinEgitimSeviyesi\":3,\"GerekliDilSeviyesi\":1,\"GerekliProgramlamaDilleri\":\"JavaScript\"}" },
                new() { DecisionTreeId = dtId, TableId = kriterTable.Id, RowIndex = 3, RowDataJson = "{\"KriterId\":3,\"PozisyonId\":3,\"MinDeneyimYili\":4,\"MinEgitimSeviyesi\":3,\"GerekliDilSeviyesi\":2,\"GerekliProgramlamaDilleri\":\"Python,Bash\"}" }
            };
            _db.DecisionTreeData.AddRange(kriterData);

            await _db.SaveChangesAsync();

            _logger.LogInformation("Job Application Evaluation data seeded successfully. DecisionTreeId: {DtId}", dtId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error seeding Job Application Evaluation data");
            throw;
        }
    }
}

