-- =============================================
-- Additional Indexes and Views
-- =============================================
USE JobScheduler;
GO

-- =============================================
-- Additional Indexes for Performance
-- =============================================

-- JobExecution - Composite index for dashboard queries
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_JobExecution_Status_QueuedAt_Composite')
BEGIN
    CREATE NONCLUSTERED INDEX IX_JobExecution_Status_QueuedAt_Composite
    ON dbo.JobExecution (Status, QueuedAtUtc)
    INCLUDE (JobDefinitionId, CompletedAtUtc, DurationSeconds);
END
GO

-- JobExecution - Index for correlation tracking
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_JobExecution_NextRetry')
BEGIN
    CREATE NONCLUSTERED INDEX IX_JobExecution_NextRetry
    ON dbo.JobExecution (NextRetryAtUtc)
    WHERE Status = 8 AND NextRetryAtUtc IS NOT NULL; -- Retrying status
END
GO

-- =============================================
-- Views for Common Queries
-- =============================================

-- View: Active Jobs with Latest Execution
CREATE OR ALTER VIEW dbo.vw_ActiveJobsWithLatestExecution
AS
SELECT
    jd.Id AS JobDefinitionId,
    jd.Name,
    jd.Description,
    jd.Category,
    jd.Status,
    jd.AllowedEnvironments,
    jd.TimeoutSeconds,
    jd.MaxConcurrentExecutions,
    jd.ExpectedDurationSeconds,
    jo.OwnerName,
    jo.OwnerEmail,
    jo.TeamName,
    (SELECT TOP 1 je.Id
     FROM dbo.JobExecution je
     WHERE je.JobDefinitionId = jd.Id
     ORDER BY je.QueuedAtUtc DESC) AS LastExecutionId,
    (SELECT TOP 1 je.Status
     FROM dbo.JobExecution je
     WHERE je.JobDefinitionId = jd.Id
     ORDER BY je.QueuedAtUtc DESC) AS LastExecutionStatus,
    (SELECT TOP 1 je.CompletedAtUtc
     FROM dbo.JobExecution je
     WHERE je.JobDefinitionId = jd.Id
     ORDER BY je.QueuedAtUtc DESC) AS LastExecutionCompletedUtc,
    (SELECT TOP 1 je.DurationSeconds
     FROM dbo.JobExecution je
     WHERE je.JobDefinitionId = jd.Id
     ORDER BY je.QueuedAtUtc DESC) AS LastExecutionDurationSeconds,
    (SELECT MIN(NextExecutionUtc)
     FROM dbo.JobSchedule js
     WHERE js.JobDefinitionId = jd.Id
       AND js.IsActive = 1
       AND js.NextExecutionUtc IS NOT NULL) AS NextScheduledExecutionUtc
FROM dbo.JobDefinition jd
LEFT JOIN dbo.JobOwnership jo ON jd.Id = jo.JobDefinitionId
WHERE jd.Status = 1; -- Active
GO

-- View: Job Execution Summary
CREATE OR ALTER VIEW dbo.vw_JobExecutionSummary
AS
SELECT
    jd.Id AS JobDefinitionId,
    jd.Name AS JobName,
    jd.Category,
    COUNT(je.Id) AS TotalExecutions,
    SUM(CASE WHEN je.Status = 3 THEN 1 ELSE 0 END) AS SuccessCount,
    SUM(CASE WHEN je.Status = 4 THEN 1 ELSE 0 END) AS FailureCount,
    SUM(CASE WHEN je.Status = 5 THEN 1 ELSE 0 END) AS TimeoutCount,
    CASE
        WHEN COUNT(je.Id) > 0 THEN
            CAST(SUM(CASE WHEN je.Status = 3 THEN 1 ELSE 0 END) AS DECIMAL(10,2)) / COUNT(je.Id) * 100
        ELSE 0
    END AS SuccessRatePercentage,
    AVG(CAST(je.DurationSeconds AS DECIMAL(10,2))) AS AvgDurationSeconds,
    MAX(je.CompletedAtUtc) AS LastExecutionUtc
