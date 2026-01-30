import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface DecisionTreeTable {
  id: number;
  decisionTreeId: number;
  tableName: string;
  direction: number; // 1=Input, 2=Output
  statusCode: number; // 1=Active, 2=Passive
  createdAtUtc?: string;
  updatedAtUtc?: string;
}

export interface TableCreateRequest {
  tableName: string;
  direction: number;
  statusCode?: number;
}

@Injectable({
  providedIn: 'root'
})
export class TableService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = 'http://localhost:5135/api';

  // GET /api/decision-trees/{dtId}/tables
  getTables(dtId: number): Observable<DecisionTreeTable[]> {
    return this.http.get<DecisionTreeTable[]>(`${this.baseUrl}/decision-trees/${dtId}/tables`);
  }

  // GET /api/decision-trees/{dtId}/tables/{tableId}
  getTableById(dtId: number, tableId: number): Observable<DecisionTreeTable> {
    return this.http.get<DecisionTreeTable>(`${this.baseUrl}/decision-trees/${dtId}/tables/${tableId}`);
  }

  // POST /api/decision-trees/{dtId}/tables
  createTable(dtId: number, data: TableCreateRequest): Observable<DecisionTreeTable> {
    return this.http.post<DecisionTreeTable>(`${this.baseUrl}/decision-trees/${dtId}/tables`, data);
  }

  // PUT /api/decision-trees/{dtId}/tables/{tableId}
  updateTable(dtId: number, tableId: number, data: Partial<DecisionTreeTable>): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/decision-trees/${dtId}/tables/${tableId}`, data);
  }

  // DELETE /api/decision-trees/{dtId}/tables/{tableId}
  deleteTable(dtId: number, tableId: number): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/decision-trees/${dtId}/tables/${tableId}`);
  }
}
