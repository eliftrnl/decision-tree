import { Routes } from '@angular/router';
import { DecisionTreeListComponent } from './pages/decision-tree-list/decision-tree-list.component';
import { TableManagementComponent } from './pages/table-management/table-management.component';

export const routes: Routes = [
  { path: '', redirectTo: '/decision-trees', pathMatch: 'full' },
  { path: 'decision-trees', component: DecisionTreeListComponent },
  { path: 'decision-trees/:id/tables', component: TableManagementComponent },
];