FROM dbo.JobDefinition jd
LEFT JOIN dbo.JobExecution je ON jd.Id = je.JobDefinitionId
    AND je.Status IN (3, 4, 5) -- Succeeded, Failed, TimedOut
GROUP BY jd.Id, jd.Name, jd.Category;
GO

-- View: Current Running Jobs
CREATE OR ALTER VIEW dbo.vw_CurrentRunningJobs
AS
SELECT
    je.Id AS ExecutionId,
    je.JobDefinitionId,
    jd.Name AS JobName,
    jd.Category,
    je.Status,
    je.QueuedAtUtc,
    je.StartedAtUtc,
    DATEDIFF(SECOND, je.StartedAtUtc, GETUTCDATE()) AS CurrentDurationSeconds,
    jd.TimeoutSeconds,
    jd.ExpectedDurationSeconds,
    je.HostInstance,
    je.TriggeredBy,
    je.IsManualTrigger,
    jo.OwnerName,
    jo.OwnerEmail,
    CASE
        WHEN DATEDIFF(SECOND, je.StartedAtUtc, GETUTCDATE()) > jd.TimeoutSeconds THEN 1
        ELSE 0
    END AS IsOverTimeout,
    CASE
        WHEN jd.ExpectedDurationSeconds IS NOT NULL
         AND DATEDIFF(SECOND, je.StartedAtUtc, GETUTCDATE()) > jd.ExpectedDurationSeconds THEN 1
        ELSE 0
    END AS IsOverExpectedDuration
FROM dbo.JobExecution je
INNER JOIN dbo.JobDefinition jd ON je.JobDefinitionId = jd.Id
LEFT JOIN dbo.JobOwnership jo ON jd.Id = jo.JobDefinitionId
WHERE je.Status IN (1, 2); -- Queued or Running
GO

-- View: Failed Jobs Today
CREATE OR ALTER VIEW dbo.vw_FailedJobsToday
AS
SELECT
    je.Id AS ExecutionId,
    je.JobDefinitionId,
    jd.Name AS JobName,
    jd.Category,
    je.QueuedAtUtc,
    je.StartedAtUtc,
    je.CompletedAtUtc,
    je.DurationSeconds,
    je.ErrorMessage,
    je.RetryAttempt,
    je.MaxRetryAttempts,
    je.TriggeredBy,
    jo.OwnerName,
    jo.OwnerEmail,
    jo.TeamName
FROM dbo.JobExecution je
INNER JOIN dbo.JobDefinition jd ON je.JobDefinitionId = jd.Id
LEFT JOIN dbo.JobOwnership jo ON jd.Id = jo.JobDefinitionId
WHERE je.Status = 4 -- Failed
  AND je.QueuedAtUtc >= CAST(GETUTCDATE() AS DATE);
GO

-- View: Upcoming Scheduled Jobs (Next 24 Hours)
CREATE OR ALTER VIEW dbo.vw_UpcomingScheduledJobs
AS
SELECT
    js.Id AS ScheduleId,
    js.JobDefinitionId,
    jd.Name AS JobName,
    jd.Category,
    js.ScheduleType,
    js.NextExecutionUtc,
    DATEDIFF(MINUTE, GETUTCDATE(), js.NextExecutionUtc) AS MinutesUntilExecution,
    jd.ExpectedDurationSeconds,
    jd.TimeoutSeconds,
    jo.OwnerName,
    jo.TeamName
FROM dbo.JobSchedule js
INNER JOIN dbo.JobDefinition jd ON js.JobDefinitionId = jd.Id
LEFT JOIN dbo.JobOwnership jo ON jd.Id = jo.JobDefinitionId
WHERE js.IsActive = 1
  AND jd.Status = 1 -- Active
  AND js.NextExecutionUtc IS NOT NULL
  AND js.NextExecutionUtc BETWEEN GETUTCDATE() AND DATEADD(HOUR, 24, GETUTCDATE())
;
GO

PRINT 'Indexes and views created successfully';
GO
