import { Routes } from '@angular/router';
import { DecisionTreeListComponent } from './pages/decision-tree-list/decision-tree-list.component';
import { TableManagementComponent } from './pages/table-management/table-management.component';
import { ColumnManagementComponent } from './pages/column-management/column-management.component';
import { DataEntryComponent } from './pages/data-entry/data-entry.component';

export const routes: Routes = [
  { path: '', redirectTo: '/decision-trees', pathMatch: 'full' },
  { path: 'decision-trees', component: DecisionTreeListComponent },
  { path: 'decision-trees/:id/tables', component: TableManagementComponent },
  { path: 'decision-trees/:id/tables/:tableId/columns', component: ColumnManagementComponent },
  { path: 'decision-trees/:id/data', component: DataEntryComponent },
  { path: 'decision-trees/:id/data/tables/:tableId', component: DataEntryComponent },
];
