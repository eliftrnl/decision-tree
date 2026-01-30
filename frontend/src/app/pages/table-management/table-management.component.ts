import { Component, inject, signal, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { TableService, DecisionTreeTable } from '../../services/table.service';

@Component({
  selector: 'app-table-management',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './table-management.component.html',
  styleUrls: ['./table-management.component.css']
})
export class TableManagementComponent implements OnInit {
  private readonly tableService = inject(TableService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  // Signals
  tables = signal<DecisionTreeTable[]>([]);
  loading = signal(false);
  error = signal<string | null>(null);
  showModal = signal(false);
  modalMode = signal<'create' | 'update'>('create');
  selectedTable = signal<DecisionTreeTable | null>(null);
  
  decisionTreeId!: number;
  decisionTreeCode: string = '';

  // Form model
  form: Partial<DecisionTreeTable> = {
    tableName: '',
    direction: 1, // 1=Input, 2=Output
    statusCode: 1
  };

  ngOnInit(): void {
    this.route.params.subscribe(params => {
      this.decisionTreeId = +params['id'];
      this.loadTables();
    });

    // Get decision tree code from query params if available
    this.route.queryParams.subscribe(params => {
      this.decisionTreeCode = params['code'] || `DT-${this.decisionTreeId}`;
    });
  }

  loadTables(): void {
    this.loading.set(true);
    this.error.set(null);

    this.tableService.getTables(this.decisionTreeId).subscribe({
      next: (data: DecisionTreeTable[]) => {
        this.tables.set(data);
        this.loading.set(false);
      },
      error: (err: any) => {
        this.error.set('Tablolar yüklenirken bir hata oluştu.');
        console.error(err);
        this.loading.set(false);
      }
    });
  }

  getInputTables(): DecisionTreeTable[] {
    return this.tables().filter(t => t.direction === 1);
  }

  getOutputTables(): DecisionTreeTable[] {
    return this.tables().filter(t => t.direction === 2);
  }

  openCreateModal(): void {
    this.modalMode.set('create');
    this.form = {
      tableName: '',
      direction: 1,
      statusCode: 1
    };
    this.showModal.set(true);
  }

  openUpdateModal(table: DecisionTreeTable): void {
    this.modalMode.set('update');
    this.selectedTable.set(table);
    this.form = {
      tableName: table.tableName,
      direction: table.direction,
      statusCode: table.statusCode
    };
    this.showModal.set(true);
  }

  closeModal(): void {
    this.showModal.set(false);
    this.selectedTable.set(null);
  }

  saveTable(): void {
    if (this.modalMode() === 'create') {
      this.createTable();
    } else {
      this.updateTable();
    }
  }

  private createTable(): void {
    this.loading.set(true);
    this.tableService.createTable(this.decisionTreeId, this.form as any).subscribe({
      next: () => {
        this.closeModal();
        this.loadTables();
        this.loading.set(false);
      },
      error: (err: any) => {
        this.error.set('Tablo eklenirken bir hata oluştu.');
        console.error(err);
        this.loading.set(false);
      }
    });
  }

  private updateTable(): void {
    const table = this.selectedTable();
    if (!table) return;

    this.loading.set(true);
    this.tableService.updateTable(this.decisionTreeId, table.id, this.form).subscribe({
      next: () => {
        this.closeModal();
        this.loadTables();
        this.loading.set(false);
      },
      error: (err: any) => {
        this.error.set('Tablo güncellenirken bir hata oluştu.');
        console.error(err);
        this.loading.set(false);
      }
    });
  }

  deleteTable(table: DecisionTreeTable): void {
    if (!confirm(`"${table.tableName}" tablosunu silmek istediğinizden emin misiniz?`)) {
      return;
    }

    this.loading.set(true);
    this.tableService.deleteTable(this.decisionTreeId, table.id).subscribe({
      next: () => {
        this.loadTables();
        this.loading.set(false);
      },
      error: (err: any) => {
        this.error.set('Tablo silinirken bir hata oluştu.');
        console.error(err);
        this.loading.set(false);
      }
    });
  }

  navigateToColumns(tableId: number, tableName: string): void {
    this.router.navigate(['/decision-trees', this.decisionTreeId, 'tables', tableId, 'columns'], {
      queryParams: { tableName }
    });
  }

  goBack(): void {
    this.router.navigate(['/']);
  }

  getStatusText(statusCode: number): string {
    return statusCode === 1 ? 'Aktif' : 'Pasif';
  }

  getStatusClass(statusCode: number): string {
    return statusCode === 1 ? 'status-active' : 'status-passive';
  }

  getDirectionText(direction: number): string {
    return direction === 1 ? 'Input' : 'Output';
  }

  getDirectionClass(direction: number): string {
    return direction === 1 ? 'direction-input' : 'direction-output';
  }
}
