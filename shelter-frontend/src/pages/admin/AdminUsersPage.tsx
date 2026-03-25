import { useState, useEffect } from 'react';
import {
  PlusIcon,
  PencilIcon,
  KeyIcon,
  UserPlusIcon,
  UserMinusIcon,
  ShieldCheckIcon,
} from '@heroicons/react/24/outline';
import { PageContainer } from '@/components/layout';
import { Button, Card, Badge, Input, Select, Spinner, Modal } from '@/components/common';
import { usersApi, GetUsersParams } from '@/api/users';
import { getErrorMessage } from '@/api/client';
import {
  UserListItem,
  UserDetail,
  RoleDto,
  CreateUserRequest,
  UpdateUserRequest,
  getRoleLabel,
  getRoleBadgeColor,
} from '@/types';
import toast from 'react-hot-toast';

const roleFilterOptions = [
  { value: '', label: 'Wszystkie role' },
  { value: 'Admin', label: 'Administrator' },
  { value: 'Staff', label: 'Pracownik' },
  { value: 'Volunteer', label: 'Wolontariusz' },
  { value: 'User', label: 'Uzytkownik' },
];

const statusFilterOptions = [
  { value: '', label: 'Wszyscy' },
  { value: 'true', label: 'Aktywni' },
  { value: 'false', label: 'Nieaktywni' },
];

