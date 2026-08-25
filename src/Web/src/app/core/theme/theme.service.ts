import { DOCUMENT } from '@angular/common';
import { Injectable, inject, signal } from '@angular/core';
import themes from 'devextreme/ui/themes';

const THEME_KEY = 'ams-admin-theme';
export type AppTheme = 'light' | 'dark';

@Injectable({ providedIn: 'root' })
export class ThemeService {
  private readonly document = inject(DOCUMENT);
  readonly current = signal<AppTheme>(this.readStoredTheme());

  constructor() {
    this.apply(this.current());
  }

  toggle(): AppTheme {
    const next: AppTheme = this.current() === 'dark' ? 'light' : 'dark';
    this.apply(next);
    return next;
  }

  private apply(theme: AppTheme): void {
    this.current.set(theme);
    this.document.documentElement.dataset['amsTheme'] = theme;
    this.document.body.classList.toggle('ams-dark', theme === 'dark');
    this.document.body.classList.toggle('ams-light', theme === 'light');
    themes.current(`material.orange.${theme}`);
    try {
      localStorage.setItem(THEME_KEY, theme);
    } catch {
      // Storage may be unavailable in hardened/private browser contexts.
    }
  }

  private readStoredTheme(): AppTheme {
    try {
      return localStorage.getItem(THEME_KEY) === 'dark' ? 'dark' : 'light';
    } catch {
      return 'light';
    }
  }
}
