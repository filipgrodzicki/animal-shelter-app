// Authentication types

export type UserRole = 'Admin' | 'Staff' | 'Volunteer' | 'User';

export interface User {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  avatarUrl?: string;
  phoneNumber?: string;
  roles: UserRole[];
}

// Helper to get full name
export function getUserFullName(user: User): string {
  return `${user.firstName} ${user.lastName}`;
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface AuthResponse {
  user: User;
  accessToken: string;
  accessTokenExpiresAt: string;
  refreshToken: string;
  refreshTokenExpiresAt: string;
}

export interface TokenResponse {
  accessToken: string;
  accessTokenExpiresAt: string;
  refreshToken: string;
  refreshTokenExpiresAt: string;
}

export interface RegisterRequest {
  email: string;
  password: string;
  confirmPassword: string;
  firstName: string;
  lastName: string;
  phoneNumber?: string;
}

export interface ForgotPasswordRequest {
  email: string;
}

export interface ResetPasswordRequest {
  email: string;
  token: string;
  newPassword: string;
  confirmPassword: string;
}

// For backwards compatibility
export interface LoginResponse extends AuthResponse {}

// Token storage keys
export const TOKEN_KEYS = {
  ACCESS_TOKEN: 'auth_access_token',
  REFRESH_TOKEN: 'auth_refresh_token',
  ACCESS_TOKEN_EXPIRES: 'auth_access_token_expires',
  REFRESH_TOKEN_EXPIRES: 'auth_refresh_token_expires',
} as const;

export function hasRole(user: User | null, role: UserRole): boolean {
  return user?.roles.includes(role) ?? false;
}

export function hasAnyRole(user: User | null, roles: UserRole[]): boolean {
  return roles.some(role => user?.roles.includes(role) ?? false);
}

export function isAdmin(user: User | null): boolean {
  return hasRole(user, 'Admin');
}

export function isStaff(user: User | null): boolean {
  return hasAnyRole(user, ['Admin', 'Staff']);
}

export function isVolunteer(user: User | null): boolean {
  return hasRole(user, 'Volunteer');
}
