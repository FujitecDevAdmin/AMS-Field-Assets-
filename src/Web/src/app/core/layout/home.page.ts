import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { DxButtonModule } from 'devextreme-angular/ui/button';

import { ToastService } from '../notifications/toast.service';

/**
 * Temporary landing page. It exists so the shell renders something before any
 * module has a screen, and it is deleted when the first one does.
 *
 * The toast buttons are here on purpose: the toaster is shared chrome, and the
 * four kinds should be reviewable without waiting for a 409 to happen.
 */
@Component({
  selector: 'ams-home',
  imports: [DxButtonModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './home.page.html',
  styleUrl: './home.page.scss',
})
export class HomePage {
  private readonly toast = inject(ToastService);

  protected readonly tiles = [
    { label: 'Assets on register', value: '—', hint: 'assets module' },
    { label: 'Allocated to staff', value: '—', hint: 'allocations module' },
    { label: 'Open tickets', value: '—', hint: 'service desk module' },
    { label: 'Awaiting verification', value: '—', hint: 'verification module' },
  ];

  protected showSuccess(): void {
    this.toast.success('Handover recorded for AST-004821.');
  }

  protected showInfo(): void {
    this.toast.info('Import rehearsal finished — 1,208 rows, 4 rejections.');
  }

  protected showWarning(): void {
    this.toast.warning('This cycle closes in 2 days.');
  }

  protected showError(): void {
    // Worded the way a real 409 arrives: the server's message, verbatim.
    this.toast.error('AST-004821 is already allocated to Priya R. Return it before reallocating.');
  }
}
