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

export interface JsonExportMetadata {
  decisionTreeId: number;
  decisionTreeCode: string;
  decisionTreeName: string;
  schemaVersion: number;
  exportedAtUtc: string;
}

export interface JsonTableValue {
  metadata: Record<string, string>[];
  data: any[][];
}

export interface JsonTableWrapper {
  name: string;
  value: JsonTableValue;
}

export interface JsonExportResponse {
  metadata: JsonExportMetadata;
  tables: JsonTableWrapper[];
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

  // GET /api/decision-trees/{dtId}/data/export-json
  exportJson(dtId: number): Observable<JsonExportResponse> {
    return this.http.get<JsonExportResponse>(
      `${this.baseUrl}/decision-trees/${dtId}/data/export-json`
    );
  }

  // GET /api/decision-trees/{dtId}/data/generate-json (DEPRECATED - redirects to exportJson)
  generateJson(dtId: number): Observable<JsonExportResponse> {
    return this.exportJson(dtId);
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
