-- ========================================================
-- İŞ BAŞVURUSU DEĞERLENDİRME SİSTEMİ - SEED DATA SCRIPT
-- Decision Tree: JOB_APPLICATION_EVAL
-- ========================================================

-- Not: Bu script MySQL'de çalıştırılmalıdır.

-- ========================================================
-- BÖLÜM 1: DECISION TREE OLUŞTURMA
-- ========================================================

INSERT INTO decision_tree (code, name, status_code, schema_version, created_at_utc, updated_at_utc)
VALUES ('JOB_APPLICATION_EVAL', 'İş Başvurusu Değerlendirme Sistemi', 1, 1, NOW(), NULL)
ON DUPLICATE KEY UPDATE name = VALUES(name);

SET @decisionTreeId = (SELECT id FROM decision_tree WHERE code = 'JOB_APPLICATION_EVAL');

-- ========================================================
-- BÖLÜM 2: INPUT TABLOLARINI OLUŞTURMA
-- ========================================================

-- Tablo 1: BasvuruBilgileri
INSERT INTO decision_tree_table (decision_tree_id, table_name, direction, status_code, created_at_utc, updated_at_utc)
VALUES (@decisionTreeId, 'BasvuruBilgileri', 1, 1, NOW(), NULL);

SET @basvuruTableId = (SELECT id FROM decision_tree_table WHERE decision_tree_id = @decisionTreeId AND table_name = 'BasvuruBilgileri');

INSERT INTO decision_tree_column (table_id, column_name, excel_header_name, description, data_type, is_required, status_code, order_index, format, max_length, precision, scale, valid_from, valid_to)
VALUES
(@basvuruTableId, 'AdayId', 'ADAY_ID', 'Aday kimlik numarası', 2, 1, 1, 0, NULL, NULL, NULL, NULL, NULL, NULL),
(@basvuruTableId, 'AdayAdi', 'ADAY_ADI', 'Adayın adı', 1, 1, 1, 1, NULL, 100, NULL, NULL, NULL, NULL),
(@basvuruTableId, 'AdaySoyadi', 'ADAY_SOYADI', 'Adayın soyadı', 1, 1, 1, 2, NULL, 100, NULL, NULL, NULL, NULL),
(@basvuruTableId, 'Email', 'EMAIL', 'E-posta adresi', 1, 1, 1, 3, NULL, 200, NULL, NULL, NULL, NULL),
(@basvuruTableId, 'Telefon', 'TELEFON', 'Telefon numarası', 1, 0, 1, 4, NULL, 20, NULL, NULL, NULL, NULL),
(@basvuruTableId, 'DeneyimYili', 'DENEYIM_YILI', 'İş deneyimi (yıl)', 2, 1, 1, 5, NULL, NULL, NULL, NULL, NULL, NULL),
(@basvuruTableId, 'EgitimSeviyesi', 'EGITIM_SEVIYESI', 'Eğitim (1=Lise,2=ÖnLisans,3=Lisans,4=YüksekLisans,5=Doktora)', 2, 1, 1, 6, NULL, NULL, NULL, NULL, NULL, NULL),
(@basvuruTableId, 'ProgramlamaDilleri', 'PROGRAMLAMA_DILLERI', 'Programlama dilleri', 1, 0, 1, 7, NULL, 500, NULL, NULL, NULL, NULL),
(@basvuruTableId, 'YabancıDilSeviyesi', 'YABANCI_DIL_SEVIYESI', 'İngilizce (1=Başlangıç,2=Orta,3=İleri)', 2, 0, 1, 8, NULL, NULL, NULL, NULL, NULL, NULL),
(@basvuruTableId, 'BasvuruTarihi', 'BASVURU_TARIHI', 'Başvuru tarihi', 4, 1, 1, 9, 'yyyy-MM-dd', NULL, NULL, NULL, NULL, NULL);

-- Tablo 2: PozisyonBilgileri
INSERT INTO decision_tree_table (decision_tree_id, table_name, direction, status_code, created_at_utc, updated_at_utc)
VALUES (@decisionTreeId, 'PozisyonBilgileri', 1, 1, NOW(), NULL);

SET @pozisyonTableId = (SELECT id FROM decision_tree_table WHERE decision_tree_id = @decisionTreeId AND table_name = 'PozisyonBilgileri');

