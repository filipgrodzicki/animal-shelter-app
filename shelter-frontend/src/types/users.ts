// User management types
// Note: UserRole is defined in auth.ts

export interface UserListItem {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  fullName: string;
  phoneNumber: string | null;
  roles: string[];
  isActive: boolean;
  emailConfirmed: boolean;
  createdAt: string;
  lastLoginAt: string | null;
}

export interface UserDetail {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  fullName: string;
  phoneNumber: string | null;
  dateOfBirth: string | null;
  address: string | null;
  city: string | null;
  postalCode: string | null;
  avatarUrl: string | null;
  roles: string[];
  isActive: boolean;
  emailConfirmed: boolean;
  createdAt: string;
  updatedAt: string | null;
  lastLoginAt: string | null;
}

export interface RoleDto {
  name: string;
  description: string | null;
}

export interface CreateUserRequest {
  email: string;
  password: string;
  firstName: string;
  lastName: string;
  role: string;
  phoneNumber?: string;
  dateOfBirth?: string;
  address?: string;
  city?: string;
  postalCode?: string;
}

export interface UpdateUserRequest {
  firstName?: string;
  lastName?: string;
  phoneNumber?: string;
  dateOfBirth?: string;
  address?: string;
  city?: string;
  postalCode?: string;
}

export interface ChangeRoleRequest {
  newRole: string;
}

export interface ResetPasswordRequest {
  newPassword: string;
}

export interface UserFilters {
  role?: string;
  searchTerm?: string;
  isActive?: boolean;
  sortBy?: string;
  sortDescending?: boolean;
}

// Helper functions
export function getRoleLabel(role: string): string {
  const labels: Record<string, string> = {
    Admin: 'Administrator',
    Staff: 'Pracownik',
    Volunteer: 'Wolontariusz',
    User: 'Uzytkownik',
  };
  return labels[role] || role;
}

export function getRoleBadgeColor(role: string): 'red' | 'blue' | 'green' | 'gray' {
  const colors: Record<string, 'red' | 'blue' | 'green' | 'gray'> = {
    Admin: 'red',
    Staff: 'blue',
    Volunteer: 'green',
    User: 'gray',
  };
  return colors[role] || 'gray';
}
