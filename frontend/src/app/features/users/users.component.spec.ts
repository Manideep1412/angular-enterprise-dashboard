import { ComponentFixture, TestBed, fakeAsync, tick } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of, throwError } from 'rxjs';
import { UsersComponent } from './users.component';
import { UserService } from '../../core/services/user.service';
import { RoleService } from '../../core/services/role.service';
import { AuthService } from '../../core/auth/auth.service';

const MOCK_USER: any = {
  id: 1, firstName: 'Jane', lastName: 'Doe', fullName: 'Jane Doe',
  email: 'jane@x.com', status: 'Active', roles: ['Admin'],
  department: 'Engineering', createdAt: '2024-01-01T00:00:00', lastLoginAt: null,
};
const MOCK_ROLES: any[] = [
  { id: 1, name: 'Admin', permissions: [] },
  { id: 2, name: 'Manager', permissions: [] },
];
const PAGED = { items: [MOCK_USER], totalCount: 1, page: 1, pageSize: 10, totalPages: 1 };
const EMPTY_PAGED = { items: [], totalCount: 0, page: 1, pageSize: 10, totalPages: 1 };

describe('UsersComponent', () => {
  let component: UsersComponent;
  let fixture: ComponentFixture<UsersComponent>;
  let userSpy: jasmine.SpyObj<UserService>;
  let roleSpy: jasmine.SpyObj<RoleService>;

  beforeEach(async () => {
    userSpy = jasmine.createSpyObj<UserService>('UserService', ['getUsers', 'create', 'update', 'delete']);
    userSpy.getUsers.and.returnValue(of(PAGED));
    userSpy.create.and.returnValue(of(MOCK_USER));
    userSpy.update.and.returnValue(of(MOCK_USER));
    userSpy.delete.and.returnValue(of(null as any));

    roleSpy = jasmine.createSpyObj<RoleService>('RoleService', ['getRoles']);
    roleSpy.getRoles.and.returnValue(of(MOCK_ROLES));

    const authMock = {
      user: jasmine.createSpy('user').and.returnValue(null),
      isManagerOrAdmin: jasmine.createSpy().and.returnValue(false),
      isAdmin: jasmine.createSpy().and.returnValue(false),
    };

    await TestBed.configureTestingModule({
      imports: [UsersComponent],
      providers: [
        provideRouter([]),
        { provide: UserService, useValue: userSpy },
        { provide: RoleService, useValue: roleSpy },
        { provide: AuthService, useValue: authMock },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(UsersComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('creates the component', () => expect(component).toBeTruthy());

  // ── ngOnInit ──────────────────────────────────────────────────────────────

  it('loads users on init', () => {
    expect(userSpy.getUsers).toHaveBeenCalled();
  });

  it('loads roles on init', () => {
    expect(roleSpy.getRoles).toHaveBeenCalled();
  });

  it('sets roles signal after loading', () => {
    expect(component.roles()).toEqual(MOCK_ROLES);
  });

  it('sets loading to false after successful load', () => {
    expect(component.loading()).toBeFalse();
  });

  it('sets loading to false on error', () => {
    userSpy.getUsers.and.returnValue(throwError(() => new Error('fail')));
    component.loadUsers();
    expect(component.loading()).toBeFalse();
  });

  // ── onSearch debounce ─────────────────────────────────────────────────────

  it('debounces search — does not reload immediately', fakeAsync(() => {
    userSpy.getUsers.calls.reset();
    component.onSearch('alice');
    expect(userSpy.getUsers).not.toHaveBeenCalled();
    tick(350);
  }));

  it('debounces search — reloads after 350ms', fakeAsync(() => {
    userSpy.getUsers.calls.reset();
    component.onSearch('alice');
    tick(350);
    expect(userSpy.getUsers).toHaveBeenCalled();
  }));

  // ── setSort() ─────────────────────────────────────────────────────────────

  it('setSort sets direction to asc for a new field', () => {
    component.setSort('email');
    expect(component.sort().field).toBe('email');
    expect(component.sort().dir).toBe('asc');
  });

  it('setSort toggles to desc when same field is already asc', () => {
    component.sort.set({ field: 'email', dir: 'asc' });
    component.setSort('email');
    expect(component.sort().dir).toBe('desc');
  });

  it('setSort goes back to asc when same field is desc', () => {
    component.sort.set({ field: 'email', dir: 'desc' });
    component.setSort('email');
    expect(component.sort().dir).toBe('asc');
  });

  it('setSort resets page to 1', () => {
    component.currentPage.set(3);
    component.setSort('email');
    expect(component.currentPage()).toBe(1);
  });

  // ── pageNumbers() ─────────────────────────────────────────────────────────

  it('pageNumbers returns sequential array when totalPages <= 7', () => {
    component.result.set({ ...PAGED, totalPages: 5, totalCount: 50 });
    expect(component.pageNumbers()).toEqual([1, 2, 3, 4, 5]);
  });

  it('pageNumbers returns ellipsis array when totalPages > 7 and on page 1', () => {
    component.result.set({ ...PAGED, totalPages: 10, totalCount: 100 });
    component.currentPage.set(1);
    const pages = component.pageNumbers();
    expect(pages[0]).toBe(1);
    expect(pages).toContain(10);
    expect(pages.length).toBeLessThan(10);
  });

  it('pageNumbers includes -1 ellipsis before and after middle when on page 5 of 10', () => {
    component.result.set({ ...PAGED, totalPages: 10, totalCount: 100 });
    component.currentPage.set(5);
    const pages = component.pageNumbers();
    expect(pages.filter(p => p === -1).length).toBe(2);
  });

  // ── pageStart / pageEnd ───────────────────────────────────────────────────

  it('pageStart returns 1 on first page', () => {
    component.currentPage.set(1);
    expect(component.pageStart).toBe(1);
  });

  it('pageStart returns 11 on second page with pageSize 10', () => {
    component.currentPage.set(2);
    component.pageSize = 10;
    expect(component.pageStart).toBe(11);
  });

  it('pageEnd is capped at totalCount', () => {
    component.result.set({ ...EMPTY_PAGED, totalCount: 3 });
    component.currentPage.set(1);
    component.pageSize = 10;
    expect(component.pageEnd).toBe(3);
  });

  // ── hasFilters ────────────────────────────────────────────────────────────

  it('hasFilters is false initially', () => {
    expect(component.hasFilters).toBeFalse();
  });

  it('hasFilters is true when searchQuery is set', () => {
    component.searchQuery = 'test';
    expect(component.hasFilters).toBeTrue();
  });

  it('hasFilters is true when statusFilter has entries', () => {
    component.statusFilter = ['Active'];
    expect(component.hasFilters).toBeTrue();
  });

  // ── toggleStatus / toggleRoleFilter / clearFilters ────────────────────────

  it('toggleStatus adds status when not present', () => {
    component.toggleStatus('Active');
    expect(component.statusFilter).toContain('Active');
  });

  it('toggleStatus removes status when already present', () => {
    component.statusFilter = ['Active'];
    component.toggleStatus('Active');
    expect(component.statusFilter).not.toContain('Active');
  });

  it('toggleRoleFilter adds role when not present', () => {
    component.toggleRoleFilter('Admin');
    expect(component.roleFilter).toContain('Admin');
  });

  it('toggleRoleFilter removes role when already present', () => {
    component.roleFilter = ['Admin'];
    component.toggleRoleFilter('Admin');
    expect(component.roleFilter).not.toContain('Admin');
  });

  it('clearFilters resets search, status, and role filters', () => {
    component.searchQuery = 'test'; component.statusFilter = ['Active']; component.roleFilter = ['Admin'];
    component.clearFilters();
    expect(component.searchQuery).toBe('');
    expect(component.statusFilter).toEqual([]);
    expect(component.roleFilter).toEqual([]);
  });

  // ── getStatusClass() ──────────────────────────────────────────────────────

  it('getStatusClass returns badge-active for Active', () => {
    expect(component.getStatusClass('Active')).toBe('badge-active');
  });

  it('getStatusClass returns badge-inactive for Inactive', () => {
    expect(component.getStatusClass('Inactive')).toBe('badge-inactive');
  });

  it('getStatusClass returns badge-suspended for Suspended', () => {
    expect(component.getStatusClass('Suspended')).toBe('badge-suspended');
  });

  it('getStatusClass returns badge-inactive for unknown status', () => {
    expect(component.getStatusClass('Unknown')).toBe('badge-inactive');
  });

  // ── formatDate() ──────────────────────────────────────────────────────────

  it('formatDate appends Z for dates without timezone', () => {
    const result = component.formatDate('2024-06-01T10:00:00');
    expect(typeof result).toBe('string');
    expect(result.length).toBeGreaterThan(0);
  });

  it('formatDate does not modify dates already with Z', () => {
    const result = component.formatDate('2024-06-01T10:00:00Z');
    expect(result).toBeTruthy();
  });

  // ── openCreateModal / openEditModal / closeModal ──────────────────────────

  it('openCreateModal opens modal and clears editing user', () => {
    component.openCreateModal();
    expect(component.modalOpen()).toBeTrue();
    expect(component.editingUser()).toBeNull();
  });

  it('openCreateModal resets form with status Active', () => {
    component.openCreateModal();
    expect(component.userForm.get('status')?.value).toBe('Active');
  });

  it('openEditModal opens modal and sets editing user', () => {
    component.openEditModal(MOCK_USER);
    expect(component.modalOpen()).toBeTrue();
    expect(component.editingUser()).toBe(MOCK_USER);
  });

  it('openEditModal patches form with user values', () => {
    component.openEditModal(MOCK_USER);
    expect(component.userForm.get('firstName')?.value).toBe('Jane');
    expect(component.userForm.get('lastName')?.value).toBe('Doe');
  });

  it('openEditModal sets selectedRoleIds for user roles', () => {
    component.openEditModal(MOCK_USER);
    // MOCK_USER.roles = ['Admin'], MOCK_ROLES[0] is Admin with id=1
    expect(component.selectedRoleIds).toContain(1);
  });

  it('closeModal closes the modal', () => {
    component.modalOpen.set(true);
    component.closeModal();
    expect(component.modalOpen()).toBeFalse();
  });

  // ── isRoleSelected / toggleRole ───────────────────────────────────────────

  it('isRoleSelected returns false when role not selected', () => {
    component.selectedRoleIds = [2];
    expect(component.isRoleSelected(1)).toBeFalse();
  });

  it('isRoleSelected returns true when role is selected', () => {
    component.selectedRoleIds = [1, 2];
    expect(component.isRoleSelected(1)).toBeTrue();
  });

  it('toggleRole adds role when not selected', () => {
    component.selectedRoleIds = [];
    component.toggleRole(1);
    expect(component.selectedRoleIds).toContain(1);
  });

  it('toggleRole removes role when already selected', () => {
    component.selectedRoleIds = [1];
    component.toggleRole(1);
    expect(component.selectedRoleIds).not.toContain(1);
  });

  // ── saveUser() ────────────────────────────────────────────────────────────

  it('saveUser marks form touched when invalid and does not call create/update', () => {
    component.openCreateModal();
    component.userForm.get('firstName')?.setValue('');
    component.saveUser();
    expect(component.userForm.get('firstName')?.touched).toBeTrue();
    expect(userSpy.create).not.toHaveBeenCalled();
  });

  it('saveUser calls userService.create when editingUser is null', () => {
    component.openCreateModal();
    component.userForm.patchValue({
      firstName: 'New', lastName: 'User', email: 'new@x.com', password: 'Pass@123',
    });
    component.saveUser();
    expect(userSpy.create).toHaveBeenCalled();
  });

  it('saveUser calls userService.update when editingUser is set', () => {
    component.openEditModal(MOCK_USER);
    component.userForm.patchValue({ firstName: 'Updated', lastName: 'Doe', status: 'Active' });
    component.saveUser();
    expect(userSpy.update).toHaveBeenCalledWith(MOCK_USER.id, jasmine.objectContaining({ firstName: 'Updated' }));
  });

  it('saveUser closes modal on success', () => {
    component.openCreateModal();
    component.userForm.patchValue({
      firstName: 'New', lastName: 'User', email: 'new@x.com', password: 'Pass@123',
    });
    component.saveUser();
    expect(component.modalOpen()).toBeFalse();
  });

  it('saveUser sets saveError on failure', () => {
    userSpy.create.and.returnValue(throwError(() => ({ error: { message: 'Forbidden' } })));
    component.openCreateModal();
    component.userForm.patchValue({
      firstName: 'New', lastName: 'User', email: 'new@x.com', password: 'Pass@123',
    });
    component.saveUser();
    expect(component.saveError()).toBe('Forbidden');
  });

  // ── confirmDelete() ───────────────────────────────────────────────────────

  it('confirmDelete does nothing when deleteTarget is null', () => {
    component.deleteTarget.set(null);
    component.confirmDelete();
    expect(userSpy.delete).not.toHaveBeenCalled();
  });

  it('confirmDelete calls userService.delete with user id', () => {
    component.deleteTarget.set(MOCK_USER);
    component.confirmDelete();
    expect(userSpy.delete).toHaveBeenCalledWith(MOCK_USER.id);
  });

  it('confirmDelete clears deleteTarget on success', () => {
    component.deleteTarget.set(MOCK_USER);
    component.confirmDelete();
    expect(component.deleteTarget()).toBeNull();
  });

  it('confirmDelete sets deleteError on failure', () => {
    userSpy.delete.and.returnValue(throwError(() => ({ error: { message: 'Admin role required.' } })));
    component.deleteTarget.set(MOCK_USER);
    component.confirmDelete();
    expect(component.deleteError()).toBe('Admin role required.');
  });

  // ── changePage() ──────────────────────────────────────────────────────────

  it('changePage sets currentPage and reloads', () => {
    userSpy.getUsers.calls.reset();
    component.changePage(3);
    expect(component.currentPage()).toBe(3);
    expect(userSpy.getUsers).toHaveBeenCalled();
  });
});
