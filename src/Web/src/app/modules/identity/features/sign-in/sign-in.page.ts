import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { ActivatedRoute, Router } from '@angular/router';
import { DxButtonModule } from 'devextreme-angular/ui/button';
import { DxTextBoxModule } from 'devextreme-angular/ui/text-box';

import { AuthStore } from '../../../../core/auth/auth.store';
import { ToastService } from '../../../../core/notifications/toast.service';

/**
 * Sign in. Two steps, because the API has two: an enrolled user gets a
 * challenge token and no access token, and is not signed in until the code is
 * verified.
 *
 * It renders OUTSIDE the shell — there is no navigation to show somebody who
 * has not signed in, and the capability set that drives that navigation is not
 * known yet.
 */
@Component({
  selector: 'ams-sign-in',
  imports: [DxTextBoxModule, DxButtonModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './sign-in.page.html',
  styleUrl: './sign-in.page.scss',
})
export class SignInPage {
  private readonly auth = inject(AuthStore);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly toast = inject(ToastService);

  protected readonly username = signal('');
  protected readonly password = signal('');
  protected readonly code = signal('');
  protected readonly busy = signal(false);
  protected readonly error = signal<string | null>(null);

  protected readonly stage = this.auth.stage;
  protected readonly canSubmitCredentials = computed(
    () => this.username().trim().length > 0 && this.password().length > 0 && !this.busy(),
  );
  protected readonly canSubmitCode = computed(() => this.code().trim().length > 0 && !this.busy());

  protected async submitCredentials(): Promise<void> {
    if (!this.canSubmitCredentials()) {
      return;
    }

    this.busy.set(true);
    this.error.set(null);

    try {
      const response = await this.auth.signIn(this.username().trim(), this.password());

      if (response.mfaRequired) {
        this.password.set('');
        return;
      }

      this.afterSignIn(response.mustChangePassword);
    } catch (error) {
      this.error.set(this.messageFor(error));
    } finally {
      this.busy.set(false);
    }
  }

  protected async submitCode(): Promise<void> {
    if (!this.canSubmitCode()) {
      return;
    }

    this.busy.set(true);
    this.error.set(null);

    try {
      const response = await this.auth.verifyMfaCode(this.code().trim());

      if (response.usedRecoveryCode) {
        // The API returns this so the client can SAY so — a spent recovery code
        // that nobody mentions is how somebody runs out without noticing.
        this.toast.warning(
          `Signed in with a recovery code. ${response.remainingRecoveryCodes} left.`,
        );
      }

      this.afterSignIn(response.mustChangePassword);
    } catch (error) {
      this.error.set(this.messageFor(error));
      this.code.set('');
    } finally {
      this.busy.set(false);
    }
  }

  protected backToCredentials(): void {
    this.auth.cancelMfa();
    this.code.set('');
    this.error.set(null);
  }

  private afterSignIn(mustChangePassword: boolean): void {
    if (mustChangePassword) {
      this.toast.warning('Your password must be changed before you can continue.');
      void this.router.navigate(['/change-password']);
      return;
    }

    const returnUrl = this.route.snapshot.queryParamMap.get('returnUrl') ?? '/';
    void this.router.navigateByUrl(returnUrl);
  }

  /**
   * The API never says WHY a sign-in failed, on purpose — a message that
   * distinguishes "no such user" from "wrong password" is a user enumeration
   * tool. This does not invent a reason it was not given.
   */
  private messageFor(error: unknown): string {
    if (error instanceof HttpErrorResponse) {
      if (error.status === 401) {
        return this.stage() === 'mfa'
          ? 'That code was not accepted. Try again, or use a recovery code.'
          : 'Those credentials were not accepted.';
      }
      if (error.status === 400) {
        return 'Check the details and try again.';
      }
      if (error.status === 0) {
        return 'The server could not be reached.';
      }
      return `Sign-in failed (HTTP ${error.status}).`;
    }
    return 'Sign-in failed.';
  }
}
