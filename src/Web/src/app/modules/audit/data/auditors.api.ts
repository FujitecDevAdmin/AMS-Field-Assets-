import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import type { Observable } from 'rxjs';

export interface CreateAuditorRequest {
  readonly username: string;
  readonly displayName: string;
  readonly password: string;
  readonly email: string | null;
  readonly employeeId: number | null;
  readonly hasAllBranches: boolean;
  readonly branchIds: readonly number[];
  readonly primaryBranchId: number | null;
  readonly requireMfa: boolean;
}

export interface CreatedAuditor {
  readonly id: number;
  readonly username: string;
  readonly displayName: string;
  readonly mustChangePassword: boolean;
  readonly mfaEnrollmentRequired: boolean;
}

export interface AuditorAccount {
  readonly id: number;
  readonly username: string;
  readonly displayName: string;
  readonly email: string | null;
  readonly employeeId: number | null;
  readonly hasAllBranches: boolean;
  readonly branchIds: readonly number[];
  readonly isActive: boolean;
  readonly isLocked: boolean;
  readonly mfaEnabled: boolean;
  readonly lastLoginOnUtc: string | null;
}

export interface AuditorAccountsResponse {
  readonly rows: readonly AuditorAccount[];
}

export interface AuditorVerificationCount {
  readonly auditorUserId: number;
  readonly verifiedAssetCount: number;
}

export interface AuditorVerificationCountsResponse {
  readonly rows: readonly AuditorVerificationCount[];
}

export interface AuditorVerificationActivityRow {
  readonly verificationId: number;
  readonly assetId: number;
  readonly assetNumber: string;
  readonly assetName: string;
  readonly auditId: number;
  readonly auditName: string;
  readonly workingCondition: string;
  readonly verifiedOnUtc: string;
  readonly remarks: string | null;
}

export interface AuditorVerificationActivityResponse {
  readonly auditorUserId: number;
  readonly totalCount: number;
  readonly rows: readonly AuditorVerificationActivityRow[];
}

@Injectable({ providedIn: 'root' })
export class AuditorsApi {
  private readonly http = inject(HttpClient);

  create(request: CreateAuditorRequest): Observable<CreatedAuditor> {
    return this.http.post<CreatedAuditor>('/api/v1/identity/auditors', request);
  }

  list(): Observable<AuditorAccountsResponse> {
    return this.http.get<AuditorAccountsResponse>('/api/v1/identity/auditors');
  }

  verificationCounts(): Observable<AuditorVerificationCountsResponse> {
    return this.http.get<AuditorVerificationCountsResponse>(
      '/api/v1/verification/auditor-verification-counts',
    );
  }

  verificationActivity(auditorUserId: number): Observable<AuditorVerificationActivityResponse> {
    return this.http.get<AuditorVerificationActivityResponse>(
      `/api/v1/verification/auditors/${auditorUserId}/verification-activity`,
    );
  }
}
