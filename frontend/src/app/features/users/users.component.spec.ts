import { ComponentFixture, TestBed } from '@angular/core/testing';
import { UsersComponent } from './users.component';
import { HttpClientTestingModule } from '@angular/common/http/testing';
import { RouterTestingModule } from '@angular/router/testing';
import { ReactiveFormsModule } from '@angular/forms';
import { UserService } from '../../core/services/user.service';
import { RoleService } from '../../core/services/role.service';
import { AuthService } from '../../core/auth/auth.service';
import { of } from 'rxjs';

const mockPagedResult = { items: [], totalCount: 0, totalPages: 1, page: 1, pageSize: 10 };

describe('UsersComponent', () => {
  let component: UsersComponent;
  let fixture: ComponentFixture<UsersComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [UsersComponent, HttpClientTestingModule, RouterTestingModule, ReactiveFormsModule],
      providers: [
        { provide: UserService, useValue: { getUsers: () => of(mockPagedResult), create: () => of({}), update: () => of({}), delete: () => of({}) } },
        { provide: RoleService, useValue: { getRoles: () => of([]) } },
        { provide: AuthService, useValue: { user: () => null, isManagerOrAdmin: () => false, isAdmin: () => false } },
      ],
    }).compileComponents();
    fixture = TestBed.createComponent(UsersComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
