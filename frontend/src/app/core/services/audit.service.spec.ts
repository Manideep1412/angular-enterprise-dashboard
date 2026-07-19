import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { AuditService } from './audit.service';
import { environment } from '../../../environments/environment';

const BASE = `${environment.apiUrl}/api/v1/audit-logs`;

const MOCK_LOG = {
  id: 1, userEmail: 'a@b.com', action: 'Login', resource: 'Auth',
  resourceId: null, description: 'User logged in', ipAddress: '127.0.0.1',
  severity: 'Low', createdAt: '2024-01-01T00:00:00',
};
const PAGED = { items: [MOCK_LOG], totalCount: 1, page: 1, pageSize: 10, totalPages: 1 };

describe('AuditService', () => {
  let service: AuditService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [AuditService, provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(AuditService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('creates the service', () => expect(service).toBeTruthy());

  it('sends GET to /audit-logs with default page and pageSize', () => {
    service.getLogs().subscribe();
    const req = httpMock.expectOne(r => r.url === BASE);
    expect(req.request.method).toBe('GET');
    expect(req.request.params.get('page')).toBe('1');
    expect(req.request.params.get('pageSize')).toBe('10');
    req.flush({ data: PAGED });
  });

  it('uses provided page and pageSize', () => {
    service.getLogs(3, 25).subscribe();
    const req = httpMock.expectOne(r => r.url === BASE);
    expect(req.request.params.get('page')).toBe('3');
    expect(req.request.params.get('pageSize')).toBe('25');
    req.flush({ data: PAGED });
  });

  it('appends multiple action params', () => {
    service.getLogs(1, 10, ['Login', 'Logout']).subscribe();
    const req = httpMock.expectOne(r => r.url === BASE);
    expect(req.request.params.getAll('action')).toEqual(['Login', 'Logout']);
    req.flush({ data: PAGED });
  });

  it('does not include action param when not provided', () => {
    service.getLogs().subscribe();
    const req = httpMock.expectOne(r => r.url === BASE);
    expect(req.request.params.has('action')).toBeFalse();
    req.flush({ data: PAGED });
  });

  it('appends multiple severity params', () => {
    service.getLogs(1, 10, undefined, ['High', 'Critical']).subscribe();
    const req = httpMock.expectOne(r => r.url === BASE);
    expect(req.request.params.getAll('severity')).toEqual(['High', 'Critical']);
    req.flush({ data: PAGED });
  });

  it('does not include severity param when not provided', () => {
    service.getLogs().subscribe();
    const req = httpMock.expectOne(r => r.url === BASE);
    expect(req.request.params.has('severity')).toBeFalse();
    req.flush({ data: PAGED });
  });

  it('includes search param when provided', () => {
    service.getLogs(1, 10, undefined, undefined, 'failed').subscribe();
    const req = httpMock.expectOne(r => r.url === BASE);
    expect(req.request.params.get('search')).toBe('failed');
    req.flush({ data: PAGED });
  });

  it('does not include search param when not provided', () => {
    service.getLogs().subscribe();
    const req = httpMock.expectOne(r => r.url === BASE);
    expect(req.request.params.has('search')).toBeFalse();
    req.flush({ data: PAGED });
  });

  it('includes sortBy when provided', () => {
    service.getLogs(1, 10, undefined, undefined, undefined, 'severity').subscribe();
    const req = httpMock.expectOne(r => r.url === BASE);
    expect(req.request.params.get('sortBy')).toBe('severity');
    req.flush({ data: PAGED });
  });

  it('includes sortDir when provided', () => {
    service.getLogs(1, 10, undefined, undefined, undefined, 'severity', 'desc').subscribe();
    const req = httpMock.expectOne(r => r.url === BASE);
    expect(req.request.params.get('sortDir')).toBe('desc');
    req.flush({ data: PAGED });
  });

  it('does not include sort params when not provided', () => {
    service.getLogs().subscribe();
    const req = httpMock.expectOne(r => r.url === BASE);
    expect(req.request.params.has('sortBy')).toBeFalse();
    expect(req.request.params.has('sortDir')).toBeFalse();
    req.flush({ data: PAGED });
  });

  it('maps response.data to result', () => {
    let result: any;
    service.getLogs().subscribe(r => (result = r));
    httpMock.expectOne(r => r.url === BASE).flush({ data: PAGED });
    expect(result.totalCount).toBe(1);
    expect(result.items[0].action).toBe('Login');
  });
});
