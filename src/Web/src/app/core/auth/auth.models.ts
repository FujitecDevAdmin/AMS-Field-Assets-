/**
 * Wire shapes for the identity module's sign-in slices. Hand-written mirrors of
 * the API records (docs/04 §1) — camelCase because the API's JSON options do
 * the casing, not the client.
 *
 * Mirrors:
 *   Identity/Features/SignIn/SignInRequest.cs        -> SignInRequest
 *   Identity/Features/SignIn/SignInResponse.cs       -> SignInResponse
 *   Identity/Features/VerifyMfaCode/*                -> VerifyMfaCode*
 *   Identity/Features/GetMyProfile/GetMyProfileResponse.cs -> MyProfile
 */

export interface SignInRequest {
  readonly username: string;
  readonly password: string;
}

export interface SignInResponse {
  readonly userId: number;
  readonly username: string;
  readonly displayName: string;
  /** New or admin-reset account: route to the password change screen first. */
  readonly mustChangePassword: boolean;
  /** Enrolled user. The session is NOT usable until VerifyMfaCode succeeds. */
  readonly mfaRequired: boolean;
  /** Identifies this half-finished sign-in. Null when MFA is not required. */
  readonly mfaChallengeToken: string | null;
  /** Null whenever mfaRequired is true — that is the point of the second factor. */
  readonly accessToken: string | null;
  readonly accessTokenExpiresOnUtc: string | null;
}

export interface VerifyMfaCodeRequest {
  readonly mfaChallengeToken: string;
  readonly code: string;
}

export interface VerifyMfaCodeResponse {
  readonly userId: number;
  readonly username: string;
  readonly displayName: string;
  readonly mustChangePassword: boolean;
  /** A recovery code was spent rather than an authenticator code. Say so. */
  readonly usedRecoveryCode: boolean;
  readonly remainingRecoveryCodes: number;
  readonly accessToken: string;
  readonly accessTokenExpiresOnUtc: string;
}

export interface MyProfile {
  readonly userId: number;
  readonly username: string;
  readonly displayName: string;
  readonly email: string | null;
  readonly mustChangePassword: boolean;
  readonly mfaEnabled: boolean;
  readonly remainingRecoveryCodes: number;
  readonly hasAllBranches: boolean;
  readonly branchIds: readonly number[];
}

/** What the app keeps once a sign-in is COMPLETE. */
export interface Session {
  readonly userId: number;
  readonly username: string;
  readonly displayName: string;
  readonly accessToken: string;
  readonly expiresOnUtc: string;
  readonly mustChangePassword: boolean;
}
