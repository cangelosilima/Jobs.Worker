-- =============================================
-- Stored Procedures for JobSchedule
-- =============================================
USE JobScheduler;
GO

-- =============================================
-- Upsert JobSchedule
-- =============================================
CREATE OR ALTER PROCEDURE dbo.usp_JobSchedule_Upsert
    @Id UNIQUEIDENTIFIER,
    @JobDefinitionId UNIQUEIDENTIFIER,
    @ScheduleType INT,
    @CronExpression NVARCHAR(100) = NULL,
    @TimeOfDay TIME = NULL,
    @DaysOfWeek INT = NULL,
    @DayOfMonth INT = NULL,
    @BusinessDayOfMonth INT = NULL,
    @AdjustToPreviousBusinessDay BIT = 0,
    @OneTimeExecutionDate DATETIME2 = NULL,
    @ConditionalExpression NVARCHAR(500) = NULL,
    @IsActive BIT = 1,
    @StartDateUtc DATETIME2 = NULL,
    @EndDateUtc DATETIME2 = NULL,
    @NextExecutionUtc DATETIME2 = NULL,
    @PerformedBy NVARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRANSACTION;

    DECLARE @IsInsert BIT = 0;

    IF EXISTS (SELECT 1 FROM dbo.JobSchedule WHERE Id = @Id)
    BEGIN
        -- Update
        UPDATE dbo.JobSchedule
        SET
            ScheduleType = @ScheduleType,
            CronExpression = @CronExpression,
            TimeOfDay = @TimeOfDay,
            DaysOfWeek = @DaysOfWeek,
            DayOfMonth = @DayOfMonth,
            BusinessDayOfMonth = @BusinessDayOfMonth,
            AdjustToPreviousBusinessDay = @AdjustToPreviousBusinessDay,
            OneTimeExecutionDate = @OneTimeExecutionDate,
            ConditionalExpression = @ConditionalExpression,
            IsActive = @IsActive,
            StartDateUtc = @StartDateUtc,
            EndDateUtc = @EndDateUtc,
            NextExecutionUtc = @NextExecutionUtc,
            UpdatedBy = @PerformedBy,
            UpdatedAtUtc = GETUTCDATE()
        WHERE Id = @Id;

        -- Audit
        INSERT INTO dbo.JobAudit (JobDefinitionId, Action, PerformedBy)
        VALUES (@JobDefinitionId, 6, @PerformedBy); -- Action 6 = ScheduleChanged
    END
    ELSE
    BEGIN
        -- Insert
        SET @IsInsert = 1;

        IF @Id IS NULL OR @Id = '00000000-0000-0000-0000-000000000000'
            SET @Id = NEWID();

        INSERT INTO dbo.JobSchedule (
            Id, JobDefinitionId, ScheduleType, CronExpression, TimeOfDay,
            DaysOfWeek, DayOfMonth, BusinessDayOfMonth, AdjustToPreviousBusinessDay,
            OneTimeExecutionDate, ConditionalExpression, IsActive, StartDateUtc,
            EndDateUtc, NextExecutionUtc, CreatedBy, CreatedAtUtc
        )
        VALUES (
            @Id, @JobDefinitionId, @ScheduleType, @CronExpression, @TimeOfDay,
            @DaysOfWeek, @DayOfMonth, @BusinessDayOfMonth, @AdjustToPreviousBusinessDay,
            @OneTimeExecutionDate, @ConditionalExpression, @IsActive, @StartDateUtc,
            @EndDateUtc, @NextExecutionUtc, @PerformedBy, GETUTCDATE()
        );

        -- Audit
        INSERT INTO dbo.JobAudit (JobDefinitionId, Action, PerformedBy)
        VALUES (@JobDefinitionId, 6, @PerformedBy); -- Action 6 = ScheduleChanged
    END

    COMMIT TRANSACTION;

    SELECT @Id AS Id, @IsInsert AS IsInsert;
END
GO

-- =============================================
-- Get JobSchedule by Id
-- =============================================
CREATE OR ALTER PROCEDURE dbo.usp_JobSchedule_GetById
    @Id UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        js.*,
        jd.Name AS JobName
    FROM dbo.JobSchedule js
    INNER JOIN dbo.JobDefinition jd ON js.JobDefinitionId = jd.Id
    WHERE js.Id = @Id;