INSERT INTO decision_tree_column (table_id, column_name, excel_header_name, description, data_type, is_required, status_code, order_index, format, max_length, precision, scale, valid_from, valid_to)
VALUES
(@pozisyonTableId, 'PozisyonId', 'POZISYON_ID', 'Pozisyon kimlik', 2, 1, 1, 0, NULL, NULL, NULL, NULL, NULL, NULL),
(@pozisyonTableId, 'PozisyonAdi', 'POZISYON_ADI', 'Pozisyon adı', 1, 1, 1, 1, NULL, 100, NULL, NULL, NULL, NULL),
(@pozisyonTableId, 'Departman', 'DEPARTMAN', 'Departman', 1, 1, 1, 2, NULL, 100, NULL, NULL, NULL, NULL),
(@pozisyonTableId, 'Lokasyon', 'LOKASYON', 'İş lokasyonu', 1, 1, 1, 3, NULL, 100, NULL, NULL, NULL, NULL),
(@pozisyonTableId, 'PozisyonSeviyesi', 'POZISYON_SEVIYESI', 'Seviye (1=Junior,2=Mid,3=Senior)', 2, 1, 1, 4, NULL, NULL, NULL, NULL, NULL, NULL),
(@pozisyonTableId, 'MaasAraligiMin', 'MAAS_ARALIGI_MIN', 'Min maaş (TL)', 3, 1, 1, 5, NULL, NULL, 10, 2, NULL, NULL),
(@pozisyonTableId, 'MaasAraligiMax', 'MAAS_ARALIGI_MAX', 'Max maaş (TL)', 3, 1, 1, 6, NULL, NULL, 10, 2, NULL, NULL),
(@pozisyonTableId, 'GerekliYetenekler', 'GEREKLI_YETENEKLER', 'Gerekli yetenekler', 1, 0, 1, 7, NULL, 1000, NULL, NULL, NULL, NULL);

-- Tablo 3: PozisyonKriterleri
INSERT INTO decision_tree_table (decision_tree_id, table_name, direction, status_code, created_at_utc, updated_at_utc)
VALUES (@decisionTreeId, 'PozisyonKriterleri', 1, 1, NOW(), NULL);

SET @kriterTableId = (SELECT id FROM decision_tree_table WHERE decision_tree_id = @decisionTreeId AND table_name = 'PozisyonKriterleri');

INSERT INTO decision_tree_column (table_id, column_name, excel_header_name, description, data_type, is_required, status_code, order_index, format, max_length, precision, scale, valid_from, valid_to)
VALUES
(@kriterTableId, 'KriterId', 'KRITER_ID', 'Kriter kimlik', 2, 1, 1, 0, NULL, NULL, NULL, NULL, NULL, NULL),
(@kriterTableId, 'PozisyonId', 'POZISYON_ID', 'Pozisyon kimlik', 2, 1, 1, 1, NULL, NULL, NULL, NULL, NULL, NULL),
(@kriterTableId, 'MinDeneyimYili', 'MIN_DENEYIM_YILI', 'Min deneyim yılı', 2, 1, 1, 2, NULL, NULL, NULL, NULL, NULL, NULL),
(@kriterTableId, 'MinEgitimSeviyesi', 'MIN_EGITIM_SEVIYESI', 'Min eğitim seviyesi', 2, 1, 1, 3, NULL, NULL, NULL, NULL, NULL, NULL),
(@kriterTableId, 'GerekliDilSeviyesi', 'GEREKLI_DIL_SEVIYESI', 'Min dil seviyesi', 2, 1, 1, 4, NULL, NULL, NULL, NULL, NULL, NULL),
(@kriterTableId, 'GerekliProgramlamaDilleri', 'GEREKLI_PROGRAMLAMA_DILLERI', 'Zorunlu diller', 1, 0, 1, 5, NULL, 500, NULL, NULL, NULL, NULL);

-- ========================================================
-- BÖLÜM 3: OUTPUT TABLOLARINI OLUŞTURMA
-- ========================================================

-- Tablo 4: DegerlendirmeSonucu
INSERT INTO decision_tree_table (decision_tree_id, table_name, direction, status_code, created_at_utc, updated_at_utc)
VALUES (@decisionTreeId, 'DegerlendirmeSonucu', 2, 1, NOW(), NULL);

SET @sonucTableId = (SELECT id FROM decision_tree_table WHERE decision_tree_id = @decisionTreeId AND table_name = 'DegerlendirmeSonucu');

