import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import type { Observable } from 'rxjs';

import type {
  AssetImportResponse,
  ImportedAssetDetailsUpdateResponse,
  AssetRegisterFilters,
  AssetRegisterResponse,
  AssetDashboardResponse,
} from './models/asset-register.models';

@Injectable({ providedIn: 'root' })
export class AssetsApi {
  private readonly http = inject(HttpClient);

  dashboard(): Observable<AssetDashboardResponse> {
    return this.http.get<AssetDashboardResponse>('/api/v1/assets/dashboard');
  }

  search(
    search: string,
    skip: number,
    take: number,
    filters: AssetRegisterFilters,
  ): Observable<AssetRegisterResponse> {
    let params = new HttpParams().set('skip', skip).set('take', take);
    if (search.trim().length > 0) {
      params = params.set('search', search.trim());
    }
    for (const [key, value] of Object.entries(filters)) {
      if (value !== undefined && value !== null && value !== '') {
        params = params.set(key, String(value));
      }
    }
    return this.http.get<AssetRegisterResponse>('/api/v1/assets', { params });
  }

  importExcel(file: File): Observable<AssetImportResponse> {
    const form = new FormData();
    form.append('file', file, file.name);
    return this.http.post<AssetImportResponse>('/api/v1/assets/imports/excel', form);
  }

  updateImportedDetails(
    assetId: number,
    fields: Readonly<Record<string, string | null>>,
  ): Observable<ImportedAssetDetailsUpdateResponse> {
    return this.http.put<ImportedAssetDetailsUpdateResponse>(
      `/api/v1/assets/${assetId}/imported-details`,
      { fields },
    );
  }
}
