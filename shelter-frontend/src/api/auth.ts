import { post, get } from './client';
import {
  User,
  LoginRequest,
  AuthResponse,
  TokenResponse,
  RegisterRequest,
  ResetPasswordRequest,
  TOKEN_KEYS,
} from '@/types';

const BASE_URL = '/auth';

// Token management helpers
const tokenManager = {
  getAccessToken: (): string | null => {
    return localStorage.getItem(TOKEN_KEYS.ACCESS_TOKEN);
  },

  getRefreshToken: (): string | null => {
    return localStorage.getItem(TOKEN_KEYS.REFRESH_TOKEN);
  },

  getAccessTokenExpiry: (): Date | null => {
    const expiry = localStorage.getItem(TOKEN_KEYS.ACCESS_TOKEN_EXPIRES);
    return expiry ? new Date(expiry) : null;
  },

  setTokens: (response: AuthResponse | TokenResponse) => {
    localStorage.setItem(TOKEN_KEYS.ACCESS_TOKEN, response.accessToken);
    localStorage.setItem(TOKEN_KEYS.REFRESH_TOKEN, response.refreshToken);
    localStorage.setItem(TOKEN_KEYS.ACCESS_TOKEN_EXPIRES, response.accessTokenExpiresAt);
    localStorage.setItem(TOKEN_KEYS.REFRESH_TOKEN_EXPIRES, response.refreshTokenExpiresAt);
  },

  clearTokens: () => {
    localStorage.removeItem(TOKEN_KEYS.ACCESS_TOKEN);
    localStorage.removeItem(TOKEN_KEYS.REFRESH_TOKEN);
    localStorage.removeItem(TOKEN_KEYS.ACCESS_TOKEN_EXPIRES);
    localStorage.removeItem(TOKEN_KEYS.REFRESH_TOKEN_EXPIRES);
  },

  isAccessTokenExpired: (): boolean => {
    const expiry = tokenManager.getAccessTokenExpiry();
    if (!expiry) return true;
    // Consider token expired 30 seconds before actual expiry
    return new Date() >= new Date(expiry.getTime() - 30000);
  },

  isRefreshTokenExpired: (): boolean => {
    const expiry = localStorage.getItem(TOKEN_KEYS.REFRESH_TOKEN_EXPIRES);
    if (!expiry) return true;
    return new Date() >= new Date(expiry);
  },
};

export const authApi = {
  // Login
  login: async (data: LoginRequest): Promise<AuthResponse> => {
    const response = await post<AuthResponse>(`${BASE_URL}/login`, data);
    tokenManager.setTokens(response);
    return response;
  },

  // Register
  register: async (data: RegisterRequest): Promise<AuthResponse> => {
    const response = await post<AuthResponse>(`${BASE_URL}/register`, data);
    tokenManager.setTokens(response);
    return response;
  },

  // Get current user
  getCurrentUser: async (): Promise<User> => {
    return get<User>(`${BASE_URL}/me`);
  },

  // Logout
  logout: async () => {
    const refreshToken = tokenManager.getRefreshToken();
    try {
      if (refreshToken) {
        await post(`${BASE_URL}/logout`, { refreshToken });
      }
    } catch {
      // Ignore errors during logout
    } finally {
      tokenManager.clearTokens();
    }
  },

  // Logout (synchronous - for use in interceptors)
  logoutSync: () => {
    tokenManager.clearTokens();
  },

  // Check if authenticated
  isAuthenticated: (): boolean => {
    return !!tokenManager.getAccessToken() && !tokenManager.isRefreshTokenExpired();
  },

  // Get token
  getToken: (): string | null => {
    return tokenManager.getAccessToken();
  },

  // Check if token needs refresh
  needsRefresh: (): boolean => {
    return tokenManager.isAccessTokenExpired() && !tokenManager.isRefreshTokenExpired();
  },

  // Refresh token
  refreshToken: async (): Promise<TokenResponse> => {
    const accessToken = tokenManager.getAccessToken();
    const refreshToken = tokenManager.getRefreshToken();

    if (!accessToken || !refreshToken) {
      throw new Error('No tokens available');
    }

    const response = await post<TokenResponse>(`${BASE_URL}/refresh`, {
      accessToken,
      refreshToken,
    });

    tokenManager.setTokens(response);
    return response;
  },

  // Forgot password
  forgotPassword: async (email: string): Promise<{ message: string }> => {
    return post<{ message: string }>(`${BASE_URL}/forgot-password`, { email });
  },

  // Reset password
  resetPassword: async (data: ResetPasswordRequest): Promise<{ message: string }> => {
    return post<{ message: string }>(`${BASE_URL}/reset-password`, data);
  },

  // Export token manager for use in axios interceptor
  tokenManager,
};

export interface ChangePasswordRequest {
  currentPassword: string;
  newPassword: string;
}
