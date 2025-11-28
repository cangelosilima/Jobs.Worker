import { create } from 'zustand';
import { persist } from 'zustand/middleware';
import { UserRole } from '@/api/types';

export interface User {
  id: string;
  name: string;
  email: string;
  roles: UserRole[];
}

interface AuthState {
  user: User | null;
  isAuthenticated: boolean;
  login: (user: User) => void;
  logout: () => void;
  hasRole: (role: UserRole) => boolean;
  hasAnyRole: (roles: UserRole[]) => boolean;
  canEdit: () => boolean;
  canDelete: () => boolean;
  canTrigger: () => boolean;
}

// Default admin user (no authentication required)
const defaultUser: User = {
  id: 'default-admin',
  name: 'Admin User',
  email: 'admin@jobscheduler.local',
  roles: [UserRole.Admin, UserRole.Operator, UserRole.JobOwner, UserRole.Viewer],
};

export const useAuthStore = create<AuthState>()(
  persist(
    (set, get) => ({
      user: defaultUser,
      isAuthenticated: true,

      login: (user) =>
        set({
          user,
          isAuthenticated: true,
        }),

      logout: () =>
        set({
          user: defaultUser,
          isAuthenticated: true,
        }),

      hasRole: (role) => {
        const { user } = get();
        return user?.roles.includes(role) ?? false;
      },

      hasAnyRole: (roles) => {
        const { user } = get();
        if (!user) return false;
        return roles.some((role) => user.roles.includes(role));
      },

      canEdit: () => {
        return true; // No auth - always allow
      },

      canDelete: () => {
        return true; // No auth - always allow
      },

      canTrigger: () => {
        return true; // No auth - always allow
      },
    }),
    {
      name: 'auth-storage',
      partialize: (state) => ({
        user: state.user,
        isAuthenticated: state.isAuthenticated,
      }),
    }
  )
);
