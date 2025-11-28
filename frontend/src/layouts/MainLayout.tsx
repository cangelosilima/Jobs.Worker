import {
  AppBar,
  Box,
  CssBaseline,
  Drawer,
  IconButton,
  List,
  ListItem,
  ListItemButton,
  ListItemIcon,
  ListItemText,
  Toolbar,
  Typography,
  Badge,
  Avatar,
} from '@mui/material';
import {
  Menu as MenuIcon,
  Dashboard as DashboardIcon,
  PlayCircle as PlayCircleIcon,
  History as HistoryIcon,
  Schedule as ScheduleIcon,
  AccountTree as AccountTreeIcon,
  Security as SecurityIcon,
  Notifications as NotificationsIcon,
  Brightness4 as Brightness4Icon,
  Brightness7 as Brightness7Icon,
} from '@mui/icons-material';
import { useNavigate, useLocation } from 'react-router-dom';
import { useUIStore } from '@/state/ui.store';
import { useAuthStore } from '@/state/auth.store';
import { useNotificationStore } from '@/state/notification.store';
import { routes } from '@/routing/routes';

const DRAWER_WIDTH = 240;

const iconMap: Record<string, React.ReactNode> = {
  Dashboard: <DashboardIcon />,
  PlayCircle: <PlayCircleIcon />,
  History: <HistoryIcon />,
  Schedule: <ScheduleIcon />,
  AccountTree: <AccountTreeIcon />,
  Security: <SecurityIcon />,
  Notifications: <NotificationsIcon />,
};

export const MainLayout: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const navigate = useNavigate();
  const location = useLocation();
  const { sidebarOpen, toggleSidebar, themeMode, toggleTheme, toggleNotificationDrawer } = useUIStore();
  const { user } = useAuthStore();
  const { unreadCount } = useNotificationStore();

  // Show all menu items (no auth required)
  const menuItems = routes.filter((route) => route.showInMenu);

  return (
    <Box sx={{ display: 'flex' }}>
      <CssBaseline />

      {/* App Bar */}
      <AppBar
        position="fixed"
        sx={{
          zIndex: (theme) => theme.zIndex.drawer + 1,
        }}
      >
        <Toolbar>
          <IconButton
            color="inherit"
            aria-label="toggle drawer"
            onClick={toggleSidebar}
            edge="start"
            sx={{ mr: 2 }}
          >
            <MenuIcon />
          </IconButton>

          <Typography variant="h6" noWrap component="div" sx={{ flexGrow: 1 }}>
            Job Scheduler Admin
          </Typography>

          <IconButton color="inherit" onClick={toggleTheme}>
            {themeMode === 'dark' ? <Brightness7Icon /> : <Brightness4Icon />}
          </IconButton>

          <IconButton color="inherit" onClick={toggleNotificationDrawer}>
            <Badge badgeContent={unreadCount} color="error">
              <NotificationsIcon />
            </Badge>
          </IconButton>

          <Box sx={{ display: 'flex', alignItems: 'center', ml: 1 }}>
            <Avatar sx={{ width: 32, height: 32, bgcolor: 'secondary.main', mr: 1 }}>
              {user?.name.charAt(0).toUpperCase()}
            </Avatar>
            <Typography variant="body2" sx={{ display: { xs: 'none', sm: 'block' } }}>
              {user?.name}
            </Typography>
          </Box>
        </Toolbar>
      </AppBar>

      {/* Sidebar */}
      <Drawer
        variant="persistent"
        open={sidebarOpen}
        sx={{
          width: DRAWER_WIDTH,
          flexShrink: 0,
          '& .MuiDrawer-paper': {
            width: DRAWER_WIDTH,
            boxSizing: 'border-box',
          },
        }}
      >
        <Toolbar />
        <Box sx={{ overflow: 'auto' }}>
          <List>
            {menuItems.map((route) => (
              <ListItem key={route.path} disablePadding>
                <ListItemButton
                  selected={location.pathname === route.path}
                  onClick={() => navigate(route.path)}
                >
                  {route.icon && (
                    <ListItemIcon>
                      {iconMap[route.icon]}
                    </ListItemIcon>
                  )}
                  <ListItemText primary={route.label} />
                </ListItemButton>
              </ListItem>
            ))}
          </List>
        </Box>
      </Drawer>

      {/* Main Content */}
      <Box
        component="main"
        sx={{
          flexGrow: 1,
          p: 3,
          width: sidebarOpen ? `calc(100% - ${DRAWER_WIDTH}px)` : '100%',
          transition: (theme) =>
            theme.transitions.create(['width', 'margin'], {
              easing: theme.transitions.easing.sharp,
              duration: theme.transitions.duration.leavingScreen,
            }),
        }}
      >
        <Toolbar />
        {children}
      </Box>
    </Box>
  );
};
