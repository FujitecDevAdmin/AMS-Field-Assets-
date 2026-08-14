import {
  ChangeDetectionStrategy,
  Component,
  computed,
  effect,
  inject,
  signal,
} from '@angular/core';
import { Router, RouterOutlet } from '@angular/router';
import { DxDrawerModule } from 'devextreme-angular/ui/drawer';
import { DxPopupModule } from 'devextreme-angular/ui/popup';

import { AuthStore } from '../auth/auth.store';
import { ToastService } from '../notifications/toast.service';
import { AppHeaderComponent, type UserMenuItem } from './app-header.component';
import { BREAKPOINT, mediaQuery } from './media-query';
import type { NavBadges } from './nav-items';
import { NotificationPanelComponent, type AppNotification } from './notification-panel.component';
import { SideNavComponent } from './side-nav.component';

/**
 * The application shell: navigation drawer on the left, notification panel on
 * the right, header and routed content between them.
 *
 * Two drawers nest rather than sit side by side so each keeps its own reveal
 * animation and the content area is squeezed by whichever is open.
 *
 * State that outlives a screen lives here (drawer positions, unread count).
 * Screen state belongs to the screen's own store — see docs/04 §1.
 */
@Component({
  selector: 'ams-shell',
  imports: [
    DxDrawerModule,
    DxPopupModule,
    RouterOutlet,
    AppHeaderComponent,
    SideNavComponent,
    NotificationPanelComponent,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './shell.page.html',
  styleUrl: './shell.page.scss',
})
export class ShellPage {
  private readonly toast = inject(ToastService);
  private readonly auth = inject(AuthStore);
  private readonly router = inject(Router);

  /** The signed-in user, for the header. */
  protected readonly displayName = this.auth.displayName;

  /** Phones: the drawer floats over the content instead of squeezing it. */
  protected readonly isHandset = mediaQuery(BREAKPOINT.handset);
  /** Phones and tablets: not enough width to hold the drawer open by default. */
  protected readonly isCompact = mediaQuery(BREAKPOINT.compact);

  protected readonly navOpened = signal(!this.isCompact());
  protected readonly panelOpened = signal(false);

  /**
   * Nav badge counts, keyed by nav id. Placeholder values — each one belongs to
   * its module's endpoint (pending approvals, open tickets, assets still to
   * verify) and moves there as the modules land. Groups roll their children up,
   * so only leaves are listed.
   */
  protected readonly navBadges = signal<NavBadges>({
    approvals: 7,
    'service-desk': 12,
    verification: 412,
    'data-import': 4,
  });

  /**
   * Placeholder rows. They come from the notifications module's endpoint once
   * it exists; the panel is written against the shape, not against these.
   */
  protected readonly notifications = signal<AppNotification[]>([
    {
      id: '1',
      title: 'Verification cycle open',
      detail: 'Cycle 2026-Q3 is active for 412 assets in your branch scope.',
      when: '10 minutes ago',
      kind: 'info',
      read: false,
    },
    {
      id: '2',
      title: 'Handover awaiting acknowledgement',
      detail: 'AST-004821 was despatched to Chennai Branch Store 3 days ago.',
      when: 'Yesterday',
      kind: 'warning',
      read: false,
    },
    {
      id: '3',
      title: 'SAP sync completed',
      detail: '1,208 asset master records reconciled, 0 rejected.',
      when: 'Yesterday',
      kind: 'success',
      read: true,
    },
  ]);

  protected readonly unreadCount = computed(
    () => this.notifications().filter((n) => !n.read).length,
  );

  constructor() {
    /* Crossing the breakpoint re-decides the drawer: open when there is room
       for it, closed when there is not. Only fires at the boundary, so it does
       not fight the user's toggle at a given width. */
    effect(() => this.navOpened.set(!this.isCompact()));

    /* The shell is the first thing a signed-in user reaches, so it is where a
       session restored from localStorage gets checked against the server. */
    void this.auth.refreshProfile();
  }

  protected toggleNav(): void {
    this.navOpened.update((open) => !open);
  }

  /**
   * On a phone the drawer covers the content, so it has to get out of the way
   * once it has been used. On a desktop it is the frame — closing it after
   * every click would be a tic.
   */
  protected onNavigated(): void {
    if (this.isHandset()) {
      this.navOpened.set(false);
    }
  }

  protected togglePanel(): void {
    this.panelOpened.update((open) => !open);
  }

  protected onSearch(term: string): void {
    // The global search endpoint belongs to the discovery module and does not
    // exist yet. Saying so is better than a box that silently does nothing.
    this.toast.info(`Search is not wired up yet — you asked for “${term}”.`);
  }

  protected onUserMenu(item: UserMenuItem): void {
    if (item.id === 'signout') {
      this.auth.signOut();
      void this.router.navigate(['/login']);
      return;
    }

    this.toast.info(`“${item.text}” is not wired up yet.`);
  }

  protected markAllRead(): void {
    this.notifications.update((list) => list.map((n) => ({ ...n, read: true })));
    this.toast.success('All notifications marked as read.');
  }
}
