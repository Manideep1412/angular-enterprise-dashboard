import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { SidebarComponent } from './sidebar.component';
import { AuthService } from '../../core/auth/auth.service';

describe('SidebarComponent', () => {
  let component: SidebarComponent;
  let fixture: ComponentFixture<SidebarComponent>;
  let userSpy: jasmine.Spy;

  beforeEach(async () => {
    userSpy = jasmine.createSpy('user').and.returnValue(null);

    await TestBed.configureTestingModule({
      imports: [SidebarComponent],
      providers: [
        provideRouter([]),
        { provide: AuthService, useValue: { user: userSpy } },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(SidebarComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('creates the component', () => expect(component).toBeTruthy());

  // ── navItems ──────────────────────────────────────────────────────────────

  it('exposes 4 nav items', () => {
    expect(component.navItems.length).toBe(4);
  });

  it('includes Dashboard, Users, Roles, and Audit Logs routes', () => {
    const routes = component.navItems.map(i => i.route);
    expect(routes).toContain('/dashboard');
    expect(routes).toContain('/users');
    expect(routes).toContain('/roles');
    expect(routes).toContain('/audit-logs');
  });

  // ── initials getter ───────────────────────────────────────────────────────

  it('initials returns "?" when user is null', () => {
    userSpy.and.returnValue(null);
    expect(component.initials).toBe('?');
  });

  it('initials returns first and second name initials uppercased', () => {
    userSpy.and.returnValue({ fullName: 'Jane Doe' });
    expect(component.initials).toBe('JD');
  });

  it('initials returns single initial for single-word name', () => {
    userSpy.and.returnValue({ fullName: 'Admin' });
    expect(component.initials).toBe('A');
  });

  // ── getIcon() ─────────────────────────────────────────────────────────────

  it('getIcon returns non-empty string for "grid"', () => {
    expect(component.getIcon('grid')).toContain('<svg');
  });

  it('getIcon returns non-empty string for "users"', () => {
    expect(component.getIcon('users')).toContain('<svg');
  });

  it('getIcon returns non-empty string for "shield"', () => {
    expect(component.getIcon('shield')).toContain('<svg');
  });

  it('getIcon returns non-empty string for "file-text"', () => {
    expect(component.getIcon('file-text')).toContain('<svg');
  });

  it('getIcon returns empty string for unknown icon name', () => {
    expect(component.getIcon('unknown-icon')).toBe('');
  });

  // ── @Input() collapsed ────────────────────────────────────────────────────

  it('collapsed input defaults to false', () => {
    expect(component.collapsed).toBeFalse();
  });

  it('collapsed input can be set to true', () => {
    component.collapsed = true;
    expect(component.collapsed).toBeTrue();
  });

  // ── @Output() toggle ──────────────────────────────────────────────────────

  it('toggle EventEmitter exists', () => {
    expect(component.toggle).toBeDefined();
  });
});
