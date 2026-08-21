import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import type { Observable } from 'rxjs';

import type {
  AssetImportResponse,
  ImportedAssetDetailsUpdateResponse,
  AssetRegisterFilters,
  AssetRegisterResponse,
  AssetDashboardResponse,
  AssetDetailResponse,
} from './models/asset-register.models';

export interface LatestAssetVerification {
  readonly isVerified: boolean;
  readonly verificationId: number | null;
  readonly auditId: number | null;
  readonly verifiedByUserId: number | null;
  readonly auditorName: string | null;
  readonly verifiedOnUtc: string | null;
  readonly remarks: string | null;
}

export interface AssetBranch {
  readonly id: number;
  readonly branchCode: string;
  readonly branchName: string;
}

interface AssetBranchesResponse {
  readonly rows: readonly AssetBranch[];
}

@Injectable({ providedIn: 'root' })
export class AssetsApi {
  private readonly http = inject(HttpClient);

  dashboard(): Observable<AssetDashboardResponse> {
    return this.http.get<AssetDashboardResponse>('/api/v1/assets/dashboard');
  }

  get(assetId: number): Observable<AssetDetailResponse> {
    return this.http.get<AssetDetailResponse>(`/api/v1/assets/${assetId}`);
  }

  latestVerification(assetId: number): Observable<LatestAssetVerification> {
    return this.http.get<LatestAssetVerification>(
      `/api/v1/verification/assets/${assetId}/latest-verification`,
    );
  }

  listBranches(): Observable<AssetBranchesResponse> {
    return this.http.get<AssetBranchesResponse>('/api/v1/verification/audit-branches');
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
