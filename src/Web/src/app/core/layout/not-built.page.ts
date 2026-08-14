import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { NavigationEnd, Router } from '@angular/router';
import { filter, map } from 'rxjs';

import { NAV_ITEMS } from './nav-items';

/**
 * Stands in for every module route until that module has screens.
 *
 * Without it the navigation is seventeen links to nowhere: Angular answers each
 * one with `NG04002: Cannot match any routes`, which reaches the user as a dead
 * click and the developer as a console error on a menu that looks finished.
 */
@Component({
  selector: 'ams-not-built',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <section class="not-built">
      <i class="dx-icon dx-icon-toolbox not-built__icon"></i>
      <h1 class="not-built__title">{{ title() }}</h1>
      <p class="not-built__body">
        This module has no screens yet. The navigation lists it so the shape of the system is
        visible; the screens arrive with their command slices.
      </p>
      <code class="not-built__route">{{ url() }}</code>
    </section>
  `,
  styles: `
    .not-built {
      display: flex;
      flex-direction: column;
      align-items: center;
      justify-content: center;
      gap: 0.5rem;
      min-height: 60vh;
      text-align: center;
    }

    .not-built__icon {
      font-size: 40px;
      width: 40px;
      height: 40px;
      line-height: 40px;
      color: var(--ams-text-muted);
      opacity: 0.5;
    }

    .not-built__title {
      margin: 0.5rem 0 0;
      font-size: 1.25rem;
      font-weight: 600;
    }

    .not-built__body {
      margin: 0;
      max-width: 46ch;
      color: var(--ams-text-muted);
    }

    .not-built__route {
      margin-top: 0.5rem;
      padding: 0.125rem 0.5rem;
      border: 1px solid var(--ams-border);
      border-radius: 4px;
      font-size: 0.75rem;
      color: var(--ams-text-muted);
    }
  `,
})
export class NotBuiltPage {
  private readonly router = inject(Router);

  protected readonly url = toSignal(
    this.router.events.pipe(
      filter((e): e is NavigationEnd => e instanceof NavigationEnd),
      map((e) => e.urlAfterRedirects),
    ),
    { initialValue: this.router.url },
  );

  /** The menu already knows what every route is called; ask it rather than
      title-casing the URL and hoping. */
  protected readonly title = computed(() => {
    const path = this.url();
    for (const group of NAV_ITEMS) {
      for (const item of group.items ?? [group]) {
        if (item.path === path) {
          return item.text;
        }
      }
    }
    return 'Not built yet';
  });
}
