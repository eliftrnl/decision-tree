import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface TableColumn {
  id: number;
  tableId: number;
  columnName: string;
  excelHeaderName?: string;
  description?: string;
  dataType: number | string; // 1=String, 2=Int, 3=Decimal, 4=Date, 5=Boolean
  isRequired: boolean;
  statusCode: number | string; // 1=Active, 2=Passive
  orderIndex: number;
  format?: string;
  maxLength?: number;
  precision?: number;
  scale?: number;
  validFrom?: string;
  validTo?: string;
}

export interface ColumnCreateRequest {
  columnName: string;
  excelHeaderName?: string;
  description?: string;
  dataType: number;
  isRequired: boolean;
  direction: number;
  orderIndex?: number;
  format?: string;
  maxLength?: number;
}

@Injectable({
  providedIn: 'root'
})
export class ColumnService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = 'http://localhost:5135/api';

  // GET /api/decision-trees/{dtId}/tables/{tableId}/columns
  getColumns(dtId: number, tableId: number): Observable<TableColumn[]> {
    return this.http.get<TableColumn[]>(
      `${this.baseUrl}/decision-trees/${dtId}/tables/${tableId}/columns`
    );
  }

  // GET /api/decision-trees/{dtId}/tables/{tableId}/columns/{columnId}
  getColumnById(dtId: number, tableId: number, columnId: number): Observable<TableColumn> {
    return this.http.get<TableColumn>(
      `${this.baseUrl}/decision-trees/${dtId}/tables/${tableId}/columns/${columnId}`
    );
  }

  // POST /api/decision-trees/{dtId}/tables/{tableId}/columns
  createColumn(dtId: number, tableId: number, data: ColumnCreateRequest): Observable<TableColumn> {
    return this.http.post<TableColumn>(
      `${this.baseUrl}/decision-trees/${dtId}/tables/${tableId}/columns`,
      data
    );
  }

  // PUT /api/decision-trees/{dtId}/tables/{tableId}/columns/{columnId}
  updateColumn(dtId: number, tableId: number, columnId: number, data: Partial<TableColumn>): Observable<void> {
    return this.http.put<void>(
      `${this.baseUrl}/decision-trees/${dtId}/tables/${tableId}/columns/${columnId}`,
      data
    );
  }

  // DELETE /api/decision-trees/{dtId}/tables/{tableId}/columns/{columnId}
  deleteColumn(dtId: number, tableId: number, columnId: number): Observable<void> {
    return this.http.delete<void>(
      `${this.baseUrl}/decision-trees/${dtId}/tables/${tableId}/columns/${columnId}`
    );
  }
}
