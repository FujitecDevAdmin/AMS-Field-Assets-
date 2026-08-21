import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import type { Observable } from 'rxjs';

export interface MyAuditAsset {
  readonly id: number;
  readonly assetNumber: string;
  readonly assetName: string;
  readonly serialNumber: string | null;
  readonly location: string | null;
  readonly quantity: number;
  readonly isBulk: boolean;
  readonly isVerified: boolean;
}

export interface MyAudit {
  readonly id: number;
  readonly auditName: string;
  readonly branchId: number;
  readonly branchName: string;
  readonly startDate: string;
  readonly endDate: string | null;
  readonly isActive: boolean;
  readonly assets: readonly MyAuditAsset[];
}

interface MyAuditsResponse {
  readonly rows: readonly MyAudit[];
}

export interface VerifyAssetRequest {
  readonly cycleId: number;
  readonly assetId: number;
  readonly clientCaptureId: string;
  readonly workingCondition: string;
  readonly serialVerified: boolean;
  readonly scannedQrValue: string | null;
  readonly remarks: string | null;
}

@Injectable({ providedIn: 'root' })
export class MyAuditsApi {
  private readonly http = inject(HttpClient);

  list(): Observable<MyAuditsResponse> {
    return this.http.get<MyAuditsResponse>('/api/v1/verification/my-audits');
  }

  verify(request: VerifyAssetRequest): Observable<unknown> {
    return this.http.post('/api/v1/verification/verifications', request);
  }
}
