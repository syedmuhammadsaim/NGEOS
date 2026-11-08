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
