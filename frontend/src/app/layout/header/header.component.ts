import { Component, OnInit, Output, EventEmitter, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../core/auth/auth.service';
import { ThemeService } from '../../core/services/theme.service';
import { AuditService } from '../../core/services/audit.service';
import { AuditLog } from '../../core/models/audit.models';

interface Notification {
  id: number;
  icon: string;
  title: string;
  desc: string;
  time: string;
  unread: boolean;
  severity: string;
}

const ACTION_ICONS: Record<string, string> = {
  Login: '🔑', Logout: '🚪', Create: '✨', Update: '✏️',
  Delete: '🗑️', View: '👁️', Export: '📤',
};

const ACTION_TITLES: Record<string, string> = {
  Login: 'User signed in', Logout: 'User signed out',
  Create: 'Record created', Update: 'Record updated',
  Delete: 'Record deleted', View: 'Resource viewed', Export: 'Data exported',
};

@Component({
  selector: 'app-header',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './header.component.html',
  styleUrl: './header.component.scss',
})
export class HeaderComponent implements OnInit {
  @Output() menuClick = new EventEmitter<void>();

  private readonly SEEN_KEY = 'ed_notif_seen_at';

  readonly auth         = inject(AuthService);
  readonly router       = inject(Router);
  readonly theme        = inject(ThemeService);
  readonly auditService = inject(AuditService);

  readonly dropdownOpen   = signal(false);
  readonly notifOpen      = signal(false);
  readonly notifications  = signal<Notification[]>([]);
  readonly notifLoading   = signal(false);

  readonly unreadCount = computed(() =>
    this.notifications().filter(n => n.unread).length
  );

  ngOnInit() { this.loadNotifications(); }

  loadNotifications() {
    this.notifLoading.set(true);
    this.auditService.getLogs(1, 8, undefined, undefined, undefined, 'createdat', 'desc')
      .subscribe({
        next: result => {
          const seenAt = localStorage.getItem(this.SEEN_KEY);
          const seenDate = seenAt ? new Date(seenAt) : null;
          this.notifications.set(
            result.items.map(log => this.toNotification(log, seenDate))
          );
          this.notifLoading.set(false);
        },
        error: () => this.notifLoading.set(false),
      });
  }

  private toNotification(log: AuditLog, seenDate: Date | null): Notification {
    // Backend omits 'Z' — force UTC parse so relative time is correct
    const raw = log.createdAt.endsWith('Z') || log.createdAt.includes('+') ? log.createdAt : log.createdAt + 'Z';
    const logDate = new Date(raw);
    return {
      id:       log.id,
      icon:     ACTION_ICONS[log.action]  ?? '📋',
      title:    ACTION_TITLES[log.action] ?? log.action,
      desc:     log.description,
      time:     this.relativeTime(logDate),
      unread:   seenDate ? logDate > seenDate : true,
      severity: log.severity,
    };
  }

  private relativeTime(date: Date): string {
    const seconds = Math.floor((Date.now() - date.getTime()) / 1000);
    if (seconds < 5)    return 'just now';
    if (seconds < 60)   return `${seconds}s ago`;
    if (seconds < 3600) return `${Math.floor(seconds / 60)}m ago`;
    if (seconds < 86400) return `${Math.floor(seconds / 3600)}h ago`;
    return `${Math.floor(seconds / 86400)}d ago`;
  }

  markAllRead() {
    localStorage.setItem(this.SEEN_KEY, new Date().toISOString());
    this.notifications.update(list => list.map(n => ({ ...n, unread: false })));
  }

  toggleNotif() {
    const opening = !this.notifOpen();
    this.notifOpen.set(opening);
    if (opening) {
      this.dropdownOpen.set(false);
      this.loadNotifications();   // refresh on open
    }
  }

  toggleDropdown() {
    this.dropdownOpen.update(v => !v);
    if (this.notifOpen()) this.notifOpen.set(false);
  }

  get initials(): string {
    const name  = this.auth.user()?.fullName ?? '';
    const parts = name.split(' ');
    return `${parts[0]?.charAt(0) ?? ''}${parts[1]?.charAt(0) ?? ''}`.toUpperCase();
  }

  get pageTitle(): string {
    const path = this.router.url.split('/')[1] ?? 'dashboard';
    return path.charAt(0).toUpperCase() + path.slice(1).replace('-', ' ');
  }

  get currentDate(): string {
    return new Date().toLocaleDateString('en-CA', {
      weekday: 'long', year: 'numeric', month: 'long', day: 'numeric',
    });
  }

  logout() {
    this.dropdownOpen.set(false);
    this.auth.logout();
  }
}