INSERT INTO decision_tree_column (table_id, column_name, excel_header_name, description, data_type, is_required, status_code, order_index, format, max_length, precision, scale, valid_from, valid_to)
VALUES
(@sonucTableId, 'SonucId', 'SONUC_ID', 'Sonuç kimlik', 2, 1, 1, 0, NULL, NULL, NULL, NULL, NULL, NULL),
(@sonucTableId, 'AdayId', 'ADAY_ID', 'Aday kimlik', 2, 1, 1, 1, NULL, NULL, NULL, NULL, NULL, NULL),
(@sonucTableId, 'PozisyonId', 'POZISYON_ID', 'Pozisyon kimlik', 2, 1, 1, 2, NULL, NULL, NULL, NULL, NULL, NULL),
(@sonucTableId, 'DegerlendirmeKarari', 'DEGERLENDIRME_KARARI', '1=Uygun,2=Koşullu,3=Uygun Değil', 2, 1, 1, 3, NULL, NULL, NULL, NULL, NULL, NULL),
(@sonucTableId, 'Skor', 'SKOR', 'Puan (0-100)', 2, 0, 1, 4, NULL, NULL, NULL, NULL, NULL, NULL),
(@sonucTableId, 'DeneyimPuani', 'DENEYIM_PUANI', 'Deneyim puanı', 2, 0, 1, 5, NULL, NULL, NULL, NULL, NULL, NULL),
(@sonucTableId, 'EgitimPuani', 'EGITIM_PUANI', 'Eğitim puanı', 2, 0, 1, 6, NULL, NULL, NULL, NULL, NULL, NULL),
(@sonucTableId, 'DilPuani', 'DIL_PUANI', 'Dil puanı', 2, 0, 1, 7, NULL, NULL, NULL, NULL, NULL, NULL),
(@sonucTableId, 'DegerlendirmeNotu', 'DEGERLENDIRME_NOTU', 'Değerlendirme notu', 1, 0, 1, 8, NULL, 2000, NULL, NULL, NULL, NULL),
(@sonucTableId, 'DegerlendirmeTarihi', 'DEGERLENDIRME_TARIHI', 'Tarih', 4, 1, 1, 9, 'yyyy-MM-dd HH:mm:ss', NULL, NULL, NULL, NULL, NULL);

-- Tablo 5: AdayUyumluluk
INSERT INTO decision_tree_table (decision_tree_id, table_name, direction, status_code, created_at_utc, updated_at_utc)
VALUES (@decisionTreeId, 'AdayUyumluluk', 2, 1, NOW(), NULL);

SET @uyumlulukTableId = (SELECT id FROM decision_tree_table WHERE decision_tree_id = @decisionTreeId AND table_name = 'AdayUyumluluk');

INSERT INTO decision_tree_column (table_id, column_name, excel_header_name, description, data_type, is_required, status_code, order_index, format, max_length, precision, scale, valid_from, valid_to)
VALUES
(@uyumlulukTableId, 'UyumlulukId', 'UYUMLULUK_ID', 'Kimlik', 2, 1, 1, 0, NULL, NULL, NULL, NULL, NULL, NULL),
(@uyumlulukTableId, 'AdayId', 'ADAY_ID', 'Aday kimlik', 2, 1, 1, 1, NULL, NULL, NULL, NULL, NULL, NULL),
(@uyumlulukTableId, 'PozisyonId', 'POZISYON_ID', 'Pozisyon kimlik', 2, 1, 1, 2, NULL, NULL, NULL, NULL, NULL, NULL),
(@uyumlulukTableId, 'GenelUyumlulukYuzdesi', 'GENEL_UYUMLULUK_YUZDESI', 'Uyumluluk %', 2, 1, 1, 3, NULL, NULL, NULL, NULL, NULL, NULL),
(@uyumlulukTableId, 'TeknikUyumluluk', 'TEKNIK_UYUMLULUK', 'Teknik uyumluluk', 2, 0, 1, 4, NULL, NULL, NULL, NULL, NULL, NULL),
(@uyumlulukTableId, 'DeneyimUyumlulugu', 'DENEYIM_UYUMLULUGU', 'Deneyim uyumu', 2, 0, 1, 5, NULL, NULL, NULL, NULL, NULL, NULL),
(@uyumlulukTableId, 'EksikBeceriler', 'EKSIK_BECERILER', 'Eksik beceriler', 1, 0, 1, 6, NULL, 1000, NULL, NULL, NULL, NULL),
(@uyumlulukTableId, 'GüclüYönler', 'GUCLU_YONLER', 'Güçlü yönler', 1, 0, 1, 7, NULL, 1000, NULL, NULL, NULL, NULL),
(@uyumlulukTableId, 'ÖnerilenAksiyon', 'ONERILEN_AKSIYON', 'Önerilen aksiyon', 2, 1, 1, 8, NULL, NULL, NULL, NULL, NULL, NULL),
(@uyumlulukTableId, 'UyumlulukAciklamasi', 'UYUMLULUK_ACIKLAMASI', 'Açıklama', 1, 0, 1, 9, NULL, 2000, NULL, NULL, NULL, NULL);

