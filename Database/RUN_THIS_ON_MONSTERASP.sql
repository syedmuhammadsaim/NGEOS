-- ============================================================================
-- NGEOS - COMPLETE PRODUCTION DATABASE PATCH FOR MONSTERASP
-- Run this in MonsterASP Control Panel -> Databases -> MS SQL -> Run T-SQL
-- Safe, idempotent script that checks and adds missing columns/tables.
-- ============================================================================

-- 1. Donations Table - PaymentGateway Column
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'Donations') AND name = N'PaymentGateway')
BEGIN
    ALTER TABLE [Donations] ADD [PaymentGateway] NVARCHAR(50) NOT NULL CONSTRAINT [DF_Donations_PaymentGateway] DEFAULT N'Card';
    PRINT 'Added PaymentGateway to Donations.';
END
ELSE
BEGIN
    PRINT 'PaymentGateway already exists on Donations.';
END
GO

-- 2. Donations Table - Currency Column
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'Donations') AND name = N'Currency')
BEGIN
    ALTER TABLE [Donations] ADD [Currency] NVARCHAR(10) NOT NULL CONSTRAINT [DF_Donations_Currency] DEFAULT N'PKR';
    PRINT 'Added Currency to Donations.';
END
ELSE
BEGIN
    PRINT 'Currency already exists on Donations.';
END
GO

-- 3. Donations Table - ProgrammeId Column & Foreign Key
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'Donations') AND name = N'ProgrammeId')
BEGIN
    ALTER TABLE [Donations] ADD [ProgrammeId] INT NULL;
    PRINT 'Added ProgrammeId to Donations.';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Donations_Programmes_ProgrammeId')
BEGIN
    ALTER TABLE [Donations] ADD CONSTRAINT [FK_Donations_Programmes_ProgrammeId] FOREIGN KEY ([ProgrammeId]) REFERENCES [Programmes] ([Id]);
    PRINT 'Created FK_Donations_Programmes_ProgrammeId.';
END
GO

-- 4. Users Table - GoogleId Column & Index
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'Users') AND name = N'GoogleId')
BEGIN
    ALTER TABLE [Users] ADD [GoogleId] NVARCHAR(100) NULL;
    PRINT 'Added GoogleId to Users.';
END
ELSE
BEGIN
    PRINT 'GoogleId already exists on Users.';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Users_GoogleId' AND object_id = OBJECT_ID(N'Users'))
BEGIN
    CREATE INDEX [IX_Users_GoogleId] ON [Users] ([GoogleId]);
    PRINT 'Created index IX_Users_GoogleId.';
END
GO

-- 5. PaymentTransactions Table
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = N'PaymentTransactions')
BEGIN
    CREATE TABLE [PaymentTransactions] (
        [Id] INT IDENTITY(1,1) NOT NULL,
        [DonationId] INT NOT NULL,
        [GatewayName] NVARCHAR(50) NOT NULL,
        [InternalReference] NVARCHAR(100) NOT NULL,
        [GatewayReference] NVARCHAR(150) NULL,
        [Amount] DECIMAL(18,2) NOT NULL,
        [Currency] NVARCHAR(10) NOT NULL,
        [Status] NVARCHAR(50) NOT NULL,
        [Environment] NVARCHAR(20) NOT NULL,
        [VerificationStatus] NVARCHAR(50) NOT NULL,
        [IdempotencyKey] NVARCHAR(100) NULL,
        [RawCallbackPayload] NVARCHAR(MAX) NULL,
        [RawWebhookPayload] NVARCHAR(MAX) NULL,
        [ErrorCode] NVARCHAR(50) NULL,
        [ErrorMessage] NVARCHAR(MAX) NULL,
        [RefundAmount] DECIMAL(18,2) NULL,
        [RefundReference] NVARCHAR(150) NULL,
        [RefundReason] NVARCHAR(250) NULL,
        [RefundedAt] DATETIME2 NULL,
        [CreatedAt] DATETIME2 NOT NULL,
        [CompletedAt] DATETIME2 NULL,
        CONSTRAINT [PK_PaymentTransactions] PRIMARY KEY ([Id])
    );
    PRINT 'Created PaymentTransactions table.';
END
ELSE
BEGIN
    PRINT 'PaymentTransactions table already exists.';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_PaymentTransactions_Donations')
BEGIN
    ALTER TABLE [PaymentTransactions] ADD CONSTRAINT [FK_PaymentTransactions_Donations] FOREIGN KEY ([DonationId]) REFERENCES [Donations] ([Id]) ON DELETE CASCADE;
    PRINT 'FK_PaymentTransactions_Donations created.';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_PaymentTransactions_DonationId' AND object_id = OBJECT_ID(N'PaymentTransactions'))
