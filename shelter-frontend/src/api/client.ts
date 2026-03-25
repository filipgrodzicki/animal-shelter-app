import axios, { AxiosError, AxiosInstance, AxiosRequestConfig, InternalAxiosRequestConfig } from 'axios';
import { ApiError, TOKEN_KEYS, TokenResponse } from '@/types';

const API_BASE_URL = import.meta.env.VITE_API_URL || '/api';

// Create axios instance
export const apiClient: AxiosInstance = axios.create({
  baseURL: API_BASE_URL,
  headers: {
    'Content-Type': 'application/json',
  },
  timeout: 30000,
});

// Token refresh state
let isRefreshing = false;
let failedQueue: Array<{
  resolve: (value: unknown) => void;
  reject: (reason?: unknown) => void;
}> = [];

const processQueue = (error: Error | null, token: string | null = null) => {
  failedQueue.forEach((prom) => {
    if (error) {
      prom.reject(error);
    } else {
      prom.resolve(token);
    }
  });
  failedQueue = [];
};

// Helper to check if token is expired
const isTokenExpired = (): boolean => {
  const expiry = localStorage.getItem(TOKEN_KEYS.ACCESS_TOKEN_EXPIRES);
  if (!expiry) return true;
  // Consider token expired 30 seconds before actual expiry
  return new Date() >= new Date(new Date(expiry).getTime() - 30000);
};

// Helper to check if refresh token is expired
const isRefreshTokenExpired = (): boolean => {
  const expiry = localStorage.getItem(TOKEN_KEYS.REFRESH_TOKEN_EXPIRES);
  if (!expiry) return true;
  return new Date() >= new Date(expiry);
};

// Clear all auth tokens
const clearTokens = () => {
  localStorage.removeItem(TOKEN_KEYS.ACCESS_TOKEN);
  localStorage.removeItem(TOKEN_KEYS.REFRESH_TOKEN);
  localStorage.removeItem(TOKEN_KEYS.ACCESS_TOKEN_EXPIRES);
  localStorage.removeItem(TOKEN_KEYS.REFRESH_TOKEN_EXPIRES);
};

// Set all auth tokens
const setTokens = (response: TokenResponse) => {
  localStorage.setItem(TOKEN_KEYS.ACCESS_TOKEN, response.accessToken);
  localStorage.setItem(TOKEN_KEYS.REFRESH_TOKEN, response.refreshToken);
  localStorage.setItem(TOKEN_KEYS.ACCESS_TOKEN_EXPIRES, response.accessTokenExpiresAt);
  localStorage.setItem(TOKEN_KEYS.REFRESH_TOKEN_EXPIRES, response.refreshTokenExpiresAt);
};

// Refresh the token
const refreshTokens = async (): Promise<string> => {
  const accessToken = localStorage.getItem(TOKEN_KEYS.ACCESS_TOKEN);
  const refreshToken = localStorage.getItem(TOKEN_KEYS.REFRESH_TOKEN);

  if (!accessToken || !refreshToken) {
    throw new Error('No tokens available');
  }

  const response = await axios.post<TokenResponse>(
    `${API_BASE_URL}/auth/refresh`,
    { accessToken, refreshToken },
    { headers: { 'Content-Type': 'application/json' } }
  );

  setTokens(response.data);
  return response.data.accessToken;
};

// Request interceptor - add auth token and handle token refresh
apiClient.interceptors.request.use(
  async (config: InternalAxiosRequestConfig) => {
    // Skip token handling for auth endpoints (except /me and /logout)
    const isAuthEndpoint = config.url?.includes('/auth/');
    const requiresToken = config.url?.includes('/auth/me') || config.url?.includes('/auth/logout');

    if (isAuthEndpoint && !requiresToken) {
      return config;
    }

    const accessToken = localStorage.getItem(TOKEN_KEYS.ACCESS_TOKEN);

    if (accessToken) {
      // Check if token needs refresh before making request
      if (isTokenExpired() && !isRefreshTokenExpired()) {
        if (!isRefreshing) {
          isRefreshing = true;
          try {
            const newToken = await refreshTokens();
            processQueue(null, newToken);
            config.headers.Authorization = `Bearer ${newToken}`;
          } catch (error) {
            processQueue(error as Error, null);
            clearTokens();
            window.location.href = '/login';
            return Promise.reject(error);
          } finally {
            isRefreshing = false;
          }
        } else {
          // Wait for the refresh to complete
          return new Promise((resolve, reject) => {
            failedQueue.push({
              resolve: (token) => {
                config.headers.Authorization = `Bearer ${token}`;
                resolve(config);
              },
              reject: (err) => {
                reject(err);
              },
            });
          });
        }
      } else {
        config.headers.Authorization = `Bearer ${accessToken}`;
      }
    }

    return config;
  },
  (error) => Promise.reject(error)
);

