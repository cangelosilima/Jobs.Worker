-- =============================================
-- Job Scheduler Database Schema
-- Version: 1.0
-- Description: Core tables for Job Scheduler Worker
-- =============================================

-- Create Database
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'JobScheduler')
BEGIN
    CREATE DATABASE JobScheduler;
END
GO

USE JobScheduler;
GO

-- =============================================
-- Table: JobDefinition
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'JobDefinition')
BEGIN
    CREATE TABLE dbo.JobDefinition (
        Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
        Name NVARCHAR(200) NOT NULL,
        Description NVARCHAR(1000) NULL,
        Category NVARCHAR(100) NOT NULL,
        Status INT NOT NULL DEFAULT 4, -- Draft
        AllowedEnvironments INT NOT NULL,
        ExecutionMode INT NOT NULL,
        ExecutionAssembly NVARCHAR(500) NULL,
        ExecutionTypeName NVARCHAR(500) NULL,
        ExecutionCommand NVARCHAR(MAX) NULL,
        ContainerImage NVARCHAR(500) NULL,
        TimeoutSeconds INT NOT NULL,
        MaxRetries INT NOT NULL DEFAULT 0,
        RetryStrategy INT NOT NULL DEFAULT 0,
        BaseDelaySeconds INT NOT NULL DEFAULT 0,
        MaxDelaySeconds INT NOT NULL DEFAULT 3600,
        MaxConcurrentExecutions INT NOT NULL DEFAULT 1,
        AllowManualTrigger BIT NOT NULL DEFAULT 1,
        ExpectedDurationSeconds INT NULL,
        ParameterSchema NVARCHAR(MAX) NOT NULL DEFAULT '{}',
        Version INT NOT NULL DEFAULT 1,
        CreatedBy NVARCHAR(100) NOT NULL,
        CreatedAtUtc DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        UpdatedBy NVARCHAR(100) NULL,
        UpdatedAtUtc DATETIME2 NULL,
        DisabledBy NVARCHAR(100) NULL,
        DisabledAtUtc DATETIME2 NULL,
        DisabledReason NVARCHAR(500) NULL,

        CONSTRAINT CK_JobDefinition_TimeoutSeconds CHECK (TimeoutSeconds > 0),
        CONSTRAINT CK_JobDefinition_MaxRetries CHECK (MaxRetries >= 0 AND MaxRetries <= 10),
        CONSTRAINT CK_JobDefinition_MaxConcurrent CHECK (MaxConcurrentExecutions > 0)
    );

    CREATE NONCLUSTERED INDEX IX_JobDefinition_Status ON dbo.JobDefinition(Status) INCLUDE (Category);
    CREATE NONCLUSTERED INDEX IX_JobDefinition_Category ON dbo.JobDefinition(Category);
    CREATE NONCLUSTERED INDEX IX_JobDefinition_Name ON dbo.JobDefinition(Name);
END
GO

