export interface AuditLog {
  id: number;
  userId?: number;
  userEmail: string;
  action: string;
  resource: string;
  description: string;
  severity: 'Info' | 'Warning' | 'Critical';
  ipAddress?: string;
  createdAt: string;
}
