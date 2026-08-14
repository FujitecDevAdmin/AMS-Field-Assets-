import { DestroyRef, inject, signal, type Signal } from '@angular/core';

/**
 * A media query as a signal, from the platform's own `matchMedia`.
 *
 * No layout library: the browser already evaluates and re-evaluates these, and
 * a resize listener re-implementing that would run on every pixel of a drag
 * where matchMedia fires once, at the boundary.
 *
 * The breakpoints below are the app's, and they are the same numbers the
 * stylesheets use. A layout that changes at one width in TypeScript and another
 * in CSS has a band where it is neither.
 */
export const BREAKPOINT = {
  /** Phones. The drawer overlaps the content rather than shrinking it. */
  handset: '(max-width: 767.98px)',
  /** Phones and tablets. The drawer starts closed. */
  compact: '(max-width: 1199.98px)',
} as const;

export function mediaQuery(query: string): Signal<boolean> {
  const list = window.matchMedia(query);
  const matches = signal(list.matches);
  const onChange = (event: MediaQueryListEvent): void => matches.set(event.matches);

  list.addEventListener('change', onChange);
  inject(DestroyRef).onDestroy(() => list.removeEventListener('change', onChange));

  return matches.asReadonly();
}
