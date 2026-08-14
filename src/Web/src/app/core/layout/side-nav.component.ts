import {
  ChangeDetectionStrategy,
  Component,
  computed,
  effect,
  inject,
  input,
  output,
  signal,
  viewChild,
} from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { NavigationEnd, Router, RouterLink } from '@angular/router';
import { DxTreeViewComponent, DxTreeViewModule } from 'devextreme-angular/ui/tree-view';
import type { ItemClickEvent } from 'devextreme/ui/tree_view';
import { filter, map } from 'rxjs';

import {
  buildNavTree,
  DEFAULT_EXPANDED,
  groupIdForPath,
  type NavBadges,
  type NavNode,
} from './nav-items';

const PINS_KEY = 'ams.nav.pins';

function loadPins(): string[] {
  try {
    const raw = localStorage.getItem(PINS_KEY);
    if (raw === null) {
      return [];
    }
    const parsed: unknown = JSON.parse(raw);
    return Array.isArray(parsed) ? parsed.filter((v): v is string => typeof v === 'string') : [];
  } catch {
    // A corrupt or unavailable store is not worth failing the shell over.
    return [];
  }
}

function savePins(pins: readonly string[]): void {
  try {
    localStorage.setItem(PINS_KEY, JSON.stringify(pins));
  } catch {
    // Private mode or a full quota — the pins just do not survive the session.
  }
}

/**
 * The module navigation. A tree because the catalogue is grouped and a flat
 * list of seventeen modules is a scroll, not a menu.
 *
 * Three things make it usable at that size: the tree's own search filters it,
 * any screen can be pinned to a group at the top, and counts roll up so a
 * collapsed group still says something is waiting inside it.
 */
@Component({
  selector: 'ams-side-nav',
  imports: [DxTreeViewModule, RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './side-nav.component.html',
  styleUrl: './side-nav.component.scss',
})
export class SideNavComponent {
  /** Collapsed rail vs full-width drawer — changes what a row shows. */
  readonly compact = input(false);

  /** Counts keyed by nav id. Groups display the sum of their children's. */
  readonly badges = input<NavBadges>({});

  /** Emitted on a leaf click so the shell can close an overlay drawer. */
  readonly navigated = output<NavNode>();

  private readonly router = inject(Router);
  private readonly pins = signal<readonly string[]>(loadPins());
  private readonly tree = viewChild<DxTreeViewComponent>('tree');

  /** The route showing now. Selection follows this, not the last row clicked —
      which is what makes a deep link, a browser Back and a nav click all land
      on the same highlighted row. */
  private readonly activePath = toSignal(
    this.router.events.pipe(
      filter((e): e is NavigationEnd => e instanceof NavigationEnd),
      map((e) => e.urlAfterRedirects),
    ),
    { initialValue: this.router.url },
  );

  /** Which groups are open, held HERE rather than in the tree widget: `items`
      is rebuilt whenever a pin or a badge changes, and the widget drops its own
      expansion state on every rebuild. */
  private readonly expanded = signal<ReadonlySet<string>>(new Set(DEFAULT_EXPANDED));

  protected readonly items = computed(() =>
    buildNavTree({
      pinned: new Set(this.pins()),
      badges: this.badges(),
      expanded: this.expanded(),
      activePath: this.activePath(),
    }),
  );

  constructor() {
    /* Opening a screen opens its section — once, on arrival. Folding this into
       the computed instead would make the active group impossible to collapse,
       because every rebuild would re-open it under the user. */
    effect(() => {
      const owner = groupIdForPath(this.activePath());
      if (owner === undefined) {
        return;
      }
      this.expanded.update((open) => (open.has(owner) ? open : new Set(open).add(owner)));
    });
  }

  protected onExpanded(e: { itemData?: NavNode }): void {
    const id = e.itemData?.id;
    if (id !== undefined) {
      this.expanded.update((open) => new Set(open).add(id));
    }
  }

  protected onCollapsed(e: { itemData?: NavNode }): void {
    const id = e.itemData?.id;
    if (id !== undefined) {
      this.expanded.update((open) => {
        const next = new Set(open);
        next.delete(id);
        return next;
      });
    }
  }

  protected onItemClick(e: ItemClickEvent): void {
    /*
     * The pin button lives inside the row, and stopping propagation on its own
     * click is not enough: the tree binds its own pointer handler, which has
     * already decided this was a row click by the time Angular's listener runs.
     * So the row handler asks where the click landed instead.
     */
    const target = e.event?.target as HTMLElement | undefined;
    if (target?.closest('.nav-row__pin')) {
      return;
    }

    const item = e.itemData as NavNode | undefined;
    if (!item) {
      return;
    }

    /* Groups are handled by the tree's own expandEvent, and recorded in
       onExpanded/onCollapsed. Nothing to do here — a group is not a
       destination. */
    if (!item.path) {
      return;
    }

    void this.router.navigateByUrl(item.path);
    this.navigated.emit(item);
  }

  /**
   * The pin button sits inside the row, so its click has to be stopped from
   * reaching the row — otherwise pinning also navigates.
   */
  protected togglePin(item: NavNode, event: MouseEvent): void {
    event.stopPropagation();
    event.preventDefault();

    this.pins.update((current) => {
      const next = current.includes(item.key)
        ? current.filter((key) => key !== item.key)
        : [...current, item.key];
      savePins(next);
      return next;
    });
  }
}