-- =============================================
-- Table: JobSchedule
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'JobSchedule')
BEGIN
    CREATE TABLE dbo.JobSchedule (
        Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
        JobDefinitionId UNIQUEIDENTIFIER NOT NULL,
        ScheduleType INT NOT NULL,
        CronExpression NVARCHAR(100) NULL,
        TimeOfDay TIME NULL,
        DaysOfWeek INT NULL,
        DayOfMonth INT NULL,
        BusinessDayOfMonth INT NULL,
        AdjustToPreviousBusinessDay BIT NOT NULL DEFAULT 0,
        OneTimeExecutionDate DATETIME2 NULL,
        ConditionalExpression NVARCHAR(500) NULL,
        IsActive BIT NOT NULL DEFAULT 1,
        StartDateUtc DATETIME2 NULL,
        EndDateUtc DATETIME2 NULL,
        LastExecutionUtc DATETIME2 NULL,
        NextExecutionUtc DATETIME2 NULL,
        CreatedBy NVARCHAR(100) NOT NULL,
        CreatedAtUtc DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        UpdatedBy NVARCHAR(100) NULL,
        UpdatedAtUtc DATETIME2 NULL,

        CONSTRAINT FK_JobSchedule_JobDefinition FOREIGN KEY (JobDefinitionId)
            REFERENCES dbo.JobDefinition(Id) ON DELETE CASCADE,
        CONSTRAINT CK_JobSchedule_DayOfMonth CHECK (DayOfMonth IS NULL OR (DayOfMonth >= 1 AND DayOfMonth <= 31)),
        CONSTRAINT CK_JobSchedule_BusinessDay CHECK (BusinessDayOfMonth IS NULL OR (BusinessDayOfMonth >= 1 AND BusinessDayOfMonth <= 31))
    );

    CREATE NONCLUSTERED INDEX IX_JobSchedule_JobDefinitionId ON dbo.JobSchedule(JobDefinitionId);
    CREATE NONCLUSTERED INDEX IX_JobSchedule_NextExecution ON dbo.JobSchedule(NextExecutionUtc)
        WHERE IsActive = 1 AND NextExecutionUtc IS NOT NULL;
    CREATE NONCLUSTERED INDEX IX_JobSchedule_Active ON dbo.JobSchedule(IsActive) INCLUDE (NextExecutionUtc, JobDefinitionId);
END
GO

-- =============================================
-- Table: JobExecution
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'JobExecution')
BEGIN
    CREATE TABLE dbo.JobExecution (
        Id UNIQUEIDENTIFIER PRIMARY KEY,
        JobDefinitionId UNIQUEIDENTIFIER NOT NULL,
        JobScheduleId UNIQUEIDENTIFIER NULL,
        CorrelationId UNIQUEIDENTIFIER NOT NULL,
        TraceId NVARCHAR(100) NOT NULL,
        Status INT NOT NULL,
        QueuedAtUtc DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        StartedAtUtc DATETIME2 NULL,
        CompletedAtUtc DATETIME2 NULL,
        DurationSeconds INT NOT NULL DEFAULT 0,
        RetryAttempt INT NOT NULL DEFAULT 0,
        MaxRetryAttempts INT NOT NULL DEFAULT 0,
        HostInstance NVARCHAR(200) NOT NULL,
        InputPayload NVARCHAR(MAX) NULL,
        OutputPayload NVARCHAR(MAX) NULL,
        ErrorMessage NVARCHAR(MAX) NULL,
        StackTrace NVARCHAR(MAX) NULL,
        TriggeredBy NVARCHAR(100) NULL,
        IsManualTrigger BIT NOT NULL DEFAULT 0,
        NextRetryAtUtc DATETIME2 NULL,

        CONSTRAINT FK_JobExecution_JobDefinition FOREIGN KEY (JobDefinitionId)
            REFERENCES dbo.JobDefinition(Id),
        CONSTRAINT FK_JobExecution_JobSchedule FOREIGN KEY (JobScheduleId)
            REFERENCES dbo.JobSchedule(Id) ON DELETE SET NULL
    );

    CREATE NONCLUSTERED INDEX IX_JobExecution_JobDefinitionId ON dbo.JobExecution(JobDefinitionId)
        INCLUDE (Status, QueuedAtUtc, CompletedAtUtc, DurationSeconds);
    CREATE NONCLUSTERED INDEX IX_JobExecution_Status ON dbo.JobExecution(Status)
        INCLUDE (JobDefinitionId, QueuedAtUtc, StartedAtUtc);
    CREATE NONCLUSTERED INDEX IX_JobExecution_QueuedAt ON dbo.JobExecution(QueuedAtUtc DESC);
    CREATE NONCLUSTERED INDEX IX_JobExecution_CorrelationId ON dbo.JobExecution(CorrelationId);
    CREATE NONCLUSTERED INDEX IX_JobExecution_TraceId ON dbo.JobExecution(TraceId);
END
GO

