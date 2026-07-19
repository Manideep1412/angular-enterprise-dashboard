import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { RoleService } from './role.service';
import { environment } from '../../../environments/environment';

const BASE = `${environment.apiUrl}/api/v1/roles`;

const MOCK_ROLES = [
  { id: 1, name: 'Admin', description: 'Administrator', color: '#f00', permissions: [], createdAt: '2024-01-01T00:00:00' },
  { id: 2, name: 'Manager', description: 'Manager role', color: '#0f0', permissions: [], createdAt: '2024-01-02T00:00:00' },
];

describe('RoleService', () => {
  let service: RoleService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [RoleService, provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(RoleService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('creates the service', () => expect(service).toBeTruthy());

  it('sends GET to /roles', () => {
    service.getRoles().subscribe();
    const req = httpMock.expectOne(BASE);
    expect(req.request.method).toBe('GET');
    req.flush({ data: MOCK_ROLES });
  });

  it('maps response.data to role array', () => {
    let result: any;
    service.getRoles().subscribe(r => (result = r));
    httpMock.expectOne(BASE).flush({ data: MOCK_ROLES });
    expect(result.length).toBe(2);
    expect(result[0].name).toBe('Admin');
    expect(result[1].name).toBe('Manager');
  });
});