-- ========================================================
-- BÖLÜM 4: ÖRNEK VERİLER
-- ========================================================

-- 4.1: BasvuruBilgileri (10 Aday)
INSERT INTO decision_tree_data (decision_tree_id, table_id, row_index, row_data_json, created_at_utc, updated_at_utc)
VALUES
(@decisionTreeId, @basvuruTableId, 1, '{"AdayId":1,"AdayAdi":"Mehmet","AdaySoyadi":"Yılmaz","Email":"mehmet@email.com","DeneyimYili":8,"EgitimSeviyesi":3,"ProgramlamaDilleri":"C#,Java,Python","YabancıDilSeviyesi":3,"BasvuruTarihi":"2024-01-15"}', NOW(), NULL),
(@decisionTreeId, @basvuruTableId, 2, '{"AdayId":2,"AdayAdi":"Ayşe","AdaySoyadi":"Demir","Email":"ayse@email.com","DeneyimYili":2,"EgitimSeviyesi":3,"ProgramlamaDilleri":"JavaScript,React","YabancıDilSeviyesi":2,"BasvuruTarihi":"2024-01-16"}', NOW(), NULL),
(@decisionTreeId, @basvuruTableId, 3, '{"AdayId":3,"AdayAdi":"Mustafa","AdaySoyadi":"Kara","Email":"mustafa@email.com","DeneyimYili":5,"EgitimSeviyesi":4,"ProgramlamaDilleri":"C#,ASP.NET,SQL","YabancıDilSeviyesi":3,"BasvuruTarihi":"2024-01-17"}', NOW(), NULL),
(@decisionTreeId, @basvuruTableId, 4, '{"AdayId":4,"AdayAdi":"Elif","AdaySoyadi":"Öztürk","Email":"elif@email.com","DeneyimYili":10,"EgitimSeviyesi":5,"ProgramlamaDilleri":"Java,Spring,Kubernetes","YabancıDilSeviyesi":3,"BasvuruTarihi":"2024-01-18"}', NOW(), NULL),
(@decisionTreeId, @basvuruTableId, 5, '{"AdayId":5,"AdayAdi":"Ali","AdaySoyadi":"Şahin","Email":"ali@email.com","DeneyimYili":1,"EgitimSeviyesi":3,"ProgramlamaDilleri":"Python,Django","YabancıDilSeviyesi":1,"BasvuruTarihi":"2024-01-19"}', NOW(), NULL),
(@decisionTreeId, @basvuruTableId, 6, '{"AdayId":6,"AdayAdi":"Zeynep","AdaySoyadi":"Arslan","Email":"zeynep@email.com","DeneyimYili":4,"EgitimSeviyesi":3,"ProgramlamaDilleri":"React,Node.js,TypeScript","YabancıDilSeviyesi":2,"BasvuruTarihi":"2024-01-20"}', NOW(), NULL),
(@decisionTreeId, @basvuruTableId, 7, '{"AdayId":7,"AdayAdi":"Emre","AdaySoyadi":"Yıldız","Email":"emre@email.com","DeneyimYili":7,"EgitimSeviyesi":4,"ProgramlamaDilleri":"Python,Bash,Go","YabancıDilSeviyesi":3,"BasvuruTarihi":"2024-01-21"}', NOW(), NULL),
(@decisionTreeId, @basvuruTableId, 8, '{"AdayId":8,"AdayAdi":"Sema","AdaySoyadi":"Koç","Email":"sema@email.com","DeneyimYili":0,"EgitimSeviyesi":3,"ProgramlamaDilleri":"Java,C++","YabancıDilSeviyesi":2,"BasvuruTarihi":"2024-01-22"}', NOW(), NULL),
(@decisionTreeId, @basvuruTableId, 9, '{"AdayId":9,"AdayAdi":"Hakan","AdaySoyadi":"Polat","Email":"hakan@email.com","DeneyimYili":6,"EgitimSeviyesi":3,"ProgramlamaDilleri":"C#,Azure,SQL Server","YabancıDilSeviyesi":2,"BasvuruTarihi":"2024-01-23"}', NOW(), NULL),
(@decisionTreeId, @basvuruTableId, 10, '{"AdayId":10,"AdayAdi":"Derya","AdaySoyadi":"Çelik","Email":"derya@email.com","DeneyimYili":9,"EgitimSeviyesi":4,"ProgramlamaDilleri":"Python,Java,Selenium","YabancıDilSeviyesi":3,"BasvuruTarihi":"2024-01-24"}', NOW(), NULL);

