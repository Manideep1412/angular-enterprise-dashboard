import { Injectable, signal, effect } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class ThemeService {
  private readonly KEY = 'ed_theme';

  /** true = dark, false = light */
  isDark = signal<boolean>(this.getInitial());

  constructor() {
    // Apply theme attribute to <html> and persist whenever signal changes
    effect(() => {
      const dark = this.isDark();
      document.documentElement.setAttribute('data-theme', dark ? 'dark' : 'light');
      localStorage.setItem(this.KEY, dark ? 'dark' : 'light');
    });
  }

  toggle() { this.isDark.update(v => !v); }

  private getInitial(): boolean {
    const saved = localStorage.getItem(this.KEY);
    if (saved === 'light') return false;
    if (saved === 'dark')  return true;
    // Fall back to OS preference
    return window.matchMedia('(prefers-color-scheme: dark)').matches;
  }
}
