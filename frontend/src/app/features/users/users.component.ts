import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { debounceTime, distinctUntilChanged, Subject } from 'rxjs';
import { UserService } from '../../core/services/user.service';
import { RoleService } from '../../core/services/role.service';
import { AuthService } from '../../core/auth/auth.service';
import { User, PagedResult } from '../../core/models/user.models';
import { Role } from '../../core/models/role.models';

interface SortState { field: string; dir: 'asc' | 'desc'; }

const COLS = [
  { label: 'User',       field: 'name' },
  { label: 'Email',      field: 'email' },
  { label: 'Department', field: 'department' },
  { label: 'Roles',      field: '' },
  { label: 'Status',     field: 'status' },
  { label: 'Last Login', field: 'lastloginat' },
];

@Component({
  selector: 'app-users',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule],
  templateUrl: './users.component.html',
  styleUrl: './users.component.scss',
})
export class UsersComponent implements OnInit {
  readonly userService = inject(UserService);
  readonly roleService = inject(RoleService);
  readonly auth = inject(AuthService);
  readonly fb = inject(FormBuilder);

  result      = signal<PagedResult<User> | null>(null);
  roles       = signal<Role[]>([]);
  loading     = signal(true);
  modalOpen   = signal(false);
  editingUser = signal<User | null>(null);
  saving      = signal(false);
  saveError   = signal<string | null>(null);
  deleting    = signal(false);
  deleteTarget = signal<User | null>(null);
  deleteError  = signal<string | null>(null);
  currentPage  = signal(1);
  sort         = signal<SortState>({ field: 'createdat', dir: 'desc' });

  searchQuery  = '';
  statusFilter: string[] = [];
  roleFilter:   string[] = [];
  pageSize     = 10;

  readonly statuses    = ['Active', 'Inactive', 'Suspended'];
  readonly roleOptions = ['Admin', 'Manager', 'Developer', 'Viewer'];
  selectedRoleIds: number[] = [];
  readonly cols = COLS;

  private search$ = new Subject<string>();

  userForm = this.fb.group({
    firstName:  ['', Validators.required],
    lastName:   ['', Validators.required],
    email:      ['', [Validators.required, Validators.email]],
    password:   ['', [Validators.required, Validators.minLength(6)]],
    department: [''],
    status:     ['Active'],
  });

  ngOnInit() {
    this.loadUsers();
    this.roleService.getRoles().subscribe(r => this.roles.set(r));
    this.search$.pipe(debounceTime(350), distinctUntilChanged()).subscribe(() => {
      this.currentPage.set(1);
      this.loadUsers();
    });
  }

  loadUsers() {
    this.loading.set(true);
    const s = this.sort();
    this.userService.getUsers(this.currentPage(), this.pageSize, this.searchQuery || undefined, this.statusFilter.length ? this.statusFilter : undefined, this.roleFilter.length ? this.roleFilter : undefined, s.field, s.dir)
      .subscribe({ next: r => { this.result.set(r); this.loading.set(false); }, error: () => this.loading.set(false) });
  }

  onSearch(v: string)      { this.search$.next(v); }
  resetAndLoad()           { this.currentPage.set(1); this.loadUsers(); }
  onPageSizeChange()       { this.currentPage.set(1); this.loadUsers(); }
  changePage(p: number)    { this.currentPage.set(p); this.loadUsers(); }

  toggleStatus(s: string)       { this.statusFilter = this.statusFilter.includes(s) ? this.statusFilter.filter(x => x !== s) : [...this.statusFilter, s]; this.resetAndLoad(); }
  toggleRoleFilter(r: string)   { this.roleFilter   = this.roleFilter.includes(r)   ? this.roleFilter.filter(x => x !== r)   : [...this.roleFilter,   r]; this.resetAndLoad(); }
  clearFilters()                { this.searchQuery = ''; this.statusFilter = []; this.roleFilter = []; this.resetAndLoad(); }
  get hasFilters()              { return !!(this.searchQuery || this.statusFilter.length || this.roleFilter.length); }

