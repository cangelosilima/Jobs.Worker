-- =============================================
-- Stored Procedures for JobExecution
-- =============================================
USE JobScheduler;
GO

-- =============================================
-- Upsert JobExecution
-- =============================================
CREATE OR ALTER PROCEDURE dbo.usp_JobExecution_Upsert
    @Id UNIQUEIDENTIFIER,
    @JobDefinitionId UNIQUEIDENTIFIER,
    @JobScheduleId UNIQUEIDENTIFIER = NULL,
    @CorrelationId UNIQUEIDENTIFIER,
    @TraceId NVARCHAR(100),
    @Status INT,
    @QueuedAtUtc DATETIME2 = NULL,
    @StartedAtUtc DATETIME2 = NULL,
    @CompletedAtUtc DATETIME2 = NULL,
    @DurationSeconds INT = 0,
    @RetryAttempt INT = 0,
    @MaxRetryAttempts INT = 0,
    @HostInstance NVARCHAR(200),
    @InputPayload NVARCHAR(MAX) = NULL,
    @OutputPayload NVARCHAR(MAX) = NULL,
    @ErrorMessage NVARCHAR(MAX) = NULL,
    @StackTrace NVARCHAR(MAX) = NULL,
    @TriggeredBy NVARCHAR(100) = NULL,
    @IsManualTrigger BIT = 0,
    @NextRetryAtUtc DATETIME2 = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRANSACTION;

    IF @QueuedAtUtc IS NULL
        SET @QueuedAtUtc = GETUTCDATE();

    IF EXISTS (SELECT 1 FROM dbo.JobExecution WHERE Id = @Id)
    BEGIN
        -- Update
        UPDATE dbo.JobExecution
        SET
            Status = @Status,
            StartedAtUtc = ISNULL(@StartedAtUtc, StartedAtUtc),
            CompletedAtUtc = ISNULL(@CompletedAtUtc, CompletedAtUtc),
            DurationSeconds = @DurationSeconds,
            RetryAttempt = @RetryAttempt,
            OutputPayload = ISNULL(@OutputPayload, OutputPayload),
            ErrorMessage = ISNULL(@ErrorMessage, ErrorMessage),
            StackTrace = ISNULL(@StackTrace, StackTrace),
            NextRetryAtUtc = @NextRetryAtUtc
        WHERE Id = @Id;
    END
    ELSE
    BEGIN
        -- Insert
        INSERT INTO dbo.JobExecution (
            Id, JobDefinitionId, JobScheduleId, CorrelationId, TraceId,
            Status, QueuedAtUtc, StartedAtUtc, CompletedAtUtc, DurationSeconds,
            RetryAttempt, MaxRetryAttempts, HostInstance, InputPayload,
            OutputPayload, ErrorMessage, StackTrace, TriggeredBy,
            IsManualTrigger, NextRetryAtUtc
        )
        VALUES (
            @Id, @JobDefinitionId, @JobScheduleId, @CorrelationId, @TraceId,
            @Status, @QueuedAtUtc, @StartedAtUtc, @CompletedAtUtc, @DurationSeconds,
            @RetryAttempt, @MaxRetryAttempts, @HostInstance, @InputPayload,
            @OutputPayload, @ErrorMessage, @StackTrace, @TriggeredBy,
            @IsManualTrigger, @NextRetryAtUtc
        );
    END

    COMMIT TRANSACTION;

    SELECT @Id AS Id;
END
GO

-- =============================================
-- Get JobExecution by Id
-- =============================================
CREATE OR ALTER PROCEDURE dbo.usp_JobExecution_GetById
    @Id UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        je.*,
        jd.Name AS JobName,
        jd.Category
    FROM dbo.JobExecution je
    INNER JOIN dbo.JobDefinition jd ON je.JobDefinitionId = jd.Id
    WHERE je.Id = @Id;
END
GO

-- =============================================
-- Get Running Executions
-- =============================================
CREATE OR ALTER PROCEDURE dbo.usp_JobExecution_GetRunning
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        je.*,
        jd.Name AS JobName,
        jd.Category,
        jd.TimeoutSeconds,
        jd.ExpectedDurationSeconds
    FROM dbo.JobExecution je
    INNER JOIN dbo.JobDefinition jd ON je.JobDefinitionId = jd.Id
    WHERE je.Status IN (1, 2) -- Queued or Running
    ORDER BY je.QueuedAtUtc;