BEGIN
    CREATE INDEX [IX_PaymentTransactions_DonationId] ON [PaymentTransactions] ([DonationId]);
    PRINT 'IX_PaymentTransactions_DonationId created.';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_PaymentTransactions_GatewayName_GatewayReference' AND object_id = OBJECT_ID(N'PaymentTransactions'))
BEGIN
    CREATE INDEX [IX_PaymentTransactions_GatewayName_GatewayReference] ON [PaymentTransactions] ([GatewayName], [GatewayReference]);
    PRINT 'IX_PaymentTransactions_GatewayName_GatewayReference created.';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_PaymentTransactions_InternalReference' AND object_id = OBJECT_ID(N'PaymentTransactions'))
BEGIN
    CREATE UNIQUE INDEX [IX_PaymentTransactions_InternalReference] ON [PaymentTransactions] ([InternalReference]);
    PRINT 'IX_PaymentTransactions_InternalReference created.';
END
GO

PRINT '================================================================';
PRINT 'All NGEOS database schema checks and updates completed successfully!';
PRINT '================================================================';

-- ============================================================================
-- AUTONOMOUS AI ECOSYSTEM TABLES (NGEOS AGENT ENGINE)
-- Idempotent creation for local, SQL Server, and cloud hosting (MonsterASP)
-- ============================================================================

-- 1. Table: [AgentConfigurations]
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = N'AgentConfigurations')
BEGIN
    CREATE TABLE [AgentConfigurations] (
        [Id] INT IDENTITY(1,1) NOT NULL,
        [IsAdminAgentEnabled] BIT NOT NULL CONSTRAINT [DF_AgentConfigurations_IsAdminAgentEnabled] DEFAULT 1,
        [IsUserAgentEnabled] BIT NOT NULL CONSTRAINT [DF_AgentConfigurations_IsUserAgentEnabled] DEFAULT 1,
        [IsAutomationPaused] BIT NOT NULL CONSTRAINT [DF_AgentConfigurations_IsAutomationPaused] DEFAULT 0,
        [IsEmergencyKillSwitchActive] BIT NOT NULL CONSTRAINT [DF_AgentConfigurations_IsEmergencyKillSwitchActive] DEFAULT 0,
        [PrimaryProvider] NVARCHAR(50) NOT NULL CONSTRAINT [DF_AgentConfigurations_PrimaryProvider] DEFAULT N'Groq',
        [FallbackProvider] NVARCHAR(50) NOT NULL CONSTRAINT [DF_AgentConfigurations_FallbackProvider] DEFAULT N'Gemini',
        [GroqModel] NVARCHAR(100) NOT NULL CONSTRAINT [DF_AgentConfigurations_GroqModel] DEFAULT N'llama-3.3-70b-versatile',
        [GeminiModel] NVARCHAR(100) NOT NULL CONSTRAINT [DF_AgentConfigurations_GeminiModel] DEFAULT N'gemini-2.0-flash',
        [DonationMonitoringIntervalMinutes] INT NOT NULL CONSTRAINT [DF_AgentConfigurations_DonationMonitoringIntervalMinutes] DEFAULT 5,
        [SecurityScanIntervalMinutes] INT NOT NULL CONSTRAINT [DF_AgentConfigurations_SecurityScanIntervalMinutes] DEFAULT 10,
        [HealthCheckIntervalMinutes] INT NOT NULL CONSTRAINT [DF_AgentConfigurations_HealthCheckIntervalMinutes] DEFAULT 15,
        [LastSuccessfulExecution] DATETIME2 NOT NULL CONSTRAINT [DF_AgentConfigurations_LastSuccessfulExecution] DEFAULT SYSUTCDATETIME(),
        [UpdatedAt] DATETIME2 NOT NULL CONSTRAINT [DF_AgentConfigurations_UpdatedAt] DEFAULT SYSUTCDATETIME(),
        CONSTRAINT [PK_AgentConfigurations] PRIMARY KEY ([Id])
    );
    PRINT 'Created AgentConfigurations table.';
END
GO

