import { describe, it, expect, beforeAll, afterAll, afterEach } from 'vitest';
import { setupServer } from 'msw/node';
import { http, HttpResponse } from 'msw';
import { dashboardApi, jobsApi, executionsApi } from '../jobs.api';
import { JobStatus } from '../types';

const server = setupServer(
  // Dashboard stats endpoint
  http.get('/api/dashboard/stats', () => {
    return HttpResponse.json({
      totalJobs: 45,
      activeJobs: 38,
      disabledJobs: 5,
      runningExecutions: 7,
      failedToday: 3,
      succeededToday: 142,
      delayedOrSkipped: 2,
      exceedingExpectedDuration: 1,
      averageExecutionTimeSeconds: 45.5,
      successRatePercentage: 97.93,
    });
  }),

  // Get all jobs endpoint
  http.get('/api/jobs', () => {
    return HttpResponse.json([
      {
        id: '123e4567-e89b-12d3-a456-426614174000',
        name: 'Test Job',
        description: 'Test Description',
        assemblyName: 'Test.Assembly',
        className: 'Test.Class',
        methodName: 'Execute',
        status: JobStatus.Active,
        timeoutSeconds: 300,
        expectedDurationSeconds: 60,
        maxConcurrentExecutions: 5,
        allowManualTrigger: true,
        retryPolicy: null,
        owner: null,
        schedules: [],
        parameters: [],
        dependencies: [],
        notifications: [],
        createdAt: '2024-01-01T00:00:00Z',
        updatedAt: '2024-01-01T00:00:00Z',
        version: 1,
      },
    ]);
  }),

  // Get running executions endpoint
  http.get('/api/executions/running', () => {
    return HttpResponse.json([]);
  })
);

beforeAll(() => server.listen());
afterEach(() => server.resetHandlers());
afterAll(() => server.close());

describe('Dashboard API', () => {
  it('should fetch dashboard stats', async () => {
    const stats = await dashboardApi.getStats();

    expect(stats).toBeDefined();
    expect(stats.totalJobs).toBe(45);
    expect(stats.activeJobs).toBe(38);
    expect(stats.successRatePercentage).toBe(97.93);
  });
});

describe('Jobs API', () => {
  it('should fetch all jobs', async () => {
    const jobs = await jobsApi.getAll();

    expect(jobs).toBeDefined();
    expect(jobs).toHaveLength(1);
    expect(jobs[0].name).toBe('Test Job');
    expect(jobs[0].status).toBe(JobStatus.Active);
  });
});

describe('Executions API', () => {
  it('should fetch running executions', async () => {
    const executions = await executionsApi.getRunning();

    expect(executions).toBeDefined();
    expect(executions).toHaveLength(0);
  });
});

describe('API Error Handling', () => {
  it('should handle 404 errors', async () => {
    server.use(
      http.get('/api/jobs/:id', () => {
        return HttpResponse.json(
          { message: 'Job not found' },
          { status: 404 }
        );
      })
    );

    await expect(
      jobsApi.getById('non-existent-id')
    ).rejects.toMatchObject({
      message: expect.stringContaining('not found'),
      statusCode: 404,
    });
  });

  it('should handle 500 errors', async () => {
    server.use(
      http.get('/api/dashboard/stats', () => {
        return HttpResponse.json(
          { message: 'Internal server error' },
          { status: 500 }
        );
      })
    );

    await expect(
      dashboardApi.getStats()
    ).rejects.toMatchObject({
      statusCode: 500,
    });
  });
});
