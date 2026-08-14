import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import type { Observable } from 'rxjs';

import type {
  MyProfile,
  SignInRequest,
  SignInResponse,
  VerifyMfaCodeRequest,
  VerifyMfaCodeResponse,
} from './auth.models';

/**
 * The only place the identity module's HTTP routes are named (docs/04 §4: no
 * HttpClient outside a data service).
 */
@Injectable({ providedIn: 'root' })
export class AuthApi {
  private static readonly BASE = '/api/v1/identity';

  private readonly http = inject(HttpClient);

  signIn(request: SignInRequest): Observable<SignInResponse> {
    return this.http.post<SignInResponse>(`${AuthApi.BASE}/sign-in`, request);
  }

  verifyMfaCode(request: VerifyMfaCodeRequest): Observable<VerifyMfaCodeResponse> {
    return this.http.post<VerifyMfaCodeResponse>(`${AuthApi.BASE}/sign-in/mfa`, request);
  }

  myProfile(): Observable<MyProfile> {
    return this.http.get<MyProfile>(`${AuthApi.BASE}/me`);
  }
}
