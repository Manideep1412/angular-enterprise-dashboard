import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RoleService } from '../../core/services/role.service';
import { Role } from '../../core/models/role.models';

const RESOURCES = ['users', 'roles', 'audit'];
const ACTIONS = ['read', 'write', 'delete', 'export'];

@Component({
  selector: 'app-roles',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './roles.component.html',
  styleUrl: './roles.component.scss',
})
export class RolesComponent implements OnInit {
  readonly roleService = inject(RoleService);
  roles = signal<Role[]>([]);
  selectedRole = signal<Role | null>(null);
  loading = signal(true);

  readonly resources = RESOURCES;
  readonly actions = ACTIONS;

  ngOnInit() {
    this.roleService.getRoles().subscribe({
      next: r => { this.roles.set(r); this.loading.set(false); },
      error: () => this.loading.set(false),
    });
  }

  hasPermission(role: Role, resource: string, action: string): boolean {
    return role.permissions.some(p => p.resource === resource && p.action === action);
  }

  uniqueResources(role: Role): string[] {
    return [...new Set(role.permissions.map(p => p.resource))];
  }
}
