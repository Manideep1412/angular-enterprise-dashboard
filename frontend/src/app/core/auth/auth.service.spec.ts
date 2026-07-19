import { TestBed, fakeAsync, tick } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { Router } from '@angular/router';
import { AuthService } from './auth.service';
import { environment } from '../../../environments/environment';

const LOGIN_URL = `${environment.apiUrl}/api/v1/auth/login`;
const ADMIN_USER = { id: 1, fullName: 'Admin User', email: 'admin@x.com', department: 'IT', roles: ['Admin'] };
const SUCCESS_RESPONSE = {
  success: true,
  data: { accessToken: 'jwt-token', tokenType: 'Bearer', expiresIn: 3600, user: ADMIN_USER },
};

describe('AuthService', () => {
  let service: AuthService;
  let httpMock: HttpTestingController;
  let router: Router;
  let store: Record<string, string>;

  beforeEach(() => {
    store = {};
    spyOn(localStorage, 'getItem').and.callFake((k: string) => store[k] ?? null);
    spyOn(localStorage, 'setItem').and.callFake((k: string, v: string) => { store[k] = v; });
    spyOn(localStorage, 'removeItem').and.callFake((k: string) => { delete store[k]; });

    TestBed.configureTestingModule({
      providers: [
        AuthService,
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
      ],
    });

    service = TestBed.inject(AuthService);
    httpMock = TestBed.inject(HttpTestingController);
    router = TestBed.inject(Router);
    spyOn(router, 'navigate').and.returnValue(Promise.resolve(true));
  });

  afterEach(() => httpMock.verify());

  // ── Initial state ─────────────────────────────────────────────────────────

  it('creates the service', () => expect(service).toBeTruthy());

  it('token() is null with empty storage', () => expect(service.token()).toBeNull());
  it('user() is null with empty storage', () => expect(service.user()).toBeNull());
  it('isAuthenticated() is false with no token', () => expect(service.isAuthenticated()).toBeFalse());
  it('isLoading() starts as false', () => expect(service.isLoading()).toBeFalse());
  it('error() starts as null', () => expect(service.error()).toBeNull());
  it('currentRoles() is empty with no user', () => expect(service.currentRoles()).toEqual([]));
  it('isAdmin() is false with no user', () => expect(service.isAdmin()).toBeFalse());
  it('isManagerOrAdmin() is false with no user', () => expect(service.isManagerOrAdmin()).toBeFalse());

  it('loads token and user from localStorage on init', () => {
    store['ed_access_token'] = 'stored-token';
    store['ed_user'] = JSON.stringify(ADMIN_USER);

    TestBed.resetTestingModule();
    TestBed.configureTestingModule({
      providers: [AuthService, provideHttpClient(), provideHttpClientTesting(), provideRouter([])],
    });
    const fresh = TestBed.inject(AuthService);
    expect(fresh.token()).toBe('stored-token');
    expect(fresh.user()?.fullName).toBe('Admin User');
    expect(fresh.isAuthenticated()).toBeTrue();
    TestBed.inject(HttpTestingController).verify();
  });

  // ── login() ───────────────────────────────────────────────────────────────

  describe('login()', () => {
    it('POSTs to the login endpoint', () => {
      service.login({ email: 'admin@x.com', password: 'pass' }).subscribe();
      const req = httpMock.expectOne(LOGIN_URL);
      expect(req.request.method).toBe('POST');
      req.flush(SUCCESS_RESPONSE);
    });

    it('sets isLoading to true during request', () => {
      service.login({ email: 'admin@x.com', password: 'pass' }).subscribe();
      expect(service.isLoading()).toBeTrue();
      httpMock.expectOne(LOGIN_URL).flush(SUCCESS_RESPONSE);
    });

    it('clears isLoading after success', fakeAsync(() => {
      service.login({ email: 'admin@x.com', password: 'pass' }).subscribe();
      httpMock.expectOne(LOGIN_URL).flush(SUCCESS_RESPONSE);
      tick();
      expect(service.isLoading()).toBeFalse();
    }));

    it('stores token and user signals on success', () => {
      service.login({ email: 'admin@x.com', password: 'pass' }).subscribe();
      httpMock.expectOne(LOGIN_URL).flush(SUCCESS_RESPONSE);
      expect(service.token()).toBe('jwt-token');
      expect(service.user()?.email).toBe('admin@x.com');
      expect(service.isAuthenticated()).toBeTrue();
    });

    it('persists token and user to localStorage', () => {
      service.login({ email: 'admin@x.com', password: 'pass' }).subscribe();
      httpMock.expectOne(LOGIN_URL).flush(SUCCESS_RESPONSE);
      expect(localStorage.setItem).toHaveBeenCalledWith('ed_access_token', 'jwt-token');
      expect(localStorage.setItem).toHaveBeenCalledWith('ed_user', jasmine.any(String));
    });

    it('navigates to /dashboard on success', fakeAsync(() => {
      service.login({ email: 'admin@x.com', password: 'pass' }).subscribe();
      httpMock.expectOne(LOGIN_URL).flush(SUCCESS_RESPONSE);
      tick();
      expect(router.navigate).toHaveBeenCalledWith(['/dashboard']);
    }));

    it('does not store auth when success=false', () => {
      service.login({ email: 'a@x.com', password: 'b' }).subscribe();
      httpMock.expectOne(LOGIN_URL).flush({ success: false, data: null });
      expect(service.token()).toBeNull();
    });

    it('sets error message from server on HTTP error', fakeAsync(() => {
      service.login({ email: 'bad@x.com', password: 'wrong' }).subscribe({ error: () => {} });
      httpMock.expectOne(LOGIN_URL).flush(
        { message: 'Invalid credentials' },
        { status: 401, statusText: 'Unauthorized' }
      );
      tick();
      expect(service.error()).toBe('Invalid credentials');
    }));

    it('uses fallback error message when server sends no message', fakeAsync(() => {
      service.login({ email: 'a@x.com', password: 'b' }).subscribe({ error: () => {} });
      httpMock.expectOne(LOGIN_URL).flush({}, { status: 500, statusText: 'Server Error' });
      tick();
      expect(service.error()).toBe('Invalid email or password');
    }));

    it('clears isLoading after error', fakeAsync(() => {
      service.login({ email: 'a@x.com', password: 'b' }).subscribe({ error: () => {} });
      httpMock.expectOne(LOGIN_URL).flush({}, { status: 401, statusText: 'Unauthorized' });
      tick();
      expect(service.isLoading()).toBeFalse();
    }));

    it('clears prior error before new login attempt', () => {
      // Simulate a prior error state via another login
      service.login({ email: 'a@x.com', password: 'b' }).subscribe({ error: () => {} });
      httpMock.expectOne(LOGIN_URL).flush({}, { status: 401, statusText: 'Unauthorized' });

      service.login({ email: 'a@x.com', password: 'b' }).subscribe();
      // error should be null as soon as the second login starts
      expect(service.error()).toBeNull();
      httpMock.expectOne(LOGIN_URL).flush(SUCCESS_RESPONSE);
    });

    it('sets isAdmin() when user has Admin role', () => {
      service.login({ email: 'admin@x.com', password: 'pass' }).subscribe();
      httpMock.expectOne(LOGIN_URL).flush(SUCCESS_RESPONSE);
      expect(service.isAdmin()).toBeTrue();
    });

    it('sets isManagerOrAdmin() when user has Manager role', () => {
      const mgrUser = { ...ADMIN_USER, roles: ['Manager'] };
      service.login({ email: 'mgr@x.com', password: 'pass' }).subscribe();
      httpMock.expectOne(LOGIN_URL).flush({
        success: true,
        data: { ...SUCCESS_RESPONSE.data, user: mgrUser },
      });
      expect(service.isManagerOrAdmin()).toBeTrue();
    });

    it('isAdmin() is false when user has only Manager role', () => {
      const mgrUser = { ...ADMIN_USER, roles: ['Manager'] };
      service.login({ email: 'mgr@x.com', password: 'pass' }).subscribe();
      httpMock.expectOne(LOGIN_URL).flush({
        success: true,
        data: { ...SUCCESS_RESPONSE.data, user: mgrUser },
      });
      expect(service.isAdmin()).toBeFalse();
    });
  });

  // ── logout() ──────────────────────────────────────────────────────────────

  describe('logout()', () => {
    beforeEach(() => {
      // Populate state first
      service.login({ email: 'admin@x.com', password: 'pass' }).subscribe();
      httpMock.expectOne(LOGIN_URL).flush(SUCCESS_RESPONSE);
    });

    it('clears token signal', () => {
      service.logout();
      expect(service.token()).toBeNull();
    });

    it('clears user signal', () => {
      service.logout();
      expect(service.user()).toBeNull();
    });

    it('isAuthenticated() becomes false', () => {
      service.logout();
      expect(service.isAuthenticated()).toBeFalse();
    });

    it('removes both localStorage keys', () => {
      service.logout();
      expect(localStorage.removeItem).toHaveBeenCalledWith('ed_access_token');
      expect(localStorage.removeItem).toHaveBeenCalledWith('ed_user');
    });

    it('navigates to /login', fakeAsync(() => {
      service.logout();
      tick();
      expect(router.navigate).toHaveBeenCalledWith(['/login']);
    }));
  });

  // ── hasRole() ─────────────────────────────────────────────────────────────

  describe('hasRole()', () => {
    it('returns false when no user is logged in', () => {
      expect(service.hasRole('Admin')).toBeFalse();
    });

    it('returns true for a role the user has', () => {
      service.login({ email: 'admin@x.com', password: 'pass' }).subscribe();
      httpMock.expectOne(LOGIN_URL).flush(SUCCESS_RESPONSE);
      expect(service.hasRole('Admin')).toBeTrue();
    });

    it('returns false for a role the user does not have', () => {
      service.login({ email: 'admin@x.com', password: 'pass' }).subscribe();
      httpMock.expectOne(LOGIN_URL).flush(SUCCESS_RESPONSE);
      expect(service.hasRole('Viewer')).toBeFalse();
    });
  });
});
