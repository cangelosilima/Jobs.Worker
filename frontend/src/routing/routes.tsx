import { lazy } from 'react';
import { UserRole } from '@/api/types';

// Lazy load modules for code splitting
const Dashboard = lazy(() => import('@/modules/dashboard/Dashboard'));
const RunningJobs = lazy(() => import('@/modules/jobs/running/RunningJobs'));
const JobHistory = lazy(() => import('@/modules/jobs/history/JobHistory'));
const JobDetails = lazy(() => import('@/modules/jobs/details/JobDetails'));
const ScheduleList = lazy(() => import('@/modules/schedules/list/ScheduleList'));
const ScheduleCreate = lazy(() => import('@/modules/schedules/create/ScheduleCreate'));
const ScheduleEdit = lazy(() => import('@/modules/schedules/edit/ScheduleEdit'));
const Dependencies = lazy(() => import('@/modules/dependencies/Dependencies'));
const AuditLogs = lazy(() => import('@/modules/audit/AuditLogs'));
const Notifications = lazy(() => import('@/modules/notifications/Notifications'));

export interface RouteConfig {
  path: string;
  element: React.ReactNode;
  label: string;
  icon?: string;
  showInMenu?: boolean;
  requiredRoles?: UserRole[];
  children?: RouteConfig[];
}

export const routes: RouteConfig[] = [
  {
    path: '/',
    element: <Dashboard />,
    label: 'Dashboard',
    icon: 'Dashboard',
    showInMenu: true,
  },
  {
    path: '/jobs/running',
    element: <RunningJobs />,
    label: 'Running Jobs',
    icon: 'PlayCircle',
    showInMenu: true,
  },
  {
    path: '/jobs/history',
    element: <JobHistory />,
    label: 'Job History',
    icon: 'History',
    showInMenu: true,
  },
  {
    path: '/jobs/:id',
    element: <JobDetails />,
    label: 'Job Details',
    showInMenu: false,
  },
  {
    path: '/schedules',
    element: <ScheduleList />,
    label: 'Schedules',
    icon: 'Schedule',
    showInMenu: true,
  },
  {
    path: '/schedules/create',
    element: <ScheduleCreate />,
    label: 'Create Schedule',
    showInMenu: false,
    requiredRoles: [UserRole.Admin, UserRole.Operator, UserRole.JobOwner],
  },
  {
    path: '/schedules/:id/edit',
    element: <ScheduleEdit />,
    label: 'Edit Schedule',
    showInMenu: false,
    requiredRoles: [UserRole.Admin, UserRole.Operator, UserRole.JobOwner],
  },
  {
    path: '/dependencies',
    element: <Dependencies />,
    label: 'Dependencies',
    icon: 'AccountTree',
    showInMenu: true,
  },
  {
    path: '/audit',
    element: <AuditLogs />,
    label: 'Audit Logs',
    icon: 'Security',
    showInMenu: true,
    requiredRoles: [UserRole.Admin],
  },
  {
    path: '/notifications',
    element: <Notifications />,
    label: 'Notifications',
    icon: 'Notifications',
    showInMenu: false,
  },
];