// Response interceptor - handle 401 errors and retry with refreshed token
apiClient.interceptors.response.use(
  (response) => response,
  async (error: AxiosError<ApiError>) => {
    const originalRequest = error.config as InternalAxiosRequestConfig & { _retry?: boolean };

    // Handle 401 Unauthorized
    if (error.response?.status === 401 && !originalRequest._retry) {
      // Check if we have a refresh token and it's not expired
      if (!isRefreshTokenExpired()) {
        if (isRefreshing) {
          // Wait for the refresh to complete
          return new Promise((resolve, reject) => {
            failedQueue.push({
              resolve: (token) => {
                originalRequest.headers.Authorization = `Bearer ${token}`;
                resolve(apiClient(originalRequest));
              },
              reject: (err) => {
                reject(err);
              },
            });
          });
        }

        originalRequest._retry = true;
        isRefreshing = true;

        try {
          const newToken = await refreshTokens();
          processQueue(null, newToken);
          originalRequest.headers.Authorization = `Bearer ${newToken}`;
          return apiClient(originalRequest);
        } catch (refreshError) {
          processQueue(refreshError as Error, null);
          clearTokens();
          window.location.href = '/login';
          return Promise.reject(refreshError);
        } finally {
          isRefreshing = false;
        }
      } else {
        // Refresh token is expired, redirect to login
        clearTokens();
        window.location.href = '/login';
      }
    }

    return Promise.reject(error);
  }
);

// Generic API methods
export async function get<T>(url: string, config?: AxiosRequestConfig): Promise<T> {
  const response = await apiClient.get<T>(url, config);
  return response.data;
}

export async function post<T, D = unknown>(url: string, data?: D, config?: AxiosRequestConfig): Promise<T> {
  const response = await apiClient.post<T>(url, data, config);
  return response.data;
}

export async function put<T, D = unknown>(url: string, data?: D, config?: AxiosRequestConfig): Promise<T> {
  const response = await apiClient.put<T>(url, data, config);
  return response.data;
}

export async function patch<T, D = unknown>(url: string, data?: D, config?: AxiosRequestConfig): Promise<T> {
  const response = await apiClient.patch<T>(url, data, config);
  return response.data;
}

export async function del<T>(url: string, config?: AxiosRequestConfig): Promise<T> {
  const response = await apiClient.delete<T>(url, config);
  return response.data;
}

// Error helper
export function getErrorMessage(error: unknown): string {
  if (axios.isAxiosError(error)) {
    const apiError = error.response?.data as ApiError;
    return apiError?.detail || apiError?.title || error.message;
  }
  if (error instanceof Error) {
    return error.message;
  }
  return 'Wystąpił nieznany błąd';
}

// Build query string from params
export function buildQueryString<T extends object>(params: T): string {
  const searchParams = new URLSearchParams();

  Object.entries(params).forEach(([key, value]) => {
    if (value !== undefined && value !== null && value !== '') {
      if (Array.isArray(value)) {
        value.forEach((v) => searchParams.append(key, String(v)));
      } else {
        searchParams.append(key, String(value));
      }
    }
  });

  const queryString = searchParams.toString();
  return queryString ? `?${queryString}` : '';
}

// Export token utilities for use in auth context
export const tokenUtils = {
  clearTokens,
  setTokens,
  isTokenExpired,
  isRefreshTokenExpired,
  getAccessToken: () => localStorage.getItem(TOKEN_KEYS.ACCESS_TOKEN),
  getRefreshToken: () => localStorage.getItem(TOKEN_KEYS.REFRESH_TOKEN),
};