-- Seed default AgentConfigurations row
IF NOT EXISTS (SELECT 1 FROM [AgentConfigurations] WHERE [Id] = 1)
BEGIN
    SET IDENTITY_INSERT [AgentConfigurations] ON;
    INSERT INTO [AgentConfigurations] ([Id], [IsAdminAgentEnabled], [IsUserAgentEnabled], [IsAutomationPaused], [IsEmergencyKillSwitchActive], [PrimaryProvider], [FallbackProvider], [GroqModel], [GeminiModel], [DonationMonitoringIntervalMinutes], [SecurityScanIntervalMinutes], [HealthCheckIntervalMinutes], [LastSuccessfulExecution], [UpdatedAt])
    VALUES (1, 1, 1, 0, 0, N'Groq', N'Gemini', N'llama-3.3-70b-versatile', N'gemini-2.0-flash', 5, 10, 15, SYSUTCDATETIME(), SYSUTCDATETIME());
    SET IDENTITY_INSERT [AgentConfigurations] OFF;
    PRINT 'Seeded default AgentConfiguration.';
END
GO

-- 2. Table: [AgentAutomationJobs]
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = N'AgentAutomationJobs')
BEGIN
    CREATE TABLE [AgentAutomationJobs] (
        [Id] INT IDENTITY(1,1) NOT NULL,
        [JobName] NVARCHAR(100) NOT NULL,
        [ExecutionId] NVARCHAR(100) NOT NULL,
        [Status] NVARCHAR(30) NOT NULL,
        [StartedAt] DATETIME2 NOT NULL,
        [CompletedAt] DATETIME2 NULL,
        [RecordsEvaluated] INT NOT NULL CONSTRAINT [DF_AgentAutomationJobs_RecordsEvaluated] DEFAULT 0,
        [ActionsTriggered] INT NOT NULL CONSTRAINT [DF_AgentAutomationJobs_ActionsTriggered] DEFAULT 0,
        [SummaryMessage] NVARCHAR(MAX) NULL,
        [ErrorMessage] NVARCHAR(MAX) NULL,
        [RetryCount] INT NOT NULL CONSTRAINT [DF_AgentAutomationJobs_RetryCount] DEFAULT 0,
        CONSTRAINT [PK_AgentAutomationJobs] PRIMARY KEY ([Id])
    );
    CREATE INDEX [IX_AgentAutomationJobs_JobName_StartedAt] ON [AgentAutomationJobs] ([JobName], [StartedAt]);
    PRINT 'Created AgentAutomationJobs table.';
END
GO

-- 3. Table: [AgentReports]
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = N'AgentReports')
BEGIN
    CREATE TABLE [AgentReports] (
        [Id] INT IDENTITY(1,1) NOT NULL,
        [ReportCode] NVARCHAR(50) NOT NULL,
        [AgentName] NVARCHAR(100) NOT NULL,
        [AgentType] NVARCHAR(50) NOT NULL,
        [ActionName] NVARCHAR(100) NOT NULL,
        [ModuleName] NVARCHAR(100) NOT NULL,
        [TargetUserId] INT NULL,
        [AffectedRecordId] INT NULL,
        [ActionDescription] NVARCHAR(1000) NOT NULL,
        [ActionReason] NVARCHAR(1000) NULL,
        [PreviousValue] NVARCHAR(MAX) NULL,
        [NewValue] NVARCHAR(MAX) NULL,
        [RiskLevel] NVARCHAR(20) NOT NULL,
        [ActionStatus] NVARCHAR(30) NOT NULL,
        [Timestamp] DATETIME2 NOT NULL,
        [ExecutionDurationMs] FLOAT NOT NULL,
        [RelatedSecurityEventId] NVARCHAR(100) NULL,
        [ErrorDetails] NVARCHAR(MAX) NULL,
        [IntegrityHash] NVARCHAR(128) NOT NULL,
        CONSTRAINT [PK_AgentReports] PRIMARY KEY ([Id])
    );
    CREATE INDEX [IX_AgentReports_Timestamp] ON [AgentReports] ([Timestamp]);
    CREATE INDEX [IX_AgentReports_ReportCode] ON [AgentReports] ([ReportCode]);
    PRINT 'Created AgentReports table.';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_AgentReports_Users_TargetUserId')
BEGIN
    ALTER TABLE [AgentReports] ADD CONSTRAINT [FK_AgentReports_Users_TargetUserId] FOREIGN KEY ([TargetUserId]) REFERENCES [Users] ([Id]);
    PRINT 'Created FK_AgentReports_Users_TargetUserId.';
END
GO

