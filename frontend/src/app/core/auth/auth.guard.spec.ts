import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { signal } from '@angular/core';
import { authGuard, guestGuard, adminGuard } from './auth.guard';
import { AuthService } from './auth.service';

describe('Auth Guards', () => {
  let router: Router;
  let isAuthSignal: ReturnType<typeof signal<boolean>>;
  let isAdminSignal: ReturnType<typeof signal<boolean>>;

  beforeEach(() => {
    isAuthSignal  = signal(false);
    isAdminSignal = signal(false);

    const mockAuth = {
      isAuthenticated: isAuthSignal.asReadonly(),
      isAdmin: isAdminSignal.asReadonly(),
    };

    TestBed.configureTestingModule({
      providers: [
        provideRouter([]),
        { provide: AuthService, useValue: mockAuth },
      ],
    });

    router = TestBed.inject(Router);
  });

  // ── authGuard ─────────────────────────────────────────────────────────────

  describe('authGuard', () => {
    it('returns true when authenticated', () => {
      isAuthSignal.set(true);
      const result = TestBed.runInInjectionContext(() =>
        authGuard({} as any, {} as any)
      );
      expect(result).toBeTrue();
    });

    it('redirects to /login when not authenticated', () => {
      isAuthSignal.set(false);
      const result = TestBed.runInInjectionContext(() =>
        authGuard({} as any, {} as any)
      );
      expect(result).toEqual(router.createUrlTree(['/login']));
    });
  });

  // ── guestGuard ────────────────────────────────────────────────────────────

  describe('guestGuard', () => {
    it('returns true when not authenticated', () => {
      isAuthSignal.set(false);
      const result = TestBed.runInInjectionContext(() =>
        guestGuard({} as any, {} as any)
      );
      expect(result).toBeTrue();
    });

    it('redirects to /dashboard when already authenticated', () => {
      isAuthSignal.set(true);
      const result = TestBed.runInInjectionContext(() =>
        guestGuard({} as any, {} as any)
      );
      expect(result).toEqual(router.createUrlTree(['/dashboard']));
    });
  });

  // ── adminGuard ────────────────────────────────────────────────────────────

  describe('adminGuard', () => {
    it('returns true when user is admin', () => {
      isAdminSignal.set(true);
      const result = TestBed.runInInjectionContext(() =>
        adminGuard({} as any, {} as any)
      );
      expect(result).toBeTrue();
    });

    it('redirects to /dashboard when user is not admin', () => {
      isAdminSignal.set(false);
      const result = TestBed.runInInjectionContext(() =>
        adminGuard({} as any, {} as any)
      );
      expect(result).toEqual(router.createUrlTree(['/dashboard']));
    });
  });
});
