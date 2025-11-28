// Enums
export enum JobStatus {
  Active = 1,
  Disabled = 2,
  Archived = 3,
  Draft = 4,
}

export enum ExecutionStatus {
  Queued = 1,
  Running = 2,
  Succeeded = 3,
  Failed = 4,
  TimedOut = 5,
  Cancelled = 6,
  Skipped = 7,
  Retrying = 8,
}

export enum RecurrenceType {
  Daily = 1,
  Weekly = 2,
  Monthly = 3,
  MonthlyBusinessDay = 4,
  Cron = 5,
  OneTime = 6,
  Conditional = 7,
}

export enum RetryStrategy {
  Linear = 1,
  Exponential = 2,
  ExponentialWithJitter = 3,
}

export enum NotificationType {
  Email = 1,
  MicrosoftTeams = 2,
  Slack = 3,
  Webhook = 4,
}

export enum NotificationEvent {
  OnSuccess = 1,
  OnFailure = 2,
  OnTimeout = 3,
  OnRetry = 4,
  OnSkip = 5,
  Always = 6,
}

export enum UserRole {
  Admin = 'Admin',
  Operator = 'Operator',
  Viewer = 'Viewer',
  JobOwner = 'JobOwner',
}

// Response Models
export interface DashboardStatsResponse {
  totalJobs: number;
  activeJobs: number;
  disabledJobs: number;
  runningExecutions: number;
  failedToday: number;
  succeededToday: number;
  delayedOrSkipped: number;
  exceedingExpectedDuration: number;
  averageExecutionTimeSeconds: number;
  successRatePercentage: number;
}

export interface JobDefinitionResponse {
  id: string;
  name: string;
  description: string;
  assemblyName: string;
  className: string;
  methodName: string;
  status: JobStatus;
  timeoutSeconds: number | null;
  expectedDurationSeconds: number | null;
  maxConcurrentExecutions: number | null;
  allowManualTrigger: boolean;
  retryPolicy: RetryPolicyResponse | null;
  owner: JobOwnershipResponse | null;
  schedules: JobScheduleResponse[];
  parameters: JobParameterResponse[];
  dependencies: JobDependencyResponse[];
  notifications: JobNotificationResponse[];
  createdAt: string;
  updatedAt: string;
  version: number;
}

export interface JobScheduleResponse {
  id: string;
  jobId: string;
  recurrenceType: RecurrenceType;
  cronExpression: string | null;
  startDate: string;
  endDate: string | null;
  timeOfDay: string | null;
  daysOfWeek: number[] | null;
  dayOfMonth: number | null;
  nthBusinessDay: number | null;
  isActive: boolean;
  nextExecutionTime: string | null;
  lastExecutionTime: string | null;
  createdAt: string;
}

export interface JobExecutionResponse {
  id: string;
  jobId: string;
  jobName: string;
  scheduleId: string | null;
  status: ExecutionStatus;
  scheduledTime: string;
  startTime: string | null;
  endTime: string | null;
  durationSeconds: number | null;
  output: string | null;
  errorMessage: string | null;
  retryCount: number;
  isManualTrigger: boolean;
  triggeredBy: string | null;
  correlationId: string;
  traceId: string;
  createdAt: string;
}

export interface RetryPolicyResponse {
  maxRetries: number;
  strategy: RetryStrategy;
  initialDelaySeconds: number;
  maxDelaySeconds: number | null;
  backoffMultiplier: number | null;
}

export interface JobOwnershipResponse {
  userId: string;
  userName: string;
  email: string;
  teamName: string | null;
  assignedAt: string;
}

export interface JobParameterResponse {
  id: string;
  jobId: string;
  key: string;
  value: string;
  isEncrypted: boolean;
  createdAt: string;
}

export interface JobDependencyResponse {
  id: string;
  jobId: string;
  dependsOnJobId: string;
  dependsOnJobName: string;
  isRequired: boolean;
  createdAt: string;
}

export interface JobNotificationResponse {
  id: string;
  jobId: string;
  notificationType: NotificationType;
  event: NotificationEvent;
  recipients: string[];
  webhookUrl: string | null;
  isActive: boolean;
  createdAt: string;
}

export interface AuditLogResponse {
  id: string;
  timestamp: string;
  userId: string;
  userName: string;
  action: string;
  entityType: string;
  entityId: string;
  before: string | null;
  after: string | null;
  changes: string | null;
}

export interface ExecutionTrendResponse {
  date: string;
  succeeded: number;
  failed: number;
  timedOut: number;
  cancelled: number;
  totalExecutions: number;
}

export interface TopFailingJobResponse {
  jobId: string;
  jobName: string;
  failureCount: number;
  lastFailureTime: string;
  lastErrorMessage: string | null;
}

export interface StaleJobResponse {
  jobId: string;
  jobName: string;
  lastExecutionTime: string;
  daysSinceLastExecution: number;
  isActive: boolean;
}

export interface UpcomingScheduleResponse {
  jobId: string;
  jobName: string;
  scheduleId: string;
  nextExecutionTime: string;
  recurrenceType: RecurrenceType;
  hoursUntilExecution: number;
}

// Command/Request Models
export interface CreateJobCommand {
  name: string;
  description: string;
  assemblyName: string;
  className: string;
  methodName: string;
  timeoutSeconds?: number;
  expectedDurationSeconds?: number;
  maxConcurrentExecutions?: number;
  allowManualTrigger: boolean;
  retryPolicy?: {
    maxRetries: number;
    strategy: RetryStrategy;
    initialDelaySeconds: number;
    maxDelaySeconds?: number;
    backoffMultiplier?: number;
  };
  owner: {
    userId: string;
    userName: string;
    email: string;
    teamName?: string;
  };
}

export interface UpdateJobCommand {
  id: string;
  name: string;
  description: string;
  timeoutSeconds?: number;
  expectedDurationSeconds?: number;
  maxConcurrentExecutions?: number;
  allowManualTrigger: boolean;
}

export interface CreateScheduleCommand {
  jobId: string;
  recurrenceType: RecurrenceType;
  cronExpression?: string;
  startDate: string;
  endDate?: string;
  timeOfDay?: string;
  daysOfWeek?: number[];
  dayOfMonth?: number;
  nthBusinessDay?: number;
}

export interface TriggerJobCommand {
  jobId: string;
  triggeredBy: string;
  parameters?: Record<string, string>;
}

export interface CancelExecutionCommand {
  executionId: string;
  cancelledBy: string;
  reason?: string;
}

// Pagination
export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
  totalPages: number;
  hasNextPage: boolean;
  hasPreviousPage: boolean;
}

export interface PagedRequest {
  pageNumber?: number;
  pageSize?: number;
  sortBy?: string;
  sortDirection?: 'asc' | 'desc';
}

// Filter Models
export interface JobExecutionFilter extends PagedRequest {
  jobId?: string;
  status?: ExecutionStatus;
  startDateFrom?: string;
  startDateTo?: string;
  isManualTrigger?: boolean;
}

export interface AuditLogFilter extends PagedRequest {
  entityType?: string;
  entityId?: string;
  action?: string;
  userId?: string;
  dateFrom?: string;
  dateTo?: string;
}
