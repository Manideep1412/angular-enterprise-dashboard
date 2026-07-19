import { TestBed } from '@angular/core/testing';
import { ThemeService } from './theme.service';

describe('ThemeService', () => {
  let store: Record<string, string>;

  beforeEach(() => {
    store = {};
    spyOn(localStorage, 'getItem').and.callFake((k: string) => store[k] ?? null);
    spyOn(localStorage, 'setItem').and.callFake((k: string, v: string) => { store[k] = v; });
    spyOn(document.documentElement, 'setAttribute');
  });

  function makeService(): ThemeService {
    TestBed.configureTestingModule({ providers: [ThemeService] });
    const svc = TestBed.inject(ThemeService);
    TestBed.flushEffects();   // run the effect registered in constructor
    return svc;
  }

  // ── getInitial() ──────────────────────────────────────────────────────────

  it('isDark defaults to false when OS is light and no storage', () => {
    spyOn(window, 'matchMedia').and.returnValue({ matches: false } as MediaQueryList);
    const svc = makeService();
    expect(svc.isDark()).toBeFalse();
  });

  it('isDark defaults to true when OS prefers dark', () => {
    spyOn(window, 'matchMedia').and.returnValue({ matches: true } as MediaQueryList);
    const svc = makeService();
    expect(svc.isDark()).toBeTrue();
  });

  it('reads "dark" from localStorage overriding OS preference', () => {
    store['ed_theme'] = 'dark';
    spyOn(window, 'matchMedia').and.returnValue({ matches: false } as MediaQueryList);
    const svc = makeService();
    expect(svc.isDark()).toBeTrue();
  });

  it('reads "light" from localStorage overriding OS preference', () => {
    store['ed_theme'] = 'light';
    spyOn(window, 'matchMedia').and.returnValue({ matches: true } as MediaQueryList);
    const svc = makeService();
    expect(svc.isDark()).toBeFalse();
  });

  // ── effect() ──────────────────────────────────────────────────────────────

  it('sets data-theme attribute on construction', () => {
    spyOn(window, 'matchMedia').and.returnValue({ matches: false } as MediaQueryList);
    makeService();
    expect(document.documentElement.setAttribute)
      .toHaveBeenCalledWith('data-theme', 'light');
  });

  it('persists theme to localStorage on construction', () => {
    spyOn(window, 'matchMedia').and.returnValue({ matches: false } as MediaQueryList);
    makeService();
    expect(localStorage.setItem).toHaveBeenCalledWith('ed_theme', 'light');
  });

  // ── toggle() ──────────────────────────────────────────────────────────────

  it('toggle() flips isDark from false to true', () => {
    spyOn(window, 'matchMedia').and.returnValue({ matches: false } as MediaQueryList);
    const svc = makeService();
    svc.toggle();
    TestBed.flushEffects();
    expect(svc.isDark()).toBeTrue();
  });

  it('toggle() flips isDark from true to false', () => {
    spyOn(window, 'matchMedia').and.returnValue({ matches: true } as MediaQueryList);
    const svc = makeService();
    svc.toggle();
    TestBed.flushEffects();
    expect(svc.isDark()).toBeFalse();
  });

  it('toggle() updates data-theme attribute', () => {
    spyOn(window, 'matchMedia').and.returnValue({ matches: false } as MediaQueryList);
    const svc = makeService();
    svc.toggle();
    TestBed.flushEffects();
    expect(document.documentElement.setAttribute)
      .toHaveBeenCalledWith('data-theme', 'dark');
  });

  it('toggle() persists new theme to localStorage', () => {
    spyOn(window, 'matchMedia').and.returnValue({ matches: false } as MediaQueryList);
    const svc = makeService();
    svc.toggle();
    TestBed.flushEffects();
    expect(localStorage.setItem).toHaveBeenCalledWith('ed_theme', 'dark');
  });
});
