export interface LoginRequest {
  email: string;
  password: string;
}

export interface UserInfo {
  id: number;
  fullName: string;
  email: string;
  department: string;
  roles: string[];
}

export interface LoginResponse {
  accessToken: string;
  tokenType: string;
  expiresIn: number;
  user: UserInfo;
}

export interface AuthState {
  user: UserInfo | null;
  token: string | null;
  isAuthenticated: boolean;
  isLoading: boolean;
  error: string | null;
}
