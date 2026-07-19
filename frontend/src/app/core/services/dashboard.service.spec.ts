import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { DashboardService } from './dashboard.service';
import { environment } from '../../../environments/environment';

const STATS_URL = `${environment.apiUrl}/api/v1/dashboard/stats`;

describe('DashboardService', () => {
  let service: DashboardService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [DashboardService, provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(DashboardService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('creates the service', () => expect(service).toBeTruthy());

  it('sends GET to /dashboard/stats', () => {
    service.getStats().subscribe();
    const req = httpMock.expectOne(STATS_URL);
    expect(req.request.method).toBe('GET');
    req.flush({ data: {} });
  });

  it('returns data property from response', () => {
    const mockStats = { kpis: { totalUsers: 42 } };
    let result: any;
    service.getStats().subscribe(r => (result = r));
    httpMock.expectOne(STATS_URL).flush({ data: mockStats });
    expect(result.kpis.totalUsers).toBe(42);
  });

  it('falls back to raw response when data property is absent', () => {
    const raw = { kpis: { totalUsers: 7 } };
    let result: any;
    service.getStats().subscribe(r => (result = r));
    httpMock.expectOne(STATS_URL).flush(raw);
    expect(result.kpis.totalUsers).toBe(7);
  });
});
