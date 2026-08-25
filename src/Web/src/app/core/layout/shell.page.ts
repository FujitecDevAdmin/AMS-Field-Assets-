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
import { catchError, firstValueFrom, forkJoin, of } from 'rxjs';

import { AssetsApi } from '../../modules/assets/data/assets.api';
import { AuditsApi } from '../../modules/audit/data/audits.api';
import { AuditorsApi } from '../../modules/audit/data/auditors.api';
import { AuthStore } from '../auth/auth.store';
import { ToastService } from '../notifications/toast.service';
import { ThemeService } from '../theme/theme.service';
import { AppHeaderComponent, type UserMenuItem } from './app-header.component';
import { BREAKPOINT, mediaQuery } from './media-query';
import type { NavBadges } from './nav-items';
import { NotificationPanelComponent, type AppNotification } from './notification-panel.component';
import { SideNavComponent } from './side-nav.component';

interface GlobalSearchResult {
  readonly id: number;
  readonly module: 'Assets' | 'Audits' | 'Auditors';
  readonly title: string;
  readonly description: string;
  readonly icon: string;
  readonly path: readonly (string | number)[];
  readonly queryParams?: Readonly<Record<string, string>>;
}

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
  private readonly assetsApi = inject(AssetsApi);
  private readonly auditsApi = inject(AuditsApi);
  private readonly auditorsApi = inject(AuditorsApi);
  private readonly theme = inject(ThemeService);

  protected readonly globalSearchVisible = signal(false);
  protected readonly globalSearchLoading = signal(false);
  protected readonly globalSearchTerm = signal('');
  protected readonly globalSearchResults = signal<readonly GlobalSearchResult[]>([]);
  protected readonly globalSearchError = signal<string | null>(null);

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

  /** Collapse to the icon rail after every module selection. On handsets the
   * rail has zero width, so the same state closes the overlay completely. */
  protected onNavigated(): void {
    this.navOpened.set(false);
  }

  protected togglePanel(): void {
    this.panelOpened.update((open) => !open);
  }

  protected async onSearch(term: string): Promise<void> {
    const query = term.trim();
    if (!query) return;

    this.navOpened.set(false);
    this.globalSearchTerm.set(query);
    this.globalSearchResults.set([]);
    this.globalSearchError.set(null);
    this.globalSearchVisible.set(true);
    this.globalSearchLoading.set(true);

    const normalized = query.toLocaleLowerCase();
    try {
      const response = await firstValueFrom(forkJoin({
        assets: this.assetsApi.search(query, 0, 10, {}).pipe(catchError(() => of({ rows: [], totalCount: 0 }))),
        audits: this.auditsApi.list().pipe(catchError(() => of({ rows: [] }))),
        auditors: this.auditorsApi.list().pipe(catchError(() => of({ rows: [] }))),
      }));

      const assets: GlobalSearchResult[] = response.assets.rows.map(asset => ({
        id: asset.id,
        module: 'Assets',
        title: asset.assetNumber,
        description: [asset.assetName, asset.serialNumber].filter(Boolean).join(' · '),
        icon: 'box',
        path: ['/field-assets', asset.id],
      }));
      const audits: GlobalSearchResult[] = response.audits.rows
        .filter(audit => [audit.cycleName, String(audit.id)].some(value => value.toLocaleLowerCase().includes(normalized)))
        .slice(0, 10)
        .map(audit => ({
          id: audit.id,
          module: 'Audits',
          title: audit.cycleName,
          description: `${audit.isActive ? 'Active' : 'Closed'} · ${audit.totalAssetCount} assets`,
          icon: 'checklist',
          path: ['/audit'],
          queryParams: { search: audit.cycleName },
        }));
      const auditors: GlobalSearchResult[] = response.auditors.rows
        .filter(auditor => [auditor.displayName, auditor.username, auditor.email ?? '', String(auditor.employeeId ?? '')]
          .some(value => value.toLocaleLowerCase().includes(normalized)))
        .slice(0, 10)
        .map(auditor => ({
          id: auditor.id,
          module: 'Auditors',
          title: auditor.displayName,
          description: [auditor.username, auditor.email].filter(Boolean).join(' · '),
          icon: 'user',
          path: ['/auditors'],
          queryParams: { search: auditor.displayName },
        }));

      this.globalSearchResults.set([...assets, ...audits, ...auditors]);
      if (this.globalSearchResults().length === 0) {
        this.globalSearchError.set(`No results found for “${query}”.`);
      }
    } catch {
      this.globalSearchError.set('Global search could not be completed. Check the API connection.');
    } finally {
      this.globalSearchLoading.set(false);
    }
  }

  protected openGlobalSearchResult(result: GlobalSearchResult): void {
    this.globalSearchVisible.set(false);
    void this.router.navigate([...result.path], { queryParams: result.queryParams });
  }

  protected onUserMenu(item: UserMenuItem): void {
    if (item.id === 'theme') {
      const selected = this.theme.toggle();
      this.toast.success(`${selected === 'dark' ? 'Dark' : 'Light'} mode enabled.`);
      return;
    }

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