-- =============================================
-- Table: JobExecutionLog
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'JobExecutionLog')
BEGIN
    CREATE TABLE dbo.JobExecutionLog (
        Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
        JobExecutionId UNIQUEIDENTIFIER NOT NULL,
        TimestampUtc DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        LogLevel NVARCHAR(20) NOT NULL,
        Message NVARCHAR(4000) NOT NULL,
        Exception NVARCHAR(MAX) NULL,
        Properties NVARCHAR(MAX) NULL,

        CONSTRAINT FK_JobExecutionLog_JobExecution FOREIGN KEY (JobExecutionId)
            REFERENCES dbo.JobExecution(Id) ON DELETE CASCADE
    );

    CREATE NONCLUSTERED INDEX IX_JobExecutionLog_ExecutionId ON dbo.JobExecutionLog(JobExecutionId, TimestampUtc);
    CREATE NONCLUSTERED INDEX IX_JobExecutionLog_Timestamp ON dbo.JobExecutionLog(TimestampUtc DESC);
END
GO

-- =============================================
-- Table: JobParameter
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'JobParameter')
BEGIN
    CREATE TABLE dbo.JobParameter (
        Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
        JobDefinitionId UNIQUEIDENTIFIER NOT NULL,
        ParameterName NVARCHAR(100) NOT NULL,
        ParameterValue NVARCHAR(MAX) NOT NULL,
        ParameterType NVARCHAR(50) NOT NULL DEFAULT 'String',
        IsEncrypted BIT NOT NULL DEFAULT 0,
        IsRequired BIT NOT NULL DEFAULT 0,
        DefaultValue NVARCHAR(MAX) NULL,
        Description NVARCHAR(500) NULL,
        CreatedBy NVARCHAR(100) NOT NULL,
        CreatedAtUtc DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        UpdatedBy NVARCHAR(100) NULL,
        UpdatedAtUtc DATETIME2 NULL,

        CONSTRAINT FK_JobParameter_JobDefinition FOREIGN KEY (JobDefinitionId)
            REFERENCES dbo.JobDefinition(Id) ON DELETE CASCADE,
        CONSTRAINT UQ_JobParameter_Name UNIQUE (JobDefinitionId, ParameterName)
    );

    CREATE NONCLUSTERED INDEX IX_JobParameter_JobDefinitionId ON dbo.JobParameter(JobDefinitionId);
END
GO

-- =============================================
-- Table: JobNotification
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'JobNotification')
BEGIN
    CREATE TABLE dbo.JobNotification (
        Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
        JobDefinitionId UNIQUEIDENTIFIER NOT NULL,
        Channels INT NOT NULL,
        Triggers INT NOT NULL,
        EmailRecipients NVARCHAR(MAX) NULL,
        TeamsWebhookUrl NVARCHAR(500) NULL,
        SlackWebhookUrl NVARCHAR(500) NULL,
        CustomWebhookUrl NVARCHAR(500) NULL,
        IsActive BIT NOT NULL DEFAULT 1,
        CreatedBy NVARCHAR(100) NOT NULL,
        CreatedAtUtc DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        UpdatedBy NVARCHAR(100) NULL,
        UpdatedAtUtc DATETIME2 NULL,

        CONSTRAINT FK_JobNotification_JobDefinition FOREIGN KEY (JobDefinitionId)
            REFERENCES dbo.JobDefinition(Id) ON DELETE CASCADE
    );

    CREATE NONCLUSTERED INDEX IX_JobNotification_JobDefinitionId ON dbo.JobNotification(JobDefinitionId);
END
GO