-- 4. Table: [AgentSecurityEvents]
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = N'AgentSecurityEvents')
BEGIN
    CREATE TABLE [AgentSecurityEvents] (
        [Id] INT IDENTITY(1,1) NOT NULL,
        [EventCode] NVARCHAR(50) NOT NULL,
        [UserId] INT NULL,
        [UserEmail] NVARCHAR(150) NULL,
        [IpAddress] NVARCHAR(50) NULL,
        [ViolationCategory] NVARCHAR(100) NOT NULL,
        [DetectionDetails] NVARCHAR(500) NOT NULL,
        [RiskScore] FLOAT NOT NULL,
        [Severity] NVARCHAR(20) NOT NULL,
        [WarningIssued] BIT NOT NULL CONSTRAINT [DF_AgentSecurityEvents_WarningIssued] DEFAULT 0,
        [RestrictionTriggered] BIT NOT NULL CONSTRAINT [DF_AgentSecurityEvents_RestrictionTriggered] DEFAULT 0,
        [DetectedAt] DATETIME2 NOT NULL,
        [DetectionEngine] NVARCHAR(100) NOT NULL,
        CONSTRAINT [PK_AgentSecurityEvents] PRIMARY KEY ([Id])
    );
    CREATE INDEX [IX_AgentSecurityEvents_DetectedAt] ON [AgentSecurityEvents] ([DetectedAt]);
    PRINT 'Created AgentSecurityEvents table.';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_AgentSecurityEvents_Users_UserId')
BEGIN
    ALTER TABLE [AgentSecurityEvents] ADD CONSTRAINT [FK_AgentSecurityEvents_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]);
    PRINT 'Created FK_AgentSecurityEvents_Users_UserId.';
END
GO

-- 5. Table: [AgentUserRestrictions]
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = N'AgentUserRestrictions')
BEGIN
    CREATE TABLE [AgentUserRestrictions] (
        [Id] INT IDENTITY(1,1) NOT NULL,
        [UserId] INT NOT NULL,
        [ViolationCategory] NVARCHAR(100) NOT NULL,
        [Reason] NVARCHAR(1000) NOT NULL,
        [RestrictedAt] DATETIME2 NOT NULL,
        [ExpiresAt] DATETIME2 NULL,
        [IsActive] BIT NOT NULL CONSTRAINT [DF_AgentUserRestrictions_IsActive] DEFAULT 1,
        [CountdownCompleted] BIT NOT NULL CONSTRAINT [DF_AgentUserRestrictions_CountdownCompleted] DEFAULT 0,
        [SuspensionEmailSent] BIT NOT NULL CONSTRAINT [DF_AgentUserRestrictions_SuspensionEmailSent] DEFAULT 0,
        [RevokedSessionId] NVARCHAR(MAX) NULL,
        [RestoredAt] DATETIME2 NULL,
        [RestoredBy] NVARCHAR(MAX) NULL,
        [RestorationReason] NVARCHAR(MAX) NULL,
        CONSTRAINT [PK_AgentUserRestrictions] PRIMARY KEY ([Id])
    );
    CREATE INDEX [IX_AgentUserRestrictions_UserId_IsActive] ON [AgentUserRestrictions] ([UserId], [IsActive]);
    PRINT 'Created AgentUserRestrictions table.';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_AgentUserRestrictions_Users_UserId')
BEGIN
    ALTER TABLE [AgentUserRestrictions] ADD CONSTRAINT [FK_AgentUserRestrictions_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]);
    PRINT 'Created FK_AgentUserRestrictions_Users_UserId.';
END
GO

-- 6. Table: [AgentAppeals]
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = N'AgentAppeals')
BEGIN
    CREATE TABLE [AgentAppeals] (
        [Id] INT IDENTITY(1,1) NOT NULL,
        [UserId] INT NOT NULL,
        [RestrictionId] INT NOT NULL,
        [Explanation] NVARCHAR(2000) NOT NULL,
        [ContactEmail] NVARCHAR(200) NULL,
        [SubmittedAt] DATETIME2 NOT NULL,
        [Status] NVARCHAR(30) NOT NULL,
        [AdminNotes] NVARCHAR(MAX) NULL,
        [ReviewedAt] DATETIME2 NULL,
        [ReviewedBy] NVARCHAR(MAX) NULL,
        CONSTRAINT [PK_AgentAppeals] PRIMARY KEY ([Id])
    );
    CREATE INDEX [IX_AgentAppeals_UserId] ON [AgentAppeals] ([UserId]);
    CREATE INDEX [IX_AgentAppeals_Status] ON [AgentAppeals] ([Status]);
    PRINT 'Created AgentAppeals table.';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_AgentAppeals_Users_UserId')
BEGIN
    ALTER TABLE [AgentAppeals] ADD CONSTRAINT [FK_AgentAppeals_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]);
    PRINT 'Created FK_AgentAppeals_Users_UserId.';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_AgentAppeals_AgentUserRestrictions_RestrictionId')
