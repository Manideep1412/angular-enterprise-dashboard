export interface Permission {
  id: number;
  resource: string;
  action: string;
}

export interface Role {
  id: number;
  name: string;
  description: string;
  color: string;
  userCount: number;
  permissions: Permission[];
}
