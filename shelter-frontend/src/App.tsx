import { Routes, Route, Navigate } from 'react-router-dom';
import { Layout } from '@/components/layout';
import {
  HomePage,
  AnimalsPage,
  AnimalDetailsPage,
  AdoptionPage,
  AdoptionFormPage,
  AdoptionConfirmationPage,
  VolunteerPage,
  VolunteerDashboardPage,
  VolunteerRegisterPage,
  LoginPage,
  RegisterPage,
  ForgotPasswordPage,
  ResetPasswordPage,
  ProfilePage,
  ContactPage,
  NotFoundPage,
  MyAdoptionsPage,
  MyAdoptionDetailsPage,
  BlogPostPage,
  BlogPage,
  AdminPage,
  AdminAdoptionsPage,
  AdminAdoptionDetailsPage,
  AdminCmsPage,
  AdminNotificationsPage,
  AdminReportsPage,
  AdminUsersPage,
  AdminConfigPage,
  AdminVolunteersPage,
  AdminSettingsPage,
} from '@/pages';
import { useAuth } from '@/context/AuthContext';
import { isStaff, isVolunteer, isAdmin } from '@/types';

interface ProtectedRouteProps {
  children: React.ReactNode;
}

function ProtectedRoute({ children }: ProtectedRouteProps) {
  const { isAuthenticated, isLoading } = useAuth();

  if (isLoading) {
    return (
      <div className="min-h-screen flex items-center justify-center">
        <div className="animate-spin rounded-full h-12 w-12 border-t-2 border-b-2 border-primary-600" />
      </div>
    );
  }

  if (!isAuthenticated) {
    return <Navigate to="/login" replace />;
  }

  return <>{children}</>;
}

interface GuestRouteProps {
  children: React.ReactNode;
}

function GuestRoute({ children }: GuestRouteProps) {
  const { isAuthenticated, isLoading } = useAuth();

  if (isLoading) {
    return (
      <div className="min-h-screen flex items-center justify-center">
        <div className="animate-spin rounded-full h-12 w-12 border-t-2 border-b-2 border-primary-600" />
      </div>
    );
  }

  if (isAuthenticated) {
    return <Navigate to="/" replace />;
  }

  return <>{children}</>;
}

interface StaffRouteProps {
  children: React.ReactNode;
}

function StaffRoute({ children }: StaffRouteProps) {
  const { user, isAuthenticated, isLoading } = useAuth();

  if (isLoading) {
    return (
      <div className="min-h-screen flex items-center justify-center">
        <div className="animate-spin rounded-full h-12 w-12 border-t-2 border-b-2 border-primary-600" />
      </div>
    );
  }

  if (!isAuthenticated) {
    return <Navigate to="/login" replace />;
  }

  if (!isStaff(user)) {
    return <Navigate to="/" replace />;
  }

  return <>{children}</>;
}

interface VolunteerRouteProps {
  children: React.ReactNode;
}

function VolunteerRoute({ children }: VolunteerRouteProps) {
  const { user, isAuthenticated, isLoading } = useAuth();

  if (isLoading) {
    return (
      <div className="min-h-screen flex items-center justify-center">
        <div className="animate-spin rounded-full h-12 w-12 border-t-2 border-b-2 border-primary-600" />
      </div>
    );
  }

  if (!isAuthenticated) {
    return <Navigate to="/login" replace />;
  }

  if (!isVolunteer(user)) {
    return <Navigate to="/" replace />;
  }

  return <>{children}</>;
}

interface AdminRouteProps {
  children: React.ReactNode;
}

function AdminRoute({ children }: AdminRouteProps) {
  const { user, isAuthenticated, isLoading } = useAuth();

  if (isLoading) {
    return (
      <div className="min-h-screen flex items-center justify-center">
        <div className="animate-spin rounded-full h-12 w-12 border-t-2 border-b-2 border-primary-600" />
      </div>
    );
  }

  if (!isAuthenticated) {
    return <Navigate to="/login" replace />;
  }

  if (!isAdmin(user)) {
    return <Navigate to="/" replace />;
  }

  return <>{children}</>;
}

