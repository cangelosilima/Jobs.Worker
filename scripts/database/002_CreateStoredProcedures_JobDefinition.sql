-- =============================================
-- Stored Procedures for JobDefinition
-- =============================================
USE JobScheduler;
GO

-- =============================================
-- Upsert JobDefinition
-- =============================================
CREATE OR ALTER PROCEDURE dbo.usp_JobDefinition_Upsert
    @Id UNIQUEIDENTIFIER,
    @Name NVARCHAR(200),
    @Description NVARCHAR(1000),
    @Category NVARCHAR(100),
    @Status INT,
    @AllowedEnvironments INT,
    @ExecutionMode INT,
    @ExecutionAssembly NVARCHAR(500),
    @ExecutionTypeName NVARCHAR(500),
    @ExecutionCommand NVARCHAR(MAX) = NULL,
    @ContainerImage NVARCHAR(500) = NULL,
    @TimeoutSeconds INT,
    @MaxRetries INT,
    @RetryStrategy INT,
    @BaseDelaySeconds INT,
    @MaxDelaySeconds INT = 3600,
    @MaxConcurrentExecutions INT = 1,
    @AllowManualTrigger BIT = 1,
    @ExpectedDurationSeconds INT = NULL,
    @ParameterSchema NVARCHAR(MAX) = '{}',
    @PerformedBy NVARCHAR(100),
    @IpAddress NVARCHAR(50) = '0.0.0.0',
    @UserAgent NVARCHAR(500) = 'System'
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRANSACTION;

    DECLARE @IsInsert BIT = 0;
    DECLARE @OldValues NVARCHAR(MAX);
    DECLARE @NewValues NVARCHAR(MAX);

    IF EXISTS (SELECT 1 FROM dbo.JobDefinition WHERE Id = @Id)
    BEGIN
        -- Update
        SELECT @OldValues = (
            SELECT * FROM dbo.JobDefinition WHERE Id = @Id FOR JSON PATH, WITHOUT_ARRAY_WRAPPER
        );

        UPDATE dbo.JobDefinition
        SET
            Name = @Name,
            Description = @Description,
            Category = @Category,
            Status = @Status,
            AllowedEnvironments = @AllowedEnvironments,
            ExecutionMode = @ExecutionMode,
            ExecutionAssembly = @ExecutionAssembly,
            ExecutionTypeName = @ExecutionTypeName,
            ExecutionCommand = @ExecutionCommand,
            ContainerImage = @ContainerImage,
            TimeoutSeconds = @TimeoutSeconds,
            MaxRetries = @MaxRetries,
            RetryStrategy = @RetryStrategy,
            BaseDelaySeconds = @BaseDelaySeconds,
            MaxDelaySeconds = @MaxDelaySeconds,
            MaxConcurrentExecutions = @MaxConcurrentExecutions,
            AllowManualTrigger = @AllowManualTrigger,
            ExpectedDurationSeconds = @ExpectedDurationSeconds,
            ParameterSchema = @ParameterSchema,
            UpdatedBy = @PerformedBy,
            UpdatedAtUtc = GETUTCDATE(),
            Version = Version + 1
        WHERE Id = @Id;

        SELECT @NewValues = (
            SELECT * FROM dbo.JobDefinition WHERE Id = @Id FOR JSON PATH, WITHOUT_ARRAY_WRAPPER
        );

        -- Audit
        INSERT INTO dbo.JobAudit (JobDefinitionId, Action, PerformedBy, OldValues, NewValues, IpAddress, UserAgent)
        VALUES (@Id, 2, @PerformedBy, @OldValues, @NewValues, @IpAddress, @UserAgent); -- Action 2 = JobUpdated
    END
    ELSE
    BEGIN
        -- Insert
        SET @IsInsert = 1;

        IF @Id IS NULL OR @Id = '00000000-0000-0000-0000-000000000000'
            SET @Id = NEWID();

        INSERT INTO dbo.JobDefinition (
            Id, Name, Description, Category, Status, AllowedEnvironments,
            ExecutionMode, ExecutionAssembly, ExecutionTypeName, ExecutionCommand,
            ContainerImage, TimeoutSeconds, MaxRetries, RetryStrategy,
            BaseDelaySeconds, MaxDelaySeconds, MaxConcurrentExecutions,
            AllowManualTrigger, ExpectedDurationSeconds, ParameterSchema,
            CreatedBy, CreatedAtUtc
        )
        VALUES (
            @Id, @Name, @Description, @Category, @Status, @AllowedEnvironments,
            @ExecutionMode, @ExecutionAssembly, @ExecutionTypeName, @ExecutionCommand,
            @ContainerImage, @TimeoutSeconds, @MaxRetries, @RetryStrategy,
            @BaseDelaySeconds, @MaxDelaySeconds, @MaxConcurrentExecutions,
            @AllowManualTrigger, @ExpectedDurationSeconds, @ParameterSchema,
            @PerformedBy, GETUTCDATE()
        );

        SELECT @NewValues = (
            SELECT * FROM dbo.JobDefinition WHERE Id = @Id FOR JSON PATH, WITHOUT_ARRAY_WRAPPER
        );

        -- Audit
        INSERT INTO dbo.JobAudit (JobDefinitionId, Action, PerformedBy, NewValues, IpAddress, UserAgent)
        VALUES (@Id, 1, @PerformedBy, @NewValues, @IpAddress, @UserAgent); -- Action 1 = JobCreated
    END

    COMMIT TRANSACTION;

    SELECT @Id AS Id, @IsInsert AS IsInsert;
