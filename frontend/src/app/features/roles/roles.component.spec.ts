import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of, throwError } from 'rxjs';
import { RolesComponent } from './roles.component';
import { RoleService } from '../../core/services/role.service';
import { Role } from '../../core/models/role.models';

const ADMIN_ROLE: Role = {
  id: 1, name: 'Admin', description: 'Administrator', color: '#f00', userCount: 3,
  permissions: [
    { id: 1, resource: 'users', action: 'read' },
    { id: 2, resource: 'users', action: 'write' },
    { id: 3, resource: 'roles', action: 'read' },
  ],
};
const VIEWER_ROLE: Role = {
  id: 2, name: 'Viewer', description: 'Viewer', color: '#00f', userCount: 5,
  permissions: [],
};

describe('RolesComponent', () => {
  let component: RolesComponent;
  let fixture: ComponentFixture<RolesComponent>;
  let roleSpy: jasmine.SpyObj<RoleService>;

  beforeEach(async () => {
    roleSpy = jasmine.createSpyObj<RoleService>('RoleService', ['getRoles']);
    roleSpy.getRoles.and.returnValue(of([ADMIN_ROLE, VIEWER_ROLE]));

    await TestBed.configureTestingModule({
      imports: [RolesComponent],
      providers: [
        provideRouter([]),
        { provide: RoleService, useValue: roleSpy },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(RolesComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('creates the component', () => expect(component).toBeTruthy());

  // ── ngOnInit ──────────────────────────────────────────────────────────────

  it('loads roles on init', () => {
    expect(roleSpy.getRoles).toHaveBeenCalled();
  });

  it('sets roles signal with loaded data', () => {
    expect(component.roles().length).toBe(2);
    expect(component.roles()[0].name).toBe('Admin');
  });

  it('sets loading to false after load', () => {
    expect(component.loading()).toBeFalse();
  });

  it('sets loading to false on error', () => {
    roleSpy.getRoles.and.returnValue(throwError(() => new Error('fail')));
    component.ngOnInit();
    expect(component.loading()).toBeFalse();
  });

  // ── hasPermission() ───────────────────────────────────────────────────────

  it('hasPermission returns true when role has matching resource + action', () => {
    expect(component.hasPermission(ADMIN_ROLE, 'users', 'read')).toBeTrue();
  });

  it('hasPermission returns false when action does not match', () => {
    expect(component.hasPermission(ADMIN_ROLE, 'users', 'delete')).toBeFalse();
  });

  it('hasPermission returns false for role with no permissions', () => {
    expect(component.hasPermission(VIEWER_ROLE, 'users', 'read')).toBeFalse();
  });

  it('hasPermission returns false when resource does not match', () => {
    expect(component.hasPermission(ADMIN_ROLE, 'audit', 'read')).toBeFalse();
  });

  // ── uniqueResources() ─────────────────────────────────────────────────────

  it('uniqueResources returns distinct resources from permissions', () => {
    // ADMIN_ROLE has users×2 and roles×1 — should return ['users', 'roles']
    const resources = component.uniqueResources(ADMIN_ROLE);
    expect(resources).toEqual(['users', 'roles']);
  });

  it('uniqueResources returns empty array for role with no permissions', () => {
    expect(component.uniqueResources(VIEWER_ROLE)).toEqual([]);
  });

  // ── constants ─────────────────────────────────────────────────────────────

  it('exposes resources constant with expected values', () => {
    expect(component.resources).toEqual(['users', 'roles', 'audit']);
  });

  it('exposes actions constant with expected values', () => {
    expect(component.actions).toEqual(['read', 'write', 'delete', 'export']);
  });
});
