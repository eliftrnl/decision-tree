import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { DecisionTreeService, DecisionTree, DecisionTreeFilter } from '../../services/decision-tree.service';

@Component({
  selector: 'app-decision-tree-list',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './decision-tree-list.component.html',
  styleUrls: ['./decision-tree-list.component.css']
})
export class DecisionTreeListComponent {
  private readonly service = inject(DecisionTreeService);

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
}
