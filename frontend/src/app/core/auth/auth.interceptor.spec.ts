import { TestBed } from '@angular/core/testing';
import { provideHttpClient, withInterceptors, HttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { signal } from '@angular/core';
import { authInterceptor } from './auth.interceptor';
import { AuthService } from './auth.service';

function setup(tokenValue: string | null) {
  const mockAuth = {
    token: signal(tokenValue).asReadonly(),
    logout: jasmine.createSpy('logout'),
  };

  TestBed.configureTestingModule({
    providers: [
      provideHttpClient(withInterceptors([authInterceptor])),
      provideHttpClientTesting(),
      { provide: AuthService, useValue: mockAuth },
    ],
  });

  return {
    http: TestBed.inject(HttpClient),
    httpMock: TestBed.inject(HttpTestingController),
    mockAuth: mockAuth as any,
  };
}

describe('authInterceptor', () => {
  afterEach(() => TestBed.inject(HttpTestingController).verify());

  it('adds Authorization header when token is present', () => {
    const { http, httpMock } = setup('my-jwt');
    http.get('/api/data').subscribe();
    const req = httpMock.expectOne('/api/data');
    expect(req.request.headers.get('Authorization')).toBe('Bearer my-jwt');
    req.flush({});
  });

  it('does not add Authorization header when token is null', () => {
    const { http, httpMock } = setup(null);
    http.get('/api/data').subscribe();
    const req = httpMock.expectOne('/api/data');
    expect(req.request.headers.has('Authorization')).toBeFalse();
    req.flush({});
  });

  it('calls auth.logout() on 401 response', () => {
    const { http, httpMock, mockAuth } = setup('expired');
    http.get('/api/data').subscribe({ error: () => {} });
    httpMock.expectOne('/api/data').flush({}, { status: 401, statusText: 'Unauthorized' });
    expect(mockAuth.logout).toHaveBeenCalled();
  });

  it('rethrows error on non-401 status without calling logout', () => {
    const { http, httpMock, mockAuth } = setup('token');
    let errored = false;
    http.get('/api/data').subscribe({ error: () => (errored = true) });
    httpMock.expectOne('/api/data').flush({}, { status: 500, statusText: 'Server Error' });
    expect(errored).toBeTrue();
    expect(mockAuth.logout).not.toHaveBeenCalled();
  });
});