export function App() {
  return (
    <Routes>
      <Route path="/" element={<Layout />}>
        {/* Public routes */}
        <Route index element={<HomePage />} />
        <Route path="animals" element={<AnimalsPage />} />
        <Route path="animals/:id" element={<AnimalDetailsPage />} />
        <Route path="adoption" element={<AdoptionPage />} />
        <Route path="adoption/apply/:animalId" element={<AdoptionFormPage />} />
        <Route path="adoption/confirmation/:applicationId" element={<AdoptionConfirmationPage />} />
        <Route path="volunteer" element={<VolunteerPage />} />
        <Route path="volunteer/register" element={<VolunteerRegisterPage />} />
        <Route path="contact" element={<ContactPage />} />
        <Route path="blog" element={<BlogPage />} />
        <Route path="blog/:id" element={<BlogPostPage />} />

        {/* Guest-only routes */}
        <Route
          path="login"
          element={
            <GuestRoute>
              <LoginPage />
            </GuestRoute>
          }
        />
        <Route
          path="register"
          element={
            <GuestRoute>
              <RegisterPage />
            </GuestRoute>
          }
        />
        <Route
          path="forgot-password"
          element={
            <GuestRoute>
              <ForgotPasswordPage />
            </GuestRoute>
          }
        />
        <Route
          path="reset-password"
          element={
            <GuestRoute>
              <ResetPasswordPage />
            </GuestRoute>
          }
        />

        {/* Protected routes */}
        <Route
          path="profile"
          element={
            <ProtectedRoute>
              <ProfilePage />
            </ProtectedRoute>
          }
        />
        <Route
          path="profile/adoptions"
          element={
            <ProtectedRoute>
              <MyAdoptionsPage />
            </ProtectedRoute>
          }
        />
        <Route
          path="profile/adoptions/:id"
          element={
            <ProtectedRoute>
              <MyAdoptionDetailsPage />
            </ProtectedRoute>
          }
        />

        {/* Volunteer routes */}
        <Route
          path="volunteer/dashboard"
          element={
            <VolunteerRoute>
              <VolunteerDashboardPage />
            </VolunteerRoute>
          }
        />

        {/* Staff/Admin routes */}
        <Route
          path="admin"
          element={
            <StaffRoute>
              <AdminPage />
            </StaffRoute>
          }
        />
        <Route
          path="admin/adoptions"
          element={
            <StaffRoute>
              <AdminAdoptionsPage />
            </StaffRoute>
          }
        />
        <Route
          path="admin/adoptions/:id"
          element={
            <StaffRoute>
              <AdminAdoptionDetailsPage />
            </StaffRoute>
          }
        />
        <Route
          path="admin/cms"
          element={
            <StaffRoute>
              <AdminCmsPage />
            </StaffRoute>
          }
        />
        <Route
          path="admin/notifications"
          element={
            <StaffRoute>
              <AdminNotificationsPage />
            </StaffRoute>
          }
        />
        <Route
          path="admin/reports"
          element={
            <StaffRoute>
              <AdminReportsPage />
            </StaffRoute>
          }
        />
        <Route
          path="admin/volunteers"
          element={
            <StaffRoute>
              <AdminVolunteersPage />
            </StaffRoute>
          }
        />

        {/* Admin-only routes */}
        <Route
          path="admin/settings"
          element={
            <AdminRoute>
              <AdminSettingsPage />
            </AdminRoute>
          }
        />
        <Route
          path="admin/users"
          element={
            <AdminRoute>
              <AdminUsersPage />
            </AdminRoute>
          }
        />
        <Route
          path="admin/config"
          element={
            <AdminRoute>
              <AdminConfigPage />
            </AdminRoute>
          }
        />

        {/* 404 */}
        <Route path="*" element={<NotFoundPage />} />
      </Route>
    </Routes>
  );
}

export default App;
