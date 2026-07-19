import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of, throwError } from 'rxjs';
import { DashboardComponent } from './dashboard.component';
import { DashboardService } from '../../core/services/dashboard.service';
import { AuthService } from '../../core/auth/auth.service';

const MOCK_STATS = {
  kpis: { totalUsers: 42, activeUsers: 30, totalRoles: 5, auditLogsToday: 12 },
  activityChart: [{ date: '2024-01-01', count: 3 }],
  roleDistribution: [{ role: 'Admin', count: 2 }],
  recentActivity: [],
};

describe('DashboardComponent', () => {
  let component: DashboardComponent;
  let fixture: ComponentFixture<DashboardComponent>;
  let dashSpy: jasmine.SpyObj<DashboardService>;
  let authMock: any;

  beforeEach(async () => {
    dashSpy = jasmine.createSpyObj<DashboardService>('DashboardService', ['getStats']);
    dashSpy.getStats.and.returnValue(of(MOCK_STATS));

    authMock = { user: jasmine.createSpy('user').and.returnValue(null) };

    await TestBed.configureTestingModule({
      imports: [DashboardComponent],
      providers: [
        provideRouter([]),
        { provide: DashboardService, useValue: dashSpy },
        { provide: AuthService, useValue: authMock },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(DashboardComponent);
    component = fixture.componentInstance;
    // Spy on initCharts to prevent Chart.js canvas operations in tests
    spyOn<any>(component, 'initCharts');
    fixture.detectChanges();
  });

  it('creates the component', () => expect(component).toBeTruthy());

  // ── ngOnInit ──────────────────────────────────────────────────────────────

  it('calls getStats on init', () => {
    expect(dashSpy.getStats).toHaveBeenCalled();
  });

  it('sets stats signal after successful load', () => {
    expect(component.stats()).toEqual(MOCK_STATS);
  });

  it('sets loading to false after successful load', () => {
    expect(component.loading()).toBeFalse();
  });

  it('sets loading to false on error', async () => {
    dashSpy.getStats.and.returnValue(throwError(() => new Error('fail')));
    const fix = TestBed.createComponent(DashboardComponent);
    spyOn<any>(fix.componentInstance, 'initCharts');
    fix.detectChanges();
    expect(fix.componentInstance.loading()).toBeFalse();
  });

  // ── firstName getter ──────────────────────────────────────────────────────

  it('firstName returns empty string when user is null', () => {
    authMock.user.and.returnValue(null);
    expect(component.firstName).toBe('');
  });

  it('firstName returns first part of fullName', () => {
    authMock.user.and.returnValue({ fullName: 'Jane Doe' });
    expect(component.firstName).toBe('Jane');
  });

  it('firstName handles single-word fullName', () => {
    authMock.user.and.returnValue({ fullName: 'Admin' });
    expect(component.firstName).toBe('Admin');
  });

  // ── timeOfDay getter ──────────────────────────────────────────────────────

  it('timeOfDay returns "morning" for hour < 12', () => {
    spyOn(Date.prototype, 'getHours').and.returnValue(9);
    expect(component.timeOfDay).toBe('morning');
  });

  it('timeOfDay returns "afternoon" for hour 12–16', () => {
    spyOn(Date.prototype, 'getHours').and.returnValue(14);
    expect(component.timeOfDay).toBe('afternoon');
  });

  it('timeOfDay returns "evening" for hour >= 17', () => {
    spyOn(Date.prototype, 'getHours').and.returnValue(20);
    expect(component.timeOfDay).toBe('evening');
  });

  // ── formatDate() ──────────────────────────────────────────────────────────

  it('formatDate returns time string for a date', () => {
    const result = component.formatDate('2024-06-01T14:30:00');
    expect(result).toMatch(/\d{2}:\d{2}/);
  });

  // ── ngOnDestroy ───────────────────────────────────────────────────────────

  it('ngOnDestroy clears the chart timer', () => {
    spyOn(window, 'clearTimeout');
    component.ngOnDestroy();
    expect(clearTimeout).toHaveBeenCalled();
  });
});
