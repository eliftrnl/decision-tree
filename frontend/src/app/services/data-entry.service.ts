import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface DataRow {
  id: number;
  tableId: number;
  rowDataJson: string;
  rowCode?: string;
  createdAtUtc: string;
  updatedAtUtc: string;
}

export interface DataRowCreate {
  rowDataJson: string;
  rowCode?: string;
}

export interface GenerateJsonRequest {
  decisionTreeId: number;
  includeInactiveTables?: boolean;
  includeInactiveColumns?: boolean;
}

export interface JsonOutput {
  decisionTreeCode: string;
  decisionTreeName: string;
  schemaVersion: number;
  generatedAt: string;
  tables: TableJsonOutput[];
}

export interface TableJsonOutput {
  tableName: string;
  tableCode: string;
  direction: string;
  metadata: ColumnMetadata[];
  rows: any[];
}

export interface ColumnMetadata {
  columnName: string;
  columnCode: string;
  dataType: string;
  isRequired: boolean;
  orderIndex: number;
}

@Injectable({
  providedIn: 'root'
})
export class DataEntryService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = 'http://localhost:5135/api';

  // GET /api/decision-trees/{dtId}/data/tables/{tableId}/rows
  getTableRows(dtId: number, tableId: number): Observable<DataRow[]> {
    return this.http.get<DataRow[]>(
      `${this.baseUrl}/decision-trees/${dtId}/data/tables/${tableId}/rows`
    );
  }

  // POST /api/decision-trees/{dtId}/data/tables/{tableId}/rows
  createRow(dtId: number, tableId: number, data: DataRowCreate): Observable<DataRow> {
    return this.http.post<DataRow>(
      `${this.baseUrl}/decision-trees/${dtId}/data/tables/${tableId}/rows`,
      data
    );
  }

  // PUT /api/decision-trees/{dtId}/data/tables/{tableId}/rows/{rowId}
  updateRow(dtId: number, tableId: number, rowId: number, data: DataRowCreate): Observable<void> {
    return this.http.put<void>(
      `${this.baseUrl}/decision-trees/${dtId}/data/tables/${tableId}/rows/${rowId}`,
      data
    );
  }

  // DELETE /api/decision-trees/{dtId}/data/tables/{tableId}/rows/{rowId}
  deleteRow(dtId: number, tableId: number, rowId: number): Observable<void> {
    return this.http.delete<void>(
      `${this.baseUrl}/decision-trees/${dtId}/data/tables/${tableId}/rows/${rowId}`
    );
  }

  // POST /api/decision-trees/{dtId}/data/generate-json
  generateJson(dtId: number, request?: GenerateJsonRequest): Observable<JsonOutput> {
    return this.http.post<JsonOutput>(
      `${this.baseUrl}/decision-trees/${dtId}/data/generate-json`,
      request || { decisionTreeId: dtId }
    );
  }

  // POST /api/decision-trees/{dtId}/data/parse-json
  parseJson(dtId: number, jsonContent: string, replaceExisting: boolean = false): Observable<any> {
    return this.http.post(
      `${this.baseUrl}/decision-trees/${dtId}/data/parse-json`,
      { jsonContent, replaceExistingData: replaceExisting }
    );
  }

  // POST /api/decision-trees/{dtId}/data/import-excel
  importExcel(dtId: number, file: File, replaceExisting: boolean = false): Observable<any> {
    const formData = new FormData();
    formData.append('file', file);

    let params = new HttpParams();
    if (replaceExisting) {
      params = params.set('replaceExisting', 'true');
    }

    return this.http.post(
      `${this.baseUrl}/decision-trees/${dtId}/data/import-excel`,
      formData,
      { params }
    );
  }

  // GET /api/decision-trees/{dtId}/data/export-excel
  exportExcel(
    dtId: number,
    includeInactiveTables: boolean = false,
    includeInactiveColumns: boolean = false
  ): Observable<Blob> {
    let params = new HttpParams();
    if (includeInactiveTables) {
      params = params.set('includeInactiveTables', 'true');
    }
    if (includeInactiveColumns) {
      params = params.set('includeInactiveColumns', 'true');
    }

    return this.http.get(
      `${this.baseUrl}/decision-trees/${dtId}/data/export-excel`,
      { params, responseType: 'blob' }
    );
  }
}
