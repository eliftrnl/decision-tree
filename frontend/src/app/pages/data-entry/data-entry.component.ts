import { Component, inject, signal, computed, OnInit, ViewChild, ElementRef, NgZone } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { DataEntryService, DataRow } from '../../services/data-entry.service';
import { TableService, DecisionTreeTable } from '../../services/table.service';
import { ColumnService, TableColumn } from '../../services/column.service';

@Component({
  selector: 'app-data-entry',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './data-entry.component.html',
  styleUrls: ['./data-entry.component.css']
})
export class DataEntryComponent implements OnInit {
  @ViewChild('excelImportInput') excelImportInput?: ElementRef<HTMLInputElement>;
  @ViewChild('csvImportInput') csvImportInput?: ElementRef<HTMLInputElement>;

  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly dataEntryService = inject(DataEntryService);
  private readonly tableService = inject(TableService);
  private readonly columnService = inject(ColumnService);
  private readonly ngZone = inject(NgZone);

  // Signals
  dtId = signal<number>(0);
  tableId = signal<number>(0);
  tables = signal<DecisionTreeTable[]>([]);
  columns = signal<TableColumn[]>([]);
  dataRows = signal<DataRow[]>([]);
  loading = signal(false);
  selectedTable = signal<DecisionTreeTable | null>(null);
  showAddModal = signal(false);
  showEditModal = signal(false);
  editingRow = signal<DataRow | null>(null);
  newRowData = signal<Record<string, any>>({});
  validationErrors = signal<string[]>([]);
  
  // Excel Import/Export Result Modal
  showImportResultModal = signal(false);
  importResultType = signal<'success' | 'error'>('success');
  importResultMessage = signal('');
  importResultErrors = signal<string[]>([]);
  importResultWarnings = signal<string[]>([]);
  importResultDetails = signal({ importedRowsCount: 0, tablesProcessed: 0 });

  // Conversion Modal
  showConversionResultModal = signal(false);
  conversionJsonResult = signal<string>('');
  conversionJsonPretty = signal<string>('');
  conversionSuccessMessage = signal<string>('');

  constructor() {
    this.route.params.subscribe(params => {
      this.dtId.set(Number(params['id']));
      if (params['tableId']) {
        this.tableId.set(Number(params['tableId']));
      }
    });
  }

  ngOnInit() {
    if (this.dtId() > 0) {
      this.loadTables();
      if (this.tableId() > 0) {
        this.loadColumns();
        this.loadDataRows();
      }
    }
  }

  loadTables() {
    this.loading.set(true);
    this.tableService.getTables(this.dtId()).subscribe({
      next: (tables: DecisionTreeTable[]) => {
        this.tables.set(tables);
        if (this.tableId() > 0) {
          this.selectedTable.set(tables.find((t: DecisionTreeTable) => t.id === this.tableId()) || null);
        }
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
      }
    });
  }

  selectTable(table: DecisionTreeTable) {
    this.tableId.set(table.id);
    this.selectedTable.set(table);
    this.loadColumns();
    this.loadDataRows();
  }

  loadColumns() {
    if (this.tableId() === 0) return;
    this.columnService.getColumns(this.dtId(), this.tableId()).subscribe({
      next: (cols) => {
        this.columns.set(cols.sort((a, b) => (a.orderIndex || 0) - (b.orderIndex || 0)));
      }
    });
  }

  loadDataRows() {
    if (this.tableId() === 0) return;
    this.loading.set(true);
    this.dataEntryService.getTableRows(this.dtId(), this.tableId()).subscribe({
      next: (rows) => {
        this.dataRows.set(rows);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
      }
    });
  }

  openAddModal() {
    // Eğer columns henüz yüklenmemişse, dropdown alanları gösteremeyiz
    if (this.tableId() === 0 || this.columns().length === 0) {
      this.validationErrors.set(['Lütfen önce bir tablo seçin ve alanlar yüklensin']);
      return;
    }
    this.newRowData.set({});
    this.validationErrors.set([]);
    this.showAddModal.set(true);
  }

