import { Component, inject, signal, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { DecisionTreeService, DecisionTree, DecisionTreeFilter } from '../../services/decision-tree.service';

@Component({
  selector: 'app-decision-tree-list',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './decision-tree-list.component.html',
  styleUrls: ['./decision-tree-list.component.css'],
  styles: [`
    * { box-sizing: border-box; }
    
    .container {
      max-width: 1600px;
      margin: 0 auto;
      padding: 2rem;
      background: #f8f9fa;
      min-height: 100vh;
    }
    
    .header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-bottom: 2.5rem;
      gap: 2rem;
    }
    
    h1 {
      color: #1a202c;
      font-size: 2rem;
      margin: 0;
      font-weight: 600;
      letter-spacing: -0.5px;
    }
    
    .btn-success {
      background: #2563eb;
      color: white;
      padding: 0.625rem 1.5rem;
      border: none;
      border-radius: 6px;
      font-size: 0.875rem;
      font-weight: 600;
      cursor: pointer;
      transition: all 0.2s ease;
      display: inline-flex;
      align-items: center;
      gap: 0.5rem;
    }
    
    .btn-success:hover {
      background: #1d4ed8;
      box-shadow: 0 4px 12px rgba(37, 99, 235, 0.15);
    }
    
    .filter-section {
      background: white;
      border-radius: 8px;
      padding: 1.5rem;
      margin-bottom: 2rem;
      box-shadow: 0 1px 3px rgba(0, 0, 0, 0.08);
      border: 1px solid #e5e7eb;
    }
    
    h2 {
      color: #374151;
      font-size: 1.125rem;
      margin: 0 0 1.5rem 0;
      font-weight: 600;
    }
    
    .filter-grid {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(280px, 1fr));
      gap: 1.5rem;
      margin-bottom: 1.5rem;
    }
    
    .form-group {
      display: flex;
      flex-direction: column;
    }
    
    .form-group label {
      font-weight: 600;
      color: #374151;
      font-size: 0.875rem;
      margin-bottom: 0.5rem;
      text-transform: uppercase;
      letter-spacing: 0.3px;
    }
    
    .form-group input,
    .form-group select {
      width: 100%;
      padding: 0.625rem 0.875rem;
      border: 1px solid #d1d5db;
      border-radius: 6px;
      font-size: 0.875rem;
      color: #1f2937;
      background: white;
      transition: all 0.2s ease;
      font-weight: 500;
    }
    
    .form-group input:focus,
    .form-group select:focus {
      outline: none;
      border-color: #2563eb;
      box-shadow: 0 0 0 3px rgba(37, 99, 235, 0.08);
      background: white;
    }
    
    .form-group input::placeholder {
      color: #9ca3af;
    }
    
    .filter-actions {
      display: flex;
      gap: 0.75rem;
      flex-wrap: wrap;
    }
    
    .btn {
      padding: 0.625rem 1.25rem;
      border: none;
      border-radius: 6px;
      font-size: 0.875rem;
      cursor: pointer;
      font-weight: 600;
      transition: all 0.2s ease;
      display: inline-flex;
      align-items: center;
      justify-content: center;
      gap: 0.5rem;
      text-transform: uppercase;
      letter-spacing: 0.3px;
    }
    
    .btn:hover:not(:disabled) {
      transform: translateY(-1px);
    }
    
    .btn-primary {
      background: #2563eb;
      color: white;
    }
    
    .btn-primary:hover:not(:disabled) {
      background: #1d4ed8;
      box-shadow: 0 4px 12px rgba(37, 99, 235, 0.15);
    }
    
    .btn-secondary {
      background: #6b7280;
      color: white;
    }
    
    .btn-secondary:hover:not(:disabled) {
      background: #4b5563;
      box-shadow: 0 4px 12px rgba(107, 114, 128, 0.1);
    }
    
    .state-message {
      text-align: center;
      padding: 3rem 1.5rem;
      color: #6b7280;
      background: white;
      border-radius: 8px;
      box-shadow: 0 1px 3px rgba(0, 0, 0, 0.08);
      border: 1px solid #e5e7eb;
    }
    
    .spinner {
      border: 3px solid #e5e7eb;
      border-top-color: #2563eb;
      border-radius: 50%;
      width: 40px;
      height: 40px;
      animation: spin 0.8s linear infinite;
      margin: 0 auto 1rem;
    }
    
    @keyframes spin {
      0% { transform: rotate(0deg); }
      100% { transform: rotate(360deg); }
    }
    
    .results-section {
      background: white;
      border-radius: 8px;
      padding: 0;
      box-shadow: 0 1px 3px rgba(0, 0, 0, 0.08);
      border: 1px solid #e5e7eb;
      overflow: hidden;
    }
    
    .data-table {
      width: 100%;
      border-collapse: collapse;
      font-size: 0.875rem;
    }
    
    .data-table thead {
      background: #f3f4f6;
      color: #1f2937;
      text-transform: uppercase;
      letter-spacing: 0.3px;
      font-size: 0.75rem;
      font-weight: 700;
    }
    
    .data-table th {
      padding: 1rem;
      text-align: left;
      border-bottom: 2px solid #d1d5db;
    }
    
    .data-table tbody tr {
      border-bottom: 1px solid #e5e7eb;
      transition: background-color 0.2s ease;
    }
    
    .data-table tbody tr:hover {
      background-color: #f9fafb;
    }
    
    .data-table td {
      padding: 0.875rem 1rem;
      color: #4b5563;
    }
    
    .data-table td strong {
      color: #1f2937;
      font-weight: 600;
    }
    
    .status-active {
      display: inline-flex;
      align-items: center;
      gap: 0.375rem;
      background: #dcfce7;
      color: #15803d;
      padding: 0.375rem 0.75rem;
      border-radius: 4px;
      font-size: 0.75rem;
      font-weight: 600;
    }
    
    .status-passive {
      display: inline-flex;
      align-items: center;
      gap: 0.375rem;
      background: #fee2e2;
      color: #991b1b;
      padding: 0.375rem 0.75rem;
      border-radius: 4px;
      font-size: 0.75rem;
      font-weight: 600;
    }
    
    .action-buttons {
      display: flex;
      gap: 0.5rem;
      flex-wrap: wrap;
    }
    
    .btn-sm {
      padding: 0.375rem 0.75rem;
      font-size: 0.75rem;
      border-radius: 4px;
    }
    
    .btn-info {
      background: #0891b2;
      color: white;
    }
    
    .btn-info:hover {
      background: #0e7490;
      box-shadow: 0 4px 12px rgba(8, 145, 178, 0.15);
    }
    
    .btn-danger {
      background: #dc2626;
      color: white;
    }
    
    .btn-danger:hover {
      background: #b91c1c;
      box-shadow: 0 4px 12px rgba(220, 38, 38, 0.15);
    }
    
    .modal-overlay {
      position: fixed;
      top: 0;
      left: 0;
      right: 0;
      bottom: 0;
      background: rgba(0, 0, 0, 0.4);
      display: flex;
      align-items: center;
      justify-content: center;
      z-index: 1000;
      animation: fadeIn 0.2s ease;
    }
    
    @keyframes fadeIn {
      from { opacity: 0; }
      to { opacity: 1; }
    }
    
    .modal-content {
      background: white;
      border-radius: 8px;
      width: 90%;
      max-width: 500px;
      box-shadow: 0 10px 25px rgba(0, 0, 0, 0.15);
      animation: slideUp 0.2s ease;
    }
    
    @keyframes slideUp {
      from {
        opacity: 0;
        transform: translateY(10px);
      }
      to {
        opacity: 1;
        transform: translateY(0);
      }
    }
    
    .modal-header {
      padding: 1.5rem;
      border-bottom: 1px solid #e5e7eb;
      display: flex;
      justify-content: space-between;
      align-items: center;
    }
    
    .modal-header h2 {
      margin: 0;
      font-size: 1.25rem;
      color: #1a202c;
      font-weight: 600;
    }
    
    .close-btn {
      background: none;
      border: none;
      font-size: 1.5rem;
      color: #6b7280;
      cursor: pointer;
      line-height: 1;
      padding: 0;
      width: 1.75rem;
      height: 1.75rem;
      display: flex;
      align-items: center;
      justify-content: center;
      border-radius: 4px;
      transition: all 0.2s ease;
    }
    
    .close-btn:hover {
      background: #f3f4f6;
      color: #1f2937;
    }
    
    .modal-body {
      padding: 1.5rem;
    }
    
    .modal-body .form-group {
      margin-bottom: 1.25rem;
    }
    
    .modal-footer {
      padding: 1rem 1.5rem;
      border-top: 1px solid #e5e7eb;
      display: flex;
      justify-content: flex-end;
      gap: 0.75rem;
    }
  `]
})
export class DecisionTreeListComponent implements OnInit {
  private readonly service = inject(DecisionTreeService);
  private readonly router = inject(Router);

  // Signals
  trees = signal<DecisionTree[]>([]);
  loading = signal(false);
  error = signal<string | null>(null);
  hasSearched = signal(false);
  showModal = signal(false);
  modalMode = signal<'create' | 'update'>('create');
  selectedTree = signal<DecisionTree | null>(null);

  // Filter model
  filter: DecisionTreeFilter = {
    code: '',
    name: '',
    statusCode: undefined
  };

  // Form model
  form: Partial<DecisionTree> = {
    code: '',
    name: '',
    statusCode: 1
  };

  ngOnInit(): void {
    this.search();
  }

  search(): void {
    this.loading.set(true);
    this.error.set(null);
    this.hasSearched.set(true);

    this.service.search(this.filter).subscribe({
      next: (data) => {
        this.trees.set(data);
        this.loading.set(false);
      },
      error: (err) => {
        this.error.set('Veriler yüklenirken bir hata oluştu.');
        console.error(err);
        this.loading.set(false);
      }
    });
  }

  clearFilters(): void {
    this.filter = {
      code: '',
      name: '',
      statusCode: undefined
    };
    this.trees.set([]);
    this.hasSearched.set(false);
    this.error.set(null);
  }

  openCreateModal(): void {
    this.modalMode.set('create');
    this.form = {
      code: '',
      name: '',
      statusCode: 1
    };
    this.showModal.set(true);
  }

  openUpdateModal(tree: DecisionTree): void {
    this.modalMode.set('update');
    this.selectedTree.set(tree);
    this.form = {
      code: tree.code,
      name: tree.name,
      statusCode: tree.statusCode
    };
    this.showModal.set(true);
  }

  closeModal(): void {
    this.showModal.set(false);
    this.selectedTree.set(null);
  }

  saveTree(): void {
    if (this.modalMode() === 'create') {
      this.createTree();
    } else {
      this.updateTree();
    }
  }

  private createTree(): void {
    this.loading.set(true);
    this.service.create(this.form as Omit<DecisionTree, 'id' | 'updatedAtUtc'>).subscribe({
      next: () => {
        this.closeModal();
        this.search(); // Refresh list
        this.loading.set(false);
      },
      error: (err) => {
        this.error.set('Kayıt eklenirken bir hata oluştu.');
        console.error(err);
        this.loading.set(false);
      }
    });
  }

  private updateTree(): void {
    const tree = this.selectedTree();
    if (!tree) return;

    this.loading.set(true);
    this.service.update(tree.id, this.form).subscribe({
      next: () => {
        this.closeModal();
        this.search(); // Refresh list
        this.loading.set(false);
      },
      error: (err) => {
        this.error.set('Kayıt güncellenirken bir hata oluştu.');
        console.error(err);
        this.loading.set(false);
      }
    });
  }

  deleteTree(tree: DecisionTree, event: Event): void {
    event.stopPropagation();
    
    if (!confirm(`"${tree.name}" kaydını silmek istediğinizden emin misiniz?`)) {
      return;
    }

    this.loading.set(true);
    this.service.delete(tree.id).subscribe({
      next: () => {
        this.search(); // Refresh list
        this.loading.set(false);
      },
      error: (err) => {
        this.error.set('Kayıt silinirken bir hata oluştu.');
        console.error(err);
        this.loading.set(false);
      }
    });
  }

  getStatusText(statusCode: number): string {
    return statusCode === 1 ? 'Aktif' : 'Pasif';
  }

  getStatusClass(statusCode: number): string {
    return statusCode === 1 ? 'status-active' : 'status-passive';
  }

  selectTree(tree: DecisionTree): void {
    // TODO: Navigate to detail view or load tables/data
    console.log('Selected tree:', tree);
  }

  navigateToTables(treeId: number): void {
    const tree = this.trees().find(t => t.id === treeId);
    this.router.navigate(['/decision-trees', treeId, 'tables'], {
      queryParams: { code: tree?.code || `DT-${treeId}` }
    });
  }
}
