import { Link, NavLink, useNavigate } from 'react-router-dom';
import { Fragment } from 'react';
import { Menu, Transition } from '@headlessui/react';
import {
  Bars3Icon,
  XMarkIcon,
  UserCircleIcon,
  ArrowRightOnRectangleIcon,
  Cog6ToothIcon,
} from '@heroicons/react/24/outline';

import { useState } from 'react';
import { clsx } from 'clsx';
import toast from 'react-hot-toast';
import { useAuth } from '@/context/AuthContext';
import { Button } from '@/components/common';
import { NotificationsPanel } from '@/components/admin';
import { isStaff, isVolunteer } from '@/types';

const baseNavigation = [
  { name: 'Strona glowna', href: '/' },
  { name: 'Zwierzeta', href: '/animals' },
  { name: 'Adopcja', href: '/adoption' },
];

export function Header() {
  const [mobileMenuOpen, setMobileMenuOpen] = useState(false);
  const { user, isAuthenticated, logout } = useAuth();
  const navigate = useNavigate();

  // Hide "Volunteer" link for volunteers and staff
  const showVolunteerLink = !isVolunteer(user) && !isStaff(user);
  const navigation = [
    ...baseNavigation,
    ...(showVolunteerLink ? [{ name: 'Wolontariat', href: '/volunteer' }] : []),
    { name: 'Kontakt', href: '/contact' },
  ];

  const handleLogout = async () => {
    await logout();
    toast.success('Wylogowano pomyślnie');
    navigate('/');
  };

  return (
    <header className="bg-white/95 backdrop-blur-sm shadow-sm sticky top-0 z-50 border-b border-gray-100" role="banner">
      <nav className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8" aria-label="Nawigacja główna">
        <div className="flex items-center justify-between py-4">
          {/* Logo + Navigation group */}
          <div className="flex items-center gap-10">
            {/* Logo */}
            <Link to="/" className="flex items-center gap-3">
              <img
                src="/images/logo-wat.png"
                alt="Logo WAT"
                className="h-10 w-auto"
              />
              <span className="font-heading font-bold text-xl text-primary-700">
                Schronisko
              </span>
            </Link>

            {/* Desktop navigation */}
            <div className="hidden md:flex items-center gap-8">
              {navigation.map((item) => (
                <NavLink
                  key={item.name}
                  to={item.href}
                  className={({ isActive }) =>
                    clsx(
                      'text-sm font-medium transition-all py-1 border-b-2',
                      isActive
                        ? 'text-primary-600 border-primary-500'
                        : 'text-gray-600 hover:text-primary-600 border-transparent hover:border-primary-300'
                    )
                  }
                >
                  {item.name}
                </NavLink>
              ))}
            </div>
          </div>

          {/* User menu / Auth buttons */}
          <div className="hidden md:flex items-center gap-4">
            {isAuthenticated && user ? (
              <>
                {/* Staff/Admin dashboard link */}
                {isStaff(user) && (
                  <>
                    <NotificationsPanel />
                    <Link
                      to="/admin"
                      className="text-sm font-medium text-gray-600 hover:text-primary-600"
                    >
                      Panel
                    </Link>
                  </>
                )}

                {/* Volunteer dashboard link */}
                {isVolunteer(user) && !isStaff(user) && (
                  <Link
                    to="/volunteer/dashboard"
                    className="text-sm font-medium text-gray-600 hover:text-primary-600"
                  >
                    Mój panel
                  </Link>
                )}

                {/* My adoptions link - for regular users and volunteers */}
                {!isStaff(user) && (
                  <Link
                    to="/profile/adoptions"
                    className="text-sm font-medium text-gray-600 hover:text-primary-600"
                  >
                    Moje adopcje
                  </Link>
                )}

                {/* User dropdown */}
                <Menu as="div" className="relative">
                  <Menu.Button
                    className="flex items-center gap-2 text-sm font-medium text-gray-700 hover:text-primary-600 focus:outline-none focus:ring-2 focus:ring-primary-500 focus:ring-offset-2 rounded-lg px-2 py-1"
                    aria-label={`Menu użytkownika ${user.firstName}`}
                  >
                    <UserCircleIcon className="h-6 w-6" aria-hidden="true" />
                    <span>{user.firstName}</span>
                  </Menu.Button>

                  <Transition
                    as={Fragment}
                    enter="transition ease-out duration-100"
                    enterFrom="transform opacity-0 scale-95"
                    enterTo="transform opacity-100 scale-100"
                    leave="transition ease-in duration-75"
                    leaveFrom="transform opacity-100 scale-100"
                    leaveTo="transform opacity-0 scale-95"
                  >
                    <Menu.Items className="absolute right-0 mt-2 w-48 origin-top-right rounded-lg bg-white shadow-lg ring-1 ring-black ring-opacity-5 focus:outline-none">
                      <div className="py-1">
                        <Menu.Item>
                          {({ active }) => (
                            <Link
                              to="/profile"
                              className={clsx(
                                'flex items-center gap-2 px-4 py-2 text-sm',
                                active ? 'bg-gray-100 text-gray-900' : 'text-gray-700'
                              )}
                            >
                              <Cog6ToothIcon className="h-4 w-4" />
                              Ustawienia
                            </Link>
                          )}
                        </Menu.Item>
                        <Menu.Item>
                          {({ active }) => (
                            <button
                              onClick={handleLogout}
                              className={clsx(
                                'flex items-center gap-2 w-full px-4 py-2 text-sm',
                                active ? 'bg-gray-100 text-gray-900' : 'text-gray-700'
                              )}
                            >
                              <ArrowRightOnRectangleIcon className="h-4 w-4" />
                              Wyloguj
                            </button>
                          )}
                        </Menu.Item>
                      </div>
                    </Menu.Items>
                  </Transition>
                </Menu>
              </>
            ) : (
              <>
                <Link
                  to="/login"
                  className="text-sm font-medium text-gray-600 hover:text-primary-600"
                >
                  Zaloguj się
                </Link>
                <Button as={Link} to="/register" size="sm">
                  Zarejestruj się
                </Button>
              </>
            )}
          </div>

          {/* Mobile menu button */}
          <button
            type="button"
            className="md:hidden p-2 text-gray-600 focus:outline-none focus:ring-2 focus:ring-primary-500 focus:ring-offset-2 rounded-lg"
            onClick={() => setMobileMenuOpen(!mobileMenuOpen)}
            aria-expanded={mobileMenuOpen}
            aria-controls="mobile-menu"
            aria-label={mobileMenuOpen ? 'Zamknij menu' : 'Otwórz menu'}
          >
            {mobileMenuOpen ? (
              <XMarkIcon className="h-6 w-6" aria-hidden="true" />
            ) : (
              <Bars3Icon className="h-6 w-6" aria-hidden="true" />
            )}
          </button>
        </div>

        {/* Mobile menu */}
        {mobileMenuOpen && (
          <div id="mobile-menu" className="md:hidden py-4 border-t border-gray-200" role="navigation" aria-label="Menu mobilne">
            <div className="flex flex-col gap-2">
              {navigation.map((item) => (
                <NavLink
                  key={item.name}
                  to={item.href}
                  onClick={() => setMobileMenuOpen(false)}
                  className={({ isActive }) =>
                    clsx(
                      'px-3 py-2 rounded-lg text-sm font-medium',
                      isActive
                        ? 'bg-primary-50 text-primary-600'
                        : 'text-gray-600 hover:bg-gray-50'
                    )
                  }
                >
                  {item.name}
                </NavLink>
              ))}

              <div className="border-t border-gray-200 mt-2 pt-2">
                {isAuthenticated && user ? (
                  <>
                    {isStaff(user) && (
                      <Link
                        to="/admin"
                        onClick={() => setMobileMenuOpen(false)}
                        className="block px-3 py-2 text-sm font-medium text-primary-600 hover:bg-primary-50 rounded-lg"
                      >
                        Panel administracyjny
                      </Link>
                    )}
                    {isVolunteer(user) && !isStaff(user) && (
                      <Link
                        to="/volunteer/dashboard"
                        onClick={() => setMobileMenuOpen(false)}
                        className="block px-3 py-2 text-sm font-medium text-primary-600 hover:bg-primary-50 rounded-lg"
                      >
                        Moj panel wolontariusza
                      </Link>
                    )}
                    {!isStaff(user) && (
                      <Link
                        to="/profile/adoptions"
                        onClick={() => setMobileMenuOpen(false)}
                        className="block px-3 py-2 text-sm font-medium text-primary-600 hover:bg-primary-50 rounded-lg"
                      >
                        Moje adopcje
                      </Link>
                    )}
                    <Link
                      to="/profile"
                      onClick={() => setMobileMenuOpen(false)}
                      className="block px-3 py-2 text-sm font-medium text-gray-600 hover:bg-gray-50 rounded-lg"
                    >
                      Moj profil
                    </Link>
                    <button
                      onClick={() => {
                        handleLogout();
                        setMobileMenuOpen(false);
                      }}
                      className="block w-full text-left px-3 py-2 text-sm font-medium text-gray-600 hover:bg-gray-50 rounded-lg"
                    >
                      Wyloguj
                    </button>
                  </>
                ) : (
                  <>
                    <Link
                      to="/login"
                      onClick={() => setMobileMenuOpen(false)}
                      className="block px-3 py-2 text-sm font-medium text-gray-600 hover:bg-gray-50 rounded-lg"
                    >
                      Zaloguj się
                    </Link>
                    <Link
                      to="/register"
                      onClick={() => setMobileMenuOpen(false)}
                      className="block px-3 py-2 text-sm font-medium text-primary-600 hover:bg-primary-50 rounded-lg"
                    >
                      Zarejestruj się
                    </Link>
                  </>
                )}
              </div>
            </div>
          </div>
        )}
      </nav>
    </header>
  );
}
