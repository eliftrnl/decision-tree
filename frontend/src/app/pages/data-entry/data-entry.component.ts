import { Component, inject, signal, computed, OnInit } from '@angular/core';
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
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly dataEntryService = inject(DataEntryService);
  private readonly tableService = inject(TableService);
  private readonly columnService = inject(ColumnService);

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

    if (this.editingRow()) {
      // Update
      this.dataEntryService
        .updateRow(this.dtId(), this.tableId(), this.editingRow()!.id, { rowDataJson })
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
        .createRow(this.dtId(), this.tableId(), { rowDataJson })
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
    this.dataEntryService.generateJson(this.dtId()).subscribe({
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
    window.location.href = `http://localhost:5135/api/decision-trees/${this.dtId()}/data/export-excel`;
  }

  triggerImportExcel() {
    // File input'u açar
    const fileInput = document.querySelector('input[type="file"]') as HTMLInputElement;
    if (fileInput) {
      fileInput.click();
    }
  }

  onExcelFileSelected(event: Event) {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];

    if (!file) {
      return;
    }

    if (!file.name.endsWith('.xlsx')) {
      alert('Sadece Excel (.xlsx) dosyaları yüklenebilir');
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
        alert(
          `✅ Import başarılı!\n\n` +
          `Yüklenen satırlar: ${response.totalRowsImported || 0}\n` +
          `İşlenen satırlar: ${response.totalRowsProcessed || 0}\n` +
          `Hatalar: ${response.totalErrors || 0}`
        );

        // Verileri yenile
        this.loadDataRows();
        this.loading.set(false);

        // File input'u temizle
        input.value = '';
      },
      error: (err: any) => {
        const errorMessage = err.error?.message || 'Bilinmeyen bir hata oluştu';
        const errors = err.error?.errors || [];
        const warnings = err.error?.warnings || [];

        let fullMessage = `❌ Import hatası:\n\n${errorMessage}`;

        if (errors.length > 0) {
          fullMessage += `\n\nHatalar (ilk 5):\n`;
          errors.slice(0, 5).forEach((e: string) => {
            fullMessage += `• ${e}\n`;
          });
          if (errors.length > 5) {
            fullMessage += `• ... ve ${errors.length - 5} daha\n`;
          }
        }

        if (warnings.length > 0) {
          fullMessage += `\n\nUyarılar (ilk 5):\n`;
          warnings.slice(0, 5).forEach((w: string) => {
            fullMessage += `• ${w}\n`;
          });
          if (warnings.length > 5) {
            fullMessage += `• ... ve ${warnings.length - 5} daha\n`;
          }
        }

        alert(fullMessage);
        this.loading.set(false);
        input.value = '';
      }
    });
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
}