-- =============================================
-- Table: JobDependency
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'JobDependency')
BEGIN
    CREATE TABLE dbo.JobDependency (
        Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
        JobDefinitionId UNIQUEIDENTIFIER NOT NULL,
        DependsOnJobId UNIQUEIDENTIFIER NOT NULL,
        DelayAfterCompletionSeconds INT NOT NULL DEFAULT 0,
        FailIfDependencyFails BIT NOT NULL DEFAULT 1,
        IsActive BIT NOT NULL DEFAULT 1,
        CreatedBy NVARCHAR(100) NOT NULL,
        CreatedAtUtc DATETIME2 NOT NULL DEFAULT GETUTCDATE(),

        CONSTRAINT FK_JobDependency_JobDefinition FOREIGN KEY (JobDefinitionId)
            REFERENCES dbo.JobDefinition(Id),
        CONSTRAINT FK_JobDependency_DependsOn FOREIGN KEY (DependsOnJobId)
            REFERENCES dbo.JobDefinition(Id),
        CONSTRAINT CK_JobDependency_NotSelf CHECK (JobDefinitionId <> DependsOnJobId),
        CONSTRAINT UQ_JobDependency UNIQUE (JobDefinitionId, DependsOnJobId)
    );

    CREATE NONCLUSTERED INDEX IX_JobDependency_JobDefinitionId ON dbo.JobDependency(JobDefinitionId);
    CREATE NONCLUSTERED INDEX IX_JobDependency_DependsOnJobId ON dbo.JobDependency(DependsOnJobId);
END
GO

-- =============================================
-- Table: JobOwnership
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'JobOwnership')
BEGIN
    CREATE TABLE dbo.JobOwnership (
        Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
        JobDefinitionId UNIQUEIDENTIFIER NOT NULL UNIQUE,
        OwnerName NVARCHAR(200) NOT NULL,
        OwnerEmail NVARCHAR(200) NOT NULL,
        TeamName NVARCHAR(200) NOT NULL,
        TeamChannel NVARCHAR(200) NULL,
        EscalationEmail NVARCHAR(200) NULL,
        CreatedBy NVARCHAR(100) NOT NULL,
        CreatedAtUtc DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        UpdatedBy NVARCHAR(100) NULL,
        UpdatedAtUtc DATETIME2 NULL,

        CONSTRAINT FK_JobOwnership_JobDefinition FOREIGN KEY (JobDefinitionId)
            REFERENCES dbo.JobDefinition(Id) ON DELETE CASCADE
    );

    CREATE NONCLUSTERED INDEX IX_JobOwnership_OwnerEmail ON dbo.JobOwnership(OwnerEmail);
    CREATE NONCLUSTERED INDEX IX_JobOwnership_TeamName ON dbo.JobOwnership(TeamName);
END
GO

-- =============================================
-- Table: JobAudit
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'JobAudit')
BEGIN
    CREATE TABLE dbo.JobAudit (
        Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
        JobDefinitionId UNIQUEIDENTIFIER NULL,
        JobExecutionId UNIQUEIDENTIFIER NULL,
        Action INT NOT NULL,
        PerformedBy NVARCHAR(100) NOT NULL,
        PerformedAtUtc DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        OldValues NVARCHAR(MAX) NULL,
        NewValues NVARCHAR(MAX) NULL,
        AdditionalData NVARCHAR(MAX) NULL,
        IpAddress NVARCHAR(50) NULL,
        UserAgent NVARCHAR(500) NULL
    );

    CREATE NONCLUSTERED INDEX IX_JobAudit_JobDefinitionId ON dbo.JobAudit(JobDefinitionId, PerformedAtUtc DESC);
    CREATE NONCLUSTERED INDEX IX_JobAudit_JobExecutionId ON dbo.JobAudit(JobExecutionId, PerformedAtUtc DESC);
    CREATE NONCLUSTERED INDEX IX_JobAudit_Action ON dbo.JobAudit(Action, PerformedAtUtc DESC);
    CREATE NONCLUSTERED INDEX IX_JobAudit_PerformedBy ON dbo.JobAudit(PerformedBy, PerformedAtUtc DESC);
    CREATE NONCLUSTERED INDEX IX_JobAudit_PerformedAt ON dbo.JobAudit(PerformedAtUtc DESC);
END
GO

PRINT 'Database tables created successfully';
GO