END
GO

-- =============================================
-- Get Failed Executions Today
-- =============================================
CREATE OR ALTER PROCEDURE dbo.usp_JobExecution_GetFailedToday
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Today DATETIME2 = CAST(GETUTCDATE() AS DATE);

    SELECT
        je.*,
        jd.Name AS JobName,
        jd.Category,
        jo.OwnerName,
        jo.OwnerEmail
    FROM dbo.JobExecution je
    INNER JOIN dbo.JobDefinition jd ON je.JobDefinitionId = jd.Id
    LEFT JOIN dbo.JobOwnership jo ON jd.Id = jo.JobDefinitionId
    WHERE je.Status = 4 -- Failed
      AND je.QueuedAtUtc >= @Today
    ORDER BY je.CompletedAtUtc DESC;
END
GO

-- =============================================
-- Get Executions by Job Id
-- =============================================
CREATE OR ALTER PROCEDURE dbo.usp_JobExecution_GetByJobId
    @JobDefinitionId UNIQUEIDENTIFIER,
    @PageSize INT = 50
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TOP (@PageSize)
        je.*,
        jd.Name AS JobName,
        jd.Category
    FROM dbo.JobExecution je
    INNER JOIN dbo.JobDefinition jd ON je.JobDefinitionId = jd.Id
    WHERE je.JobDefinitionId = @JobDefinitionId
    ORDER BY je.QueuedAtUtc DESC;
END
GO

-- =============================================
-- Get Executions by Status
-- =============================================
CREATE OR ALTER PROCEDURE dbo.usp_JobExecution_GetByStatus
    @Status INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        je.*,
        jd.Name AS JobName,
        jd.Category
    FROM dbo.JobExecution je
    INNER JOIN dbo.JobDefinition jd ON je.JobDefinitionId = jd.Id
    WHERE je.Status = @Status
    ORDER BY je.QueuedAtUtc DESC;
END
GO

-- =============================================
-- Get Active Execution Count for Job
-- =============================================
CREATE OR ALTER PROCEDURE dbo.usp_JobExecution_GetActiveCount
    @JobDefinitionId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;

    SELECT COUNT(*) AS ActiveCount
    FROM dbo.JobExecution
    WHERE JobDefinitionId = @JobDefinitionId
      AND Status IN (1, 2); -- Queued or Running
END
GO

-- =============================================
-- Get Executions Exceeding Expected Duration
-- =============================================
CREATE OR ALTER PROCEDURE dbo.usp_JobExecution_GetExceedingDuration
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        je.*,
        jd.Name AS JobName,
        jd.Category,
        jd.ExpectedDurationSeconds,
        DATEDIFF(SECOND, je.StartedAtUtc, GETUTCDATE()) AS CurrentDurationSeconds
    FROM dbo.JobExecution je
    INNER JOIN dbo.JobDefinition jd ON je.JobDefinitionId = jd.Id
    WHERE je.Status = 2 -- Running
      AND jd.ExpectedDurationSeconds IS NOT NULL
      AND DATEDIFF(SECOND, je.StartedAtUtc, GETUTCDATE()) > jd.ExpectedDurationSeconds
    ORDER BY CurrentDurationSeconds DESC;
END
GO

-- =============================================
-- Insert JobExecutionLog
-- =============================================
CREATE OR ALTER PROCEDURE dbo.usp_JobExecutionLog_Insert
    @JobExecutionId UNIQUEIDENTIFIER,
    @LogLevel NVARCHAR(20),
    @Message NVARCHAR(4000),
    @Exception NVARCHAR(MAX) = NULL,
    @Properties NVARCHAR(MAX) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.JobExecutionLog (
        JobExecutionId, TimestampUtc, LogLevel, Message, Exception, Properties
    )
    VALUES (
        @JobExecutionId, GETUTCDATE(), @LogLevel, @Message, @Exception, @Properties
    );
END
GO

PRINT 'JobExecution stored procedures created successfully';
GO
