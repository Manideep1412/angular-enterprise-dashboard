import { ComponentFixture, TestBed } from '@angular/core/testing';
import { AuditLogsComponent } from './audit-logs.component';
import { HttpClientTestingModule } from '@angular/common/http/testing';
import { RouterTestingModule } from '@angular/router/testing';
import { AuditService } from '../../core/services/audit.service';
import { of } from 'rxjs';

const mockPagedResult = { items: [], totalCount: 0, totalPages: 1, page: 1, pageSize: 10 };

describe('AuditLogsComponent', () => {
  let component: AuditLogsComponent;
  let fixture: ComponentFixture<AuditLogsComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [AuditLogsComponent, HttpClientTestingModule, RouterTestingModule],
      providers: [
        { provide: AuditService, useValue: { getLogs: () => of(mockPagedResult) } },
      ],
    }).compileComponents();
    fixture = TestBed.createComponent(AuditLogsComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