BEGIN
    ALTER TABLE [AgentAppeals] ADD CONSTRAINT [FK_AgentAppeals_AgentUserRestrictions_RestrictionId] FOREIGN KEY ([RestrictionId]) REFERENCES [AgentUserRestrictions] ([Id]);
    PRINT 'Created FK_AgentAppeals_AgentUserRestrictions_RestrictionId.';
END
GO

-- 7. Table: [AgentConversations]
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = N'AgentConversations')
BEGIN
    CREATE TABLE [AgentConversations] (
        [Id] INT IDENTITY(1,1) NOT NULL,
        [SessionId] NVARCHAR(50) NOT NULL,
        [UserId] INT NULL,
        [AgentType] NVARCHAR(30) NOT NULL,
        [Language] NVARCHAR(20) NOT NULL,
        [CreatedAt] DATETIME2 NOT NULL,
        [LastActiveAt] DATETIME2 NOT NULL,
        CONSTRAINT [PK_AgentConversations] PRIMARY KEY ([Id])
    );
    CREATE UNIQUE INDEX [IX_AgentConversations_SessionId] ON [AgentConversations] ([SessionId]);
    PRINT 'Created AgentConversations table.';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_AgentConversations_Users_UserId')
BEGIN
    ALTER TABLE [AgentConversations] ADD CONSTRAINT [FK_AgentConversations_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]);
    PRINT 'Created FK_AgentConversations_Users_UserId.';
END
GO

-- 8. Table: [AgentMessages]
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = N'AgentMessages')
BEGIN
    CREATE TABLE [AgentMessages] (
        [Id] INT IDENTITY(1,1) NOT NULL,
        [ConversationId] INT NOT NULL,
        [SenderRole] NVARCHAR(20) NOT NULL,
        [Content] NVARCHAR(MAX) NOT NULL,
        [ProviderUsed] NVARCHAR(50) NULL,
        [ProcessingTimeSec] FLOAT NULL,
        [SentAt] DATETIME2 NOT NULL,
        CONSTRAINT [PK_AgentMessages] PRIMARY KEY ([Id])
    );
    CREATE INDEX [IX_AgentMessages_ConversationId] ON [AgentMessages] ([ConversationId]);
    PRINT 'Created AgentMessages table.';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_AgentMessages_AgentConversations_ConversationId')
BEGIN
    ALTER TABLE [AgentMessages] ADD CONSTRAINT [FK_AgentMessages_AgentConversations_ConversationId] FOREIGN KEY ([ConversationId]) REFERENCES [AgentConversations] ([Id]) ON DELETE CASCADE;
    PRINT 'Created FK_AgentMessages_AgentConversations_ConversationId.';
END
GO

-- 9. Table: [AgentUsageStatistics]
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = N'AgentUsageStatistics')
BEGIN
    CREATE TABLE [AgentUsageStatistics] (
        [Id] INT IDENTITY(1,1) NOT NULL,
        [Provider] NVARCHAR(50) NOT NULL,
        [ModelName] NVARCHAR(100) NOT NULL,
        [OperationType] NVARCHAR(50) NOT NULL,
        [PromptTokens] INT NOT NULL,
        [CompletionTokens] INT NOT NULL,
        [TotalTokens] INT NOT NULL,
        [LatencyMs] FLOAT NOT NULL,
        [IsSuccessful] BIT NOT NULL,
        [ErrorCategory] NVARCHAR(MAX) NULL,
        [Timestamp] DATETIME2 NOT NULL,
        CONSTRAINT [PK_AgentUsageStatistics] PRIMARY KEY ([Id])
    );
    CREATE INDEX [IX_AgentUsageStatistics_Timestamp] ON [AgentUsageStatistics] ([Timestamp]);
    PRINT 'Created AgentUsageStatistics table.';
END
GO

-- 10. Update Admin User Password Hashes to NGEOS@Admin123 and Profile Image to NGO Logo
UPDATE [Users]
SET [PasswordHash] = '01a7836dc951c8a141bc82b6d4600c5722d92618cdf56399500989afb3d6d664',
    [ProfileImageUrl] = '/images/ngeos-logo.svg',
    [IsActive] = 1,
    [Role] = 'Admin'
WHERE [Email] IN ('admin@ngeos.org', 'ngeos@gmail.com') OR [Role] = 'Admin';
PRINT 'Updated Admin password hashes to NGEOS@Admin123 and ProfileImageUrl to NGO logo.';
GO