import { create } from 'zustand';
import { persist } from 'zustand/middleware';

type ThemeMode = 'light' | 'dark';

interface UIState {
  sidebarOpen: boolean;
  themeMode: ThemeMode;
  notificationDrawerOpen: boolean;
  toggleSidebar: () => void;
  setSidebarOpen: (open: boolean) => void;
  toggleTheme: () => void;
  setThemeMode: (mode: ThemeMode) => void;
  toggleNotificationDrawer: () => void;
  setNotificationDrawerOpen: (open: boolean) => void;
}

export const useUIStore = create<UIState>()(
  persist(
    (set) => ({
      sidebarOpen: true,
      themeMode: 'light',
      notificationDrawerOpen: false,

      toggleSidebar: () =>
        set((state) => ({
          sidebarOpen: !state.sidebarOpen,
        })),

      setSidebarOpen: (open) =>
        set({
          sidebarOpen: open,
        }),

      toggleTheme: () =>
        set((state) => ({
          themeMode: state.themeMode === 'light' ? 'dark' : 'light',
        })),

      setThemeMode: (mode) =>
        set({
          themeMode: mode,
        }),

      toggleNotificationDrawer: () =>
        set((state) => ({
          notificationDrawerOpen: !state.notificationDrawerOpen,
        })),

      setNotificationDrawerOpen: (open) =>
        set({
          notificationDrawerOpen: open,
        }),
    }),
    {
      name: 'ui-storage',
      partialize: (state) => ({
        themeMode: state.themeMode,
        sidebarOpen: state.sidebarOpen,
      }),
    }
  )
);
