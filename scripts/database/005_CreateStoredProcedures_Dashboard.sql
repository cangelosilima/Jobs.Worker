-- =============================================
-- Stored Procedures for Dashboard & Analytics
-- =============================================
USE JobScheduler;
GO

-- =============================================
-- Get Dashboard Stats
-- =============================================
CREATE OR ALTER PROCEDURE dbo.usp_Dashboard_GetStats
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @TotalJobs INT;
    DECLARE @ActiveJobs INT;
    DECLARE @DisabledJobs INT;
    DECLARE @RunningExecutions INT;
    DECLARE @FailedToday INT;
    DECLARE @SucceededToday INT;
    DECLARE @DelayedOrSkipped INT;
    DECLARE @ExceedingDuration INT;
    DECLARE @AvgExecutionTime DECIMAL(10,2);
    DECLARE @SuccessRate DECIMAL(5,2);
    DECLARE @Today DATETIME2 = CAST(GETUTCDATE() AS DATE);

    -- Total jobs
    SELECT @TotalJobs = COUNT(*) FROM dbo.JobDefinition;

    -- Active jobs
    SELECT @ActiveJobs = COUNT(*) FROM dbo.JobDefinition WHERE Status = 1;

    -- Disabled jobs
    SELECT @DisabledJobs = COUNT(*) FROM dbo.JobDefinition WHERE Status = 2;

    -- Running executions
    SELECT @RunningExecutions = COUNT(*)
    FROM dbo.JobExecution
    WHERE Status IN (1, 2); -- Queued or Running

    -- Failed today
    SELECT @FailedToday = COUNT(*)
    FROM dbo.JobExecution
    WHERE Status = 4 AND QueuedAtUtc >= @Today;

    -- Succeeded today
    SELECT @SucceededToday = COUNT(*)
    FROM dbo.JobExecution
    WHERE Status = 3 AND CompletedAtUtc >= @Today;

    -- Delayed or skipped
    SELECT @DelayedOrSkipped = COUNT(*)
    FROM dbo.JobExecution
    WHERE Status = 7 AND QueuedAtUtc >= @Today;

    -- Exceeding expected duration
    SELECT @ExceedingDuration = COUNT(*)
    FROM dbo.JobExecution je
    INNER JOIN dbo.JobDefinition jd ON je.JobDefinitionId = jd.Id
    WHERE je.Status = 2 -- Running
      AND jd.ExpectedDurationSeconds IS NOT NULL
      AND DATEDIFF(SECOND, je.StartedAtUtc, GETUTCDATE()) > jd.ExpectedDurationSeconds;

    -- Average execution time (today)
    SELECT @AvgExecutionTime = ISNULL(AVG(CAST(DurationSeconds AS DECIMAL(10,2))), 0)
    FROM dbo.JobExecution
    WHERE CompletedAtUtc >= @Today
      AND Status IN (3, 4); -- Succeeded or Failed

    -- Success rate (today)
    DECLARE @TotalToday INT = @SucceededToday + @FailedToday;
    IF @TotalToday > 0
        SET @SuccessRate = CAST(@SucceededToday AS DECIMAL(10,2)) / @TotalToday * 100;
    ELSE
        SET @SuccessRate = 100.00;

    -- Return results
    SELECT
        @TotalJobs AS TotalJobs,
        @ActiveJobs AS ActiveJobs,
        @DisabledJobs AS DisabledJobs,
        @RunningExecutions AS RunningExecutions,
        @FailedToday AS FailedToday,
        @SucceededToday AS SucceededToday,
        @DelayedOrSkipped AS DelayedOrSkipped,
        @ExceedingDuration AS ExceedingExpectedDuration,
        @AvgExecutionTime AS AverageExecutionTimeSeconds,
        @SuccessRate AS SuccessRatePercentage;
END
GO

-- =============================================
-- Get Job Execution Trends (Last 7 Days)
-- =============================================
CREATE OR ALTER PROCEDURE dbo.usp_Dashboard_GetExecutionTrends
    @Days INT = 7
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        CAST(QueuedAtUtc AS DATE) AS ExecutionDate,
        COUNT(*) AS TotalExecutions,
        SUM(CASE WHEN Status = 3 THEN 1 ELSE 0 END) AS SuccessCount,
        SUM(CASE WHEN Status = 4 THEN 1 ELSE 0 END) AS FailureCount,
        SUM(CASE WHEN Status = 5 THEN 1 ELSE 0 END) AS TimeoutCount,
        AVG(CAST(DurationSeconds AS DECIMAL(10,2))) AS AvgDurationSeconds
    FROM dbo.JobExecution
    WHERE QueuedAtUtc >= DATEADD(DAY, -@Days, GETUTCDATE())
    GROUP BY CAST(QueuedAtUtc AS DATE)
    ORDER BY ExecutionDate DESC;
END
GO

