import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of, throwError } from 'rxjs';
import { AuditLogsComponent } from './audit-logs.component';
import { AuditService } from '../../core/services/audit.service';

const MOCK_LOG: any = {
  id: 1, userEmail: 'a@b.com', action: 'Login', resource: 'Auth',
  resourceId: null, description: 'Signed in', ipAddress: '127.0.0.1',
  severity: 'Info', createdAt: '2024-01-01T10:00:00',
};
const PAGED = { items: [MOCK_LOG], totalCount: 1, page: 1, pageSize: 10, totalPages: 1 };
const EMPTY_PAGED = { items: [], totalCount: 0, page: 1, pageSize: 10, totalPages: 1 };

describe('AuditLogsComponent', () => {
  let component: AuditLogsComponent;
  let fixture: ComponentFixture<AuditLogsComponent>;
  let auditSpy: jasmine.SpyObj<AuditService>;

  beforeEach(async () => {
    auditSpy = jasmine.createSpyObj<AuditService>('AuditService', ['getLogs']);
    auditSpy.getLogs.and.returnValue(of(PAGED));

    await TestBed.configureTestingModule({
      imports: [AuditLogsComponent],
      providers: [
        provideRouter([]),
        { provide: AuditService, useValue: auditSpy },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(AuditLogsComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('creates the component', () => expect(component).toBeTruthy());

  // ── ngOnInit ──────────────────────────────────────────────────────────────

  it('loads logs on init', () => {
    expect(auditSpy.getLogs).toHaveBeenCalled();
  });

  it('sets result signal after load', () => {
    expect(component.result()?.totalCount).toBe(1);
  });

  it('sets loading to false after success', () => {
    expect(component.loading()).toBeFalse();
  });

  it('sets loading to false on error', () => {
    auditSpy.getLogs.and.returnValue(throwError(() => new Error('fail')));
    component.loadLogs();
    expect(component.loading()).toBeFalse();
  });

  // ── setSort() ─────────────────────────────────────────────────────────────
  // Note: initial dir is 'desc'; setSort toggles to opposite when same field

  it('setSort sets new field to desc by default', () => {
    component.sort.set({ field: 'createdat', dir: 'asc' }); // start with asc
    component.setSort('severity');  // different field → goes to desc
    expect(component.sort().field).toBe('severity');
    expect(component.sort().dir).toBe('desc');
  });

  it('setSort toggles desc → asc on the same field', () => {
    component.sort.set({ field: 'severity', dir: 'desc' });
    component.setSort('severity');
    expect(component.sort().dir).toBe('asc');
  });

  it('setSort resets page to 1', () => {
    component.currentPage.set(5);
    component.setSort('action');
    expect(component.currentPage()).toBe(1);
  });

  // ── toggleAction / toggleSeverity ─────────────────────────────────────────

  it('toggleAction adds action when not present', () => {
    component.toggleAction('Login');
    expect(component.actionFilter).toContain('Login');
  });

  it('toggleAction removes action when already present', () => {
    component.actionFilter = ['Login'];
    component.toggleAction('Login');
    expect(component.actionFilter).not.toContain('Login');
  });

  it('toggleSeverity adds severity when not present', () => {
    component.toggleSeverity('Critical');
    expect(component.severityFilter).toContain('Critical');
  });

  it('toggleSeverity removes severity when already present', () => {
    component.severityFilter = ['Critical'];
    component.toggleSeverity('Critical');
    expect(component.severityFilter).not.toContain('Critical');
  });

  // ── hasFilters ────────────────────────────────────────────────────────────

  it('hasFilters is false initially', () => {
    expect(component.hasFilters).toBeFalse();
  });

  it('hasFilters is true when searchQuery is set', () => {
    component.searchQuery = 'test';
    expect(component.hasFilters).toBeTrue();
  });

  it('hasFilters is true when actionFilter has entries', () => {
    component.actionFilter = ['Login'];
    expect(component.hasFilters).toBeTrue();
  });

  it('hasFilters is true when severityFilter has entries', () => {
    component.severityFilter = ['Info'];
    expect(component.hasFilters).toBeTrue();
  });

  // ── clearFilters() ────────────────────────────────────────────────────────

  it('clearFilters resets all filters and sort', () => {
    component.searchQuery = 'test';
    component.actionFilter = ['Login'];
    component.severityFilter = ['Critical'];
    component.clearFilters();
    expect(component.searchQuery).toBe('');
    expect(component.actionFilter).toEqual([]);
    expect(component.severityFilter).toEqual([]);
    expect(component.sort()).toEqual({ field: 'createdat', dir: 'desc' });
  });

  // ── severityChipStyle() ───────────────────────────────────────────────────

  it('severityChipStyle returns empty object when severity is not selected', () => {
    component.severityFilter = [];
    expect(component.severityChipStyle('Info')).toEqual({});
  });

  it('severityChipStyle returns style object for selected Info', () => {
    component.severityFilter = ['Info'];
    const style = component.severityChipStyle('Info') as any;
    expect(style['color']).toBeDefined();
  });

  it('severityChipStyle returns style object for selected Warning', () => {
    component.severityFilter = ['Warning'];
    const style = component.severityChipStyle('Warning') as any;
    expect(style['color']).toBe('#f59e0b');
  });

  it('severityChipStyle returns style object for selected Critical', () => {
    component.severityFilter = ['Critical'];
    const style = component.severityChipStyle('Critical') as any;
    expect(style['color']).toBeDefined();
  });

  it('severityChipStyle returns empty object for unknown severity even if in filter', () => {
    component.severityFilter = ['Unknown'];
    const style = component.severityChipStyle('Unknown') as any;
    // map['Unknown'] is undefined so falls back to {}
    expect(Object.keys(style).length).toBe(0);
  });

  // ── getActionIcon() ───────────────────────────────────────────────────────

  it('getActionIcon returns emoji for known actions', () => {
    expect(component.getActionIcon('Login')).toBe('🔑');
    expect(component.getActionIcon('Create')).toBe('✨');
    expect(component.getActionIcon('Delete')).toBe('🗑️');
  });

  it('getActionIcon returns bullet for unknown action', () => {
    expect(component.getActionIcon('Unknown')).toBe('•');
  });

  // ── getSeverityClass() ────────────────────────────────────────────────────

  it('getSeverityClass returns severity-info for Info', () => {
    expect(component.getSeverityClass('Info')).toBe('severity-info');
  });

  it('getSeverityClass returns severity-warning for Warning', () => {
    expect(component.getSeverityClass('Warning')).toBe('severity-warning');
  });

  it('getSeverityClass returns severity-critical for Critical', () => {
    expect(component.getSeverityClass('Critical')).toBe('severity-critical');
  });

  it('getSeverityClass returns severity-info for unknown severity', () => {
    expect(component.getSeverityClass('Unknown')).toBe('severity-info');
  });

  // ── formatDate() ──────────────────────────────────────────────────────────

  it('formatDate returns a non-empty string', () => {
    expect(component.formatDate('2024-06-01T10:30:00')).toBeTruthy();
  });

  it('formatDate handles dates already ending with Z', () => {
    expect(component.formatDate('2024-06-01T10:30:00Z')).toBeTruthy();
  });

  // ── pageNumbers() ─────────────────────────────────────────────────────────

  it('pageNumbers returns sequential array when totalPages <= 7', () => {
    component.result.set({ ...PAGED, totalPages: 4, totalCount: 40 });
    expect(component.pageNumbers()).toEqual([1, 2, 3, 4]);
  });

  it('pageNumbers returns ellipsis pages when totalPages > 7', () => {
    component.result.set({ ...PAGED, totalPages: 10, totalCount: 100 });
    component.currentPage.set(1);
    const pages = component.pageNumbers();
    expect(pages).toContain(1);
    expect(pages).toContain(10);
    expect(pages.length).toBeLessThan(10);
  });

  // ── pageStart / pageEnd ───────────────────────────────────────────────────

  it('pageStart is 1 on first page', () => {
    component.currentPage.set(1);
    expect(component.pageStart).toBe(1);
  });

  it('pageEnd is capped at totalCount', () => {
    component.result.set({ ...EMPTY_PAGED, totalCount: 3 });
    component.pageSize = 10;
    component.currentPage.set(1);
    expect(component.pageEnd).toBe(3);
  });

  // ── changePage() ──────────────────────────────────────────────────────────

  it('changePage updates currentPage and reloads', () => {
    auditSpy.getLogs.calls.reset();
    component.changePage(2);
    expect(component.currentPage()).toBe(2);
    expect(auditSpy.getLogs).toHaveBeenCalled();
  });
});
