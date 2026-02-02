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