-- =============================================
-- Get Job Execution Duration Trends
-- =============================================
CREATE OR ALTER PROCEDURE dbo.usp_Dashboard_GetJobDurationTrends
    @JobDefinitionId UNIQUEIDENTIFIER,
    @Days INT = 30
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        CAST(CompletedAtUtc AS DATE) AS ExecutionDate,
        COUNT(*) AS ExecutionCount,
        AVG(CAST(DurationSeconds AS DECIMAL(10,2))) AS AvgDurationSeconds,
        MIN(DurationSeconds) AS MinDurationSeconds,
        MAX(DurationSeconds) AS MaxDurationSeconds
    FROM dbo.JobExecution
    WHERE JobDefinitionId = @JobDefinitionId
      AND Status IN (3, 4) -- Succeeded or Failed
      AND CompletedAtUtc >= DATEADD(DAY, -@Days, GETUTCDATE())
    GROUP BY CAST(CompletedAtUtc AS DATE)
    ORDER BY ExecutionDate DESC;
END
GO

-- =============================================
-- Get Top Failing Jobs
-- =============================================
CREATE OR ALTER PROCEDURE dbo.usp_Dashboard_GetTopFailingJobs
    @TopN INT = 10,
    @Days INT = 7
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TOP (@TopN)
        jd.Id AS JobDefinitionId,
        jd.Name AS JobName,
        jd.Category,
        COUNT(*) AS FailureCount,
        MAX(je.CompletedAtUtc) AS LastFailureUtc,
        jo.OwnerName,
        jo.OwnerEmail
    FROM dbo.JobExecution je
    INNER JOIN dbo.JobDefinition jd ON je.JobDefinitionId = jd.Id
    LEFT JOIN dbo.JobOwnership jo ON jd.Id = jo.JobDefinitionId
    WHERE je.Status = 4 -- Failed
      AND je.QueuedAtUtc >= DATEADD(DAY, -@Days, GETUTCDATE())
    GROUP BY jd.Id, jd.Name, jd.Category, jo.OwnerName, jo.OwnerEmail
    ORDER BY FailureCount DESC;
END
GO

-- =============================================
-- Get Job Execution Summary
-- =============================================
CREATE OR ALTER PROCEDURE dbo.usp_Dashboard_GetJobExecutionSummary
    @JobDefinitionId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        COUNT(*) AS TotalExecutions,
        SUM(CASE WHEN Status = 3 THEN 1 ELSE 0 END) AS SuccessCount,
        SUM(CASE WHEN Status = 4 THEN 1 ELSE 0 END) AS FailureCount,
        SUM(CASE WHEN Status = 5 THEN 1 ELSE 0 END) AS TimeoutCount,
        SUM(CASE WHEN Status = 6 THEN 1 ELSE 0 END) AS CancelledCount,
        SUM(CASE WHEN Status = 7 THEN 1 ELSE 0 END) AS SkippedCount,
        AVG(CAST(DurationSeconds AS DECIMAL(10,2))) AS AvgDurationSeconds,
        MIN(DurationSeconds) AS MinDurationSeconds,
        MAX(DurationSeconds) AS MaxDurationSeconds,
        MAX(CompletedAtUtc) AS LastExecutionUtc
    FROM dbo.JobExecution
    WHERE JobDefinitionId = @JobDefinitionId
      AND Status IN (3, 4, 5, 6, 7); -- Completed statuses
END
GO

-- =============================================
-- Get Stale Jobs (Not Executed Recently)
-- =============================================
CREATE OR ALTER PROCEDURE dbo.usp_Dashboard_GetStaleJobs
    @HoursSinceLastRun INT = 24
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @ThresholdUtc DATETIME2 = DATEADD(HOUR, -@HoursSinceLastRun, GETUTCDATE());

    SELECT
        jd.Id AS JobDefinitionId,
        jd.Name AS JobName,
        jd.Category,
        jd.Status,
        MAX(js.LastExecutionUtc) AS LastExecutionUtc,
        DATEDIFF(HOUR, MAX(js.LastExecutionUtc), GETUTCDATE()) AS HoursSinceLastRun,
        jo.OwnerName,
        jo.OwnerEmail
    FROM dbo.JobDefinition jd
    LEFT JOIN dbo.JobSchedule js ON jd.Id = js.JobDefinitionId
    LEFT JOIN dbo.JobOwnership jo ON jd.Id = jo.JobDefinitionId
    WHERE jd.Status = 1 -- Active
      AND (
          js.LastExecutionUtc < @ThresholdUtc
          OR js.LastExecutionUtc IS NULL
      )
    GROUP BY jd.Id, jd.Name, jd.Category, jd.Status, jo.OwnerName, jo.OwnerEmail
    ORDER BY HoursSinceLastRun DESC;
END
GO

PRINT 'Dashboard stored procedures created successfully';
GO
