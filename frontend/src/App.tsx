import { useEffect, Suspense } from 'react';
import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import { ThemeProvider } from '@mui/material/styles';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { ReactQueryDevtools } from '@tanstack/react-query-devtools';
import { Box, CircularProgress } from '@mui/material';
import { lightTheme, darkTheme } from '@/theme/theme';
import { useUIStore } from '@/state/ui.store';
import { useNotificationStore } from '@/state/notification.store';
import { MainLayout } from '@/layouts/MainLayout';
import { routes } from '@/routing/routes';
import { signalRService } from '@/services/signalr';
import type { NotificationMessage } from '@/services/signalr';

// Create React Query client
const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      refetchOnWindowFocus: false,
      retry: 1,
      staleTime: 30000,
    },
  },
});

const LoadingFallback = () => (
  <Box
    sx={{
      display: 'flex',
      justifyContent: 'center',
      alignItems: 'center',
      height: '100vh',
    }}
  >
    <CircularProgress />
  </Box>
);

function App() {
  const { themeMode } = useUIStore();
  const { addNotification } = useNotificationStore();

  useEffect(() => {
    // Start SignalR connection
    signalRService.start();

    // Subscribe to notifications
    const handleNotification = (notification: NotificationMessage) => {
      addNotification(notification);
    };

    signalRService.on('NotificationReceived', handleNotification);

    // Cleanup on unmount
    return () => {
      signalRService.off('NotificationReceived', handleNotification);
      signalRService.stop();
    };
  }, [addNotification]);

  const theme = themeMode === 'dark' ? darkTheme : lightTheme;

  return (
    <QueryClientProvider client={queryClient}>
      <ThemeProvider theme={theme}>
        <BrowserRouter>
          <Suspense fallback={<LoadingFallback />}>
            <MainLayout>
              <Routes>
                {routes.map((route) => (
                  <Route
                    key={route.path}
                    path={route.path}
                    element={route.element}
                  />
                ))}
                <Route path="*" element={<Navigate to="/" replace />} />
              </Routes>
            </MainLayout>
          </Suspense>
        </BrowserRouter>
      </ThemeProvider>
      <ReactQueryDevtools initialIsOpen={false} />
    </QueryClientProvider>
  );
}

export default App;
