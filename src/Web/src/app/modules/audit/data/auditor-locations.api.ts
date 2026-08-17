import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import type { Observable } from 'rxjs';

export interface AuditorLocation {
  readonly id: number;
  readonly locationId: number;
  readonly locationName: string;
}

interface AuditorLocationsResponse {
  readonly rows: readonly AuditorLocation[];
}

@Injectable({ providedIn: 'root' })
export class AuditorLocationsApi {
  private readonly http = inject(HttpClient);

  list(): Observable<AuditorLocationsResponse> {
    return this.http.get<AuditorLocationsResponse>('/api/v1/assets/auditor-locations');
  }

  create(locationName: string): Observable<AuditorLocation> {
    return this.http.post<AuditorLocation>('/api/v1/assets/auditor-locations', { locationName });
  }
}