export function AdminUsersPage() {
  const [users, setUsers] = useState<UserListItem[]>([]);
  const [roles, setRoles] = useState<RoleDto[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [searchTerm, setSearchTerm] = useState('');
  const [roleFilter, setRoleFilter] = useState('');
  const [statusFilter, setStatusFilter] = useState('');
  const [page, setPage] = useState(1);
  const [totalPages, setTotalPages] = useState(1);
  const [totalCount, setTotalCount] = useState(0);

  // Modal states
  const [isUserModalOpen, setIsUserModalOpen] = useState(false);
  const [editingUser, setEditingUser] = useState<UserDetail | null>(null);
  const [isRoleModalOpen, setIsRoleModalOpen] = useState(false);
  const [selectedUserForRole, setSelectedUserForRole] = useState<UserListItem | null>(null);
  const [isPasswordModalOpen, setIsPasswordModalOpen] = useState(false);
  const [selectedUserForPassword, setSelectedUserForPassword] = useState<UserListItem | null>(null);

  const fetchUsers = async () => {
    setIsLoading(true);
    setError(null);
    try {
      const params: GetUsersParams = {
        page,
        pageSize: 10,
        searchTerm: searchTerm || undefined,
        role: roleFilter || undefined,
        isActive: statusFilter ? statusFilter === 'true' : undefined,
        sortBy: 'createdAt',
        sortDescending: true,
      };
      const result = await usersApi.getUsers(params);
      setUsers(result.items);
      setTotalPages(result.totalPages);
      setTotalCount(result.totalCount);
    } catch (err) {
      setError(getErrorMessage(err));
    } finally {
      setIsLoading(false);
    }
  };

  const fetchRoles = async () => {
    try {
      const result = await usersApi.getRoles();
      setRoles(result);
    } catch (err) {
      console.error('Failed to fetch roles:', err);
    }
  };

  useEffect(() => {
    fetchRoles();
  }, []);

  useEffect(() => {
    fetchUsers();
  }, [page, searchTerm, roleFilter, statusFilter]);

  const handleCreateUser = () => {
    setEditingUser(null);
    setIsUserModalOpen(true);
  };

  const handleEditUser = async (user: UserListItem) => {
    try {
      const detail = await usersApi.getUser(user.id);
      setEditingUser(detail);
      setIsUserModalOpen(true);
    } catch (err) {
      toast.error(getErrorMessage(err));
    }
  };

  const handleChangeRole = (user: UserListItem) => {
    setSelectedUserForRole(user);
    setIsRoleModalOpen(true);
  };

  const handleResetPassword = (user: UserListItem) => {
    setSelectedUserForPassword(user);
    setIsPasswordModalOpen(true);
  };

  const handleToggleActive = async (user: UserListItem) => {
    const action = user.isActive ? 'dezaktywowac' : 'aktywowac';
    if (!confirm(`Czy na pewno chcesz ${action} uzytkownika ${user.fullName}?`)) return;

    try {
      if (user.isActive) {
        await usersApi.deactivate(user.id);
        toast.success('Uzytkownik zostal dezaktywowany');
      } else {
        await usersApi.activate(user.id);
        toast.success('Uzytkownik zostal aktywowany');
      }
      fetchUsers();
    } catch (err) {
      toast.error(getErrorMessage(err));
    }
  };

  // Stats
  const activeCount = users.filter(u => u.isActive).length;
  const adminCount = users.filter(u => u.roles.includes('Admin')).length;
  const staffCount = users.filter(u => u.roles.includes('Staff')).length;

  return (
    <PageContainer>
      <div className="mb-8">
        <h1 className="text-3xl font-bold text-gray-900">Zarzadzanie uzytkownikami</h1>
        <p className="mt-2 text-gray-600">Tworzenie, edycja i zarzadzanie kontami uzytkownikow</p>
      </div>

      {/* Stats */}
      <div className="grid grid-cols-2 md:grid-cols-4 gap-4 mb-6">
        <Card className="p-4">
          <p className="text-sm text-gray-500">Wszystkich</p>
          <p className="text-2xl font-bold text-gray-900">{totalCount}</p>
        </Card>
        <Card className="p-4">
          <p className="text-sm text-gray-500">Aktywnych</p>
          <p className="text-2xl font-bold text-green-600">{activeCount}</p>
        </Card>
        <Card className="p-4">
          <p className="text-sm text-gray-500">Administratorow</p>
          <p className="text-2xl font-bold text-red-600">{adminCount}</p>
        </Card>
        <Card className="p-4">
          <p className="text-sm text-gray-500">Pracownikow</p>
          <p className="text-2xl font-bold text-blue-600">{staffCount}</p>
        </Card>
      </div>

      {/* Filters */}
      <div className="flex flex-wrap items-center gap-3 mb-6">
        <Button onClick={handleCreateUser} leftIcon={<PlusIcon className="h-5 w-5" />}>
          Dodaj uzytkownika
        </Button>
        <Input
          placeholder="Szukaj po nazwie lub email..."
          value={searchTerm}
          onChange={(e) => setSearchTerm(e.target.value)}
          wrapperClassName="w-72"
        />
        <Select
          options={roleFilterOptions}
          value={roleFilter}
          onChange={(e) => { setRoleFilter(e.target.value); setPage(1); }}
          wrapperClassName="w-44"
        />
        <Select
          options={statusFilterOptions}
          value={statusFilter}
          onChange={(e) => { setStatusFilter(e.target.value); setPage(1); }}
          wrapperClassName="w-36"
        />
      </div>

      {/* Users Table */}
      <Card className="overflow-hidden">
        {isLoading ? (
          <div className="p-8 flex justify-center">
            <Spinner size="lg" />
          </div>
        ) : error ? (
          <div className="p-8 text-center text-red-600">{error}</div>
        ) : users.length === 0 ? (
          <div className="p-8 text-center text-gray-500">Brak uzytkownikow</div>
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full">
              <thead className="bg-gray-50 border-b border-gray-200">
                <tr>
                  <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase">
                    Uzytkownik
                  </th>
                  <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase">
                    Rola
                  </th>
                  <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase">
                    Status
                  </th>
                  <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase">
                    Ostatnie logowanie
                  </th>
                  <th className="px-4 py-3 text-right text-xs font-medium text-gray-500 uppercase">
                    Akcje
                  </th>
                </tr>
              </thead>
              <tbody className="divide-y divide-gray-200">
                {users.map((user) => (
                  <tr key={user.id} className="hover:bg-gray-50">
                    <td className="px-4 py-4">
                      <div>
                        <p className="font-medium text-gray-900">{user.fullName}</p>
                        <p className="text-sm text-gray-500">{user.email}</p>
                      </div>
                    </td>
                    <td className="px-4 py-4 whitespace-nowrap">
                      {user.roles.map((role) => (
                        <Badge key={role} variant={getRoleBadgeColor(role)} className="mr-1">
                          {getRoleLabel(role)}
                        </Badge>
                      ))}
                    </td>
                    <td className="px-4 py-4 whitespace-nowrap">
                      <Badge variant={user.isActive ? 'green' : 'gray'}>
                        {user.isActive ? 'Aktywny' : 'Nieaktywny'}
                      </Badge>
                    </td>
                    <td className="px-4 py-4 whitespace-nowrap text-sm text-gray-500">
                      {user.lastLoginAt
                        ? new Date(user.lastLoginAt).toLocaleString('pl-PL')
                        : 'Nigdy'}
                    </td>
                    <td className="px-4 py-4 whitespace-nowrap text-right">
                      <div className="flex justify-end gap-1">
                        <button
                          onClick={() => handleEditUser(user)}
                          className="p-1.5 text-gray-500 hover:text-primary-600 hover:bg-gray-100 rounded"
                          title="Edytuj"
                        >
                          <PencilIcon className="h-5 w-5" />
                        </button>
                        <button
                          onClick={() => handleChangeRole(user)}
                          className="p-1.5 text-gray-500 hover:text-primary-600 hover:bg-gray-100 rounded"
                          title="Zmien role"
                        >
                          <ShieldCheckIcon className="h-5 w-5" />
                        </button>
                        <button
                          onClick={() => handleResetPassword(user)}
                          className="p-1.5 text-gray-500 hover:text-primary-600 hover:bg-gray-100 rounded"
                          title="Resetuj haslo"
                        >
                          <KeyIcon className="h-5 w-5" />
                        </button>
                        <button
                          onClick={() => handleToggleActive(user)}
                          className={`p-1.5 hover:bg-gray-100 rounded ${
                            user.isActive
                              ? 'text-gray-500 hover:text-red-600'
                              : 'text-gray-500 hover:text-green-600'
                          }`}
                          title={user.isActive ? 'Dezaktywuj' : 'Aktywuj'}
                        >
                          {user.isActive ? (
                            <UserMinusIcon className="h-5 w-5" />
                          ) : (
                            <UserPlusIcon className="h-5 w-5" />
                          )}
                        </button>
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}

        {/* Pagination */}
        {totalPages > 1 && (
          <div className="px-4 py-3 border-t border-gray-200 flex items-center justify-between">
            <p className="text-sm text-gray-500">
              Strona {page} z {totalPages}
            </p>
            <div className="flex gap-2">
              <Button
                variant="outline"
                size="sm"
                disabled={page <= 1}
                onClick={() => setPage(page - 1)}
              >
                Poprzednia
              </Button>
              <Button
                variant="outline"
                size="sm"
                disabled={page >= totalPages}
                onClick={() => setPage(page + 1)}
              >
                Nastepna
              </Button>
            </div>
          </div>
        )}
      </Card>

      {/* User Form Modal */}
      <UserFormModal
        isOpen={isUserModalOpen}
        onClose={() => {
          setIsUserModalOpen(false);
          setEditingUser(null);
        }}
        onSuccess={() => {
          setIsUserModalOpen(false);
          setEditingUser(null);
          fetchUsers();
        }}
        user={editingUser}
        roles={roles}
      />

      {/* Change Role Modal */}
      <ChangeRoleModal
        isOpen={isRoleModalOpen}
        onClose={() => {
          setIsRoleModalOpen(false);
          setSelectedUserForRole(null);
        }}
        onSuccess={() => {
          setIsRoleModalOpen(false);
          setSelectedUserForRole(null);
          fetchUsers();
        }}
        user={selectedUserForRole}
        roles={roles}
      />

      {/* Reset Password Modal */}
      <ResetPasswordModal
        isOpen={isPasswordModalOpen}
        onClose={() => {
          setIsPasswordModalOpen(false);
          setSelectedUserForPassword(null);
        }}
        onSuccess={() => {
          setIsPasswordModalOpen(false);
          setSelectedUserForPassword(null);
        }}
        user={selectedUserForPassword}
      />
    </PageContainer>
  );
}

// User Form Modal Component
interface UserFormModalProps {
  isOpen: boolean;
  onClose: () => void;
  onSuccess: () => void;
  user: UserDetail | null;
  roles: RoleDto[];
}

function UserFormModal({ isOpen, onClose, onSuccess, user, roles }: UserFormModalProps) {
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [formData, setFormData] = useState<CreateUserRequest & UpdateUserRequest>({
    email: '',
    password: '',
    firstName: '',
    lastName: '',
    role: 'User',
    phoneNumber: '',
    dateOfBirth: '',
    address: '',
    city: '',
    postalCode: '',
  });

  useEffect(() => {
    if (user) {
      setFormData({
        email: user.email,
        password: '',
        firstName: user.firstName,
        lastName: user.lastName,
        role: user.roles[0] || 'User',
        phoneNumber: user.phoneNumber || '',
        dateOfBirth: user.dateOfBirth ? user.dateOfBirth.split('T')[0] : '',
        address: user.address || '',
        city: user.city || '',
        postalCode: user.postalCode || '',
      });
    } else {
      setFormData({
        email: '',
        password: '',
        firstName: '',
        lastName: '',
        role: 'User',
        phoneNumber: '',
        dateOfBirth: '',
        address: '',
        city: '',
        postalCode: '',
      });
    }
  }, [user, isOpen]);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setIsSubmitting(true);

    try {
      if (user) {
        // Update existing user
        await usersApi.update(user.id, {
          firstName: formData.firstName,
          lastName: formData.lastName,
          phoneNumber: formData.phoneNumber || undefined,
          dateOfBirth: formData.dateOfBirth || undefined,
          address: formData.address || undefined,
          city: formData.city || undefined,
          postalCode: formData.postalCode || undefined,
        });
        toast.success('Uzytkownik zostal zaktualizowany');
      } else {
        // Create new user
        await usersApi.create({
          email: formData.email,
          password: formData.password,
          firstName: formData.firstName,
          lastName: formData.lastName,
          role: formData.role,
          phoneNumber: formData.phoneNumber || undefined,
          dateOfBirth: formData.dateOfBirth || undefined,
          address: formData.address || undefined,
          city: formData.city || undefined,
          postalCode: formData.postalCode || undefined,
        });
        toast.success('Uzytkownik zostal utworzony');
      }
      onSuccess();
    } catch (err) {
      toast.error(getErrorMessage(err));
    } finally {
      setIsSubmitting(false);
    }
  };

  const roleOptions = roles.map((r) => ({
    value: r.name,
    label: getRoleLabel(r.name),
  }));

  return (
    <Modal isOpen={isOpen} onClose={onClose} title={user ? 'Edytuj uzytkownika' : 'Dodaj uzytkownika'}>
      <form onSubmit={handleSubmit} className="space-y-4">
        <div className="grid grid-cols-2 gap-4">
          <Input
            label="Imie"
            value={formData.firstName}
            onChange={(e) => setFormData({ ...formData, firstName: e.target.value })}
            required
          />
          <Input
            label="Nazwisko"
            value={formData.lastName}
            onChange={(e) => setFormData({ ...formData, lastName: e.target.value })}
            required
          />
        </div>

        <Input
          label="Email"
          type="email"
          value={formData.email}
          onChange={(e) => setFormData({ ...formData, email: e.target.value })}
          required
          disabled={!!user}
        />

        {!user && (
          <>
            <Input
              label="Haslo"
              type="password"
              value={formData.password}
              onChange={(e) => setFormData({ ...formData, password: e.target.value })}
              required
              minLength={8}
            />
            <Select
              label="Rola"
              options={roleOptions}
              value={formData.role}
              onChange={(e) => setFormData({ ...formData, role: e.target.value })}
              required
            />
          </>
        )}

        <Input
          label="Telefon"
          type="tel"
          value={formData.phoneNumber}
          onChange={(e) => setFormData({ ...formData, phoneNumber: e.target.value })}
        />

        <Input
          label="Data urodzenia"
          type="date"
          value={formData.dateOfBirth}
          onChange={(e) => setFormData({ ...formData, dateOfBirth: e.target.value })}
        />

        <Input
          label="Adres"
          value={formData.address}
          onChange={(e) => setFormData({ ...formData, address: e.target.value })}
        />

        <div className="grid grid-cols-2 gap-4">
          <Input
            label="Miasto"
            value={formData.city}
            onChange={(e) => setFormData({ ...formData, city: e.target.value })}
          />
          <Input
            label="Kod pocztowy"
            value={formData.postalCode}
            onChange={(e) => setFormData({ ...formData, postalCode: e.target.value })}
          />
        </div>

        <div className="flex justify-end gap-3 pt-4">
          <Button type="button" variant="outline" onClick={onClose}>
            Anuluj
          </Button>
          <Button type="submit" isLoading={isSubmitting}>
            {user ? 'Zapisz zmiany' : 'Utworz uzytkownika'}
          </Button>
        </div>
      </form>
    </Modal>
  );
}

// Change Role Modal Component
interface ChangeRoleModalProps {
  isOpen: boolean;
  onClose: () => void;
  onSuccess: () => void;
  user: UserListItem | null;
  roles: RoleDto[];
}

function ChangeRoleModal({ isOpen, onClose, onSuccess, user, roles }: ChangeRoleModalProps) {
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [selectedRole, setSelectedRole] = useState('');

  useEffect(() => {
    if (user) {
      setSelectedRole(user.roles[0] || 'User');
    }
  }, [user, isOpen]);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!user) return;

    setIsSubmitting(true);
    try {
      await usersApi.changeRole(user.id, { newRole: selectedRole });
      toast.success('Rola zostala zmieniona');
      onSuccess();
    } catch (err) {
      toast.error(getErrorMessage(err));
    } finally {
      setIsSubmitting(false);
    }
  };

  const roleOptions = roles.map((r) => ({
    value: r.name,
    label: getRoleLabel(r.name),
  }));

  return (
    <Modal isOpen={isOpen} onClose={onClose} title="Zmien role uzytkownika">
      {user && (
        <form onSubmit={handleSubmit} className="space-y-4">
          <p className="text-gray-600">
            Zmiana roli dla: <strong>{user.fullName}</strong>
          </p>

          <Select
            label="Nowa rola"
            options={roleOptions}
            value={selectedRole}
            onChange={(e) => setSelectedRole(e.target.value)}
            required
          />

          <div className="flex justify-end gap-3 pt-4">
            <Button type="button" variant="outline" onClick={onClose}>
              Anuluj
            </Button>
            <Button type="submit" isLoading={isSubmitting}>
              Zmien role
            </Button>
          </div>
        </form>
      )}
    </Modal>
  );
}

// Reset Password Modal Component
interface ResetPasswordModalProps {
  isOpen: boolean;
  onClose: () => void;
  onSuccess: () => void;
  user: UserListItem | null;
}

function ResetPasswordModal({ isOpen, onClose, onSuccess, user }: ResetPasswordModalProps) {
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [newPassword, setNewPassword] = useState('');

  useEffect(() => {
    setNewPassword('');
  }, [isOpen]);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!user) return;

    setIsSubmitting(true);
    try {
      await usersApi.resetPassword(user.id, { newPassword });
      toast.success('Haslo zostalo zresetowane');
      onSuccess();
    } catch (err) {
      toast.error(getErrorMessage(err));
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <Modal isOpen={isOpen} onClose={onClose} title="Resetuj haslo">
      {user && (
        <form onSubmit={handleSubmit} className="space-y-4">
          <p className="text-gray-600">
            Reset hasla dla: <strong>{user.fullName}</strong>
          </p>

          <Input
            label="Nowe haslo"
            type="password"
            value={newPassword}
            onChange={(e) => setNewPassword(e.target.value)}
            required
            minLength={8}
            placeholder="Minimum 8 znakow"
          />

          <div className="flex justify-end gap-3 pt-4">
            <Button type="button" variant="outline" onClick={onClose}>
              Anuluj
            </Button>
            <Button type="submit" isLoading={isSubmitting}>
              Resetuj haslo
            </Button>
          </div>
        </form>
      )}
    </Modal>
  );
}
