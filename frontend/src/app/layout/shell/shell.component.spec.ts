import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of } from 'rxjs';
import { ShellComponent } from './shell.component';
import { AuthService } from '../../core/auth/auth.service';
import { ThemeService } from '../../core/services/theme.service';
import { AuditService } from '../../core/services/audit.service';

describe('ShellComponent', () => {
  let component: ShellComponent;
  let fixture: ComponentFixture<ShellComponent>;
  let store: Record<string, string>;

  beforeEach(async () => {
    store = {};
    spyOn(localStorage, 'getItem').and.callFake((k: string) => store[k] ?? null);
    spyOn(localStorage, 'setItem').and.callFake((k: string, v: string) => { store[k] = v; });
    spyOn(document.documentElement, 'setAttribute');

    const authMock = {
      user: jasmine.createSpy('user').and.returnValue(null),
      logout: jasmine.createSpy('logout'),
      isAuthenticated: jasmine.createSpy('isAuthenticated').and.returnValue(false),
      isAdmin: jasmine.createSpy('isAdmin').and.returnValue(false),
    };
    const themeMock = { isDark: () => false, toggle: jasmine.createSpy() };
    const auditMock = {
      getLogs: jasmine.createSpy('getLogs').and.returnValue(
        of({ items: [], totalCount: 0, page: 1, pageSize: 10, totalPages: 0 })
      ),
    };

    await TestBed.configureTestingModule({
      imports: [ShellComponent],
      providers: [
        provideRouter([]),
        { provide: AuthService, useValue: authMock },
        { provide: ThemeService, useValue: themeMock },
        { provide: AuditService, useValue: auditMock },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(ShellComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('creates the component', () => expect(component).toBeTruthy());

  // ── sidebarCollapsed signal ───────────────────────────────────────────────

  it('sidebarCollapsed defaults to false when storage is empty', () => {
    expect(component.sidebarCollapsed()).toBeFalse();
  });

  it('sidebarCollapsed reads "true" from localStorage as boolean true', () => {
    // Recreate component with storage preset
    store['ed_sidebar_collapsed'] = 'true';
    const fix2 = TestBed.createComponent(ShellComponent);
    expect(fix2.componentInstance.sidebarCollapsed()).toBeTrue();
  });

  it('sidebarCollapsed is false when localStorage has "false"', () => {
    store['ed_sidebar_collapsed'] = 'false';
    const fix2 = TestBed.createComponent(ShellComponent);
    expect(fix2.componentInstance.sidebarCollapsed()).toBeFalse();
  });

  // ── constructor effect ────────────────────────────────────────────────────

  it('constructor effect persists sidebarCollapsed to localStorage', () => {
    TestBed.flushEffects();
    expect(localStorage.setItem).toHaveBeenCalledWith('ed_sidebar_collapsed', 'false');
  });

  it('effect writes updated value when sidebarCollapsed changes', () => {
    component.sidebarCollapsed.set(true);
    fixture.detectChanges();
    expect(localStorage.setItem).toHaveBeenCalledWith('ed_sidebar_collapsed', 'true');
  });
});
