import { Component, OnInit, OnDestroy, inject, signal, effect, ElementRef, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { DashboardService } from '../../core/services/dashboard.service';
import { AuthService } from '../../core/auth/auth.service';
import { Chart, registerables } from 'chart.js';

Chart.register(...registerables);

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.scss',
})
export class DashboardComponent implements OnInit, OnDestroy {
  @ViewChild('activityChart') activityChartRef!: ElementRef<HTMLCanvasElement>;
  @ViewChild('roleChart') roleChartRef!: ElementRef<HTMLCanvasElement>;

  readonly dashService = inject(DashboardService);
  readonly auth = inject(AuthService);
  readonly stats = signal<any>(null);
  readonly loading = signal(true);

  private activityChart?: Chart;
  private roleChart?: Chart;
  private chartTimer?: ReturnType<typeof setTimeout>;

  constructor() {
    // Re-init charts whenever stats signal changes and DOM has updated
    effect(() => {
      if (this.stats()) {
        // Two ticks: one for Angular to render @if block, one for canvas to be ready
        clearTimeout(this.chartTimer);
        this.chartTimer = setTimeout(() => this.initCharts(), 50);
      }
    });
  }

  get firstName(): string {
    const parts = this.auth.user()?.fullName?.split(' ') ?? [];
    return parts[0] ?? '';
  }

  get timeOfDay(): string {
    const h = new Date().getHours();
    if (h < 12) return 'morning';
    if (h < 17) return 'afternoon';
    return 'evening';
  }

  ngOnInit() {
    this.dashService.getStats().subscribe({
      next: data => { this.stats.set(data); this.loading.set(false); },
      error: () => this.loading.set(false),
    });
  }

  ngOnDestroy() {
    clearTimeout(this.chartTimer);
    this.activityChart?.destroy();
    this.roleChart?.destroy();
  }

  private initCharts() {
    const s = this.stats();
    if (!s || !this.activityChartRef?.nativeElement) return;

    this.activityChart?.destroy();
    this.activityChart = new Chart(this.activityChartRef.nativeElement, {
      type: 'line',
      data: {
        labels: s.activityChart.map((d: any) => d.date),
        datasets: [{
          label: 'Events',
          data: s.activityChart.map((d: any) => d.count),
          borderColor: '#4f8ef7',
          backgroundColor: 'rgba(79,142,247,0.08)',
          borderWidth: 2,
          fill: true,
          tension: 0.4,
          pointBackgroundColor: '#4f8ef7',
          pointBorderColor: '#0d0d1a',
          pointBorderWidth: 2,
          pointRadius: 5,
        }],
      },
      options: {
        responsive: true,
        maintainAspectRatio: false,
        plugins: { legend: { display: false } },
        scales: {
          x: {
            grid: { color: 'rgba(30,30,58,0.6)' },
            ticks: { color: '#475569', font: { size: 11 } },
            border: { color: 'rgba(30,30,58,0.8)' },
          },
          y: {
            grid: { color: 'rgba(30,30,58,0.6)' },
            ticks: { color: '#475569', font: { size: 11 }, stepSize: 1 },
            border: { color: 'rgba(30,30,58,0.8)' },
            beginAtZero: true,
          },
        },
      },
    });

    this.roleChart?.destroy();
    this.roleChart = new Chart(this.roleChartRef.nativeElement, {
      type: 'doughnut',
      data: {
        labels: s.roleDistribution.map((r: any) => r.role),
        datasets: [{
          data: s.roleDistribution.map((r: any) => r.count),
          backgroundColor: ['#ef4444', '#a855f7', '#22c55e', '#4f8ef7'],
          borderColor: '#0d0d1a',
          borderWidth: 3,
          hoverOffset: 6,
        }],
      },
      options: {
        responsive: true,
        maintainAspectRatio: false,
        plugins: { legend: { display: false } },
        cutout: '70%',
      },
    });
  }

  formatDate(date: string): string {
    return new Date(date).toLocaleTimeString('en-CA', { hour: '2-digit', minute: '2-digit' });
  }
}
