import { Component, inject, signal, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { ColumnService, TableColumn } from '../../services/column.service';

@Component({
  selector: 'app-column-management',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './column-management.component.html',
  styleUrls: ['./column-management.component.css']
})
export class ColumnManagementComponent implements OnInit {
  private readonly columnService = inject(ColumnService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  // Signals
  columns = signal<TableColumn[]>([]);
  loading = signal(false);
  error = signal<string | null>(null);
  showModal = signal(false);
  modalMode = signal<'create' | 'update'>('create');
  selectedColumn = signal<TableColumn | null>(null);

  decisionTreeId!: number;
  tableId!: number;
  tableName: string = '';

  // Form model
  form: Partial<TableColumn> = {
    columnName: '',
    dataType: 1,
    isRequired: false,
    orderIndex: 0,
    format: '',
    maxLength: undefined
  };

  // Enum mapping
  dataTypeOptions = [
    { value: 1, label: 'String' },
    { value: 2, label: 'Int' },
    { value: 3, label: 'Decimal' },
    { value: 4, label: 'Date' },
    { value: 5, label: 'Boolean' }
  ];

  statusOptions = [
    { value: 1, label: 'Aktif' },
    { value: 2, label: 'Pasif' }
  ];

  ngOnInit(): void {
    this.route.params.subscribe(params => {
      this.decisionTreeId = +params['id'];
      this.tableId = +params['tableId'];
      this.loadColumns();
    });

    this.route.queryParams.subscribe(params => {
      this.tableName = params['tableName'] || `Tablo ${this.tableId}`;
    });
  }

  loadColumns(): void {
    this.loading.set(true);
    this.error.set(null);

    this.columnService.getColumns(this.decisionTreeId, this.tableId).subscribe({
      next: (data: TableColumn[]) => {
        this.columns.set(data);
        this.loading.set(false);
      },
      error: (err: any) => {
        this.error.set('Kolonlar yüklenirken bir hata oluştu.');
        console.error(err);
        this.loading.set(false);
      }
    });
  }

  openCreateModal(): void {
    this.modalMode.set('create');
    this.form = {
      columnName: '',
      dataType: 1,
      isRequired: false,
      orderIndex: this.columns().length,
      format: '',
      maxLength: undefined
    };
    this.showModal.set(true);
  }

  openUpdateModal(column: TableColumn): void {
    this.modalMode.set('update');
    this.selectedColumn.set(column);
    this.form = {
      columnName: column.columnName,
      excelHeaderName: column.excelHeaderName,
      description: column.description,
      dataType: column.dataType,
      isRequired: column.isRequired,
      statusCode: column.statusCode,
      orderIndex: column.orderIndex,
      format: column.format,
      maxLength: column.maxLength
    };
    this.showModal.set(true);
  }

  closeModal(): void {
    this.showModal.set(false);
    this.selectedColumn.set(null);
  }

  saveColumn(): void {
    if (!this.form.columnName || !this.form.columnName.trim()) {
      this.error.set('Kolon adı boş olamaz.');
      return;
    }

    const request = {
      tableId: this.tableId,
      columnName: this.form.columnName!,
      excelHeaderName: this.form.excelHeaderName || null,
      description: this.form.description || null,
      dataType: this.form.dataType!,
      isRequired: this.form.isRequired || false,
      statusCode: this.form.statusCode || 1,
      orderIndex: this.form.orderIndex || 0,
      format: this.form.format || null,
      maxLength: this.form.maxLength || null
    };

    if (this.modalMode() === 'create') {
      this.columnService.createColumn(this.decisionTreeId, this.tableId, request as any).subscribe({
        next: () => {
          this.loadColumns();
          this.closeModal();
          this.error.set(null);
        },
        error: (err: any) => {
          this.error.set(err.error?.message || 'Kolon eklenirken bir hata oluştu.');
          console.error(err);
        }
      });
    } else {
      const columnId = this.selectedColumn()?.id!;
      this.columnService.updateColumn(this.decisionTreeId, this.tableId, columnId, request as any).subscribe({
        next: () => {
          this.loadColumns();
          this.closeModal();
          this.error.set(null);
        },
        error: (err: any) => {
          this.error.set(err.error?.message || 'Kolon güncellenirken bir hata oluştu.');
          console.error(err);
        }
      });
    }
  }

  deleteColumn(column: TableColumn): void {
    if (!confirm(`Kolon "${column.columnName}" silinecektir. Emin misiniz?`)) {
      return;
    }

    this.columnService.deleteColumn(this.decisionTreeId, this.tableId, column.id).subscribe({
      next: () => {
        this.loadColumns();
        this.error.set(null);
      },
      error: (err: any) => {
        this.error.set(err.error?.message || 'Kolon silinirken bir hata oluştu.');
        console.error(err);
      }
    });
  }

  goBack(): void {
    this.router.navigate(['/decision-trees', this.decisionTreeId, 'tables']);
  }

  navigateToDataEntry(): void {
    this.router.navigate(['/decision-trees', this.decisionTreeId, 'data', 'tables', this.tableId]);
  }

  getDataTypeLabel(dataType: number | string): string {
    const typeMap: { [key: string]: string } = {
      '0': 'String',
      '1': 'String',
      '2': 'Int',
      '3': 'Decimal',
      '4': 'Date',
      '5': 'Boolean',
      'String': 'String',
      'Int': 'Int',
      'Decimal': 'Decimal',
      'Date': 'Date',
      'Boolean': 'Boolean'
    };
    
    const key = String(dataType);
    return typeMap[key] || 'Unknown';
  }

  getStatusLabel(statusCode: number | string): string {
    return statusCode === 1 || statusCode === 'Active' ? 'Aktif' : 'Pasif';
  }

  getStatusClass(statusCode: number | string): string {
    return statusCode === 1 || statusCode === 'Active' ? 'badge badge-success' : 'badge badge-warning';
  }

  getDataTypeText(dataType: number | string): string {
    const typeMap: { [key: string]: string } = {
      '0': 'String',
      '1': 'String',
      '2': 'Int',
      '3': 'Decimal',
      '4': 'Date',
      '5': 'Boolean'
    };
    return typeMap[String(dataType)] || 'Unknown';
  }
}
