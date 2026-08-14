import { Injectable } from '@angular/core';
import notify from 'devextreme/ui/notify';

export type ToastKind = 'success' | 'error' | 'warning' | 'info';

/**
 * The one place a toast is raised. Every screen calls this rather than
 * `notify()` directly, so position, width and duration are decided once and a
 * 409 looks the same on every screen (docs/04 §3).
 *
 * Errors stay up longer than confirmations and are dismissable: a success
 * message the user misses costs nothing, a conflict message costs a re-entry.
 */
@Injectable({ providedIn: 'root' })
export class ToastService {
  private static readonly DURATION: Record<ToastKind, number> = {
    success: 2500,
    info: 3000,
    warning: 5000,
    error: 8000,
  };

  success(message: string): void {
    this.show(message, 'success');
  }

  info(message: string): void {
    this.show(message, 'info');
  }

  warning(message: string): void {
    this.show(message, 'warning');
  }

  /**
   * For a 409 the server writes a readable message on purpose — pass it
   * through verbatim rather than replacing it with a generic one.
   */
  error(message: string): void {
    this.show(message, 'error');
  }

  private show(message: string, kind: ToastKind): void {
    /*
     * The stock animation is left alone on purpose. A custom `slide` with a
     * `from.top` offset laid the toast out at the very bottom edge with zero
     * height — visible to the DOM, invisible to the user. Position and duration
     * are the only things worth deciding here.
     */
    notify(
      {
        message,
        width: 'auto',
        maxWidth: 480,
        shading: false,
        closeOnClick: true,
        displayTime: ToastService.DURATION[kind],
        position: { my: 'bottom right', at: 'bottom right', offset: '-24 -24' },
      },
      kind,
    );
  }
}
