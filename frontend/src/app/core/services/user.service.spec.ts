import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { UserService } from './user.service';
import { environment } from '../../../environments/environment';

const BASE = `${environment.apiUrl}/api/v1/users`;

const MOCK_USER = {
  id: 1, firstName: 'Jane', lastName: 'Doe', fullName: 'Jane Doe',
  email: 'jane@x.com', status: 'Active' as const, roles: [], createdAt: '2024-01-01T00:00:00',
};
const PAGED = { items: [MOCK_USER], totalCount: 1, page: 1, pageSize: 10, totalPages: 1 };

describe('UserService', () => {
  let service: UserService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [UserService, provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(UserService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('creates the service', () => expect(service).toBeTruthy());

  // ── getUsers() ─────────────────────────────────────────────────────────────

  describe('getUsers()', () => {
    it('sends GET with default page / pageSize', () => {
      service.getUsers().subscribe();
      const req = httpMock.expectOne(r => r.url === BASE);
      expect(req.request.method).toBe('GET');
      expect(req.request.params.get('page')).toBe('1');
      expect(req.request.params.get('pageSize')).toBe('10');
      req.flush({ data: PAGED, success: true });
    });

    it('includes search param when provided', () => {
      service.getUsers(1, 10, 'alice').subscribe();
      const req = httpMock.expectOne(r => r.url === BASE);
      expect(req.request.params.get('search')).toBe('alice');
      req.flush({ data: PAGED, success: true });
    });

    it('does not include search param when omitted', () => {
      service.getUsers(1, 10).subscribe();
      const req = httpMock.expectOne(r => r.url === BASE);
      expect(req.request.params.has('search')).toBeFalse();
      req.flush({ data: PAGED, success: true });
    });

    it('appends multiple status params', () => {
      service.getUsers(1, 10, undefined, ['Active', 'Inactive']).subscribe();
      const req = httpMock.expectOne(r => r.url === BASE);
      expect(req.request.params.getAll('status')).toEqual(['Active', 'Inactive']);
      req.flush({ data: PAGED, success: true });
    });

    it('appends multiple role params', () => {
      service.getUsers(1, 10, undefined, undefined, ['Admin', 'Manager']).subscribe();
      const req = httpMock.expectOne(r => r.url === BASE);
      expect(req.request.params.getAll('role')).toEqual(['Admin', 'Manager']);
      req.flush({ data: PAGED, success: true });
    });

    it('includes sortBy and sortDir when provided', () => {
      service.getUsers(1, 10, undefined, undefined, undefined, 'email', 'desc').subscribe();
      const req = httpMock.expectOne(r => r.url === BASE);
      expect(req.request.params.get('sortBy')).toBe('email');
      expect(req.request.params.get('sortDir')).toBe('desc');
      req.flush({ data: PAGED, success: true });
    });

    it('does not include sort params when omitted', () => {
      service.getUsers().subscribe();
      const req = httpMock.expectOne(r => r.url === BASE);
      expect(req.request.params.has('sortBy')).toBeFalse();
      expect(req.request.params.has('sortDir')).toBeFalse();
      req.flush({ data: PAGED, success: true });
    });

    it('maps response.data to result', () => {
      let result: any;
      service.getUsers().subscribe(r => (result = r));
      httpMock.expectOne(r => r.url === BASE).flush({ data: PAGED, success: true });
      expect(result.totalCount).toBe(1);
      expect(result.items[0].email).toBe('jane@x.com');
    });
  });

  // ── getById() ──────────────────────────────────────────────────────────────

  describe('getById()', () => {
    it('sends GET to /users/:id', () => {
      service.getById(7).subscribe();
      const req = httpMock.expectOne(`${BASE}/7`);
      expect(req.request.method).toBe('GET');
      req.flush({ data: MOCK_USER, success: true });
    });

    it('maps response.data', () => {
      let result: any;
      service.getById(1).subscribe(r => (result = r));
      httpMock.expectOne(`${BASE}/1`).flush({ data: MOCK_USER, success: true });
      expect(result.email).toBe('jane@x.com');
    });
  });

  // ── create() ───────────────────────────────────────────────────────────────

  describe('create()', () => {
    it('sends POST with request body', () => {
      const body = { firstName: 'Jane', lastName: 'Doe', email: 'jane@x.com', password: 'P@ss1', roleIds: [1] };
      service.create(body).subscribe();
      const req = httpMock.expectOne(BASE);
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual(body);
      req.flush({ data: MOCK_USER, success: true });
    });

    it('maps response.data', () => {
      let result: any;
      service.create({ firstName: 'J', lastName: 'D', email: 'j@x.com', password: 'P', roleIds: [] }).subscribe(r => (result = r));
      httpMock.expectOne(BASE).flush({ data: MOCK_USER, success: true });
      expect(result.id).toBe(1);
    });
  });

  // ── update() ───────────────────────────────────────────────────────────────

  describe('update()', () => {
    it('sends PUT to /users/:id', () => {
      const body = { firstName: 'Jane', lastName: 'Doe', status: 'Active', roleIds: [] };
      service.update(3, body).subscribe();
      const req = httpMock.expectOne(`${BASE}/3`);
      expect(req.request.method).toBe('PUT');
      expect(req.request.body).toEqual(body);
      req.flush({ data: MOCK_USER, success: true });
    });
  });

  // ── delete() ───────────────────────────────────────────────────────────────

  describe('delete()', () => {
    it('sends DELETE to /users/:id', () => {
      service.delete(5).subscribe();
      const req = httpMock.expectOne(`${BASE}/5`);
      expect(req.request.method).toBe('DELETE');
      req.flush(null);
    });
  });
});