END
GO

-- =============================================
-- Get JobDefinition by Id
-- =============================================
CREATE OR ALTER PROCEDURE dbo.usp_JobDefinition_GetById
    @Id UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        jd.*,
        jo.OwnerName,
        jo.OwnerEmail,
        jo.TeamName,
        jo.TeamChannel,
        jo.EscalationEmail
    FROM dbo.JobDefinition jd
    LEFT JOIN dbo.JobOwnership jo ON jd.Id = jo.JobDefinitionId
    WHERE jd.Id = @Id;
END
GO

-- =============================================
-- Get All Active JobDefinitions
-- =============================================
CREATE OR ALTER PROCEDURE dbo.usp_JobDefinition_GetActive
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        jd.*,
        jo.OwnerName,
        jo.OwnerEmail,
        jo.TeamName
    FROM dbo.JobDefinition jd
    LEFT JOIN dbo.JobOwnership jo ON jd.Id = jo.JobDefinitionId
    WHERE jd.Status = 1 -- Active
    ORDER BY jd.Category, jd.Name;
END
GO

-- =============================================
-- Get JobDefinitions by Category
-- =============================================
CREATE OR ALTER PROCEDURE dbo.usp_JobDefinition_GetByCategory
    @Category NVARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        jd.*,
        jo.OwnerName,
        jo.OwnerEmail,
        jo.TeamName
    FROM dbo.JobDefinition jd
    LEFT JOIN dbo.JobOwnership jo ON jd.Id = jo.JobDefinitionId
    WHERE jd.Category = @Category
    ORDER BY jd.Name;
END
GO

-- =============================================
-- Delete JobDefinition (Soft delete - Archive)
-- =============================================
CREATE OR ALTER PROCEDURE dbo.usp_JobDefinition_Delete
    @Id UNIQUEIDENTIFIER,
    @PerformedBy NVARCHAR(100),
    @IpAddress NVARCHAR(50) = '0.0.0.0',
    @UserAgent NVARCHAR(500) = 'System'
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRANSACTION;

    UPDATE dbo.JobDefinition
    SET
        Status = 3, -- Archived
        UpdatedBy = @PerformedBy,
        UpdatedAtUtc = GETUTCDATE()
    WHERE Id = @Id;

    -- Audit
    INSERT INTO dbo.JobAudit (JobDefinitionId, Action, PerformedBy, IpAddress, UserAgent)
    VALUES (@Id, 5, @PerformedBy, @IpAddress, @UserAgent); -- Action 5 = JobDeleted

    COMMIT TRANSACTION;
END
GO

-- =============================================
-- Update JobDefinition Status
-- =============================================
CREATE OR ALTER PROCEDURE dbo.usp_JobDefinition_UpdateStatus
    @Id UNIQUEIDENTIFIER,
    @NewStatus INT,
    @Reason NVARCHAR(500) = NULL,
    @PerformedBy NVARCHAR(100),
    @IpAddress NVARCHAR(50) = '0.0.0.0',
    @UserAgent NVARCHAR(500) = 'System'
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @OldStatus INT;
    DECLARE @AuditAction INT;

    BEGIN TRANSACTION;

    SELECT @OldStatus = Status FROM dbo.JobDefinition WHERE Id = @Id;

    IF @NewStatus = 2 -- Disabled
    BEGIN
        SET @AuditAction = 3; -- JobDisabled

        UPDATE dbo.JobDefinition
        SET
            Status = @NewStatus,
            DisabledBy = @PerformedBy,
            DisabledAtUtc = GETUTCDATE(),
            DisabledReason = @Reason,
            UpdatedBy = @PerformedBy,
            UpdatedAtUtc = GETUTCDATE()
        WHERE Id = @Id;
    END
    ELSE IF @NewStatus = 1 -- Active
    BEGIN
        SET @AuditAction = 4; -- JobEnabled

        UPDATE dbo.JobDefinition
        SET
            Status = @NewStatus,
            DisabledBy = NULL,
            DisabledAtUtc = NULL,
            DisabledReason = NULL,
            UpdatedBy = @PerformedBy,
            UpdatedAtUtc = GETUTCDATE()
        WHERE Id = @Id;
    END
    ELSE
    BEGIN
        UPDATE dbo.JobDefinition
        SET
            Status = @NewStatus,
            UpdatedBy = @PerformedBy,
            UpdatedAtUtc = GETUTCDATE()
        WHERE Id = @Id;

        SET @AuditAction = 2; -- JobUpdated
    END

    -- Audit
    INSERT INTO dbo.JobAudit (
        JobDefinitionId, Action, PerformedBy,
        OldValues, NewValues, IpAddress, UserAgent
    )
    VALUES (
        @Id, @AuditAction, @PerformedBy,
        CONCAT('{"Status":', @OldStatus, '}'),
        CONCAT('{"Status":', @NewStatus, ',"Reason":"', ISNULL(@Reason, ''), '"}'),
        @IpAddress, @UserAgent
    );

    COMMIT TRANSACTION;
END
GO

PRINT 'JobDefinition stored procedures created successfully';
GO
