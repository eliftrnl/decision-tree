-- Insert sample data for Job Application Evaluation
-- This script assumes the schema is already created by migrations

USE decision_tree_db;

-- Insert into decision_tree_data for BasvuruBilgileri (Table ID: 1)
INSERT INTO decision_tree_data (TableId, RowIndex, RowDataJson, CreatedAtUtc, UpdatedAtUtc) VALUES
(1, 1, '{"AdayId":1,"AdayAdi":"Mehmet","AdaySoyadi":"Yilmaz","Email":"mehmet@email.com","Telefon":"0532111","DeneyimYili":8,"EgitimSeviyesi":3,"ProgramlamaDilleri":"C#,Java,Python","YabancıDilSeviyesi":3,"BasvuruTarihi":"2024-01-15"}', NOW(), NOW()),
(1, 2, '{"AdayId":2,"AdayAdi":"Ayşe","AdaySoyadi":"Demir","Email":"ayse@email.com","Telefon":"0532222","DeneyimYili":2,"EgitimSeviyesi":3,"ProgramlamaDilleri":"JavaScript,React","YabancıDilSeviyesi":2,"BasvuruTarihi":"2024-01-16"}', NOW(), NOW()),
(1, 3, '{"AdayId":3,"AdayAdi":"Mustafa","AdaySoyadi":"Kara","Email":"mustafa@email.com","Telefon":"0532333","DeneyimYili":5,"EgitimSeviyesi":4,"ProgramlamaDilleri":"C#,ASP.NET,SQL","YabancıDilSeviyesi":3,"BasvuruTarihi":"2024-01-17"}', NOW(), NOW()),
(1, 4, '{"AdayId":4,"AdayAdi":"Elif","AdaySoyadi":"Ozturk","Email":"elif@email.com","Telefon":"0532444","DeneyimYili":10,"EgitimSeviyesi":5,"ProgramlamaDilleri":"Java,Spring,Kubernetes","YabancıDilSeviyesi":3,"BasvuruTarihi":"2024-01-18"}', NOW(), NOW()),
(1, 5, '{"AdayId":5,"AdayAdi":"Ali","AdaySoyadi":"Sahin","Email":"ali@email.com","Telefon":"0532555","DeneyimYili":1,"EgitimSeviyesi":3,"ProgramlamaDilleri":"Python,Django","YabancıDilSeviyesi":1,"BasvuruTarihi":"2024-01-19"}', NOW(), NOW()),
(1, 6, '{"AdayId":6,"AdayAdi":"Zeynep","AdaySoyadi":"Arslan","Email":"zeynep@email.com","Telefon":"0532666","DeneyimYili":4,"EgitimSeviyesi":3,"ProgramlamaDilleri":"React,Node.js,TypeScript","YabancıDilSeviyesi":2,"BasvuruTarihi":"2024-01-20"}', NOW(), NOW()),
(1, 7, '{"AdayId":7,"AdayAdi":"Emre","AdaySoyadi":"Yildiz","Email":"emre@email.com","Telefon":"0532777","DeneyimYili":7,"EgitimSeviyesi":4,"ProgramlamaDilleri":"Python,Bash,Go","YabancıDilSeviyesi":3,"BasvuruTarihi":"2024-01-21"}', NOW(), NOW()),
(1, 8, '{"AdayId":8,"AdayAdi":"Sema","AdaySoyadi":"Koc","Email":"sema@email.com","Telefon":"0532888","DeneyimYili":0,"EgitimSeviyesi":3,"ProgramlamaDilleri":"Java,C++","YabancıDilSeviyesi":2,"BasvuruTarihi":"2024-01-22"}', NOW(), NOW()),
(1, 9, '{"AdayId":9,"AdayAdi":"Hakan","AdaySoyadi":"Polat","Email":"hakan@email.com","Telefon":"0532999","DeneyimYili":6,"EgitimSeviyesi":3,"ProgramlamaDilleri":"C#,Azure,SQL Server","YabancıDilSeviyesi":2,"BasvuruTarihi":"2024-01-23"}', NOW(), NOW()),
(1, 10, '{"AdayId":10,"AdayAdi":"Derya","AdaySoyadi":"Celik","Email":"derya@email.com","Telefon":"05321010","DeneyimYili":9,"EgitimSeviyesi":4,"ProgramlamaDilleri":"Python,Java,Selenium","YabancıDilSeviyesi":3,"BasvuruTarihi":"2024-01-24"}', NOW(), NOW());

-- Insert into decision_tree_data for PozisyonBilgileri (Table ID: 2)
INSERT INTO decision_tree_data (TableId, RowIndex, RowDataJson, CreatedAtUtc, UpdatedAtUtc) VALUES
(2, 1, '{"PozisyonId":1,"PozisyonAdi":"Senior Yazilim Gelistirici","Departman":"Yazilim","Lokasyon":"Istanbul","PozisyonSeviyesi":3,"MaasAraligiMin":45000,"MaasAraligiMax":75000,"GerekliYetenekler":"C#,ASP.NET Core,SQL"}', NOW(), NOW()),
(2, 2, '{"PozisyonId":2,"PozisyonAdi":"Junior Yazilim Gelistirici","Departman":"Yazilim","Lokasyon":"Ankara","PozisyonSeviyesi":1,"MaasAraligiMin":25000,"MaasAraligiMax":35000,"GerekliYetenekler":"JavaScript,HTML,CSS"}', NOW(), NOW()),
(2, 3, '{"PozisyonId":3,"PozisyonAdi":"DevOps Muhendisi","Departman":"Altyapi","Lokasyon":"Izmir","PozisyonSeviyesi":3,"MaasAraligiMin":50000,"MaasAraligiMax":80000,"GerekliYetenekler":"Docker,Kubernetes,Terraform"}', NOW(), NOW());

-- Insert into decision_tree_data for PozisyonKriterleri (Table ID: 3)
INSERT INTO decision_tree_data (TableId, RowIndex, RowDataJson, CreatedAtUtc, UpdatedAtUtc) VALUES
(3, 1, '{"KriterId":1,"PozisyonId":1,"MinDeneyimYili":5,"MinEgitimSeviyesi":3,"GerekliDilSeviyesi":2,"GerekliProgramlamaDilleri":"C#,ASP.NET Core"}', NOW(), NOW()),
(3, 2, '{"KriterId":2,"PozisyonId":2,"MinDeneyimYili":0,"MinEgitimSeviyesi":3,"GerekliDilSeviyesi":1,"GerekliProgramlamaDilleri":"JavaScript"}', NOW(), NOW()),
(3, 3, '{"KriterId":3,"PozisyonId":3,"MinDeneyimYili":4,"MinEgitimSeviyesi":3,"GerekliDilSeviyesi":2,"GerekliProgramlamaDilleri":"Python,Bash"}', NOW(), NOW());
