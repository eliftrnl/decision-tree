import { Routes } from '@angular/router';
import { DecisionTreeListComponent } from './pages/decision-tree-list/decision-tree-list.component';

export const routes: Routes = [
  { path: '', redirectTo: '/decision-trees', pathMatch: 'full' },
  { path: 'decision-trees', component: DecisionTreeListComponent },
];
