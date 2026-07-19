import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ReactiveFormsModule } from '@angular/forms';
import { provideRouter } from '@angular/router';
import { signal } from '@angular/core';
import { of } from 'rxjs';
import { LoginComponent } from './login.component';
import { AuthService } from '../../../core/auth/auth.service';

function makeAuthMock() {
  return {
    login: jasmine.createSpy('login').and.returnValue(of({})),
    isLoading: signal(false),
    error: signal<string | null>(null),
    isAuthenticated: signal(false),
  };
}

describe('LoginComponent', () => {
  let component: LoginComponent;
  let fixture: ComponentFixture<LoginComponent>;
  let authMock: any;

  beforeEach(async () => {
    authMock = makeAuthMock();

    await TestBed.configureTestingModule({
      imports: [LoginComponent],
      providers: [
        provideRouter([]),
        { provide: AuthService, useValue: authMock },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(LoginComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('creates the component', () => expect(component).toBeTruthy());

  // ── Form ──────────────────────────────────────────────────────────────────

  it('initializes form with default credentials', () => {
    expect(component.form.get('email')?.value).toBe('admin@enterprise.dev');
    expect(component.form.get('password')?.value).toBe('Admin@123');
  });

  it('form is valid with default values', () => {
    expect(component.form.valid).toBeTrue();
  });

  it('form is invalid when email is blank', () => {
    component.form.patchValue({ email: '' });
    expect(component.form.invalid).toBeTrue();
  });

  it('form is invalid when email has wrong format', () => {
    component.form.patchValue({ email: 'not-an-email' });
    expect(component.form.invalid).toBeTrue();
  });

  it('form is invalid when password is too short', () => {
    component.form.patchValue({ password: 'abc' });
    expect(component.form.invalid).toBeTrue();
  });

  // ── isFieldInvalid() ──────────────────────────────────────────────────────

  it('isFieldInvalid returns false for untouched invalid field', () => {
    component.form.patchValue({ email: '' });
    expect(component.isFieldInvalid('email')).toBeFalse();
  });

  it('isFieldInvalid returns true for touched and invalid field', () => {
    component.form.patchValue({ email: 'bad' });
    component.form.get('email')?.markAsTouched();
    expect(component.isFieldInvalid('email')).toBeTrue();
  });

  it('isFieldInvalid returns false for valid and touched field', () => {
    component.form.patchValue({ email: 'good@email.com' });
    component.form.get('email')?.markAsTouched();
    expect(component.isFieldInvalid('email')).toBeFalse();
  });

  // ── onSubmit() ────────────────────────────────────────────────────────────

  it('onSubmit marks all fields touched when form is invalid', () => {
    component.form.patchValue({ email: '' });
    component.onSubmit();
    expect(component.form.get('email')?.touched).toBeTrue();
    expect(authMock.login).not.toHaveBeenCalled();
  });

  it('onSubmit calls auth.login with email and password when form is valid', () => {
    component.form.patchValue({ email: 'user@test.com', password: 'Pass@123' });
    component.onSubmit();
    expect(authMock.login).toHaveBeenCalledOnceWith({ email: 'user@test.com', password: 'Pass@123' });
  });

  // ── showPassword signal ───────────────────────────────────────────────────

  it('showPassword starts as false', () => {
    expect(component.showPassword()).toBeFalse();
  });

  it('showPassword can be set to true', () => {
    component.showPassword.set(true);
    expect(component.showPassword()).toBeTrue();
  });
});