  closeAddModal() {
    this.showAddModal.set(false);
    this.newRowData.set({});
  }

  openEditModal(row: DataRow) {
    this.editingRow.set(row);
    try {
      const data = JSON.parse(row.rowDataJson);
      this.newRowData.set(data);
    } catch {
      this.newRowData.set({});
    }
    this.validationErrors.set([]);
    this.showEditModal.set(true);
  }

  closeEditModal() {
    this.showEditModal.set(false);
    this.editingRow.set(null);
    this.newRowData.set({});
  }

  saveRow() {
    if (this.tableId() === 0) return;

    const rowDataJson = JSON.stringify(this.newRowData());
    const currentRowCount = this.dataRows().length;
    const rowIndex = currentRowCount + 1; // Auto increment

    if (this.editingRow()) {
      // Update
      this.dataEntryService
        .updateRow(this.dtId(), this.tableId(), this.editingRow()!.id, { rowDataJson, rowIndex })
        .subscribe({
          next: () => {
            this.closeEditModal();
            this.loadDataRows();
          },
          error: (err) => {
            this.validationErrors.set([err.error?.message || 'Hata oluştu']);
          }
        });
    } else {
      // Create
      this.dataEntryService
        .createRow(this.dtId(), this.tableId(), { rowDataJson, rowIndex })
        .subscribe({
          next: () => {
            this.closeAddModal();
            this.loadDataRows();
          },
          error: (err) => {
            this.validationErrors.set([err.error?.message || 'Hata oluştu']);
          }
        });
    }
  }

  deleteRow(row: DataRow) {
    if (confirm('Bu satırı silmek istediğinize emin misiniz?')) {
      this.dataEntryService.deleteRow(this.dtId(), this.tableId(), row.id).subscribe({
        next: () => {
          this.loadDataRows();
        },
        error: (err) => {
          alert(err.error?.message || 'Silme işlemi başarısız');
        }
      });
    }
  }

  exportJson() {
    this.dataEntryService.exportJsonInputsOnly(this.dtId()).subscribe({
      next: (json) => {
        const element = document.createElement('a');
        element.setAttribute('href', 'data:text/json;charset=utf-8,' + encodeURIComponent(JSON.stringify(json, null, 2)));
        element.setAttribute('download', `decision-tree-${this.dtId()}.json`);
        element.style.display = 'none';
        document.body.appendChild(element);
        element.click();
        document.body.removeChild(element);
      }
    });
  }

  exportExcel() {
    // GET /api/decision-trees/{dtId}/data/export-excel
    window.location.href = `http://localhost:5136/api/decision-trees/${this.dtId()}/data/export-excel`;
  }

  exportCsv() {
    // GET /api/decision-trees/{dtId}/data/export-csv
    window.location.href = `http://localhost:5136/api/decision-trees/${this.dtId()}/data/export-csv`;
  }

  triggerImportCsv() {
    // Create input for file selection
    const input = document.createElement('input');
    input.type = 'file';
    input.accept = 'text/csv,.csv';
    input.style.display = 'none';
    
    // Handle file selection - wrap in NgZone to trigger change detection
    input.addEventListener('change', (event: Event) => {
      this.ngZone.run(() => {
        this.onCsvFileSelected(event);
      });
      
      // Remove input after processing
      setTimeout(() => {
        if (document.body.contains(input)) {
          document.body.removeChild(input);
        }
      }, 0);
    }, { once: true });
    
    // Add to DOM and trigger file picker
    document.body.appendChild(input);
    input.click();
  }

  onCsvFileSelected(event: Event) {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];

    if (!file) {
      return;
    }

    if (!file.name.endsWith('.csv')) {
      this.importResultType.set('error');
      this.importResultMessage.set('Sadece CSV (.csv) dosyaları yüklenebilir');
      this.importResultErrors.set([`Yüklenen dosya: ${file.name}`]);
      this.importResultWarnings.set([]);
      this.showImportResultModal.set(true);
      if (this.csvImportInput?.nativeElement) {
        this.csvImportInput.nativeElement.value = '';
      }
      return;
    }

