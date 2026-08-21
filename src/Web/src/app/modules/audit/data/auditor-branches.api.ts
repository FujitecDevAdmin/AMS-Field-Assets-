import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import type { Observable } from 'rxjs';

export interface AuditorBranch {
  readonly id: number;
  readonly branchCode: string;
  readonly branchName: string;
  readonly regionId: number | null;
  readonly regionName: string | null;
  readonly timeZoneId: string;
  readonly isHeadOffice: boolean;
  readonly isActive: boolean;
}

export interface AuditorBranchesResponse {
  readonly rows: readonly AuditorBranch[];
}

@Injectable({ providedIn: 'root' })
export class AuditorBranchesApi {
  private readonly http = inject(HttpClient);

  list(): Observable<AuditorBranchesResponse> {
    return this.http.get<AuditorBranchesResponse>('/api/v1/organization/branches', {
      params: { isActive: true },
    });
  }

  listForAudit(): Observable<AuditorBranchesResponse> {
    return this.http.get<AuditorBranchesResponse>('/api/v1/verification/audit-branches');
  }

  create(branchCode: string, branchName: string, timeZoneId: string): Observable<AuditorBranch> {
    return this.http.post<AuditorBranch>('/api/v1/organization/branches', {
      branchCode,
      branchName,
      regionId: null,
      timeZoneId,
      isHeadOffice: false,
    });
  }
}
