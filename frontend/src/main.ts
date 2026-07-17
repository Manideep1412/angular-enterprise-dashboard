import { bootstrapApplication } from '@angular/platform-browser';
import { appConfig } from './app/app.config';
import { AppComponent } from './app/app.component';

// Apply saved theme before Angular boots to prevent flash
const savedTheme = localStorage.getItem('ed_theme');
const prefersDark = window.matchMedia('(prefers-color-scheme: dark)').matches;
const isDark = savedTheme ? savedTheme === 'dark' : prefersDark;
document.documentElement.setAttribute('data-theme', isDark ? 'dark' : 'light');

bootstrapApplication(AppComponent, appConfig)
  .catch((err) => console.error(err));
