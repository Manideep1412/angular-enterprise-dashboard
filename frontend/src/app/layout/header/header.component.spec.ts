import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { of, throwError } from 'rxjs';
import { HeaderComponent } from './header.component';
import { AuthService } from '../../core/auth/auth.service';
import { ThemeService } from '../../core/services/theme.service';
import { AuditService } from '../../core/services/audit.service';

const MOCK_LOG: any = {
  id: 1, userEmail: 'a@b.com', action: 'Login', resource: 'Auth',
  resourceId: null, description: 'User signed in', ipAddress: '127.0.0.1',
  severity: 'Info', createdAt: new Date(Date.now() - 30000).toISOString().replace('Z', ''),
};
const PAGED = { items: [MOCK_LOG], totalCount: 1, page: 1, pageSize: 10, totalPages: 1 };

describe('HeaderComponent', () => {
  let component: HeaderComponent;
  let fixture: ComponentFixture<HeaderComponent>;
  let authMock: any;
  let auditSpy: jasmine.SpyObj<AuditService>;
  let store: Record<string, string>;

  beforeEach(async () => {
    store = {};
    spyOn(localStorage, 'getItem').and.callFake((k: string) => store[k] ?? null);
    spyOn(localStorage, 'setItem').and.callFake((k: string, v: string) => { store[k] = v; });

    authMock = {
      user: jasmine.createSpy('user').and.returnValue({ fullName: 'Jane Doe' }),
      logout: jasmine.createSpy('logout'),
    };

    auditSpy = jasmine.createSpyObj<AuditService>('AuditService', ['getLogs']);
    auditSpy.getLogs.and.returnValue(of(PAGED));

    const themeMock = { isDark: () => false, toggle: jasmine.createSpy() };

    await TestBed.configureTestingModule({
      imports: [HeaderComponent],
      providers: [
        provideRouter([]),
        { provide: AuthService, useValue: authMock },
        { provide: ThemeService, useValue: themeMock },
        { provide: AuditService, useValue: auditSpy },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(HeaderComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('creates the component', () => expect(component).toBeTruthy());

  // ── loadNotifications() ───────────────────────────────────────────────────

  it('calls auditService.getLogs on init', () => {
    expect(auditSpy.getLogs).toHaveBeenCalledWith(1, 8, undefined, undefined, undefined, 'createdat', 'desc');
  });

  it('sets notifications signal with mapped logs', () => {
    expect(component.notifications().length).toBe(1);
    expect(component.notifications()[0].id).toBe(1);
  });

  it('sets notifLoading to false after load', () => {
    expect(component.notifLoading()).toBeFalse();
  });

  it('sets notifLoading to false on error', () => {
    auditSpy.getLogs.and.returnValue(throwError(() => new Error('fail')));
    component.loadNotifications();
    expect(component.notifLoading()).toBeFalse();
  });

  it('marks notification as unread when no seenAt in storage', () => {
    store = {}; // no ed_notif_seen_at
    component.loadNotifications();
    expect(component.notifications()[0].unread).toBeTrue();
  });

  it('marks notification as read when seenAt is after log date', () => {
    store['ed_notif_seen_at'] = new Date(Date.now() + 10000).toISOString();
    component.loadNotifications();
    expect(component.notifications()[0].unread).toBeFalse();
  });

  it('maps known action to title', () => {
    expect(component.notifications()[0].title).toBe('User signed in');
  });

  it('maps known action to icon', () => {
    expect(component.notifications()[0].icon).toBe('🔑');
  });

  // ── unreadCount computed ──────────────────────────────────────────────────

  it('unreadCount reflects number of unread notifications', () => {
    store = {}; // all unread
    component.loadNotifications();
    expect(component.unreadCount()).toBe(1);
  });

  // ── markAllRead() ─────────────────────────────────────────────────────────

  it('markAllRead sets all notifications unread to false', () => {
    component.markAllRead();
    expect(component.notifications().every(n => !n.unread)).toBeTrue();
  });

  it('markAllRead persists seenAt to localStorage', () => {
    component.markAllRead();
    expect(localStorage.setItem).toHaveBeenCalledWith('ed_notif_seen_at', jasmine.any(String));
  });

  // ── toggleNotif() ─────────────────────────────────────────────────────────

  it('toggleNotif opens notification panel', () => {
    component.notifOpen.set(false);
    component.toggleNotif();
    expect(component.notifOpen()).toBeTrue();
  });

  it('toggleNotif closes notification panel when already open', () => {
    component.notifOpen.set(true);
    component.toggleNotif();
    expect(component.notifOpen()).toBeFalse();
  });

  it('toggleNotif closes dropdown when opening notif panel', () => {
    component.dropdownOpen.set(true);
    component.notifOpen.set(false);
    component.toggleNotif();
    expect(component.dropdownOpen()).toBeFalse();
  });

  it('toggleNotif reloads notifications when opening', () => {
    auditSpy.getLogs.calls.reset();
    component.notifOpen.set(false);
    component.toggleNotif();
    expect(auditSpy.getLogs).toHaveBeenCalled();
  });

  // ── toggleDropdown() ──────────────────────────────────────────────────────

  it('toggleDropdown opens dropdown when closed', () => {
    component.dropdownOpen.set(false);
    component.toggleDropdown();
    expect(component.dropdownOpen()).toBeTrue();
  });

  it('toggleDropdown closes dropdown when open', () => {
    component.dropdownOpen.set(true);
    component.toggleDropdown();
    expect(component.dropdownOpen()).toBeFalse();
  });

  it('toggleDropdown closes notif panel if open', () => {
    component.notifOpen.set(true);
    component.toggleDropdown();
    expect(component.notifOpen()).toBeFalse();
  });

  // ── initials getter ───────────────────────────────────────────────────────

  it('initials returns two uppercase letters from fullName', () => {
    authMock.user.and.returnValue({ fullName: 'Jane Doe' });
    expect(component.initials).toBe('JD');
  });

  it('initials returns empty string when user has no fullName', () => {
    authMock.user.and.returnValue(null);
    expect(component.initials).toBe('');
  });

  // ── pageTitle getter ──────────────────────────────────────────────────────

  it('pageTitle capitalizes and formats the URL path segment', () => {
    const router = TestBed.inject(Router);
    spyOnProperty(router, 'url').and.returnValue('/users');
    expect(component.pageTitle).toBe('Users');
  });

  it('pageTitle replaces hyphens with spaces', () => {
    const router = TestBed.inject(Router);
    spyOnProperty(router, 'url').and.returnValue('/audit-logs');
    expect(component.pageTitle).toBe('Audit logs');
  });

  // ── currentDate getter ────────────────────────────────────────────────────

  it('currentDate returns a non-empty date string', () => {
    expect(component.currentDate).toBeTruthy();
    expect(typeof component.currentDate).toBe('string');
  });

  // ── logout() ─────────────────────────────────────────────────────────────

  it('logout calls auth.logout and closes dropdown', () => {
    component.dropdownOpen.set(true);
    component.logout();
    expect(authMock.logout).toHaveBeenCalled();
    expect(component.dropdownOpen()).toBeFalse();
  });
});
