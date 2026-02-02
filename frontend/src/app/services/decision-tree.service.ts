import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface DecisionTree {
  id: number;
  code: string;
  name: string;
  statusCode: number;
  lastOperationDateUtc?: string;
}

export interface DecisionTreeFilter {
  code?: string;
  name?: string;
  statusCode?: number;
}

@Injectable({
  providedIn: 'root'
})
export class DecisionTreeService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = 'http://localhost:5135/api/decision-trees';

  search(filter: DecisionTreeFilter): Observable<DecisionTree[]> {
    let params = new HttpParams();
    
    if (filter.code?.trim()) {
      params = params.set('code', filter.code.trim());
    }
    
    if (filter.name?.trim()) {
      params = params.set('name', filter.name.trim());
    }
    
    if (filter.statusCode !== undefined && filter.statusCode !== null) {
      params = params.set('status', filter.statusCode.toString());
    }
    
    return this.http.get<DecisionTree[]>(this.apiUrl, { params });
  }

  getById(id: number): Observable<DecisionTree> {
    return this.http.get<DecisionTree>(`${this.apiUrl}/${id}`);
  }

  create(data: Omit<DecisionTree, 'id' | 'updatedAtUtc'>): Observable<DecisionTree> {
    return this.http.post<DecisionTree>(this.apiUrl, data);
  }

  update(id: number, data: Partial<DecisionTree>): Observable<void> {
    return this.http.put<void>(`${this.apiUrl}/${id}`, data);
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }
}
