import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AuditService } from '../../core/services/audit.service';
import { AuditLog } from '../../core/models/audit.models';
import { PagedResult } from '../../core/models/user.models';

interface SortState { field: string; dir: 'asc' | 'desc'; }

const ACTION_ICONS: Record<string, string> = {
  Login: '🔑', Logout: '🚪', Create: '✨', Update: '✏️', Delete: '🗑️', View: '👁️', Export: '📤',
};

const COLS = [
  { label: 'Timestamp', field: 'createdat' },
  { label: 'User',      field: 'useremail' },
  { label: 'Action',    field: 'action' },
  { label: 'Resource',  field: 'resource' },
  { label: 'Description', field: '' },
  { label: 'Severity',  field: 'severity' },
  { label: 'IP Address', field: '' },
];

@Component({
  selector: 'app-audit-logs',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './audit-logs.component.html',
  styleUrl: './audit-logs.component.scss',
})
export class AuditLogsComponent implements OnInit {
  readonly auditService = inject(AuditService);

  result        = signal<PagedResult<AuditLog> | null>(null);
  loading       = signal(true);
  currentPage   = signal(1);
  sort          = signal<SortState>({ field: 'createdat', dir: 'desc' });
  searchQuery:    string   = '';
  actionFilter:   string[] = [];
  severityFilter: string[] = [];
  pageSize = 10;

  readonly actions    = ['Login', 'Logout', 'Create', 'Update', 'Delete', 'View', 'Export'];
  readonly severities = ['Info', 'Warning', 'Critical'];
  readonly cols       = COLS;
  get hasFilters()    { return !!(this.searchQuery || this.actionFilter.length || this.severityFilter.length); }

  ngOnInit() { this.loadLogs(); }

  loadLogs() {
    this.loading.set(true);
    const s = this.sort();
    this.auditService.getLogs(this.currentPage(), this.pageSize, this.actionFilter.length ? this.actionFilter : undefined, this.severityFilter.length ? this.severityFilter : undefined, this.searchQuery || undefined, s.field, s.dir)
      .subscribe({ next: r => { this.result.set(r); this.loading.set(false); }, error: () => this.loading.set(false) });
  }

  resetAndLoad()     { this.currentPage.set(1); this.loadLogs(); }
  onPageSizeChange() { this.currentPage.set(1); this.loadLogs(); }
  changePage(p: number) { this.currentPage.set(p); this.loadLogs(); }

  setSort(field: string) {
    const cur = this.sort();
    this.sort.set({ field, dir: cur.field === field && cur.dir === 'desc' ? 'asc' : 'desc' });
    this.currentPage.set(1);
    this.loadLogs();
  }

  toggleAction(a: string)   { this.actionFilter   = this.actionFilter.includes(a)   ? this.actionFilter.filter(x => x !== a)   : [...this.actionFilter, a];   this.resetAndLoad(); }
  toggleSeverity(s: string) { this.severityFilter = this.severityFilter.includes(s) ? this.severityFilter.filter(x => x !== s) : [...this.severityFilter, s]; this.resetAndLoad(); }

  severityChipStyle(s: string): object {
    if (!this.severityFilter.includes(s)) return {};
    const map: Record<string, object> = {
      Info:     { background: 'rgba(79,142,247,0.15)',  color: 'var(--accent-blue)',   borderColor: 'var(--accent-blue)' },
      Warning:  { background: 'rgba(245,158,11,0.15)', color: '#f59e0b',               borderColor: '#f59e0b' },
      Critical: { background: 'rgba(239,68,68,0.15)',  color: 'var(--accent-red)',     borderColor: 'var(--accent-red)' },
    };
    return map[s] ?? {};
  }

  clearFilters() {
    this.searchQuery = ''; this.actionFilter = []; this.severityFilter = [];
    this.sort.set({ field: 'createdat', dir: 'desc' });
    this.resetAndLoad();
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

  getActionIcon(a: string)    { return ACTION_ICONS[a] ?? '•'; }
  getSeverityClass(s: string) {
    return ({ Info: 'severity-info', Warning: 'severity-warning', Critical: 'severity-critical' } as Record<string,string>)[s] ?? 'severity-info';
  }
  formatDate(d: string) {
    const utc = d.endsWith('Z') || d.includes('+') ? d : d + 'Z';
    return new Date(utc).toLocaleString('en-CA', { month: 'short', day: '2-digit', hour: '2-digit', minute: '2-digit', second: '2-digit' });
  }
}