    // Onay iste (veri değişir mi?)
    const confirmReplace = confirm(
      'Mevcut verileri yeni CSV dosyasının verileriyle değiştirilsin mi?\n\n' +
      'EVET - Eski veriler silinip yeni veriler yüklenir\n' +
      'HAYIR - Yeni veriler mevcut verilere eklenir'
    );

    this.loading.set(true);

    this.dataEntryService.importCsv(this.dtId(), file, confirmReplace).subscribe({
      next: (response: any) => {
        this.importResultType.set('success');
        this.importResultMessage.set(
          confirmReplace 
            ? '✅ CSV başarıyla içe aktarıldı (Veriler değiştirildi)' 
            : '✅ CSV başarıyla içe aktarıldı (Veriler birleştirildi)'
        );
        this.importResultErrors.set(response.errors || []);
        this.importResultWarnings.set(response.warnings || []);
        this.importResultDetails.set({
          importedRowsCount: response.importedRowsCount || 0,
          tablesProcessed: response.tablesProcessed || 0
        });
        this.showImportResultModal.set(true);

        // Seçili table'ı yeniden seç ve verileri taze yükle
        if (this.selectedTable()) {
          setTimeout(() => {
            this.selectTable(this.selectedTable()!);
          }, 500);
        }

        this.loading.set(false);

        // File input'u temizle
        if (this.csvImportInput?.nativeElement) {
          this.csvImportInput.nativeElement.value = '';
        }
      },
      error: (err: any) => {
        const errorMessage = err.error?.message || 'Bilinmeyen bir hata oluştu';
        const errorCode = err.error?.code || 'UNKNOWN_ERROR';
        const errors = err.error?.errors || [];
        const warnings = err.error?.warnings || [];
        const details = err.error?.details || null;
        const uploadedFileName = err.error?.uploadedFileName || null;

        this.importResultType.set('error');
        this.importResultMessage.set(`${errorCode}: ${errorMessage}`);
        
        let allErrors = [...errors];
        if (uploadedFileName) {
          allErrors.unshift(`Dosya: ${uploadedFileName}`);
        }
        if (details) {
          allErrors.unshift(`Detaylar: ${details}`);
        }
        
        this.importResultErrors.set(allErrors);
        this.importResultWarnings.set(warnings);
        this.importResultDetails.set({ importedRowsCount: 0, tablesProcessed: 0 });
        this.showImportResultModal.set(true);

        console.error('CSV Import Error:', err);
        this.loading.set(false);
        if (this.csvImportInput?.nativeElement) {
          this.csvImportInput.nativeElement.value = '';
        }
      }
    });
  }

  triggerImportExcel() {
    // Create input for file selection
    const input = document.createElement('input');
    input.type = 'file';
    input.accept = '.xlsx,.xlsm,.xls,application/vnd.openxmlformats-officedocument.spreadsheetml.sheet,application/vnd.ms-excel.sheet.macroEnabled.12,application/vnd.ms-excel';
    input.style.display = 'none';
    
    // Handle file selection - wrap in NgZone to trigger change detection
    input.addEventListener('change', (event: Event) => {
      this.ngZone.run(() => {
        this.onExcelFileSelected(event);
      });
      
      // Remove input after processing
      setTimeout(() => {
        if (document.body.contains(input)) {
          document.body.removeChild(input);
        }
      }, 0);
    }, { once: true });
    
    // Add to DOM and trigger file picker
    document.body.appendChild(input);
    input.click();
  }

  onExcelFileSelected(event: Event) {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];

    if (!file) {
      return;
    }

    const lowerFileName = file.name.toLowerCase();
    if (lowerFileName.endsWith('.numbers')) {
      this.importResultType.set('error');
      this.importResultMessage.set('Numbers dosyası doğrudan içe aktarılamaz');
      this.importResultErrors.set([
        `Seçilen dosya: ${file.name}`,
        'Numbers uygulamasında File > Export To > Excel... adımıyla dışa aktarın.',
        'Oluşan .xlsx dosyasını seçip tekrar içe aktarın.'
      ]);
      this.importResultWarnings.set([]);
      this.showImportResultModal.set(true);
      return;
    }

    const isKnownExcelExtension =
      lowerFileName.endsWith('.xlsx') ||
      lowerFileName.endsWith('.xlsm') ||
      lowerFileName.endsWith('.xls');

    if (!isKnownExcelExtension) {
      this.importResultType.set('error');
      this.importResultMessage.set('Seçilen dosya geçerli bir Excel dosyası değil');
      this.importResultErrors.set([`Yüklenen dosya: ${file.name}`]);
      this.importResultWarnings.set([]);
      this.showImportResultModal.set(true);
      if (this.excelImportInput?.nativeElement) {
        this.excelImportInput.nativeElement.value = '';
      }
      return;
    }

    // Backend import endpoint currently supports only .xlsx
    if (!lowerFileName.endsWith('.xlsx')) {
      this.importResultType.set('error');
      this.importResultMessage.set('Bu import akışı sadece .xlsx dosyalarını kabul ediyor');
      this.importResultErrors.set([
        `Seçilen dosya: ${file.name}`,
        'Excel dosyasını "Excel Workbook (*.xlsx)" formatında kaydedip tekrar deneyin.'
      ]);
      this.importResultWarnings.set([]);
      this.showImportResultModal.set(true);
      if (this.excelImportInput?.nativeElement) {
        this.excelImportInput.nativeElement.value = '';
      }
      return;
    }

    // Excel açıkken oluşan geçici kilit dosyaları (~$...) gerçek workbook değildir
    if (file.name.startsWith('~$')) {
      this.importResultType.set('error');
      this.importResultMessage.set('Geçici Excel kilit dosyası seçildi');
      this.importResultErrors.set([
        `Seçilen dosya: ${file.name}`,
        'Orijinal .xlsx dosyasını seçin (adı "~$" ile başlamamalı).'
      ]);
      this.importResultWarnings.set([]);
      this.showImportResultModal.set(true);
      return;
    }

    // Onay iste (veri değişir mi?)
    const confirmReplace = confirm(
      'Mevcut verileri yeni Excel dosyasının verileriyle değiştirilsin mi?\n\n' +
      'EVET - Eski veriler silinip yeni veriler yüklenir\n' +
      'HAYIR - Yeni veriler mevcut verilere eklenir'
    );

    this.loading.set(true);

    this.dataEntryService.importExcel(this.dtId(), file, confirmReplace).subscribe({
      next: (response: any) => {
        this.importResultType.set('success');
        this.importResultMessage.set(
          confirmReplace 
            ? '✅ Excel başarıyla içe aktarıldı (Veriler değiştirildi)' 
            : '✅ Excel başarıyla içe aktarıldı (Veriler birleştirildi)'
        );
        this.importResultErrors.set(response.errors || []);
        this.importResultWarnings.set(response.warnings || []);
        this.importResultDetails.set({
          importedRowsCount: response.importedRowsCount || 0,
          tablesProcessed: response.tablesProcessed || 0
        });
        this.showImportResultModal.set(true);

        // Seçili table'ı yeniden seç ve verileri taze yükle
        if (this.selectedTable()) {
          setTimeout(() => {
            this.selectTable(this.selectedTable()!);
          }, 500);
        }

        this.loading.set(false);

        // File input'u temizle
        if (this.excelImportInput?.nativeElement) {
          this.excelImportInput.nativeElement.value = '';
        }
      },
      error: (err: any) => {
        const errorMessage = err.error?.message || 'Bilinmeyen bir hata oluştu';
        const errorCode = err.error?.code || 'UNKNOWN_ERROR';
        const errors = err.error?.errors || [];
        const warnings = err.error?.warnings || [];
        const details = err.error?.details || null;
        const uploadedFileName = err.error?.uploadedFileName || null;

        this.importResultType.set('error');
        this.importResultMessage.set(`${errorCode}: ${errorMessage}`);
        
        let allErrors = [...errors];
        if (uploadedFileName) {
          allErrors.unshift(`Dosya: ${uploadedFileName}`);
        }
        if (details) {
          allErrors.unshift(`Detaylar: ${details}`);
        }
        
        this.importResultErrors.set(allErrors);
        this.importResultWarnings.set(warnings);
        this.importResultDetails.set({ importedRowsCount: 0, tablesProcessed: 0 });
        this.showImportResultModal.set(true);

        console.error('Excel Import Error:', err);
        this.loading.set(false);
        if (this.excelImportInput?.nativeElement) {
          this.excelImportInput.nativeElement.value = '';
        }
      }
    });
  }

  closeImportResultModal() {
    this.showImportResultModal.set(false);
  }

  goBack() {
    this.router.navigate(['/decision-trees', this.dtId(), 'tables']);
  }

  getColumnValue(row: DataRow, columnName: string): any {
    try {
      const data = JSON.parse(row.rowDataJson);
      return data[columnName] || '-';
    } catch {
      return '-';
    }
  }

  updateFieldValue(fieldName: string, value: any) {
    const current = this.newRowData();
    this.newRowData.set({ ...current, [fieldName]: value });
  }

  // Excel to JSON Conversion
  triggerExcelToJsonConversion() {
    const input = document.createElement('input');
    input.type = 'file';
    input.accept = '.xlsx';
    input.onchange = (event: any) => {
      const file = event.target.files?.[0];
      if (file) {
        this.convertExcelToJson(file);
      }
    };
    input.click();
  }

  convertExcelToJson(file: File) {
    this.loading.set(true);
    this.dataEntryService.convertExcelToJson(this.dtId(), file).subscribe({
      next: (response) => {
        if (response.success) {
          const jsonObj = JSON.parse(response.json);
          const prettyJson = JSON.stringify(jsonObj, null, 2);
          
          this.conversionJsonResult.set(response.json);
          this.conversionJsonPretty.set(prettyJson);
          this.conversionSuccessMessage.set('✅ Excel başarıyla JSON\'a dönüştürüldü!');
          this.showConversionResultModal.set(true);
        } else {
          this.importResultType.set('error');
          this.importResultMessage.set('Dönüştürme başarısız oldu');
          const errors = response.errors || response.message || ['Bilinmeyen hata'];
          this.importResultErrors.set(Array.isArray(errors) ? errors : [errors]);
          this.showImportResultModal.set(true);
        }
        this.loading.set(false);
      },
      error: (err) => {
        this.loading.set(false);
        this.importResultType.set('error');
        this.importResultMessage.set('Dönüştürme işlemi başarısız');
        
        // Parse error response
        let errorMsg = 'Bilinmeyen hata';
        if (err.error) {
          if (err.error.message) {
            errorMsg = err.error.message;
          } else if (err.error.errors && Array.isArray(err.error.errors)) {
            errorMsg = err.error.errors.join('; ');
          }
        } else if (err.message) {
          errorMsg = err.message;
        }
        
        this.importResultErrors.set([errorMsg]);
        this.showImportResultModal.set(true);
      }
    });
  }

  // JSON to Excel Conversion (from modal JSON)
  convertJsonStringToExcel() {
    if (!this.conversionJsonResult()) {
      alert('JSON içeriği boş. Lütfen önce dönüştürün.');
      return;
    }

    this.loading.set(true);
    this.dataEntryService.convertJsonStringToExcel(this.dtId(), this.conversionJsonResult()).subscribe({
      next: (blob) => {
        const url = window.URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href = url;
        link.download = `DecisionTree_${this.dtId()}_${new Date().getTime()}.xlsx`;
        link.click();
        window.URL.revokeObjectURL(url);
        this.loading.set(false);
        this.closeConversionResultModal();
        
        // Success notification
        this.importResultType.set('success');
        this.importResultMessage.set('JSON Excel\'e başarıyla dönüştürüldü ve indirildi!');
        this.showImportResultModal.set(true);
      },
      error: (err) => {
        this.loading.set(false);
        this.importResultType.set('error');
        this.importResultMessage.set('JSON Excel dönüştürme başarısız');
        this.importResultErrors.set([err.error?.message || 'Bilinmeyen hata']);
        this.showImportResultModal.set(true);
      }
    });
  }

  // JSON file to Excel Conversion
  triggerJsonToExcelConversion() {
    const input = document.createElement('input');
    input.type = 'file';
    input.accept = 'application/json,.json';
    input.style.display = 'none';

    input.addEventListener('change', (event: Event) => {
      this.ngZone.run(() => {
        this.onJsonFileSelected(event);
      });

      setTimeout(() => {
        if (document.body.contains(input)) {
          document.body.removeChild(input);
        }
      }, 0);
    }, { once: true });

    document.body.appendChild(input);
    input.click();
  }

  onJsonFileSelected(event: Event) {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];

    if (!file) {
      return;
    }

    if (!file.name.toLowerCase().endsWith('.json')) {
      this.importResultType.set('error');
      this.importResultMessage.set('Sadece JSON (.json) dosyaları yüklenebilir');
      this.importResultErrors.set([`Yüklenen dosya: ${file.name}`]);
      this.importResultWarnings.set([]);
      this.showImportResultModal.set(true);
      return;
    }

    this.loading.set(true);
    file.text()
      .then((jsonText) => {
        // Quick validation before API call
        JSON.parse(jsonText);
        this.dataEntryService.convertJsonStringToExcel(this.dtId(), jsonText).subscribe({
          next: (blob) => {
            const url = window.URL.createObjectURL(blob);
            const link = document.createElement('a');
            link.href = url;
            link.download = `DecisionTree_${this.dtId()}_${new Date().getTime()}.xlsx`;
            link.click();
            window.URL.revokeObjectURL(url);

            this.loading.set(false);
            this.importResultType.set('success');
            this.importResultMessage.set('JSON dosyası Excel\'e başarıyla dönüştürüldü ve indirildi!');
            this.importResultErrors.set([]);
            this.importResultWarnings.set([]);
            this.showImportResultModal.set(true);
          },
          error: (err) => {
            this.loading.set(false);
            this.importResultType.set('error');
            this.importResultMessage.set('JSON Excel dönüştürme başarısız');
            this.importResultErrors.set([err.error?.message || 'Bilinmeyen hata']);
            this.importResultWarnings.set([]);
            this.showImportResultModal.set(true);
          }
        });
      })
      .catch(() => {
        this.loading.set(false);
        this.importResultType.set('error');
        this.importResultMessage.set('JSON dosyası geçersiz');
        this.importResultErrors.set(['Dosya geçerli bir JSON formatında değil']);
        this.importResultWarnings.set([]);
        this.showImportResultModal.set(true);
      });
  }

  // JSON to Excel Conversion
  convertJsonToExcel() {
    this.loading.set(true);
    this.dataEntryService.convertJsonToExcel(this.dtId()).subscribe({
      next: (blob) => {
        const url = window.URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href = url;
        link.download = `DecisionTree_${this.dtId()}_${new Date().getTime()}.xlsx`;
        link.click();
        window.URL.revokeObjectURL(url);
        this.loading.set(false);
        
        // Success notification
        this.importResultType.set('success');
        this.importResultMessage.set('Veriler Excel\'e başarıyla dönüştürüldü ve indirildi!');
        this.showImportResultModal.set(true);
      },
      error: (err) => {
        this.loading.set(false);
        this.importResultType.set('error');
        this.importResultMessage.set('Excel dönüştürme başarısız');
        this.importResultErrors.set([err.error?.message || 'Bilinmeyen hata']);
        this.showImportResultModal.set(true);
      }
    });
  }

  closeConversionResultModal() {
    this.showConversionResultModal.set(false);
  }

  copyJsonToClipboard() {
    const json = this.conversionJsonResult();
    navigator.clipboard.writeText(json).then(() => {
      alert('✅ JSON kopyalandı!');
    });
  }
}
