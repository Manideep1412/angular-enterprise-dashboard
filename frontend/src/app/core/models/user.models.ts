export interface User {
  id: number;
  firstName: string;
  lastName: string;
  fullName: string;
  email: string;
  department?: string;
  status: 'Active' | 'Inactive' | 'Suspended';
  roles: string[];
  lastLoginAt?: string;
  createdAt: string;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

export interface CreateUserRequest {
  firstName: string;
  lastName: string;
  email: string;
  password: string;
  department?: string;
  roleIds: number[];
}

export interface UpdateUserRequest {
  firstName: string;
  lastName: string;
  department?: string;
  status: string;
  roleIds: number[];
}

export interface ApiResponse<T> {
  data: T;
  success: boolean;
  message?: string;
}
