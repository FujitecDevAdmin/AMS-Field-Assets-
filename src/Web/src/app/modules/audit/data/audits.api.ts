import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import type { Observable } from 'rxjs';

export interface AuditCycle {
  readonly id: number;
  readonly cycleName: string;
  readonly branchId: number;
  readonly startDate: string;
  readonly endDate: string | null;
  readonly isActive: boolean;
  readonly closedOnUtc: string | null;
  readonly totalAssetCount: number;
  readonly auditorUserIds: readonly number[];
  readonly locationBranchIds: readonly number[];
  readonly verifiedCount: number;
  readonly exceptionCount: number;
}

interface AuditCyclesResponse {
  readonly rows: readonly AuditCycle[];
}

export interface CreateAuditRequest {
  readonly cycleName: string;
  readonly branchId: number;
  readonly startDate: string;
  readonly endDate: string | null;
  readonly auditorUserIds: readonly number[];
  readonly locationBranchIds: readonly number[];
}

export interface CreatedAudit {
  readonly id: number;
  readonly cycleName: string;
  readonly startDate: string;
  readonly totalAssetCount: number;
}

export interface AuditAssetCountResponse {
  readonly totalAssetCount: number;
}

export interface AddAuditAuditorsResponse {
  readonly cycleId: number;
  readonly addedAuditorUserIds: readonly number[];
  readonly auditorUserIds: readonly number[];
}

export interface AuditAsset {
  readonly id: number;
  readonly assetNumber: string;
  readonly assetName: string;
  readonly serialNumber: string | null;
  readonly location: string | null;
  readonly quantity: number;
  readonly isVerified: boolean;
  readonly verifiedByUserId: number | null;
  readonly verifiedBy: string | null;
  readonly verifiedOnUtc: string | null;
}

export interface AuditAssetsResponse {
  readonly auditId: number;
  readonly auditName: string;
  readonly branchName: string;
  readonly auditStatus: 'Active' | 'Closed';
  readonly rows: readonly AuditAsset[];
}

export interface VerificationReportRow {
  readonly id: number;
  readonly physicalVerificationCycleId: number;
  readonly assetId: number;
  readonly auditName: string | null;
  readonly assetNumber: string | null;
  readonly assetName: string | null;
  readonly branchId: number | null;
  readonly branchName: string | null;
  readonly locationName: string | null;
  readonly auditorName: string | null;
  readonly scannedQrValue: string | null;
  readonly workingCondition: string;
  readonly hasQrMismatch: boolean;
  readonly serialVerified: boolean;
  readonly verifiedOnUtc: string;
  readonly remarks: string | null;
}

export interface VerificationReportResponse {
  readonly rows: readonly VerificationReportRow[];
  readonly totalCount: number;
  readonly exceptionCount: number;
}

@Injectable({ providedIn: 'root' })
export class AuditsApi {
  private readonly http = inject(HttpClient);

  list(): Observable<AuditCyclesResponse> {
    return this.http.get<AuditCyclesResponse>('/api/v1/verification/cycles');
  }

  create(request: CreateAuditRequest): Observable<CreatedAudit> {
    return this.http.post<CreatedAudit>('/api/v1/verification/cycles', request);
  }

  calculateAssetCount(locationBranchIds: readonly number[]): Observable<AuditAssetCountResponse> {
    return this.http.post<AuditAssetCountResponse>('/api/v1/verification/audit-asset-count', {
      locationBranchIds,
    });
  }

  listAssets(auditId: number): Observable<AuditAssetsResponse> {
    return this.http.get<AuditAssetsResponse>(`/api/v1/verification/cycles/${auditId}/assets`);
  }

  addAuditors(auditId: number, auditorUserIds: readonly number[]): Observable<AddAuditAuditorsResponse> {
    return this.http.post<AddAuditAuditorsResponse>(
      `/api/v1/verification/cycles/${auditId}/auditors`,
      { auditorUserIds },
    );
  }

  verificationReport(filters: {
    readonly branchId?: number;
    readonly location?: string;
    readonly search?: string;
  }): Observable<VerificationReportResponse> {
    let params = new HttpParams().set('skip', 0).set('take', 200);
    if (filters.branchId) params = params.set('branchId', filters.branchId);
    if (filters.location) params = params.set('location', filters.location);
    if (filters.search?.trim()) params = params.set('search', filters.search.trim());
    return this.http.get<VerificationReportResponse>('/api/v1/verification/verifications', { params });
  }
}
