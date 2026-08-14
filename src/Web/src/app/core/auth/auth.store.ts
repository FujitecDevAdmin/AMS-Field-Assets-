import { computed, inject, Injectable, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';

import { AuthApi } from './auth.api';
import type { Session, SignInResponse, VerifyMfaCodeResponse } from './auth.models';

const SESSION_KEY = 'ams.session';

function load(): Session | null {
  try {
    const raw = localStorage.getItem(SESSION_KEY);
    if (raw === null) {
      return null;
    }
    const parsed = JSON.parse(raw) as Session;
    return typeof parsed.accessToken === 'string' ? parsed : null;
  } catch {
    return null;
  }
}

function save(session: Session | null): void {
  try {
    if (session === null) {
      localStorage.removeItem(SESSION_KEY);
    } else {
      localStorage.setItem(SESSION_KEY, JSON.stringify(session));
    }
  } catch {
    // Private mode: the session simply does not outlive the tab.
  }
}

/** What the sign-in screen is currently asking for. */
export type SignInStage = 'credentials' | 'mfa';

/**
 * The session, and the two-step sign-in that produces one.
 *
 * The API deliberately issues NO access token beside an MFA challenge — an
 * enrolled user is not signed in until VerifyMfaCode succeeds. This store keeps
 * that property: `session` stays null through the challenge, so every guard and
 * interceptor treats a half-finished sign-in as signed out.
 */
@Injectable({ providedIn: 'root' })
export class AuthStore {
  private readonly api = inject(AuthApi);

  private readonly sessionState = signal<Session | null>(load());
  private readonly challengeToken = signal<string | null>(null);
  private readonly pendingChangePassword = signal(false);

  readonly session = this.sessionState.asReadonly();
  readonly stage = computed<SignInStage>(() =>
    this.challengeToken() === null ? 'credentials' : 'mfa',
  );

  readonly isSignedIn = computed(() => {
    const session = this.sessionState();
    return session !== null && new Date(session.expiresOnUtc).getTime() > Date.now();
  });

  readonly displayName = computed(() => this.sessionState()?.displayName ?? '');

  /** True when the signed-in user must change their password before anything else. */
  readonly mustChangePassword = computed(() => this.sessionState()?.mustChangePassword ?? false);

  /**
   * Step one. Returns the response so the screen can say what happens next —
   * an MFA prompt, a forced password change, or straight in.
   */
  async signIn(username: string, password: string): Promise<SignInResponse> {
    const response = await firstValueFrom(this.api.signIn({ username, password }));

    if (response.mfaRequired) {
      this.challengeToken.set(response.mfaChallengeToken);
      this.pendingChangePassword.set(response.mustChangePassword);
      return response;
    }

    // A complete sign-in always carries a token; the API's own contract says so.
    if (response.accessToken !== null && response.accessTokenExpiresOnUtc !== null) {
      this.sessionState.set({
        userId: response.userId,
        username: response.username,
        displayName: response.displayName,
        accessToken: response.accessToken,
        expiresOnUtc: response.accessTokenExpiresOnUtc,
        mustChangePassword: response.mustChangePassword,
      });
      save(this.sessionState());
    }

    return response;
  }

  /** Step two, for enrolled users. This is where the session begins. */
  async verifyMfaCode(code: string): Promise<VerifyMfaCodeResponse> {
    const token = this.challengeToken();
    if (token === null) {
      throw new Error('No MFA challenge is in progress.');
    }

    const response = await firstValueFrom(
      this.api.verifyMfaCode({ mfaChallengeToken: token, code }),
    );

    this.sessionState.set({
      userId: response.userId,
      username: response.username,
      displayName: response.displayName,
      accessToken: response.accessToken,
      expiresOnUtc: response.accessTokenExpiresOnUtc,
      mustChangePassword: response.mustChangePassword || this.pendingChangePassword(),
    });
    save(this.sessionState());
    this.challengeToken.set(null);
    this.pendingChangePassword.set(false);

    return response;
  }

  /**
   * Confirms a restored session against the server, and takes the server's word
   * for the display name and the password flag.
   *
   * A token in localStorage proves only that a sign-in once succeeded. It says
   * nothing about whether the account has since been locked, or the password
   * reset, or the token revoked — and the client's own expiry check cannot know
   * any of that. A 401 here is handled by the interceptor, which ends the
   * session, so the failure path is deliberately empty.
   */
  async refreshProfile(): Promise<void> {
    if (!this.isSignedIn()) {
      return;
    }

    try {
      const profile = await firstValueFrom(this.api.myProfile());
      this.sessionState.update((current) =>
        current === null
          ? null
          : {
              ...current,
              displayName: profile.displayName,
              mustChangePassword: profile.mustChangePassword,
            },
      );
      save(this.sessionState());
    } catch {
      // A 401 already signed us out. Anything else is a server that is down,
      // and a stored session is the only reason the app is usable at all then.
    }
  }

  /** Abandon a half-finished sign-in and go back to the credentials step. */
  cancelMfa(): void {
    this.challengeToken.set(null);
    this.pendingChangePassword.set(false);
  }

  signOut(): void {
    this.sessionState.set(null);
    this.challengeToken.set(null);
    this.pendingChangePassword.set(false);
    save(null);
  }
}
