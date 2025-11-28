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

export const useAuthStore = create<AuthState>()(
  persist(
    (set, get) => ({
      user: null,
      isAuthenticated: false,

      login: (user) =>
        set({
          user,
          isAuthenticated: true,
        }),

      logout: () =>
        set({
          user: null,
          isAuthenticated: false,
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
        const { hasAnyRole } = get();
        return hasAnyRole([UserRole.Admin, UserRole.Operator, UserRole.JobOwner]);
      },

      canDelete: () => {
        const { hasAnyRole } = get();
        return hasAnyRole([UserRole.Admin]);
      },

      canTrigger: () => {
        const { hasAnyRole } = get();
        return hasAnyRole([UserRole.Admin, UserRole.Operator, UserRole.JobOwner]);
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