  setSort(field: string) {
    const cur = this.sort();
    this.sort.set({ field, dir: cur.field === field && cur.dir === 'asc' ? 'desc' : 'asc' });
    this.currentPage.set(1);
    this.loadUsers();
  }

  pageNumbers(): number[] {
    const total = this.result()?.totalPages ?? 1;
    const cur   = this.currentPage();
    if (total <= 7) return Array.from({ length: total }, (_, i) => i + 1);
    const pages: number[] = [1];
    if (cur > 3) pages.push(-1);
    for (let i = Math.max(2, cur - 1); i <= Math.min(total - 1, cur + 1); i++) pages.push(i);
    if (cur < total - 2) pages.push(-1);
    pages.push(total);
    return pages;
  }

  get pageStart() { return ((this.currentPage() - 1) * this.pageSize) + 1; }
  get pageEnd()   { return Math.min(this.currentPage() * this.pageSize, this.result()?.totalCount ?? 0); }

  getStatusClass(s: string) {
    return ({ Active: 'badge-active', Inactive: 'badge-inactive', Suspended: 'badge-suspended' } as Record<string,string>)[s] ?? 'badge-inactive';
  }
  formatDate(d: string) {
    const utc = d.endsWith('Z') || d.includes('+') ? d : d + 'Z';
    return new Date(utc).toLocaleDateString('en-CA');
  }

  openCreateModal() {
    this.editingUser.set(null); this.saveError.set(null); this.selectedRoleIds = [];
    this.userForm.reset({ status: 'Active' });
    this.userForm.get('email')?.enable();
    this.userForm.get('password')?.enable();
    this.userForm.get('password')?.setValidators([Validators.required, Validators.minLength(6)]);
    this.userForm.get('password')?.updateValueAndValidity();
    this.modalOpen.set(true);
  }

  openEditModal(user: User) {
    this.editingUser.set(user); this.saveError.set(null);
    this.selectedRoleIds = this.roles().filter(r => user.roles.includes(r.name)).map(r => r.id);
    this.userForm.patchValue({ firstName: user.firstName, lastName: user.lastName, department: user.department ?? '', status: user.status });
    this.userForm.get('email')?.disable();
    this.userForm.get('password')?.disable();
    this.userForm.get('password')?.clearValidators();
    this.userForm.get('password')?.updateValueAndValidity();
    this.modalOpen.set(true);
  }

  closeModal() { this.modalOpen.set(false); this.saveError.set(null); }
  isRoleSelected(id: number) { return this.selectedRoleIds.includes(id); }
  toggleRole(id: number) {
    this.selectedRoleIds = this.selectedRoleIds.includes(id)
      ? this.selectedRoleIds.filter(r => r !== id) : [...this.selectedRoleIds, id];
  }

  saveUser() {
    if (this.userForm.invalid) { this.userForm.markAllAsTouched(); return; }
    this.saving.set(true); this.saveError.set(null);
    const v = this.userForm.getRawValue();
    const obs = this.editingUser()
      ? this.userService.update(this.editingUser()!.id, { firstName: v.firstName!, lastName: v.lastName!, department: v.department || undefined, status: v.status!, roleIds: this.selectedRoleIds })
      : this.userService.create({ firstName: v.firstName!, lastName: v.lastName!, email: v.email!, password: v.password!, department: v.department || undefined, roleIds: this.selectedRoleIds });
    obs.subscribe({
      next: () => { this.saving.set(false); this.closeModal(); this.loadUsers(); },
      error: (err) => { this.saving.set(false); this.saveError.set(err?.error?.message ?? 'Request failed. Check permissions.'); },
    });
  }

  confirmDelete() {
    const user = this.deleteTarget();
    if (!user) return;
    this.deleting.set(true); this.deleteError.set(null);
    this.userService.delete(user.id).subscribe({
      next: () => { this.deleting.set(false); this.deleteTarget.set(null); this.loadUsers(); },
      error: (err) => { this.deleting.set(false); this.deleteError.set(err?.error?.message ?? 'Delete failed. Admin role required.'); },
    });
  }
}
