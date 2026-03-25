import { createContext, useContext, useEffect, useState, useCallback, ReactNode } from 'react';
import { User, UserRole, RegisterRequest, hasRole, hasAnyRole, isAdmin, isStaff } from '@/types';
import { authApi, getErrorMessage, tokenUtils } from '@/api';

interface AuthContextType {
  user: User | null;
  isLoading: boolean;
  isAuthenticated: boolean;
  login: (email: string, password: string) => Promise<void>;
  register: (data: RegisterRequest) => Promise<void>;
  logout: () => Promise<void>;
  refreshUser: () => Promise<void>;
  forgotPassword: (email: string) => Promise<string>;
  resetPassword: (email: string, token: string, newPassword: string, confirmPassword: string) => Promise<string>;
  hasRole: (role: UserRole) => boolean;
  hasAnyRole: (roles: UserRole[]) => boolean;
  isAdmin: boolean;
  isStaff: boolean;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<User | null>(null);
  const [isLoading, setIsLoading] = useState(true);

  const isAuthenticated = !!user;

  // Initialize auth state from stored tokens
  useEffect(() => {
    const initAuth = async () => {
      if (authApi.isAuthenticated()) {
        try {
          const currentUser = await authApi.getCurrentUser();
          setUser(currentUser);
        } catch {
          // Token is invalid, clear it
          tokenUtils.clearTokens();
        }
      }
      setIsLoading(false);
    };

    initAuth();
  }, []);

  const login = useCallback(async (email: string, password: string) => {
    try {
      const response = await authApi.login({ email, password });
      setUser(response.user);
    } catch (error) {
      throw new Error(getErrorMessage(error));
    }
  }, []);

  const register = useCallback(async (data: RegisterRequest) => {
    try {
      const response = await authApi.register(data);
      setUser(response.user);
    } catch (error) {
      throw new Error(getErrorMessage(error));
    }
  }, []);

  const logout = useCallback(async () => {
    try {
      await authApi.logout();
    } finally {
      setUser(null);
    }
  }, []);

  const refreshUser = useCallback(async () => {
    if (authApi.isAuthenticated()) {
      try {
        const currentUser = await authApi.getCurrentUser();
        setUser(currentUser);
      } catch {
        tokenUtils.clearTokens();
        setUser(null);
      }
    }
  }, []);

  const forgotPassword = useCallback(async (email: string): Promise<string> => {
    try {
      const response = await authApi.forgotPassword(email);
      return response.message;
    } catch (error) {
      throw new Error(getErrorMessage(error));
    }
  }, []);

  const resetPassword = useCallback(
    async (email: string, token: string, newPassword: string, confirmPassword: string): Promise<string> => {
      try {
        const response = await authApi.resetPassword({ email, token, newPassword, confirmPassword });
        return response.message;
      } catch (error) {
        throw new Error(getErrorMessage(error));
      }
    },
    []
  );

  const checkHasRole = useCallback(
    (role: UserRole): boolean => {
      return hasRole(user, role);
    },
    [user]
  );

  const checkHasAnyRole = useCallback(
    (roles: UserRole[]): boolean => {
      return hasAnyRole(user, roles);
    },
    [user]
  );

  return (
    <AuthContext.Provider
      value={{
        user,
        isLoading,
        isAuthenticated,
        login,
        register,
        logout,
        refreshUser,
        forgotPassword,
        resetPassword,
        hasRole: checkHasRole,
        hasAnyRole: checkHasAnyRole,
        isAdmin: isAdmin(user),
        isStaff: isStaff(user),
      }}
    >
      {children}
    </AuthContext.Provider>
  );
}

export function useAuth() {
  const context = useContext(AuthContext);
  if (context === undefined) {
    throw new Error('useAuth must be used within an AuthProvider');
  }
  return context;
}