-- 4.2: PozisyonBilgileri
INSERT INTO decision_tree_data (decision_tree_id, table_id, row_index, row_data_json, created_at_utc, updated_at_utc)
VALUES
(@decisionTreeId, @pozisyonTableId, 1, '{"PozisyonId":1,"PozisyonAdi":"Senior Yazılım Geliştirici","Departman":"Yazılım","Lokasyon":"İstanbul","PozisyonSeviyesi":3,"MaasAraligiMin":45000,"MaasAraligiMax":75000,"GerekliYetenekler":"C#,ASP.NET Core,SQL"}', NOW(), NULL),
(@decisionTreeId, @pozisyonTableId, 2, '{"PozisyonId":2,"PozisyonAdi":"Junior Yazılım Geliştirici","Departman":"Yazılım","Lokasyon":"Ankara","PozisyonSeviyesi":1,"MaasAraligiMin":25000,"MaasAraligiMax":35000,"GerekliYetenekler":"JavaScript,HTML,CSS"}', NOW(), NULL),
(@decisionTreeId, @pozisyonTableId, 3, '{"PozisyonId":3,"PozisyonAdi":"DevOps Mühendisi","Departman":"Altyapı","Lokasyon":"İzmir","PozisyonSeviyesi":3,"MaasAraligiMin":50000,"MaasAraligiMax":80000,"GerekliYetenekler":"Docker,Kubernetes,Terraform"}', NOW(), NULL);

-- 4.3: PozisyonKriterleri
INSERT INTO decision_tree_data (decision_tree_id, table_id, row_index, row_data_json, created_at_utc, updated_at_utc)
VALUES
(@decisionTreeId, @kriterTableId, 1, '{"KriterId":1,"PozisyonId":1,"MinDeneyimYili":5,"MinEgitimSeviyesi":3,"GerekliDilSeviyesi":2,"GerekliProgramlamaDilleri":"C#,ASP.NET Core"}', NOW(), NULL),
(@decisionTreeId, @kriterTableId, 2, '{"KriterId":2,"PozisyonId":2,"MinDeneyimYili":0,"MinEgitimSeviyesi":3,"GerekliDilSeviyesi":1,"GerekliProgramlamaDilleri":"JavaScript"}', NOW(), NULL),
(@decisionTreeId, @kriterTableId, 3, '{"KriterId":3,"PozisyonId":3,"MinDeneyimYili":4,"MinEgitimSeviyesi":3,"GerekliDilSeviyesi":2,"GerekliProgramlamaDilleri":"Python,Bash"}', NOW(), NULL);

-- ========================================================
-- VERİLERİ DOĞRULA
-- ========================================================
SELECT 'Decision Tree' AS Tablo, COUNT(*) AS KayitSayisi FROM decision_tree WHERE code = 'JOB_APPLICATION_EVAL'
UNION ALL SELECT 'Tables', COUNT(*) FROM decision_tree_table WHERE decision_tree_id = @decisionTreeId
UNION ALL SELECT 'Columns', COUNT(*) FROM decision_tree_column WHERE table_id IN (@basvuruTableId, @pozisyonTableId, @kriterTableId, @sonucTableId, @uyumlulukTableId)
UNION ALL SELECT 'Data Rows', COUNT(*) FROM decision_tree_data WHERE decision_tree_id = @decisionTreeId;

