import { apiClient } from './client';
import type {
  DashboardStatsResponse,
  JobDefinitionResponse,
  JobExecutionResponse,
  JobScheduleResponse,
  AuditLogResponse,
  ExecutionTrendResponse,
  TopFailingJobResponse,
  StaleJobResponse,
  UpcomingScheduleResponse,
  CreateJobCommand,
  UpdateJobCommand,
  CreateScheduleCommand,
  TriggerJobCommand,
  CancelExecutionCommand,
  PagedResult,
  JobExecutionFilter,
  AuditLogFilter,
} from './types';

// Dashboard APIs
export const dashboardApi = {
  getStats: () => apiClient.get<DashboardStatsResponse>('/dashboard/stats'),

  getExecutionTrends: (days: number = 7) =>
    apiClient.get<ExecutionTrendResponse[]>(`/dashboard/execution-trends?days=${days}`),

  getTopFailingJobs: (limit: number = 10) =>
    apiClient.get<TopFailingJobResponse[]>(`/dashboard/top-failing-jobs?limit=${limit}`),

  getStaleJobs: (daysThreshold: number = 7) =>
    apiClient.get<StaleJobResponse[]>(`/dashboard/stale-jobs?daysThreshold=${daysThreshold}`),

  getUpcomingSchedules: (hoursAhead: number = 24) =>
    apiClient.get<UpcomingScheduleResponse[]>(`/dashboard/upcoming-schedules?hoursAhead=${hoursAhead}`),
};

// Job Definition APIs
export const jobsApi = {
  getAll: () => apiClient.get<JobDefinitionResponse[]>('/jobs'),

  getById: (id: string) => apiClient.get<JobDefinitionResponse>(`/jobs/${id}`),

  create: (command: CreateJobCommand) => apiClient.post<{ id: string }>('/jobs', command),

  update: (command: UpdateJobCommand) => apiClient.put<void>(`/jobs/${command.id}`, command),

  delete: (id: string) => apiClient.delete<void>(`/jobs/${id}`),

  activate: (id: string) => apiClient.post<void>(`/jobs/${id}/activate`),

  disable: (id: string) => apiClient.post<void>(`/jobs/${id}/disable`),

  archive: (id: string) => apiClient.post<void>(`/jobs/${id}/archive`),

  trigger: (command: TriggerJobCommand) =>
    apiClient.post<{ executionId: string }>(`/jobs/${command.jobId}/trigger`, command),
};

// Job Schedule APIs
export const schedulesApi = {
  getByJobId: (jobId: string) => apiClient.get<JobScheduleResponse[]>(`/jobs/${jobId}/schedules`),

  getById: (jobId: string, scheduleId: string) =>
    apiClient.get<JobScheduleResponse>(`/jobs/${jobId}/schedules/${scheduleId}`),

  create: (command: CreateScheduleCommand) =>
    apiClient.post<{ id: string }>(`/jobs/${command.jobId}/schedules`, command),

  update: (jobId: string, scheduleId: string, command: CreateScheduleCommand) =>
    apiClient.put<void>(`/jobs/${jobId}/schedules/${scheduleId}`, command),

  delete: (jobId: string, scheduleId: string) =>
    apiClient.delete<void>(`/jobs/${jobId}/schedules/${scheduleId}`),

  activate: (jobId: string, scheduleId: string) =>
    apiClient.post<void>(`/jobs/${jobId}/schedules/${scheduleId}/activate`),

  deactivate: (jobId: string, scheduleId: string) =>
    apiClient.post<void>(`/jobs/${jobId}/schedules/${scheduleId}/deactivate`),
};

// Job Execution APIs
export const executionsApi = {
  getAll: (filter?: JobExecutionFilter) => {
    const params = new URLSearchParams();
    if (filter?.jobId) params.append('jobId', filter.jobId);
    if (filter?.status !== undefined) params.append('status', filter.status.toString());
    if (filter?.startDateFrom) params.append('startDateFrom', filter.startDateFrom);
    if (filter?.startDateTo) params.append('startDateTo', filter.startDateTo);
    if (filter?.isManualTrigger !== undefined) params.append('isManualTrigger', filter.isManualTrigger.toString());
    if (filter?.pageNumber) params.append('pageNumber', filter.pageNumber.toString());
    if (filter?.pageSize) params.append('pageSize', filter.pageSize.toString());
    if (filter?.sortBy) params.append('sortBy', filter.sortBy);
    if (filter?.sortDirection) params.append('sortDirection', filter.sortDirection);

    return apiClient.get<PagedResult<JobExecutionResponse>>(`/executions?${params.toString()}`);
  },

  getById: (id: string) => apiClient.get<JobExecutionResponse>(`/executions/${id}`),

  getRunning: () => apiClient.get<JobExecutionResponse[]>('/executions/running'),

  getByJobId: (jobId: string, filter?: JobExecutionFilter) => {
    const params = new URLSearchParams();
    if (filter?.pageNumber) params.append('pageNumber', filter.pageNumber.toString());
    if (filter?.pageSize) params.append('pageSize', filter.pageSize.toString());

    return apiClient.get<PagedResult<JobExecutionResponse>>(`/jobs/${jobId}/executions?${params.toString()}`);
  },

  cancel: (command: CancelExecutionCommand) =>
    apiClient.post<void>(`/executions/${command.executionId}/cancel`, command),

  getLogs: (executionId: string) =>
    apiClient.get<string[]>(`/executions/${executionId}/logs`),
};

// Audit Log APIs
export const auditApi = {
  getAll: (filter?: AuditLogFilter) => {
    const params = new URLSearchParams();
    if (filter?.entityType) params.append('entityType', filter.entityType);
    if (filter?.entityId) params.append('entityId', filter.entityId);
    if (filter?.action) params.append('action', filter.action);
    if (filter?.userId) params.append('userId', filter.userId);
    if (filter?.dateFrom) params.append('dateFrom', filter.dateFrom);
    if (filter?.dateTo) params.append('dateTo', filter.dateTo);
    if (filter?.pageNumber) params.append('pageNumber', filter.pageNumber.toString());
    if (filter?.pageSize) params.append('pageSize', filter.pageSize.toString());

    return apiClient.get<PagedResult<AuditLogResponse>>(`/audit?${params.toString()}`);
  },

  getById: (id: string) => apiClient.get<AuditLogResponse>(`/audit/${id}`),

  getByEntity: (entityType: string, entityId: string) =>
    apiClient.get<AuditLogResponse[]>(`/audit/${entityType}/${entityId}`),
};

// Health Check APIs
export const healthApi = {
  getLive: () => apiClient.get<any>('/health/live'),
  getReady: () => apiClient.get<any>('/health/ready'),
  getHealth: () => apiClient.get<any>('/health'),
};
