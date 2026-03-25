import { get, post, put, buildQueryString } from './client';
import {
  UserListItem,
  UserDetail,
  RoleDto,
  CreateUserRequest,
  UpdateUserRequest,
  ChangeRoleRequest,
  AdminResetPasswordRequest,
  UserFilters,
  PagedResult,
  PaginationParams,
} from '@/types';

const BASE_URL = '/users';

export interface GetUsersParams extends PaginationParams, UserFilters {}

export const usersApi = {
  // Get paginated list of users
  getUsers: async (params: GetUsersParams): Promise<PagedResult<UserListItem>> => {
    const queryString = buildQueryString(params);
    return get<PagedResult<UserListItem>>(`${BASE_URL}${queryString}`);
  },

  // Get user by ID
  getUser: async (id: string): Promise<UserDetail> => {
    return get<UserDetail>(`${BASE_URL}/${id}`);
  },

  // Get available roles
  getRoles: async (): Promise<RoleDto[]> => {
    return get<RoleDto[]>(`${BASE_URL}/roles`);
  },

  // Create new user
  create: async (data: CreateUserRequest): Promise<UserDetail> => {
    return post<UserDetail>(BASE_URL, data);
  },

  // Update user
  update: async (id: string, data: UpdateUserRequest): Promise<UserDetail> => {
    return put<UserDetail>(`${BASE_URL}/${id}`, data);
  },

  // Change user role
  changeRole: async (id: string, data: ChangeRoleRequest): Promise<UserDetail> => {
    return put<UserDetail>(`${BASE_URL}/${id}/role`, data);
  },

  // Deactivate user
  deactivate: async (id: string): Promise<UserDetail> => {
    return put<UserDetail>(`${BASE_URL}/${id}/deactivate`, {});
  },

  // Activate user
  activate: async (id: string): Promise<UserDetail> => {
    return put<UserDetail>(`${BASE_URL}/${id}/activate`, {});
  },

  // Reset user password
  resetPassword: async (id: string, data: AdminResetPasswordRequest): Promise<{ message: string }> => {
    return put<{ message: string }>(`${BASE_URL}/${id}/reset-password`, data);
  },
};