END
GO

-- =============================================
-- Get Schedules for Job
-- =============================================
CREATE OR ALTER PROCEDURE dbo.usp_JobSchedule_GetByJobId
    @JobDefinitionId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;

    SELECT *
    FROM dbo.JobSchedule
    WHERE JobDefinitionId = @JobDefinitionId
    ORDER BY IsActive DESC, NextExecutionUtc;
END
GO

-- =============================================
-- Get Due Schedules
-- =============================================
CREATE OR ALTER PROCEDURE dbo.usp_JobSchedule_GetDue
    @CurrentTimeUtc DATETIME2 = NULL
AS
BEGIN
    SET NOCOUNT ON;

    IF @CurrentTimeUtc IS NULL
        SET @CurrentTimeUtc = GETUTCDATE();

    SELECT
        js.*,
        jd.Name AS JobName,
        jd.Status AS JobStatus,
        jd.MaxConcurrentExecutions,
        jd.MaxRetries,
        jd.RetryStrategy,
        jd.BaseDelaySeconds
    FROM dbo.JobSchedule js
    INNER JOIN dbo.JobDefinition jd ON js.JobDefinitionId = jd.Id
    WHERE js.IsActive = 1
      AND jd.Status = 1 -- Active
      AND js.NextExecutionUtc IS NOT NULL
      AND js.NextExecutionUtc <= @CurrentTimeUtc
      AND (js.StartDateUtc IS NULL OR js.StartDateUtc <= @CurrentTimeUtc)
      AND (js.EndDateUtc IS NULL OR js.EndDateUtc >= @CurrentTimeUtc)
    ORDER BY js.NextExecutionUtc;
END
GO

-- =============================================
-- Get Upcoming Schedules
-- =============================================
CREATE OR ALTER PROCEDURE dbo.usp_JobSchedule_GetUpcoming
    @StartTimeUtc DATETIME2,
    @EndTimeUtc DATETIME2
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        js.*,
        jd.Name AS JobName,
        jd.Category
    FROM dbo.JobSchedule js
    INNER JOIN dbo.JobDefinition jd ON js.JobDefinitionId = jd.Id
    WHERE js.IsActive = 1
      AND js.NextExecutionUtc IS NOT NULL
      AND js.NextExecutionUtc >= @StartTimeUtc
      AND js.NextExecutionUtc <= @EndTimeUtc
    ORDER BY js.NextExecutionUtc;
END
GO

-- =============================================
-- Update Schedule Last Execution
-- =============================================
CREATE OR ALTER PROCEDURE dbo.usp_JobSchedule_UpdateLastExecution
    @Id UNIQUEIDENTIFIER,
    @LastExecutionUtc DATETIME2 = NULL,
    @NextExecutionUtc DATETIME2 = NULL
AS
BEGIN
    SET NOCOUNT ON;

    IF @LastExecutionUtc IS NULL
        SET @LastExecutionUtc = GETUTCDATE();

    UPDATE dbo.JobSchedule
    SET
        LastExecutionUtc = @LastExecutionUtc,
        NextExecutionUtc = @NextExecutionUtc
    WHERE Id = @Id;
END
GO

-- =============================================
-- Delete JobSchedule
-- =============================================
CREATE OR ALTER PROCEDURE dbo.usp_JobSchedule_Delete
    @Id UNIQUEIDENTIFIER,
    @PerformedBy NVARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @JobDefinitionId UNIQUEIDENTIFIER;

    BEGIN TRANSACTION;

    SELECT @JobDefinitionId = JobDefinitionId
    FROM dbo.JobSchedule
    WHERE Id = @Id;

    DELETE FROM dbo.JobSchedule
    WHERE Id = @Id;

    -- Audit
    INSERT INTO dbo.JobAudit (JobDefinitionId, Action, PerformedBy, AdditionalData)
    VALUES (@JobDefinitionId, 6, @PerformedBy, CONCAT('{"ScheduleId":"', @Id, '","Action":"Deleted"}'));

    COMMIT TRANSACTION;
END
GO

PRINT 'JobSchedule stored procedures created successfully';
GO
